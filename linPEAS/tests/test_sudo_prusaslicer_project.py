"""Passive PrusaSlicer sudo cue must require an unrestricted root grant."""

import re
import subprocess
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1]
          / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
FUNCTION = re.search(
    r"^sudo_prusaslicer_project_review\(\) \{\n.*?^\}",
    MODULE.read_text(), re.MULTILINE | re.DOTALL,
).group()


def scan(policy):
    result = subprocess.run(
        ["sh", "-c", FUNCTION + '\nsudo_prusaslicer_project_review "$1"',
         "sh", policy],
        text=True, capture_output=True, check=True, timeout=3,
    )
    if result.stderr:
        raise AssertionError(result.stderr)
    return result.stdout


class SudoPrusaSlicerProjectTests(unittest.TestCase):
    def test_root_capable_unrestricted_grants(self):
        for runas in ("root", "ALL", "ALL : ALL", "#0"):
            with self.subTest(runas=runas):
                self.assertIn("Sudo PrusaSlicer project post-processing review candidate:",
                              scan(f"    ({runas}) NOPASSWD: /opt/PrusaSlicer/prusaslicer\n"))
        self.assertIn("review candidate", scan("    (root) /usr/bin/prusa-slicer *\n"))

    def test_non_root_and_negated_runas_suppressed(self):
        for runas in ("builder", "ALL, !root", "#1000", "root, !root"):
            with self.subTest(runas=runas):
                self.assertEqual("", scan(f"    ({runas}) /usr/bin/prusaslicer\n"))

    def test_fixed_arguments_and_noexec_suppressed(self):
        for policy in (
            "    (root) /usr/bin/prusaslicer -s /opt/models/fixed.3mf\n",
            "    (root) /usr/bin/prusaslicer \"\"\n",
            "    (root) NOEXEC: /usr/bin/prusaslicer\n",
            "    (root) NOEXEC: /usr/bin/true, /usr/bin/prusaslicer\n",
        ):
            with self.subTest(policy=policy):
                self.assertEqual("", scan(policy))

    def test_denials_and_ambiguous_output_suppressed(self):
        valid = "    (root) /usr/bin/prusaslicer\n"
        for policy in (
            valid + "    (root) ! /usr/bin/prusaslicer -s *\n",
            valid + "    (root) !ALL\n",
            valid + "    --fixed-option /tmp/file\n",
            "x" * 2049 + "\n" + valid,
            "x\n" * 3001 + valid,
        ):
            with self.subTest(policy=policy[:70]):
                self.assertEqual("", scan(policy))

    def test_unrelated_tools_and_deduplication(self):
        self.assertEqual("", scan("    (root) /usr/bin/prusaslicer-helper\n"))
        self.assertEqual("", scan("    (root) /usr/bin/other\n"))
        valid = "    (root) /usr/bin/prusaslicer\n"
        self.assertEqual(1, scan(valid * 3).count("review candidate:"))


if __name__ == "__main__":
    unittest.main()
