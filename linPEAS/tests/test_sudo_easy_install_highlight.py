"""The sudo highlight marks unrestricted easy_install paths for review."""

import shlex
import subprocess
import unittest
from pathlib import Path


VARIABLE = (Path(__file__).resolve().parents[1] /
            "builder/linpeas_parts/variables/sudoB.sh")
SUDO_MODULE = (Path(__file__).resolve().parents[1] /
               "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")


class SudoEasyInstallHighlightTests(unittest.TestCase):
    def colorize(self, value):
        source = SUDO_MODULE.read_text()
        start = source.index("sudo_l_colorize() {")
        end = source.index("\n}", start) + 2
        function = source[start:end]
        body = (f". {shlex.quote(str(VARIABLE))}\n"
                "E=E; SED_RED='<H>'; SED_GREEN='<G>'; "
                "SED_RED_YELLOW='<Y>'; sudoG=__none__; "
                "sudoVB1=__none__; sudoVB2=__none__\n"
                f"{function}\nsudo_l_colorize")
        result = subprocess.run(["sh", "-c", body], input=value + "\n",
                                capture_output=True, text=True, timeout=5)
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout.strip()

    def test_unrestricted_exact_path(self):
        for rule in ("(operator) /usr/local/bin/easy_install",
                     "(operator) /usr/bin/easy_install, /usr/bin/true"):
            with self.subTest(rule=rule):
                self.assertIn("<H>", self.colorize(rule))

    def test_fixed_args_lookalikes_and_denial_do_not_match(self):
        for rule in ("(operator) /usr/local/bin/easy_install /opt/trusted",
                     "(operator) /usr/local/bin/easy_install --help",
                     "(operator) /usr/local/bin/easy_install-wrapper",
                     "(operator) /usr/local/bin/easy_install.py",
                     "(operator) !/usr/local/bin/easy_install"):
            with self.subTest(rule=rule):
                self.assertNotIn("<H>", self.colorize(rule))

    def test_existing_highlights_still_work(self):
        for rule in ("(operator) /usr/bin/dotnet",
                     "(operator) /usr/bin/ssh"):
            with self.subTest(rule=rule):
                self.assertIn("<H>", self.colorize(rule))

    def test_variable_module_shell_syntax(self):
        for path in (VARIABLE, SUDO_MODULE):
            with self.subTest(path=path):
                result = subprocess.run(["sh", "-n", str(path)],
                                        capture_output=True, text=True)
                self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
