"""Read-only detection of sudo rules that expose Docker exec options."""

import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1] /
          "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
MARKER = "Sudo Docker exec privilege review candidate"


class SudoDockerExecTests(unittest.TestCase):
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

    def test_root_capable_wildcard_grants(self):
        rules = (
            "    (root) NOPASSWD: /snap/bin/docker exec *\n",
            "    (ALL : ALL) PASSWD: /usr/bin/docker exec -it *\n",
            "    (#0) NOPASSWD: /usr/local/bin/docker exec --privileged --user root *\n",
            "    (root) NOPASSWD: /bin/id, /usr/bin/docker exec *\n",
            "    (root) NOPASSWD: /usr/bin/docker exec *, !/usr/bin/docker ps *\n",
        )
        for rule in rules:
            with self.subTest(rule=rule):
                output = self.run_policy(rule)
                self.assertEqual(output.count(MARKER), 1, output)
                self.assertIn("verify effective policy", output)

    def test_constrained_or_ambiguous_rules_are_not_reported(self):
        rules = (
            "",
            "sudo: a password is required\n",
            "    (daemon) NOPASSWD: /usr/bin/docker exec *\n",
            "    (ALL, !root) NOPASSWD: /usr/bin/docker exec *\n",
            "    (root) NOPASSWD: !/usr/bin/docker exec *\n",
            "    (root) NOPASSWD: /usr/bin/docker exec *, !/usr/bin/docker exec --privileged *\n",
            "    (root) NOPASSWD: /usr/bin/docker ps *\n",
            "    (root) NOPASSWD: /usr/bin/notdocker exec *\n",
            "    (root) NOPASSWD: /usr/bin/docker exec\n",
            '    (root) NOPASSWD: /usr/bin/docker exec ""\n',
            "    (root) NOPASSWD: /usr/bin/docker exec fixed /bin/true\n",
            "    (root) NOPASSWD: /usr/bin/docker exec fixed *\n",
            "    (root) NOPASSWD: /usr/bin/docker exec fix*\n",
            "    (root) NOPASSWD: /usr/bin/docker exec \\*\n",
            "    (root) NOPASSWD: /usr/bin/docker exec *\n"
            "        --privileged\n",
        )
        for rule in rules:
            with self.subTest(rule=rule):
                self.assertNotIn(MARKER, self.run_policy(rule))

    def test_repeated_capture_and_input_limits(self):
        rule = "    (root) NOPASSWD: /usr/bin/docker exec *\n"
        self.assertEqual(self.run_policy(rule).count(MARKER), 1)
        self.assertNotIn(MARKER, self.run_policy(rule + "x\n" * 3001))
        self.assertNotIn(MARKER, self.run_policy(rule + "x" * 2049 + "\n"))

    def test_module_metadata_and_syntax(self):
        from linPEAS.builder.src.linpeasModule import LinpeasModule

        module = LinpeasModule(str(MODULE))
        self.assertEqual(module.id, "UG_Sudo_l")
        self.assertTrue(module.is_small)
        self.assertFalse(module.is_fat)
        check = subprocess.run(["sh", "-n", str(MODULE)], capture_output=True,
                               text=True)
        self.assertEqual(check.returncode, 0, check.stderr)


if __name__ == "__main__":
    unittest.main()
