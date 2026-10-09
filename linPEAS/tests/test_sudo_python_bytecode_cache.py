import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SudoPythonBytecodeCacheTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
        )

    def run_case(self, *, rule="root", source="from helper import run\n",
                 pyc_tag="cpython-312", cache_mode=0o777, pyc_mode=0o644,
                 script_suffix="", version="3.12.3", fake_root_owned=False,
                 sudo_command=None, script_mode=0o755, trusted_interpreter=True):
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp)
            bindir = base / "bin"
            bindir.mkdir()
            interpreter = bindir / "python3.12"
            calls = base / "interpreter-calls"
            interpreter.write_text(
                '#!/bin/sh\n'
                'printf "%s\\n" "$*" >> "$PYTHON_CALLS"\n'
                '[ "$1" = -I ] && [ "$2" = -S ] && '
                '[ "$3" = --version ] || exit 7\n'
                'printf "Python %s\\n" "$FAKE_PYTHON_VERSION"\n'
            )
            interpreter.chmod(0o755)
            script = base / f"entry{script_suffix}.py"
            script.write_text(f"#!{interpreter}\n" + source +
                              'SECRET = "do-not-print-me"\n')
            script.chmod(script_mode)
            module = base / "helper.py"
            module.write_text('SECRET = "do-not-print-me"\n')
            cache = base / "__pycache__"
            cache.mkdir()
            pyc = cache / f"helper.{pyc_tag}.pyc"
            pyc.write_bytes(b"placeholder bytecode")
            pyc.chmod(pyc_mode)
            cache.chmod(cache_mode)
            sudo = bindir / "sudo"
            sudo.write_text("#!/bin/sh\nprintf '%s\\n' \"$FAKE_SUDO_RULE\"\n")
            sudo.chmod(0o755)
            timeout = bindir / "timeout"
            timeout.write_text(
                '#!/bin/sh\n'
                '[ "$1" = -k ] && [ "$2" = 1 ] && [ "$3" = 2 ] || exit 2\n'
                'shift 3\nexec "$@"\n'
            )
            timeout.chmod(0o755)
            if fake_root_owned:
                stat = bindir / "stat"
                stat.write_text(
                    '#!/bin/sh\n'
                    'if [ "$2" = %u ]; then printf "0\\n"; '
                    'else /usr/bin/stat "$@"; fi\n'
                )
                stat.chmod(0o755)

            env = os.environ.copy()
            env.update({
                "PATH": f"{bindir}:{env['PATH']}",
                "PYTHON_CALLS": str(calls),
                "FAKE_PYTHON_VERSION": version,
                "FAKE_SUDO_RULE": (
                    sudo_command or f"    ({rule}) NOPASSWD: {script}"
                ),
            })
            body = "\n".join([
                "E=E",
                "sudoB=__unlikely_sudoB__",
                "sudoG=__unlikely_sudoG__",
                "sudoVB1=__unlikely_sudoVB1__",
                "sudoVB2=__unlikely_sudoVB2__",
                "SED_RED='&'",
                "SED_RED_YELLOW='&'",
                "SED_GREEN='&'",
                "PASSWORD=",
                "TIMEOUT=",
                "print_2title() { :; }",
                "print_info() { :; }",
                "echo_not_found() { :; }",
                # Policy fixtures use an explicitly trusted fake interpreter; the
                # negative case below exercises the real filesystem guard.
                ('lp_trusted_version_path() { printf "%s\\n" "$1"; }'
                 if trusted_interpreter else
                 f". {shlex.quote(str(self.module.parent.parent / 'functions/lp_trusted_version_path.sh'))}"),
                f". {shlex.quote(str(self.module))}",
            ])
            result = subprocess.run(["sh", "-c", body], env=env,
                                    capture_output=True, text=True, timeout=10)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertNotIn("do-not-print-me", result.stdout)
            if not trusted_interpreter:
                self.assertFalse((base / "interpreter-calls").exists())
            if calls.exists():
                self.assertEqual(set(calls.read_text().splitlines()),
                                 {"-I -S --version"})
            return result.stdout, str(script), str(module), str(pyc)

    def test_reports_same_directory_import_and_replaceable_matching_cache(self):
        output, script, module, pyc = self.run_case(pyc_mode=0o444)
        self.assertIn("Privileged sudo Python bytecode cache review", output)
        self.assertIn(script, output)
        self.assertIn(module, output)
        self.assertIn(pyc, output)
        self.assertIn("Top-level import at line 2: helper", output)
        self.assertIn("cache directory permits bytecode replacement", output)

    def test_reports_writable_bytecode_in_nonwritable_cache(self):
        output, _, _, _ = self.run_case(cache_mode=0o555, pyc_mode=0o666)
        self.assertIn("bytecode file is writable", output)

    def test_skips_mismatches_and_unreplaceable_cache(self):
        cases = (
            {"rule": "nobody"},
            {"script_mode": 0o644},
            {"sudo_command": "    (root) NOPASSWD: /tmp/*.py"},
            {"source": "# from helper import run\n"},
            {"source": "from elsewhere.helper import run\n"},
            {"source": "def run():\n    import helper\n"},
            {"pyc_tag": "cpython-311"},
            {"cache_mode": 0o555, "pyc_mode": 0o444},
            {"cache_mode": 0o1777, "pyc_mode": 0o444,
             "fake_root_owned": True},
            {"version": "3.11.9"},
            {"source": "x = 1\n" * 401 + "import helper\n"},
            {"source": "x = 1\n" * 17000 + "import helper\n"},
        )
        for case in cases:
            with self.subTest(case=case):
                output, _, _, _ = self.run_case(**case)
                self.assertNotIn("bytecode cache review", output)

    def test_limits_imports_to_first_twenty(self):
        source = "".join(f"import item{i}\n" for i in range(20)) + "import helper\n"
        output, _, _, _ = self.run_case(source=source)
        self.assertNotIn("bytecode cache review", output)

    def test_untrusted_interpreter_is_not_executed(self):
        output, _, _, _ = self.run_case(trusted_interpreter=False)
        self.assertNotIn("bytecode cache review", output)

    def test_version_probe_output_is_bounded(self):
        output, _, _, _ = self.run_case(version="x" * 4096 + "\nPython 3.12.3")
        self.assertNotIn("bytecode cache review", output)

    def test_quotes_paths_as_data(self):
        output, _, _, pyc = self.run_case(script_suffix="'quoted")
        self.assertIn(pyc, output)


if __name__ == "__main__":
    unittest.main()
