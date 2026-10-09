"""Focused, passive sudo policy/script correlation fixtures."""

import re
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
)
FUNCTION = re.search(
    r"^sudo_env_keep_script_candidates\(\) \{\n.*?^\}",
    MODULE.read_text(),
    re.MULTILINE | re.DOTALL,
).group()


class SudoEnvKeepScriptTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.script = Path(self.tmp.name) / "fixed-cleaner.sh"

    def scan(self, body, *, defaults="env_keep+=CHECK_CONTENT", runas="ALL",
             interp="/usr/bin/bash", policy_suffix="*.png",
             policy_prefix="", extra_rules=""):
        self.script.write_text(body)
        policy = (
            policy_prefix
            + "Matching Defaults entries for user on host:\n"
            f"    env_reset, {defaults}\n"
            "User user may run the following commands on host:\n"
            f"    ({runas}) NOPASSWD: {interp} {self.script} {policy_suffix}\n"
            + extra_rules
        )
        result = subprocess.run(
            ["sh", "-c", FUNCTION + '\nsudo_env_keep_script_candidates "$1"',
             "sh", policy],
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=3,
            check=True,
        )
        self.assertEqual(result.stderr, "")
        return result.stdout

    def test_preserved_command_word_in_if(self):
        output = self.scan("if $CHECK_CONTENT; then\n  :\nfi\n")
        self.assertIn("Potential sudo script command execution:", output)
        self.assertIn("preserved variable CHECK_CONTENT", output)

    def test_quoted_defaults_and_braced_command_word(self):
        output = self.scan(
            "  ${CHECK_CONTENT}\n",
            defaults='env_keep += "OTHER CHECK_CONTENT"',
            runas="root",
            interp="/bin/sh",
        )
        self.assertIn("preserved variable CHECK_CONTENT", output)

    def test_unpreserved_command_word(self):
        self.assertEqual(
            self.scan("if $CHECK_CONTENT; then :; fi\n", defaults="env_keep+=OTHER"),
            "",
        )

    def test_non_root_rule(self):
        self.assertEqual(
            self.scan("if $CHECK_CONTENT; then :; fi\n", runas="service"),
            "",
        )

    def test_variable_used_only_as_argument(self):
        self.assertEqual(
            self.scan('printf "%s\\n" "$CHECK_CONTENT"\n'),
            "",
        )

    def test_comment_and_longer_variable_name_do_not_match(self):
        self.assertEqual(
            self.scan("# if $CHECK_CONTENT; then\nif $CHECK_CONTENT_EXTRA; then :; fi\n"),
            "",
        )

    def test_interpreter_option_is_not_a_fixed_script(self):
        self.assertEqual(
            self.scan("if $CHECK_CONTENT; then :; fi\n", policy_suffix="",
                      interp="/usr/bin/bash -c"),
            "",
        )

    def test_oversize_script_is_skipped(self):
        self.assertEqual(
            self.scan("if $CHECK_CONTENT; then :; fi\n" + "#" * 65536),
            "",
        )

    def test_lines_after_cap_are_skipped(self):
        self.assertEqual(
            self.scan(":\n" * 200 + "if $CHECK_CONTENT; then :; fi\n"),
            "",
        )

    def test_negated_interpreter_rule_suppresses_candidate(self):
        self.assertEqual(
            self.scan(
                "if $CHECK_CONTENT; then :; fi\n",
                extra_rules=f"    (ALL) !/usr/bin/bash {self.script} restricted.png\n",
            ),
            "",
        )

    def test_same_line_negated_interpreter_suppresses_candidate(self):
        self.assertEqual(
            self.scan(
                "if $CHECK_CONTENT; then :; fi\n",
                policy_suffix=f"*.png, !/usr/bin/bash {self.script} restricted.png",
            ),
            "",
        )

    def test_overlong_policy_line_suppresses_candidate(self):
        self.assertEqual(
            self.scan("if $CHECK_CONTENT; then :; fi\n",
                      policy_prefix="x" * 2049 + "\n"),
            "",
        )

    def test_policy_line_limit_suppresses_candidate(self):
        self.assertEqual(
            self.scan("if $CHECK_CONTENT; then :; fi\n",
                      policy_prefix="x\n" * 3001),
            "",
        )

    def test_script_contents_are_not_printed(self):
        output = self.scan(
            "SECRET_LITERAL='private-test-value'\nif $CHECK_CONTENT; then :; fi\n"
        )
        self.assertIn("preserved variable CHECK_CONTENT", output)
        self.assertNotIn("private-test-value", output)


if __name__ == "__main__":
    unittest.main()
