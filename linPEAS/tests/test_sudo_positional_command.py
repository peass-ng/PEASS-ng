"""Focused static review of sudo Bash wrappers with a positional command."""

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
REVIEW = re.search(r"^sudo_positional_command_review\(\) \{\n.*?^\}",
                   SOURCE, re.MULTILINE | re.DOTALL).group()
MARKER = "Sudo positional-command wrapper review candidate:"


class SudoPositionalCommandTests(unittest.TestCase):
    def scan(self, source="#!/bin/bash\n$1 $2 $3 $4\n", runas="root",
             args="", tag="NOPASSWD: ", extra="", symlink=False,
             parent_symlink=False):
        temp_parent = "/private/tmp" if Path("/private/tmp").is_dir() else None
        with tempfile.TemporaryDirectory(dir=temp_parent) as tmp:
            root = Path(tmp)
            script_dir = root / "scripts"
            script_dir.mkdir()
            target = script_dir / "helper"
            actual = root / "actual" if symlink else target
            if parent_symlink:
                alias = root / "alias"
                alias.symlink_to(script_dir, target_is_directory=True)
                target = alias / "helper"
                actual = script_dir / "helper"
            actual.write_text(source)
            actual.chmod(0o755)
            if symlink:
                target.symlink_to(actual)
            marker = root / "executed"
            if source == "#!/bin/bash\n$1 $2 $3 $4\n":
                actual.write_text(source + f"touch '{marker}'\n")
            rule = f"    ({runas}) {tag}{target}{args}\n" + extra.replace("{script}", str(target))
            result = subprocess.run(
                ["sh", "-c", PATH_HELPER + "\n" + REVIEW +
                 '\nsudo_positional_command_review "$1"', "sh", rule],
                text=True, capture_output=True, timeout=3,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual("", result.stderr)
            self.assertFalse(marker.exists(), "the wrapper must not execute")
            return result.stdout

    def test_exact_root_rule_and_first_command(self):
        for runas in ("root", "ALL", "ALL : ALL", "#0"):
            with self.subTest(runas=runas):
                self.assertEqual(1, self.scan(runas=runas).count(MARKER))
        self.assertIn(MARKER, self.scan(args=" *"))
        self.assertIn(MARKER, self.scan(source="#!/usr/bin/env bash\n# note\n\n${1} ${2}\n"))
        self.assertIn("NOEXEC and shell behavior", self.scan(tag="NOEXEC: "))

    def test_policy_boundaries(self):
        for kwargs in (
            {"runas": "builder"}, {"runas": "ALL, !root"},
            {"args": ' ""'}, {"args": " --check"},
            {"extra": "    (root) ! {script}\n"},
            {"extra": "    (root) !ALL\n"},
            {"extra": "    (root) ! /tmp/*\n"},
            {"extra": "        --continuation\n"},
            {"symlink": True}, {"parent_symlink": True},
        ):
            with self.subTest(kwargs=kwargs):
                self.assertEqual("", self.scan(**kwargs))

    def test_source_boundaries(self):
        for source in (
            "#!/bin/sh\n$1 $2\n",
            "#!/bin/bash\necho ready\n$1 $2\n",
            "#!/bin/bash\n# $1 $2\necho ready\n",
            "#!/bin/bash\n\"$1\" $2\n",
            "#!/bin/bash\n$1; id\n",
            "#!/bin/bash\n$1 $(id)\n",
            "#!/bin/bash\n" + ("# comment\n" * 65) + "$1\n",
            "#!/bin/bash\n" + (" " * 1025) + "$1\n",
            "#!/bin/bash\n" + ("# x\n" * 1300) + "$1\n",
        ):
            with self.subTest(source=source[:40]):
                self.assertEqual("", self.scan(source=source))


if __name__ == "__main__":
    unittest.main()
