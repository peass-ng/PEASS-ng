"""Exact sudo chattr executable paths are review leads, not verdicts."""

import re
import shlex
import shutil
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
VARIABLE = ROOT / "builder/linpeas_parts/variables/sudoB.sh"
MODULE = ROOT / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
FUNCTION = re.search(r"^sudo_l_colorize\(\) \{\n.*?^\}",
                     MODULE.read_text(), re.MULTILINE | re.DOTALL).group()


class SudoChattrHighlightTests(unittest.TestCase):
    def colorize(self, rule):
        body = ("whoami() { printf '%s\\n' __unmatched_user__; }\n"
                f". {shlex.quote(str(VARIABLE))}\n"
                "E=E; SED_RED='<H>'; SED_GREEN='<G>'; "
                "SED_RED_YELLOW='<Y>'; sudoG=__none__; "
                "sudoVB1=__none__; sudoVB2=__none__\n"
                f"{FUNCTION}\nsudo_l_colorize")
        result = subprocess.run(["sh", "-c", body], input=rule + "\n",
                                text=True, capture_output=True, timeout=3)
        self.assertEqual(0, result.returncode, result.stderr)
        return result.stdout

    def test_exact_paths_are_highlighted(self):
        for rule in (
            "(root) /usr/bin/chattr",
            "(root) /bin/chattr +i /etc/example",
            "(root) /usr/local/bin/chattr, /usr/bin/true",
        ):
            with self.subTest(rule=rule):
                self.assertIn("<H>", self.colorize(rule))

    def test_lookalikes_and_negated_paths_are_not_highlighted(self):
        for rule in (
            "(root) !/usr/bin/chattr",
            "(root) /usr/bin/chattr-helper",
            "(root) /usr/bin/chattr.sh",
            "(root) /usr/bin/notchattr",
            "(root) chattr",
            "(root) /usr/bin/chattr_backup",
        ):
            with self.subTest(rule=rule):
                self.assertNotIn("<H>", self.colorize(rule))

    def test_existing_highlight_and_shell_syntax(self):
        self.assertIn("<H>", self.colorize("(root) /usr/bin/luvit"))
        for path in (VARIABLE, MODULE):
            result = subprocess.run(["sh", "-n", str(path)], text=True,
                                    capture_output=True, timeout=3)
            self.assertEqual(0, result.returncode, result.stderr)

    def test_busybox_sed_when_available(self):
        busybox = shutil.which("busybox")
        if not busybox:
            self.skipTest("BusyBox is unavailable")
        body = ("whoami() { printf '%s\\n' __unmatched_user__; }\n"
                f". {shlex.quote(str(VARIABLE))}\n"
                "printf '%s\\n' '(root) /usr/bin/chattr' | "
                f"{shlex.quote(busybox)} sed -E \"s@${{sudoB}}@<H>@g\"")
        result = subprocess.run(["sh", "-c", body], text=True,
                                capture_output=True, timeout=3)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("<H>", result.stdout)


if __name__ == "__main__":
    unittest.main()
