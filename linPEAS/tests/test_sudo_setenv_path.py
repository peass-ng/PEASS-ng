"""Bounded root sudo-script PATH review distinguishes evidence from unknowns."""

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
REVIEW = re.search(r"^sudo_setenv_path_review\(\) \{\n.*?^\}",
                   SOURCE, re.MULTILINE | re.DOTALL).group()
MARKER = "Sudo PATH review candidate:"
UNKNOWN = "Sudo PATH policy unknown:"


class SudoSetenvPathTests(unittest.TestCase):
    def scan(self, source="#!/bin/bash\nfind source_images -type f\n",
             runas="root", tag="SETENV: NOPASSWD: ", args="", extra="",
             defaults="Matching Defaults entries for user on host:\n    !secure_path\n",
             symlink=False, parent_symlink=False):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            root = Path(tmp)
            marker = root / "executed"
            path = root / "maintenance"
            if parent_symlink:
                real_dir = root / "actual"
                real_dir.mkdir()
                alias = root / "alias"
                alias.symlink_to(real_dir, target_is_directory=True)
                path = alias / "maintenance"
                real_path = real_dir / "maintenance"
            elif symlink:
                real_path = root / "actual"
            else:
                real_path = path
            real_path.write_text(source + "\ntouch '" + str(marker) + "'\n")
            real_path.chmod(0o755)
            if symlink:
                path.symlink_to(real_path)
            rule = defaults + f"    ({runas}) {tag}{path}{args}\n"
            rule += extra.replace("{script}", str(path))
            result = subprocess.run(
                ["sh", "-c", PATH_HELPER + "\n" + REVIEW +
                 '\nsudo_setenv_path_review "$1"', "sh", rule],
                cwd=root, text=True, capture_output=True, timeout=3,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual("", result.stderr)
            self.assertFalse(marker.exists(), "The privileged script must never run")
            return result.stdout

    def test_exact_root_setenv_bare_external_command(self):
        for runas in ("root", "ALL", "#0", "ALL : ALL"):
            with self.subTest(runas=runas):
                output = self.scan(runas=runas)
                self.assertEqual(1, output.count(MARKER))
                self.assertIn("bare command", output)
                self.assertIn("verify effective PATH", output)
        self.assertEqual(1, self.scan(extra="    (root) SETENV: {script}\n").count(MARKER))

    def test_explicit_path_preservation_and_bare_gzip(self):
        source = "#!/bin/bash\ngzip -c /var/log/app.log > /var/backups/app.gz\n"
        for defaults in (
            'Matching Defaults entries for user on host:\n    !secure_path, env_keep += "PATH"\n',
            "Matching Defaults entries for user on host:\n    !secure_path, !env_reset\n",
        ):
            with self.subTest(defaults=defaults):
                output = self.scan(source=source, tag="NOPASSWD: ", defaults=defaults)
                self.assertIn(MARKER, output)
                self.assertIn("bare command", output)

    def test_partial_policy_is_unknown_not_a_positive_candidate(self):
        source = "#!/bin/bash\ngzip -c /var/log/app.log > /var/backups/app.gz\n"
        for defaults, tag in (
            ("", "NOPASSWD: "),
            ("Matching Defaults entries for user on host:\n    !secure_path\n", "NOPASSWD: "),
            ('Matching Defaults entries for user on host:\n    env_keep += "PATH"\n', "NOPASSWD: "),
            ("Matching Defaults entries for user on host:\n    !secure_path\n", "SETENV: NOSETENV: "),
        ):
            with self.subTest(defaults=defaults, tag=tag):
                output = self.scan(source=source, tag=tag, defaults=defaults)
                self.assertIn(UNKNOWN, output)
                self.assertNotIn(MARKER, output)

    def test_secure_path_or_conflicting_defaults_suppress_candidate(self):
        self.assertEqual(
            "",
            self.scan(defaults="Matching Defaults entries for user on host:\n    secure_path=/usr/bin:/bin\n"),
        )
        for defaults in (
            "Matching Defaults entries for user on host:\n    !secure_path, secure_path=/usr/bin:/bin\n",
            "Matching Defaults entries for user on host:\n    secure_path=/usr/bin:/bin, !secure_path\n",
        ):
            with self.subTest(defaults=defaults):
                output = self.scan(defaults=defaults)
                self.assertIn(UNKNOWN, output)
                self.assertNotIn(MARKER, output)
        conflicting_keep = (
            'Matching Defaults entries for user on host:\n'
            '    !secure_path, env_keep += "PATH", env_keep -= "PATH"\n'
        )
        self.assertNotIn(MARKER, self.scan(tag="NOPASSWD: ", defaults=conflicting_keep))

    def test_bash_disabled_builtin_is_a_separate_candidate(self):
        source = "#!/bin/bash\nenable -n [ # review command lookup\nif [ -s /var/log/app ]; then\n  :\nfi\n"
        self.assertIn("disabled [ builtin", self.scan(source=source))
        self.assertEqual("", self.scan(source="#!/bin/bash\nenable -n [\n"))

    def test_policy_and_file_boundaries(self):
        for option in (
            {"runas": "builder"},
            {"runas": "ALL, !root"},
            {"tag": "SETENV: NOEXEC: NOPASSWD: "},
            {"args": " --fixed"},
            {"extra": "    (root) ! {script}\n"},
            {"extra": "    (root) !ALL\n"},
            {"symlink": True},
            {"parent_symlink": True},
            {"source": "#!/bin/sh\nfind source_images\n"},
            {"source": "#!/bin/bash\n/usr/bin/find source_images\n"},
            {"source": "#!/bin/bash\n# find source_images\n"},
            {"source": "#!/bin/bash\necho find source_images\n"},
            {"source": "#!/bin/bash\nfind source_images\n" + "x" * 65536},
            {"source": "#!/bin/bash\nfind source_images\n" + "# filler\n" * 201},
            {"source": "#!/bin/bash\nfind source_images\n" + "x" * 2049 + "\n"},
        ):
            with self.subTest(option=str(option)[:80]):
                self.assertEqual("", self.scan(**option))

    def test_no_new_execution_and_shell_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)], text=True,
                                capture_output=True, timeout=3)
        self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
