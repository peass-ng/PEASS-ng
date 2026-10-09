"""Bounded passive review of sudo Python wrappers that run Docker Compose."""

import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1] /
          "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
MARKER = "Sudo Compose wrapper review candidate"
GOOD_SCRIPT = ("#!/usr/bin/python3\nimport sys, subprocess\n"
               "filename = sys.argv[1]\n"
               "subprocess.run(['/usr/bin/docker-compose', 'up', '--build'])\n")


class SudoComposeWrapperTests(unittest.TestCase):
    def run_fixture(self, script_text=GOOD_SCRIPT, rule=None, script_mode=0o755,
                    symlink_ancestor=False):
        safe_tmp = "/private/tmp" if Path("/private/tmp").is_dir() else "/tmp"
        with tempfile.TemporaryDirectory(dir=safe_tmp) as tmp:
            base = Path(tmp)
            bindir = base / "bin"
            bindir.mkdir()
            if symlink_ancestor:
                (base / "real").mkdir()
                (base / "alias").symlink_to(base / "real", target_is_directory=True)
                script = base / "alias" / "wrapper.py"
            else:
                script = base / "wrapper.py"
            script.write_text(script_text)
            script.chmod(script_mode)
            policy = (rule or "    (root) NOPASSWD: {script} *.yml\n").format(script=script)
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
                                    capture_output=True, text=True, timeout=20)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(calls.read_text().splitlines(), ["-n -l", "-n -l"])
            return result.stdout, str(script)

    def test_exact_root_rule_and_python_compose_call(self):
        output, script = self.run_fixture()
        self.assertEqual(output.count(MARKER), 1, output)
        self.assertIn(script, output)
        self.assertIn("wrapper validation", output)
        yaml_output, _ = self.run_fixture(
            rule="    (ALL : ALL) PASSWD: {script} *.yaml\n")
        self.assertEqual(yaml_output.count(MARKER), 1, yaml_output)

    def test_non_root_deny_and_constrained_rules(self):
        for template in (
            "    (daemon) NOPASSWD: {script} *.yml\n",
            "    (root) NOEXEC: {script} *.yml\n",
            "    (root) NOPASSWD: {script} fixed.yml\n",
            "    (root) NOPASSWD: {script} *.yml, !{script} *.yml\n",
            "    (ALL, !root) NOPASSWD: {script} *.yml\n",
        ):
            with self.subTest(template=template):
                output, _ = self.run_fixture(rule=template)
                self.assertNotIn(MARKER, output)

    def test_script_gates_and_bounds(self):
        for content in (
            GOOD_SCRIPT.replace("sys.argv[1]", "'fixed.yml'"),
            GOOD_SCRIPT.replace("docker-compose", "podman-compose"),
            GOOD_SCRIPT.replace("docker-compose", "notdocker-compose"),
            GOOD_SCRIPT.replace("'up'", "'config'"),
            GOOD_SCRIPT.replace("'up'", "'backup'"),
            "#!/usr/bin/python3\n" + "x" * 65537,
        ):
            with self.subTest(content=content[:40]):
                output, _ = self.run_fixture(script_text=content)
                self.assertNotIn(MARKER, output)
        output, _ = self.run_fixture(script_mode=0o644)
        self.assertNotIn(MARKER, output)
        output, _ = self.run_fixture(symlink_ancestor=True)
        self.assertNotIn(MARKER, output)

    def test_policy_limits_and_shell_syntax(self):
        output, _ = self.run_fixture(rule="x\n" * 3001)
        self.assertNotIn(MARKER, output)
        output, _ = self.run_fixture(
            rule="    (root) NOPASSWD: {script} *.yml\n"
                 "        --unexpected-continuation\n")
        self.assertNotIn(MARKER, output)
        syntax = subprocess.run(["sh", "-n", str(MODULE)], capture_output=True,
                                text=True)
        self.assertEqual(syntax.returncode, 0, syntax.stderr)


if __name__ == "__main__":
    unittest.main()
