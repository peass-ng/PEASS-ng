"""Passive sudo policy fixtures for unrestricted build and package tools."""

import re
import subprocess
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
)
FUNCTION = re.search(
    r"^sudo_build_package_tool_review\(\) \{\n.*?^\}",
    MODULE.read_text(),
    re.MULTILINE | re.DOTALL,
).group()


def scan(policy):
    result = subprocess.run(
        ["sh", "-c", FUNCTION + '\nsudo_build_package_tool_review "$1"',
         "sh", policy],
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        timeout=3,
        check=True,
    )
    if result.stderr:
        raise AssertionError(result.stderr)
    return result.stdout


class SudoBuildPackageToolTests(unittest.TestCase):
    def test_forge_under_non_root_runas(self):
        output = scan(
            "User user may run the following commands on host:\n"
            "    (builder) NOPASSWD: /home/builder/.foundry/bin/forge\n"
        )
        self.assertIn("Sudo Forge unrestricted RunAs review candidate:", output)
        self.assertNotIn("pacman", output)

    def test_pacman_requires_root_capable_runas(self):
        rule = "    ({}) NOPASSWD: /usr/bin/pacman\n"
        self.assertIn("Sudo pacman unrestricted root review candidate:",
                      scan(rule.format("ALL : ALL")))
        self.assertIn("Sudo pacman unrestricted root review candidate:",
                      scan(rule.format("root")))
        self.assertEqual(scan(rule.format("builder")), "")
        self.assertEqual(scan(rule.format("ALL, !root")), "")

    def test_bare_wildcards_and_comma_separated_grants(self):
        output = scan(
            "    (builder) SETENV: /usr/bin/id, /opt/foundry/forge *\n"
            "    (ALL) NOEXEC: /usr/bin/true, /usr/bin/pacman *\n"
        )
        self.assertIn("Sudo Forge unrestricted RunAs review candidate:", output)
        self.assertIn("Sudo pacman unrestricted root review candidate:", output)

    def test_constrained_read_only_arguments(self):
        self.assertEqual(
            scan(
                "    (builder) /opt/foundry/forge --version\n"
                "    (root) /usr/bin/pacman -Qi package\n"
            ),
            "",
        )

    def test_negated_grants_suppress_matching_tool(self):
        output = scan(
            "    (builder) /opt/foundry/forge, !/opt/foundry/forge flatten *\n"
            "    (root) /usr/bin/pacman, !/usr/bin/pacman -U *\n"
        )
        self.assertEqual(output, "")

    def test_separate_negated_rule_suppresses_candidate(self):
        self.assertEqual(
            scan(
                "    (root) /usr/bin/pacman\n"
                "    (root) ! /usr/bin/pacman --hookdir *\n"
            ),
            "",
        )

    def test_unrelated_names_and_no_policy(self):
        self.assertEqual(
            scan(
                "    (root) /usr/bin/pacman-helper\n"
                "    (builder) /opt/myforge\n"
            ),
            "",
        )
        self.assertEqual(scan(""), "")

    def test_partial_policy_is_suppressed(self):
        rule = "    (root) /usr/bin/pacman\n"
        self.assertEqual(scan("x" * 2049 + "\n" + rule), "")
        self.assertEqual(scan("x\n" * 3001 + rule), "")
        self.assertEqual(scan("    (root) /usr/bin/pacman\n    --hookdir /tmp\n"), "")

    def test_duplicate_captures_emit_one_candidate(self):
        rule = "    (root) /usr/bin/pacman\n"
        self.assertEqual(
            scan(rule * 3).count("Sudo pacman unrestricted root review candidate:"),
            1,
        )


if __name__ == "__main__":
    unittest.main()
