"""Passive, bounded review of an unrestricted sudo preset loader."""

import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1] /
          "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
MARKER = "Sudo BBOT preset-loader review candidate"


class SudoBbotPresetTests(unittest.TestCase):
    def run_policy(self, policy):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            bindir = base / "bin"
            bindir.mkdir()
            rules = base / "rules"
            rules.write_text(policy)
            calls = base / "calls"
            sudo = bindir / "sudo"
            sudo.write_text(
                '#!/bin/sh\n'
                'printf "%s\\n" "$*" >> "$FAKE_SUDO_CALLS"\n'
                '[ "$1 $2" = "-n -l" ] || exit 99\n'
                'cat "$FAKE_SUDO_RULES"\n'
            )
            sudo.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{bindir}:{env['PATH']}"
            env["FAKE_SUDO_RULES"] = str(rules)
            env["FAKE_SUDO_CALLS"] = str(calls)
            body = "\n".join([
                "E=E", "sudoB=__unused__", "sudoG=__unused__",
                "sudoVB1=__unused__", "sudoVB2=__unused__",
                "SED_RED='&'", "SED_RED_YELLOW='&'", "SED_GREEN='&'",
                "PASSWORD=", "TIMEOUT=", "ROOT_FOLDER=",
                "print_2title() { :; }", "print_info() { :; }",
                "echo_not_found() { :; }",
                f". {shlex.quote(str(MODULE))}",
            ])
            result = subprocess.run(["sh", "-c", body], env=env,
                                    capture_output=True, text=True, timeout=15)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(calls.read_text().splitlines(), ["-n -l", "-n -l"])
            return result.stdout

    def test_bare_root_grant_is_a_single_candidate(self):
        for runas in ("root", "#0", "ALL"):
            with self.subTest(runas=runas):
                policy = f"    ({runas}) NOPASSWD: /usr/local/bin/bbot\n"
                output = self.run_policy(policy)
                self.assertEqual(output.count(MARKER), 1)
                self.assertIn("verify effective policy", output)
        output = self.run_policy("    (root) PASSWD: /opt/bbot\n")
        self.assertEqual(output.count(MARKER), 1)
        self.assertNotIn("passwordless", output)

    def test_restrictions_and_negations_do_not_trigger(self):
        policies = (
            "    (nobody) NOPASSWD: /usr/local/bin/bbot\n",
            "    (ALL, !root) NOPASSWD: /usr/local/bin/bbot\n",
            "    (root) NOPASSWD: /usr/local/bin/bbot -p /opt/trusted.yml\n",
            '    (root) NOPASSWD: /usr/local/bin/bbot ""\n',
            "    (root) NOPASSWD: !/usr/local/bin/bbot\n",
            "    (root) NOPASSWD: /usr/local/bin/bbot, !/usr/local/bin/bbot\n",
            "    (root) NOPASSWD: /usr/local/bin/bbot\n"
            "    (root) NOPASSWD: !/usr/local/bin/bbot\n",
            "    (root) NOPASSWD: BBOT_ALIAS\n",
            "    (root) NOPASSWD: /usr/local/bin/bbot-helper\n",
            "    (root) NOPASSWD: /usr/local/bin/bbot\n"
            "        -p /opt/trusted.yml\n",
        )
        for policy in policies:
            with self.subTest(policy=policy):
                self.assertNotIn(MARKER, self.run_policy(policy))

    def test_comma_list_and_repeated_output_are_deduplicated(self):
        policy = "    (ALL) NOPASSWD: /bin/id, /usr/local/bin/bbot\n"
        self.assertEqual(self.run_policy(policy).count(MARKER), 1)

    def test_input_limits_suppress_uncertain_match(self):
        rule = "    (ALL) NOPASSWD: /usr/local/bin/bbot\n"
        self.assertNotIn(MARKER, self.run_policy(rule + "x\n" * 3001))
        self.assertNotIn(MARKER, self.run_policy(rule + "x" * 2049 + "\n"))


if __name__ == "__main__":
    unittest.main()
