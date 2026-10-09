"""Existing sudo output highlights exact Git apply and ClamScan debug grants."""

import shlex
import subprocess
import unittest
from pathlib import Path


VARIABLE = (Path(__file__).resolve().parents[1] /
            "builder/linpeas_parts/variables/sudoB.sh")


class SudoGitClamscanHighlightTests(unittest.TestCase):
    def colorize(self, value):
        body = (f". {shlex.quote(str(VARIABLE))}\n"
                'sed -E "s,$sudoB,<H>,g"')
        result = subprocess.run(["sh", "-c", body], input=value + "\n",
                                capture_output=True, text=True, timeout=5)
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout.strip()

    def test_exact_command_and_anchored_sudoers_forms(self):
        cases = (
            "(operator) /usr/bin/git apply -v patch",
            "(operator) /usr/bin/git ^apply -v patch",
            "(operator) /usr/local/bin/clamscan --debug /tmp/a.dmg",
            "(operator) /usr/local/bin/clamscan ^--debug /tmp/a.dmg",
        )
        for rule in cases:
            with self.subTest(rule=rule):
                self.assertIn("<H>", self.colorize(rule))

    def test_lookalikes_negations_and_other_options(self):
        cases = (
            "(operator) !/usr/bin/git apply patch",
            "(operator) /usr/bin/git-helper apply patch",
            "(operator) /usr/bin/git apply-extra patch",
            "(operator) /usr/bin/git status",
            "(operator) !/usr/local/bin/clamscan --debug file.dmg",
            "(operator) /usr/local/bin/clamscan-helper --debug file.dmg",
            "(operator) /usr/local/bin/clamscan --debugging file.dmg",
            "(operator) /usr/local/bin/clamscan -f list.txt",
        )
        for rule in cases:
            with self.subTest(rule=rule):
                self.assertNotIn("<H>", self.colorize(rule))

    def test_variable_module_shell_syntax(self):
        result = subprocess.run(["sh", "-n", str(VARIABLE)],
                                capture_output=True, text=True, timeout=5)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
