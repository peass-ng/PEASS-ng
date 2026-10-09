"""A sudo script helper must depend on a writable caller-selected CWD."""

import re
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1]
          / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
SOURCE = MODULE.read_text()
PATH_HELPER = re.search(r"^sudo_python_import_plain_path\(\) \{\n.*?^\}",
                        SOURCE, re.MULTILINE | re.DOTALL).group()
REVIEW = re.search(r"^sudo_relative_cwd_review\(\) \{\n.*?^\}",
                   SOURCE, re.MULTILINE | re.DOTALL).group()
MARKER = "Sudo relative-CWD helper review candidate:"
CP_MARKER = "Sudo GNU cp glob review candidate:"
GO_MARKER = "Sudo Go relative-CWD WASM review candidate:"


class SudoRelativeCwdTests(unittest.TestCase):
    def scan(self, source="#!/bin/bash\n./initdb.sh 2>/dev/null\n", runas="root",
             args="", tag="NOPASSWD: ", extra="", symlink=False,
             interpreter="", readable=True, suffix=".py"):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            root = Path(tmp)
            working = root / "working"
            working.mkdir()
            target = root / ("syscheck" + suffix if interpreter else "syscheck")
            actual = root / "actual" if symlink else target
            actual.write_text(source)
            actual.chmod(0o755 if readable else 0o111)
            if symlink:
                target.symlink_to(actual)
            invoked = working / "initdb.sh"
            marker = root / "executed"
            invoked.write_text(f"#!/bin/sh\ntouch '{marker}'\n")
            invoked.chmod(0o755)
            command = f"{interpreter} {target}" if interpreter else str(target)
            rule = f"    ({runas}) {tag}{command}{args}\n" + extra.replace("{script}", str(target))
            result = subprocess.run(
                ["sh", "-c", PATH_HELPER + "\n" + REVIEW +
                 '\nsudo_relative_cwd_review "$1"', "sh", rule],
                cwd=working, text=True, capture_output=True, timeout=3,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual("", result.stderr)
            self.assertFalse(marker.exists(), "The relative helper must never run")
            return result.stdout

    def test_exact_root_policy_and_direct_relative_call(self):
        for runas in ("root", "ALL", "ALL : ALL", "#0"):
            with self.subTest(runas=runas):
                out = self.scan(runas=runas)
                self.assertEqual(1, out.count(MARKER))
                self.assertIn("./initdb.sh", out)
                self.assertIn("writable CWD example:", out)
        self.assertIn(MARKER, self.scan(args=" *"))

    def test_requires_shell_source_and_unrestricted_effective_policy(self):
        for kwargs in (
            {"runas": "builder"},
            {"runas": "ALL, !root"},
            {"args": " --check"},
            {"args": ' ""'},
            {"tag": "NOEXEC: "},
            {"extra": "    (root) ! {script}\n"},
            {"extra": "    (root) !ALL\n"},
            {"extra": "Defaults runchdir=/srv/fixed\n"},
            {"extra": "        --fixed\n"},
            {"symlink": True},
            {"source": "#!/usr/bin/python3\n./initdb.sh\n"},
        ):
            with self.subTest(kwargs=kwargs):
                self.assertEqual("", self.scan(**kwargs))

    def test_rejects_directory_changes_and_noncommand_mentions(self):
        for source in (
            "#!/bin/sh\ncd /opt/app\n./initdb.sh\n",
            "#!/bin/sh\npushd /opt/app\n./initdb.sh\n",
            "#!/bin/sh\n# ./initdb.sh\n",
            '#!/bin/sh\necho "./initdb.sh"\n',
            "#!/bin/sh\n./initdb.sh\n" + "x" * 2049 + "\n",
            "#!/bin/sh\n./initdb.sh\n" + "x\n" * 201,
        ):
            with self.subTest(source=source[:50]):
                self.assertEqual("", self.scan(source=source))

    def test_deduplicates_and_parses(self):
        self.assertEqual(1, self.scan(extra="    (root) {script}\n").count(MARKER))
        self.assertEqual(1, self.scan(extra="    (root) ! /usr/bin/python3 {script} *\n").count(MARKER))
        result = subprocess.run(["sh", "-n", str(MODULE)], text=True,
                                capture_output=True, timeout=3)
        self.assertEqual(0, result.returncode, result.stderr)

    def test_root_sudo_shell_cp_glob_is_conditional_candidate(self):
        for command in ("cp .version * /etc/app/staged/",
                        "/usr/bin/cp * /etc/app/staged"):
            with self.subTest(command=command):
                out = self.scan(source="#!/usr/bin/bash\n" + command + "\n", suffix=".sh")
                self.assertEqual(1, out.count(CP_MARKER))
                self.assertIn("line 2", out)
                self.assertIn("writable CWD example:", out)
                self.assertIn("verify script branch", out)
        out = self.scan(source="#!/bin/sh\ncp .version * /etc/app/staged/\n",
                        suffix=".sh", extra="    (root) {script}\n")
        self.assertEqual(1, out.count(CP_MARKER))

    def test_cp_glob_requires_exact_policy_plain_glob_and_caller_cwd(self):
        positive = "#!/bin/sh\ncp .version * /etc/app/staged/\n"
        for kwargs in (
            {"runas": "builder"},
            {"runas": "ALL, !root"},
            {"args": " --fixed"},
            {"tag": "NOEXEC: "},
            {"extra": "    (root) ! {script}\n"},
            {"extra": "Defaults runchdir=/srv/fixed\n"},
            {"symlink": True},
            {"readable": False},
            {"source": "#!/bin/sh\ncd /opt/app\ncp .version * /etc/app/staged/\n"},
            {"source": "#!/bin/sh\nif cd /opt/app; then :; fi\ncp .version * /etc/app/staged/\n"},
            {"source": "#!/bin/sh\nif true; then cd /opt/app; fi\ncp .version * /etc/app/staged/\n"},
            {"source": "#!/bin/sh\ncp .version -- * /etc/app/staged/\n"},
            {"source": "#!/bin/sh\ncp -- * /etc/app/staged/\n"},
            {"source": "#!/bin/sh\ncp .version '*' /etc/app/staged/\n"},
            {"source": "#!/bin/sh\n# cp .version * /etc/app/staged/\n"},
            {"source": "#!/bin/sh\necho cp .version * /etc/app/staged/\n"},
            {"source": "#!/bin/sh\ncp .version * relative/staged/\n"},
            {"source": positive + "x\n" * 201},
        ):
            with self.subTest(kwargs=kwargs):
                self.assertNotIn(CP_MARKER, self.scan(**{
                    "source": positive, "suffix": ".sh", **kwargs}))

    def test_python_literal_helper_under_exact_interpreter_rule(self):
        source = ("#!/usr/bin/python3\n"
                  "import subprocess\n"
                  "def run_command(cmd):\n"
                  "    return subprocess.run(cmd, capture_output=True)\n"
                  "arg_list = ['./initdb.sh']\n"
                  "print(run_command(arg_list))\n")
        out = self.scan(source=source, interpreter="/usr/bin/python3", args=" *")
        self.assertEqual(1, out.count(MARKER))
        self.assertIn("./initdb.sh", out)
        self.assertIn("branch reachability", out)
        unrelated_denial = self.scan(
            source=source, interpreter="/usr/bin/python3", args=" *",
            extra="    (root) ! /usr/bin/python3 /opt/other.py *\n",
        )
        self.assertEqual(1, unrelated_denial.count(MARKER))
        for kwargs in (
            {"args": " --fixed"},
            {"runas": "builder"},
            {"tag": "NOEXEC: "},
            {"extra": "    (root) ! /usr/bin/python3 {script} *\n"},
            {"extra": "    (root) ! /usr/bin/python3 *\n"},
            {"extra": "Defaults runchdir=/srv/fixed\n"},
        ):
            with self.subTest(kwargs=kwargs):
                options = {"source": source, "interpreter": "/usr/bin/python3",
                           "args": " *"}
                options.update(kwargs)
                self.assertEqual("", self.scan(**options))
        unreadable = self.scan(source=source, interpreter="/usr/bin/python3",
                               args=" *", readable=False)
        self.assertIn("source unreadable", unreadable)
        self.assertIn("behavior unknown", unreadable)
        self.assertNotIn(MARKER, unreadable)

    def test_python_cue_requires_call_and_no_fixed_cwd(self):
        prefix = ("#!/usr/bin/python3\nimport subprocess\n"
                  "def run_command(cmd):\n"
                  "    return subprocess.run(cmd, capture_output=True)\n")
        for source in (
            prefix + "arg_list = ['./initdb.sh']\n",
            prefix + "# arg_list = ['./initdb.sh']\nprint(run_command(arg_list))\n",
            prefix + "import os\nos.chdir('/opt')\narg_list = ['./initdb.sh']\nprint(run_command(arg_list))\n",
            prefix + "arg_list = ['./initdb.sh']\nprint(subprocess.run(arg_list, cwd='/opt'))\n",
            "#!/usr/bin/python3\narg_list = ['./initdb.sh']\nprint(run_command(arg_list))\n",
            prefix + "arg_list = ['./initdb.sh']\nprint(run_command(arg_list))\n" + "x" * 2049,
        ):
            with self.subTest(source=source[:80]):
                self.assertEqual("", self.scan(source=source,
                                                interpreter="/usr/bin/python3", args=" *"))

    def test_ruby_relative_yaml_under_exact_root_rule(self):
        source = ('require "yaml"\n'
                  'YAML.load(File.read("dependencies.yml"))\n')
        options = {"source": source, "interpreter": "/usr/bin/ruby",
                   "suffix": ".rb"}
        marker = "Sudo relative-CWD Ruby YAML review candidate:"
        out = self.scan(**options)
        self.assertEqual(1, out.count(marker))
        self.assertIn("line 2", out)
        self.assertIn("loader/Psych version", out)
        self.assertEqual(1, self.scan(**options, args=" *").count(marker))
        self.assertEqual(1, self.scan(**options,
            extra="    (root) ! /usr/bin/ruby /opt/other.rb\n").count(marker))

        for change in (
            {"runas": "builder"},
            {"args": " --check"},
            {"tag": "NOEXEC: "},
            {"extra": "    (root) ! /usr/bin/ruby {script}\n"},
            {"extra": "    (root) ! /usr/bin/ruby *\n"},
            {"extra": "Defaults runchdir=/srv/fixed\n"},
            {"symlink": True},
            {"source": 'YAML.safe_load(File.read("dependencies.yml"))\n'},
            {"source": 'YAML.load(File.read("/etc/dependencies.yml"))\n'},
            {"source": 'Dir.chdir("/opt")\nYAML.load(File.read("dependencies.yml"))\n'},
            {"source": '# YAML.load(File.read("dependencies.yml"))\n'},
            {"source": source + "x" * 2049 + "\n"},
        ):
            with self.subTest(change=change):
                self.assertEqual("", self.scan(**{**options, **change}))
        unreadable = self.scan(**options, readable=False)
        self.assertIn("Sudo Ruby script source unreadable", unreadable)
        self.assertNotIn(marker, unreadable)

    def test_go_wasm_gate_and_relative_shell_under_exact_root_rule(self):
        source = (
            'package main\n'
            'func main() {\n'
            '  bytes, _ := wasm.ReadBytes("main.wasm")\n'
            '  instance, _ := wasm.NewInstance(bytes)\n'
            '  init := instance.Exports["info"]\n'
            '  result, _ := init()\n'
            '  f := result.String()\n'
            '  if (f != "1") {\n'
            '    println("not ready")\n'
            '  } else {\n'
            '    exec.Command("/bin/sh", "deploy.sh").Output()\n'
            '  }\n'
            '}\n'
        )
        options = {"source": source, "interpreter": "/usr/bin/go run",
                   "suffix": ".go"}
        for runas in ("root", "ALL", "#0"):
            out = self.scan(**options, runas=runas)
            self.assertEqual(1, out.count(GO_MARKER))
            self.assertIn("line 3", out)
            self.assertIn("line 11", out)
            self.assertIn("result-to-branch flow", out)
        self.assertIn(GO_MARKER, self.scan(**options,
            extra="    (root) ! /usr/bin/go run /opt/other.go\n"))
        for kwargs in (
            {"runas": "builder"},
            {"runas": "ALL, !root"},
            {"args": " --fixed"},
            {"tag": "NOEXEC: "},
            {"extra": "    (root) ! /usr/bin/go run {script}\n"},
            {"extra": "    (root) ! /usr/bin/go run *\n"},
            {"extra": "    (root) ! /usr/bin/go run {script} *\n"},
            {"extra": "Defaults runchdir=/srv/fixed\n"},
            {"symlink": True},
            {"source": source.replace('wasm.ReadBytes("main.wasm")',
                                       'wasm.ReadBytes("/opt/main.wasm")')},
            {"source": source.replace('wasm.NewInstance(bytes)',
                                       'someOtherFunction(bytes)')},
            {"source": source.replace('  } else {\n', '  }\n')},
            {"source": source.replace('    exec.Command("/bin/sh", "deploy.sh").Output()\n'
                                      '  }',
                                      '    println("no deploy")\n'
                                      '  }\n'
                                      '  exec.Command("/bin/sh", "deploy.sh").Output()')},
            {"source": source.replace('exec.Command("/bin/sh", "deploy.sh")',
                                       'exec.Command("/bin/sh", "/opt/deploy.sh")')},
            {"source": source.replace('  bytes, _ :=',
                                       '  os.Chdir("/opt")\n  bytes, _ :=')},
            {"source": source.replace('  bytes, _ :=',
                                       '  cmd.Dir = "/opt"\n  bytes, _ :=')},
            {"source": source + "x" * 2049 + "\n"},
        ):
            with self.subTest(kwargs=kwargs):
                self.assertNotIn(GO_MARKER, self.scan(**{**options, **kwargs}))
        unreadable = self.scan(**options, readable=False)
        self.assertIn("Sudo Go script source unreadable", unreadable)
        self.assertNotIn(GO_MARKER, unreadable)


if __name__ == "__main__":
    unittest.main()
