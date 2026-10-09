import json
import os
import shlex
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


class SudoNeedrestartTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
        )

    def run_case(self, policy, *, allow=False, root_folder="/", password="",
                 config_dir=True):
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp)
            bindir = base / "bin"
            bindir.mkdir()
            log = base / "sudo_calls.jsonl"
            sudo = bindir / "sudo"
            sudo.write_text(
                f"#!{sys.executable}\n"
                "import json, os, sys\n"
                "from pathlib import Path\n"
                "args = sys.argv[1:]\n"
                "with open(os.environ['FAKE_SUDO_LOG'], 'a') as out:\n"
                "    out.write(json.dumps(args) + '\\n')\n"
                "if args in (['-n', '-l'], ['-S', '-l']):\n"
                "    print(os.environ['FAKE_SUDO_POLICY'])\n"
                "    sys.exit(0 if os.environ['FAKE_SUDO_POLICY'] else 1)\n"
                "if (len(args) == 8 and args[0] in ('-n', '-S') and\n"
                "    args[1:7] == ['-l', '-u', 'root', '--',\n"
                "                  '/usr/sbin/needrestart', '-c'] and\n"
                "    args[7].endswith('.conf') and\n"
                "    not Path(args[7]).exists()):\n"
                "    sys.exit(0 if os.environ['FAKE_QUERY_ALLOW'] == '1' else 1)\n"
                "sys.exit(99)  # No needrestart execution or other sudo command.\n"
            )
            sudo.chmod(0o755)
            timeout = bindir / "timeout"
            timeout.write_text("#!/bin/sh\nshift\nexec \"$@\"\n")
            timeout.chmod(0o755)
            config = base / "config"
            if config_dir:
                config.mkdir()
            env = os.environ.copy()
            env.update({
                "PATH": f"{bindir}:{env['PATH']}",
                "FAKE_SUDO_LOG": str(log),
                "FAKE_SUDO_POLICY": policy,
                "FAKE_QUERY_ALLOW": "1" if allow else "0",
                "TMPDIR": str(config),
            })
            body = "\n".join([
                "E=E",
                "sudoB=__unlikely_sudoB__",
                "sudoG=__unlikely_sudoG__",
                "sudoVB1=__unlikely_sudoVB1__",
                "sudoVB2=__unlikely_sudoVB2__",
                "SED_RED='&'",
                "SED_RED_YELLOW='&'",
                "SED_GREEN='&'",
                f"ROOT_FOLDER={shlex.quote(root_folder)}",
                f"PASSWORD={shlex.quote(password)}",
                f"TIMEOUT={shlex.quote(str(timeout))}",
                "print_2title() { :; }",
                "print_info() { :; }",
                "echo_not_found() { :; }",
                f". {shlex.quote(str(self.module))}",
            ])
            result = subprocess.run(["sh", "-c", body], env=env,
                                    capture_output=True, text=True, timeout=10)
            self.assertEqual(result.returncode, 0, result.stderr)
            calls = [json.loads(line) for line in log.read_text().splitlines()]
            queries = [call for call in calls if "-u" in call]
            self.assertLessEqual(len(queries), 1)
            self.assertTrue(all("-l" in call for call in calls), calls)
            return result.stdout, queries

    def test_unrestricted_root_rule_confirms_exact_config_authorization(self):
        output, queries = self.run_case(
            "    (root) NOPASSWD: /usr/sbin/needrestart", allow=True)
        self.assertIn("sudo permits root needrestart with a caller-controlled -c config", output)
        self.assertEqual(len(queries), 1)
        self.assertEqual(queries[0][0], "-n")

    def test_fixed_package_and_disabled_interpscan_do_not_hide_config_route(self):
        from linPEAS.tests.test_needrestart import NeedrestartCVE202448990Tests

        cve_fixture = NeedrestartCVE202448990Tests()
        NeedrestartCVE202448990Tests.setUpClass()
        with tempfile.TemporaryDirectory() as temp:
            root = cve_fixture._make_root(Path(temp), main_value="1",
                                          snippet_value="0")
            cve_result = cve_fixture._run_check(root, "3.6-7ubuntu4.3")
        self.assertEqual(cve_result.returncode, 0, cve_result.stderr)
        self.assertIn("Effective interpreter scanning: 0", cve_result.stdout)
        self.assertIn("is not vulnerable to CVE-2024-48990", cve_result.stdout)

        output, queries = self.run_case(
            "    (ALL : ALL) NOPASSWD: /usr/sbin/needrestart", allow=True)
        self.assertIn("sudo permits root needrestart", output)
        self.assertEqual(len(queries), 1)
        self.assertNotIn("CVE-2024-48990", output)

    def test_fixed_arguments_nonroot_and_negation_are_not_confirmed(self):
        for policy in (
            '    (root) NOPASSWD: /usr/sbin/needrestart ""',
            "    (root) NOPASSWD: /usr/sbin/needrestart -b",
            "    (daemon) NOPASSWD: /usr/sbin/needrestart",
            "    (root) NOPASSWD: ALL, !/usr/sbin/needrestart",
        ):
            with self.subTest(policy=policy):
                output, queries = self.run_case(policy)
                self.assertNotIn("sudo permits root needrestart", output)
                self.assertEqual(len(queries), 1)

    def test_no_writable_config_dir_or_exact_command_skips_query(self):
        rule = "    (root) NOPASSWD: /usr/sbin/needrestart"
        for policy, config_dir in (
            (rule, False),
            ("", True),
            ("    (root) NOPASSWD: /usr/sbin/needrestart-helper", True),
            ("    (root) NOPASSWD: /usr/bin/needrestart", True),
        ):
            with self.subTest(policy=policy, config_dir=config_dir):
                output, queries = self.run_case(policy, allow=True,
                                                config_dir=config_dir)
                self.assertNotIn("sudo permits root needrestart", output)
                self.assertEqual(queries, [])

    def test_authentication_unavailable_remains_unverified(self):
        output, queries = self.run_case(
            "    (root) /usr/sbin/needrestart")
        self.assertIn("sudo needrestart -c authorization unverified", output)
        self.assertNotIn("sudo permits root needrestart", output)
        self.assertEqual(len(queries), 1)

    def test_supplied_password_uses_one_exact_query(self):
        output, queries = self.run_case(
            "    (root) /usr/sbin/needrestart", allow=True, password="fixture-password")
        self.assertIn("sudo permits root needrestart", output)
        self.assertEqual(len(queries), 1)
        self.assertEqual(queries[0][0], "-S")

    def test_offline_root_folder_skips_live_query(self):
        output, queries = self.run_case(
            "    (root) NOPASSWD: /usr/sbin/needrestart", allow=True,
            root_folder="/offline-root")
        self.assertNotIn("sudo permits root needrestart", output)
        self.assertEqual(queries, [])


if __name__ == "__main__":
    unittest.main()
