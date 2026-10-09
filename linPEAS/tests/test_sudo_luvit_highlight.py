"""Exact Luvit sudo grants are highlighted without matching lookalikes."""

import re
import shlex
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
VARIABLE = ROOT / "builder/linpeas_parts/variables/sudoB.sh"
MODULE = ROOT / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
FUNCTION = re.search(r"^sudo_l_colorize\(\) \{\n.*?^\}",
                     MODULE.read_text(), re.MULTILINE | re.DOTALL).group()


class SudoLuvitHighlightTests(unittest.TestCase):
    def colorize(self, rule):
        body = (f". {shlex.quote(str(VARIABLE))}\n"
                "E=E; SED_RED='<H>'; SED_GREEN='<G>'; "
                "SED_RED_YELLOW='<Y>'; sudoG=__none__; "
                "sudoVB1=__none__; sudoVB2=__none__\n"
                f"{FUNCTION}\nsudo_l_colorize")
        result = subprocess.run(["sh", "-c", body], input=rule + "\n",
                                text=True, capture_output=True, timeout=3)
        self.assertEqual(0, result.returncode, result.stderr)
        return result.stdout

    def test_exact_unrestricted_cross_user_grants(self):
        for rule in (
            "(operator) /home/operator/luvit",
            "(operator) /usr/local/bin/luvit, /usr/bin/true",
            "(operator) /usr/bin/luvit  ",
        ):
            with self.subTest(rule=rule):
                self.assertIn("<H>", self.colorize(rule))

    def test_fixed_args_denials_and_similar_names_do_not_match(self):
        for rule in (
            "(operator) /home/operator/luvit /opt/trusted.lua",
            "(operator) !/home/operator/luvit",
            "(operator) /home/operator/luvit-helper",
            "(operator) /home/operator/luvit.sh",
            "(operator) /home/operator/notluvit",
            "(operator) luvit",
        ):
            with self.subTest(rule=rule):
                self.assertNotIn("<H>", self.colorize(rule))

    def test_existing_command_highlight_and_shell_syntax(self):
        self.assertIn("<H>", self.colorize("(operator) /usr/bin/dotnet"))
        for path in (VARIABLE, MODULE):
            result = subprocess.run(["sh", "-n", str(path)], text=True,
                                    capture_output=True, timeout=3)
            self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
