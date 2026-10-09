"""Only bounded, exact root sudo Bash scripts get a pattern-auth review cue."""

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
REVIEW = re.search(r"^sudo_bash_pattern_auth_review\(\) \{\n.*?^\}",
                   SOURCE, re.MULTILINE | re.DOTALL).group()
MARKER = "Sudo Bash pattern-comparison review candidate:"
SCRIPT = ('#!/bin/bash\nDB_PASS=$(cat /root/.creds)\n'
          'read -s -p "Password: " USER_PASS\n'
          'if [[ $DB_PASS == $USER_PASS ]]; then\n'
          '  echo accepted\nfi\n')


class SudoBashPatternAuthTests(unittest.TestCase):
    def scan(self, script=SCRIPT, runas="root", args="", tag="NOPASSWD: ",
             extra="", symlink=False):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            root = Path(tmp)
            target = root / "review.sh"
            actual = root / "actual.sh" if symlink else target
            actual.write_text(script)
            actual.chmod(0o755)
            if symlink:
                target.symlink_to(actual)
            marker = root / "executed"
            actual.write_text(actual.read_text().replace("echo accepted", f"touch '{marker}'"))
            rule = f"    ({runas}) {tag}{target}{args}\n" + extra.replace("{script}", str(target))
            result = subprocess.run(
                ["sh", "-c", PATH_HELPER + "\n" + REVIEW +
                 '\nsudo_bash_pattern_auth_review "$1"', "sh", rule],
                text=True, capture_output=True, timeout=3,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual("", result.stderr)
            self.assertFalse(marker.exists(), "The allowed script must not execute")
            self.assertNotIn("DB_PASS", result.stdout)
            self.assertNotIn("USER_PASS", result.stdout)
            return result.stdout

    def test_exact_root_capable_policy_and_pattern_rhs(self):
        for runas in ("root", "ALL", "ALL : ALL", "#0"):
            with self.subTest(runas=runas):
                self.assertEqual(1, self.scan(runas=runas).count(MARKER))
        self.assertEqual(1, self.scan(args=" *").count(MARKER))
        self.assertEqual(1, self.scan(script=SCRIPT.replace("[[ $DB_PASS ==", '[[ "$DB_PASS" ==')).count(MARKER))
        self.assertIn("line 4", self.scan())

    def test_quoted_rhs_and_uncontrolled_rhs_are_excluded(self):
        for script in (
            SCRIPT.replace("$USER_PASS ]];", '"$USER_PASS" ]];'),
            SCRIPT.replace("$USER_PASS ]];", "$OTHER_PASS ]];"),
            SCRIPT.replace("read -s -p \"Password: \" USER_PASS\n", ""),
            SCRIPT.replace("== $USER_PASS", "= $USER_PASS"),
            SCRIPT.replace("#!/bin/bash", "#!/bin/sh"),
            SCRIPT + "x" * 2049 + "\n",
            SCRIPT + "x\n" * 201,
            SCRIPT + "x" * 66000,
        ):
            with self.subTest(script=script[-50:]):
                self.assertEqual("", self.scan(script=script))

    def test_ambiguous_or_denied_sudo_policy_is_excluded(self):
        for kwargs in (
            {"runas": "operator"}, {"runas": "ALL, !root"},
            {"args": " --backup"}, {"args": ' ""'},
            {"tag": "NOEXEC: "}, {"symlink": True},
            {"extra": "    (root) ! {script}\n"},
            {"extra": "    (root) !ALL\n"},
            {"extra": "        --ambiguous\n"},
        ):
            with self.subTest(kwargs=kwargs):
                self.assertEqual("", self.scan(**kwargs))

    def test_shell_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)], text=True,
                                capture_output=True, timeout=3)
        self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
