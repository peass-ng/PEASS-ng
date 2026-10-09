import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SudoTcpdumpWildcardTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
        )

    def run_policy(self, policy):
        with tempfile.TemporaryDirectory() as temp:
            bindir = Path(temp) / "bin"
            bindir.mkdir()
            sudo = bindir / "sudo"
            sudo.write_text(
                "#!/bin/sh\n"
                "[ \"$1 $2\" = '-n -l' ] || exit 99\n"
                "printf '%s\\n' \"$FAKE_SUDO_POLICY\"\n"
            )
            sudo.chmod(0o755)
            env = os.environ.copy()
            env.update(PATH=f"{bindir}:{env['PATH']}", FAKE_SUDO_POLICY=policy)
            shell = "\n".join((
                "E=E", "sudoB=__no_color__", "sudoG=__no_color__",
                "sudoVB1=__no_color__", "sudoVB2=__no_color__",
                "SED_RED='&'", "SED_RED_YELLOW='&'", "SED_GREEN='&'",
                "ROOT_FOLDER=/", "PASSWORD=", "TIMEOUT=",
                "print_2title() { :; }", "print_info() { :; }",
                "echo_not_found() { :; }",
                f". {shlex.quote(str(self.module))}",
            ))
            result = subprocess.run(
                ["sh", "-c", shell], env=env, capture_output=True,
                text=True, timeout=15,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            return result.stdout

    def assert_candidate(self, policy, expected):
        output = self.run_policy(policy)
        self.assertEqual(output.count("Sudo tcpdump argument wildcard review candidate"),
                         expected, output)
        if expected:
            self.assertIn("verify effective options, authentication, and target confinement", output)

    def test_wrapped_argument_glob_is_reported_once(self):
        self.assert_candidate(
            "User operator may run the following commands on host:\n"
            "    (ALL : ALL) NOPASSWD: /usr/bin/tcpdump -c10 "
            "-w/var/cache/captures/*/report-*\n"
            "        -F/var/cache/captures/filter.*",
            1,
        )

    def test_wrapped_wildcard_on_next_line_and_comma_list(self):
        self.assert_candidate(
            "    (root) NOPASSWD: /usr/bin/id, /usr/bin/tcpdump -w\n"
            "        /var/cache/captures/*",
            1,
        )

    def test_unrelated_or_unavailable_rules_do_not_trigger(self):
        cases = (
            "",
            "sudo: a password is required",
            "The * below is a literal prose example about tcpdump.",
            "    (root) NOPASSWD: /usr/bin/tcpdump -w /tmp/fixed.pcap",
            "    (root) NOPASSWD: /usr/bin/tcpdump* -w /tmp/fixed.pcap",
            "    (root) NOPASSWD: /usr/bin/tcpdump* -w /tmp/*",
            "    (root) NOPASSWD: /usr/bin/tcpdump -w /tmp/fixed.pcap, /usr/bin/echo *",
            "    (root) NOPASSWD: /usr/bin/tcpdump -w /tmp/fixed.pcap\n"
            "        /usr/bin/echo *",
            "    (root) NOPASSWD: /usr/bin/tcpdump -w /tmp/fixed.pcap\n"
            "    (root) NOPASSWD: /usr/bin/echo *",
            "    (operator) NOPASSWD: /usr/bin/tcpdump -w /tmp/*",
            "    (ALL, !root : ALL) NOPASSWD: /usr/bin/tcpdump -w /tmp/*",
            "    (root) NOPASSWD: !/usr/bin/tcpdump -w /tmp/*",
            "    (root) NOPASSWD: /usr/bin/tcpdump -w /tmp/\\*",
        )
        for policy in cases:
            with self.subTest(policy=policy):
                self.assert_candidate(policy, 0)

    def test_module_metadata_and_shell_syntax(self):
        from linPEAS.builder.src.linpeasModule import LinpeasModule

        module = LinpeasModule(str(self.module))
        self.assertEqual(module.id, "UG_Sudo_l")
        self.assertTrue(module.is_small)
        self.assertFalse(module.is_fat)
        result = subprocess.run(["sh", "-n", str(self.module)], capture_output=True,
                                text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
