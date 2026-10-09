"""Policy-only fixtures for privileged Facter custom-fact loading."""

import re
import subprocess
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
)
FUNCTION = re.search(
    r"^sudo_facter_custom_dir_review\(\) \{\n.*?^\}",
    MODULE.read_text(),
    re.MULTILINE | re.DOTALL,
).group()
MARKER = "Sudo Facter custom-fact review candidate:"


class SudoFacterCustomDirTests(unittest.TestCase):
    def scan(self, rule="(ALL) NOPASSWD: /usr/bin/facter", extra=""):
        policy = (
            "User user may run the following commands on host:\n"
            f"    {rule}\n{extra}"
        )
        result = subprocess.run(
            ["sh", "-c", FUNCTION + '\nsudo_facter_custom_dir_review "$1"',
             "sh", policy],
            text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
            timeout=3, check=True,
        )
        self.assertEqual(result.stderr, "")
        return result.stdout

    def test_root_capable_unrestricted_rule(self):
        self.assertIn(MARKER, self.scan())
        self.assertIn(MARKER, self.scan(rule="(root : root) /opt/bin/facter"))

    def test_restricted_or_nonroot_rule_is_not_reported(self):
        self.assertEqual(self.scan(rule="(service) NOPASSWD: /usr/bin/facter"), "")
        self.assertEqual(self.scan(rule='(root) /usr/bin/facter ""'), "")
        self.assertEqual(self.scan(rule="(root) /usr/bin/facter --no-custom-facts"), "")
        self.assertEqual(self.scan(rule="(root) /usr/bin/myfacter"), "")

    def test_explicit_denial_and_ambiguous_policy_are_not_reported(self):
        self.assertEqual(self.scan(extra="    (root) !/usr/bin/facter\n"), "")
        self.assertEqual(self.scan(extra="    (root) !ALL\n"), "")
        self.assertEqual(self.scan(extra="x" * 2049 + "\n"), "")


if __name__ == "__main__":
    unittest.main()
