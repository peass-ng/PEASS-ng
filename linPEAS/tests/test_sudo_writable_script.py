"""Focused read-only sudo-rule/writable-script correlation fixtures."""

import os
import re
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
FUNCTION = re.search(
    r"^sudo_writable_script_candidates\(\) \{\n.*?^\}",
    MODULE.read_text(),
    re.MULTILINE | re.DOTALL,
).group()
MARKER = "Sudo writable script review candidate:"


class SudoWritableScriptTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory(prefix="sudo-script-")
        self.addCleanup(self.tmp.cleanup)
        self.script = Path(self.tmp.name) / "task.sh"
        self.script.write_text("#!/bin/sh\nexit 0\n")
        self.script.chmod(0o700)

    def scan(self, policy, root=False):
        result = subprocess.run(
            ["sh", "-c", FUNCTION + '\nsudo_writable_script_candidates "$1"', "sh", policy],
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            env={**os.environ, "IAMROOT": "1" if root else ""},
            timeout=3,
            check=True,
        )
        self.assertEqual(result.stderr, "")
        return result.stdout

    def rule(self, path=None, runas="root", tags="NOPASSWD: ", args=""):
        return f"    ({runas}) {tags}{path or self.script}{args}\n"

    def test_exact_root_capable_writable_executable_script(self):
        for runas in ("root", "ALL", "ALL : ALL"):
            with self.subTest(runas=runas):
                self.assertIn(MARKER, self.scan(self.rule(runas=runas)))
        self.assertIn(MARKER, self.scan(self.rule(args=' ""')))

    def test_unrelated_rule_arguments_and_nonroot_identity(self):
        self.assertEqual(self.scan(self.rule(path="/usr/bin/id")), "")
        self.assertEqual(self.scan(self.rule(runas="service")), "")
        self.assertEqual(self.scan(self.rule(args=" --other")), "")
        self.assertEqual(self.scan(self.rule(path=f"{self.script}2")), "")
        self.assertEqual(self.scan(self.rule(), root=True), "")

    def test_denial_suppresses_even_when_allow_is_seen_first(self):
        self.assertEqual(self.scan(self.rule() + self.rule(tags="!")), "")
        self.assertEqual(self.scan(self.rule() + "    (root) !ALL\n"), "")

    def test_unwritable_nonexecutable_and_symlink_are_not_reported(self):
        if os.geteuid() != 0:
            self.script.chmod(0o500)
            self.assertEqual(self.scan(self.rule()), "")
            self.script.chmod(0o600)
            self.assertEqual(self.scan(self.rule()), "")
        self.script.chmod(0o700)
        link = Path(self.tmp.name) / "link.sh"
        link.symlink_to(self.script)
        self.assertEqual(self.scan(self.rule(path=link)), "")

    def test_policy_bounds_suppress_partial_decisions(self):
        self.assertEqual(self.scan(self.rule() + "x\n" * 3001), "")
        self.assertEqual(self.scan(self.rule() + "x" * 2049 + "\n"), "")
        many = self.rule() + "".join(
            self.rule(path=f"{self.tmp.name}/other{i}.sh") for i in range(33)
        )
        self.assertEqual(self.scan(many), "")


if __name__ == "__main__":
    unittest.main()
