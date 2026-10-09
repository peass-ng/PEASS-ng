"""Passive correlation of paired sudo packet-filter write permissions."""

import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1] /
          "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
MARKER = "Sudo packet-filter export review candidate"


class SudoPacketFilterExportTests(unittest.TestCase):
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

    def test_bare_matched_grants_are_reported_once(self):
        for names in (
            ("iptables", "iptables-save"),
            ("ip6tables", "ip6tables-save"),
            ("iptables-nft", "iptables-nft-save"),
            ("ip6tables-legacy", "ip6tables-legacy-save"),
        ):
            for runas in ("root", "#0", "ALL"):
                with self.subTest(names=names, runas=runas):
                    policy = (f"    ({runas}) NOPASSWD: /usr/sbin/{names[0]}, "
                              f"/usr/sbin/{names[1]}\n")
                    output = self.run_policy(policy)
                    self.assertEqual(output.count(MARKER), 1, output)
                    self.assertIn("verify effective policy", output)

    def test_separate_grant_lines_correlate(self):
        policy = ("    (root) NOPASSWD: /usr/sbin/iptables\n"
                  "    (root) PASSWD: /usr/sbin/iptables-save\n")
        self.assertEqual(self.run_policy(policy).count(MARKER), 1)

    def test_constrained_or_nonmatching_grants_do_not_trigger(self):
        policies = (
            "    (root) NOPASSWD: /usr/sbin/iptables\n",
            "    (root) NOPASSWD: /usr/sbin/iptables-save\n",
            "    (daemon) NOPASSWD: /usr/sbin/iptables, /usr/sbin/iptables-save\n",
            "    (ALL, !root) NOPASSWD: /usr/sbin/iptables, /usr/sbin/iptables-save\n",
            "    (root) NOPASSWD: /usr/sbin/ip6tables, /usr/sbin/iptables-save\n",
            "    (root) NOPASSWD: /usr/sbin/iptables, /usr/sbin/iptables-nft-save\n",
            "    (root) NOPASSWD: /usr/sbin/iptables -L, /usr/sbin/iptables-save\n",
            "    (root) NOPASSWD: /usr/sbin/iptables, /usr/sbin/iptables-save -f /etc/iptables/rules.v4\n",
            '    (root) NOPASSWD: /usr/sbin/iptables, /usr/sbin/iptables-save ""\n',
            "    (root) NOPASSWD: /usr/sbin/iptables, !/usr/sbin/iptables-save\n",
            "    (root) NOPASSWD: /usr/sbin/iptables, /usr/sbin/iptables-save, !/usr/sbin/iptables-save\n",
            "    (root) NOPASSWD: /usr/sbin/iptables, /usr/sbin/iptables-save\n"
            "        --file /tmp/out\n",
            "    (root) NOPASSWD: /usr/sbin/iptables, /usr/sbin/iptables-save\n"
            "    (root) NOPASSWD: !FIREWALL_ALIAS\n",
            "    (root) NOPASSWD: /usr/sbin/iptables, /usr/sbin/iptables-save, !ALL\n",
            "    (root) NOPASSWD: sha256:abc /usr/sbin/iptables, /usr/sbin/iptables-save\n",
        )
        for policy in policies:
            with self.subTest(policy=policy):
                self.assertNotIn(MARKER, self.run_policy(policy))

    def test_input_limits_suppress_uncertain_match(self):
        rule = "    (root) NOPASSWD: /usr/sbin/iptables, /usr/sbin/iptables-save\n"
        self.assertNotIn(MARKER, self.run_policy(rule + "x\n" * 3001))
        self.assertNotIn(MARKER, self.run_policy(rule + "x" * 2049 + "\n"))

    def test_module_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
