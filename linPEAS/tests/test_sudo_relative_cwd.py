"""A sudo shell helper must depend on a writable caller-selected CWD."""

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


class SudoRelativeCwdTests(unittest.TestCase):
    def scan(self, source="#!/bin/bash\n./initdb.sh 2>/dev/null\n", runas="root",
             args="", tag="NOPASSWD: ", extra="", symlink=False):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            root = Path(tmp)
            working = root / "working"
            working.mkdir()
            target = root / "syscheck"
            actual = root / "actual" if symlink else target
            actual.write_text(source)
            actual.chmod(0o755)
            if symlink:
                target.symlink_to(actual)
            invoked = working / "initdb.sh"
            marker = root / "executed"
            invoked.write_text(f"#!/bin/sh\ntouch '{marker}'\n")
            invoked.chmod(0o755)
            rule = f"    ({runas}) {tag}{target}{args}\n" + extra.replace("{script}", str(target))
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
        result = subprocess.run(["sh", "-n", str(MODULE)], text=True,
                                capture_output=True, timeout=3)
        self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
