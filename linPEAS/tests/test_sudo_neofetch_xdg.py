"""Bounded sudo policy-only Neofetch configuration review fixtures."""

import re
import subprocess
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
)
FUNCTION = re.search(
    r"^sudo_neofetch_xdg_review\(\) \{\n.*?^\}",
    MODULE.read_text(),
    re.MULTILINE | re.DOTALL,
).group()
MARKER = "Sudo Neofetch config review candidate:"


class SudoNeofetchXdgTests(unittest.TestCase):
    def scan(self, rule='(root) NOPASSWD: /usr/bin/neofetch ""',
             defaults='env_reset, env_keep += "XDG_CONFIG_HOME"', extra=''):
        policy = (
            "Matching Defaults entries for user on host:\n"
            f"    {defaults}\n"
            "User user may run the following commands on host:\n"
            f"    {rule}\n{extra}"
        )
        result = subprocess.run(
            ["sh", "-c", FUNCTION + '\nsudo_neofetch_xdg_review "$1"',
             "sh", policy],
            text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
            timeout=3, check=True,
        )
        self.assertEqual(result.stderr, "")
        return result.stdout

    def test_exact_root_grant_and_preserved_variable(self):
        self.assertIn(MARKER, self.scan())
        self.assertIn(MARKER, self.scan(defaults="env_keep+=XDG_CONFIG_HOME"))

    def test_no_preserved_variable_or_lookalike(self):
        self.assertEqual(self.scan(defaults="env_keep+=OTHER"), "")
        self.assertEqual(self.scan(defaults="env_keep+=MY_XDG_CONFIG_HOME"), "")
        self.assertEqual(self.scan(defaults="env_keep-=XDG_CONFIG_HOME"), "")

    def test_only_root_capable_exact_empty_arguments(self):
        self.assertEqual(self.scan(rule='(service) NOPASSWD: /usr/bin/neofetch ""'), "")
        self.assertEqual(self.scan(rule='(root) NOPASSWD: /usr/bin/neofetch --config none'), "")
        self.assertEqual(self.scan(rule='(root) NOPASSWD: /usr/bin/myneofetch ""'), "")
        self.assertEqual(self.scan(rule='(root) NOPASSWD: /usr/bin/neofetcher ""'), "")

    def test_denied_command_or_environment_suppresses(self):
        self.assertEqual(
            self.scan(extra='    (root) !/usr/bin/neofetch ""\n'), ""
        )
        self.assertEqual(
            self.scan(rule='(root) NOPASSWD: /usr/bin/neofetch "", !/usr/bin/neofetch ""'),
            "",
        )
        self.assertEqual(
            self.scan(defaults='env_keep+=XDG_CONFIG_HOME, env_keep-=XDG_CONFIG_HOME'),
            "",
        )

    def test_denial_in_another_cached_policy_block_suppresses(self):
        another_block = (
            "Matching Defaults entries for user on host:\n"
            "    env_keep += \"XDG_CONFIG_HOME\"\n"
            "User user may run the following commands on host:\n"
            "    (root) NOPASSWD: /usr/bin/neofetch \"\"\n"
        )
        denied_block = (
            "Matching Defaults entries for user on host:\n"
            "    env_reset\n"
            "User user may run the following commands on host:\n"
            "    (root) !/usr/bin/neofetch \"\"\n"
        )
        self.assertEqual(self.scan(extra=another_block + denied_block), "")
        self.assertEqual(self.scan(extra=another_block + "    (root) !ALL\n"), "")

    def test_bounded_policy(self):
        self.assertEqual(self.scan(extra="    " + "x" * 2049 + "\n"), "")
        self.assertEqual(self.scan(extra="x\n" * 3001), "")


if __name__ == "__main__":
    unittest.main()
