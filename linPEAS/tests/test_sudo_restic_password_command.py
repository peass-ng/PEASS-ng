import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SudoResticPasswordCommandTests(unittest.TestCase):
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
            calls = Path(temp) / "sudo-calls"
            sudo = bindir / "sudo"
            sudo.write_text(
                "#!/bin/sh\n"
                "printf '%s\\n' \"$*\" >> \"$FAKE_SUDO_CALLS\"\n"
                "[ \"$1 $2\" = '-n -l' ] || exit 99\n"
                "printf '%s\\n' \"$FAKE_SUDO_POLICY\"\n",
                encoding="utf-8",
            )
            sudo.chmod(0o755)
            env = os.environ.copy()
            env.update(
                PATH=f"{bindir}:{env['PATH']}",
                FAKE_SUDO_CALLS=str(calls),
                FAKE_SUDO_POLICY=policy,
            )
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
            self.assertEqual(calls.read_text().splitlines(), ["-n -l", "-n -l"])
            return result.stdout

    def test_unrestricted_root_grant_is_candidate_only(self):
        output = self.run_policy(
            "User operator may run the following commands on host:\n"
            "    (ALL : ALL) NOPASSWD: /usr/bin/restic"
        )
        self.assertEqual(output.count("Sudo restic password-command review candidate"), 1)
        self.assertIn("verify effective policy and authentication", output)
        self.assertNotIn("root command execution", output)

    def test_restricted_nonroot_negated_and_log_mentions_do_not_trigger(self):
        policies = (
            '    (root) NOPASSWD: /usr/bin/restic ""',
            '    (root) NOPASSWD: /usr/bin/restic check',
            '    (root) NOPASSWD: /usr/bin/restic --password-command=/fixed',
            '    (operator) NOPASSWD: /usr/bin/restic',
            '    (ALL, !root) NOPASSWD: /usr/bin/restic',
            '    (root) NOPASSWD: !/usr/bin/restic',
            '    (root) NOPASSWD: /usr/bin/restic, !/usr/bin/restic --password-command=*',
            '    (root) NOPASSWD: /usr/bin/restic-helper',
            '    (root) NOPASSWD: /usr/bin/old-restic',
            'log: restic check --password-command=helper',
        )
        for policy in policies:
            with self.subTest(policy=policy):
                self.assertNotIn(
                    "Sudo restic password-command review candidate",
                    self.run_policy(policy),
                )

    def test_comma_rule_and_duplicate_sudo_output_emit_once(self):
        output = self.run_policy(
            "    (root) NOPASSWD: /bin/true, /usr/local/bin/restic"
        )
        self.assertEqual(output.count("Sudo restic password-command review candidate"), 1)

    def test_backup_repository_wildcard_is_conditional_candidate(self):
        for command in (
            "/usr/bin/restic backup -r rest*",
            "/usr/local/bin/restic backup --repo=rest:http://backup/*",
            "/usr/bin/restic backup --repo rest*",
        ):
            with self.subTest(command=command):
                output = self.run_policy(f"    (root) NOPASSWD: {command}")
                self.assertEqual(output.count("Sudo restic backup disclosure review candidate"), 1)
                self.assertIn("verify effective policy", output)
                self.assertNotIn("confirmed disclosure", output)
                self.assertNotIn("Sudo restic password-command review candidate", output)

    def test_fixed_nonroot_and_negated_backup_rules_do_not_trigger(self):
        policies = (
            "    (root) NOPASSWD: /usr/bin/restic backup -r rest:http://fixed/repo /fixed/source",
            "    (root) NOPASSWD: /usr/bin/restic backup -r rest* /fixed/source",
            "    (root) NOPASSWD: /usr/bin/restic check -r rest*",
            "    (operator) NOPASSWD: /usr/bin/restic backup -r rest*",
            "    (ALL, !root) NOPASSWD: /usr/bin/restic backup -r rest*",
            "    (root) NOPASSWD: !/usr/bin/restic backup -r rest*",
            "    (root) NOPASSWD: /usr/bin/restic backup -r rest*, !/usr/bin/restic backup -r rest*",
            "    (root) NOPASSWD: /usr/bin/restic-helper backup -r rest*",
            "log: /usr/bin/restic backup -r rest*",
        )
        for policy in policies:
            with self.subTest(policy=policy):
                self.assertNotIn(
                    "Sudo restic backup disclosure review candidate",
                    self.run_policy(policy),
                )

    def test_backup_comma_rule_and_duplicate_sudo_output_emit_once(self):
        output = self.run_policy(
            "    (root) NOPASSWD: /bin/true, /usr/bin/restic backup -r rest*"
        )
        self.assertEqual(output.count("Sudo restic backup disclosure review candidate"), 1)

    def test_shell_syntax_and_metadata(self):
        from linPEAS.builder.src.linpeasModule import LinpeasModule

        module = LinpeasModule(str(self.module))
        self.assertEqual(module.id, "UG_Sudo_l")
        result = subprocess.run(
            ["sh", "-n", str(self.module)], capture_output=True, text=True
        )
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
