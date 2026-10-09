"""An unrestricted sudo Ansible playbook path receives a review highlight."""

import shlex
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
VARIABLE = ROOT / "builder/linpeas_parts/variables/sudoB.sh"
SUDO_MODULE = ROOT / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"


class SudoAnsiblePlaybookHighlightTests(unittest.TestCase):
    def colorize(self, rule):
        source = SUDO_MODULE.read_text()
        start = source.index("sudo_l_colorize() {")
        end = source.index("\n}", start) + 2
        shell = (
            f". {shlex.quote(str(VARIABLE))}\n"
            "sudoB=${sudoB#*|}\n"
            "E=E; SED_RED='<H>'; SED_GREEN='<G>'; "
            "SED_RED_YELLOW='<Y>'; sudoG=__none__; "
            "sudoVB1=__none__; sudoVB2=__none__\n"
            f"{source[start:end]}\nsudo_l_colorize"
        )
        result = subprocess.run(
            ["sh", "-c", shell], input=rule + "\n", capture_output=True,
            text=True, timeout=5,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_exact_unrestricted_path_is_highlighted(self):
        for rule in (
            "(root) /usr/bin/ansible-playbook",
            "(root) /usr/bin/ansible-playbook *",
            "(root) /usr/bin/ansible-playbook *, /usr/bin/true",
        ):
            with self.subTest(rule=rule):
                self.assertIn("<H>", self.colorize(rule))

    def test_restricted_and_lookalike_paths_are_not_highlighted(self):
        for rule in (
            "(root) /usr/bin/ansible-playbook /opt/fixed.yml",
            "(root) /usr/bin/ansible-playbook-wrapper",
            "(root) /usr/bin/ansible-playbook.py",
            "(root) !/usr/bin/ansible-playbook",
        ):
            with self.subTest(rule=rule):
                self.assertNotIn("<H>", self.colorize(rule))

    def test_existing_highlight_survives(self):
        self.assertIn("<H>", self.colorize("(root) /usr/bin/tcpdump"))


if __name__ == "__main__":
    unittest.main()
