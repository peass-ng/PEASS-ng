"""Exact root sudo Python debugger review must stay passive and bounded."""

import re
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
SOURCE = MODULE.read_text(encoding="utf-8")
PATH_HELPER = re.search(
    r"^sudo_python_import_plain_path\(\) \{\n.*?^\}", SOURCE, re.MULTILINE | re.DOTALL
).group()
REVIEW = re.search(
    r"^sudo_python_debugger_review\(\) \{\n.*?^\}", SOURCE, re.MULTILINE | re.DOTALL
).group()
MARKER = "Sudo Python debugger review candidate:"
SCRIPT = (
    "import pdb\n"
    "secret = 'DO_NOT_PRINT_THIS_SECRET'\n"
    "pdb.post_mortem(error.__traceback__)\n"
)


class SudoPythonDebuggerTests(unittest.TestCase):
    def scan(self, script=SCRIPT, runas="root", interpreter="/usr/bin/python3",
             suffix="", extra="", file_symlink=False, parent_symlink=False):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            root = Path(tmp)
            actual_dir = root / "actual"
            actual_dir.mkdir()
            path_dir = root / "alias" if parent_symlink else actual_dir
            if parent_symlink:
                path_dir.symlink_to(actual_dir, target_is_directory=True)
            actual = actual_dir / "helper.py"
            marker = root / "executed"
            actual.write_text(script + f"\nopen({str(marker)!r}, 'w')\n", encoding="utf-8")
            target = path_dir / "helper.py"
            if file_symlink:
                actual.rename(actual_dir / "real.py")
                target.symlink_to(actual_dir / "real.py")
            policy = f"    ({runas}) NOPASSWD: {interpreter} {target}{suffix}\n"
            policy += extra.replace("{script}", str(target))
            result = subprocess.run(
                ["sh", "-c", PATH_HELPER + "\n" + REVIEW +
                 '\nsudo_python_debugger_review "$1"', "sh", policy],
                capture_output=True, text=True, timeout=3, check=False,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual("", result.stderr)
            self.assertFalse(marker.exists(), "The privileged script must not run")
            self.assertNotIn("DO_NOT_PRINT_THIS_SECRET", result.stdout)
            return result.stdout

    def test_exact_root_python_grant_reports_conditional_path_only(self):
        for runas in ("root", "ALL", "ALL : ALL", "#0"):
            for interpreter in ("/usr/bin/python", "/usr/bin/python3", "/usr/local/bin/python3.11"):
                with self.subTest(runas=runas, interpreter=interpreter):
                    output = self.scan(runas=runas, interpreter=interpreter)
                    self.assertEqual(1, output.count(MARKER))
                    self.assertIn("confirm debugger call reachability", output)
                    self.assertNotIn("post_mortem", output)

    def test_other_debugger_call_is_detected_without_execution(self):
        self.assertEqual(1, self.scan(script="import pdb\npdb.set_trace()\n").count(MARKER))

    def test_unrelated_or_ambiguous_source_is_skipped(self):
        for script in (
            "import pdb\nprint('hello')\n",
            "# pdb.post_mortem(exc.__traceback__)\n",
            "other.post_mortem(exc.__traceback__)\n",
            SCRIPT + "x" * 66000,
            SCRIPT + "# padding\n" * 401,
            SCRIPT + "x" * 2049 + "\n",
        ):
            with self.subTest(source_size=len(script)):
                self.assertEqual("", self.scan(script=script))

    def test_nonroot_deny_wildcard_extra_args_and_symlinks_are_skipped(self):
        for kwargs in (
            {"runas": "service"}, {"runas": "ALL, !root"},
            {"interpreter": "/usr/bin/python3 -I"},
            {"interpreter": "/usr/bin/my_python3"},
            {"suffix": " --action"}, {"suffix": " *"},
            {"file_symlink": True}, {"parent_symlink": True},
            {"extra": "    (root) !/usr/bin/python3 {script}\n"},
            {"extra": "    (root) !ALL\n"},
            {"extra": "        --ambiguous-continuation\n"},
            {"extra": "x" * 2049 + "\n"},
            {"extra": "x\n" * 3001},
        ):
            with self.subTest(kwargs=kwargs):
                self.assertEqual("", self.scan(**kwargs))

    def test_duplicate_rules_and_eight_script_cap(self):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            root = Path(tmp)
            policy = ""
            paths = []
            for index in range(9):
                path = root / f"helper{index}.py"
                path.write_text(SCRIPT, encoding="utf-8")
                paths.append(path)
                policy += f"    (root) /usr/bin/python3 {path}\n"
            policy += f"    (root) /usr/bin/python3 {paths[0]}\n"
            result = subprocess.run(
                ["sh", "-c", PATH_HELPER + "\n" + REVIEW +
                 '\nsudo_python_debugger_review "$1"', "sh", policy],
                capture_output=True, text=True, timeout=3, check=False,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual(8, result.stdout.count(MARKER))
            self.assertNotIn(str(paths[8]), result.stdout)


if __name__ == "__main__":
    unittest.main()
