"""A sudo ProcMon grant is a passive cross-user trace review lead."""

import os
import re
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1]
          / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
FUNCTION = re.search(
    r"^sudo_procmon_trace_review\(\) \{\n.*?^\}",
    MODULE.read_text(), re.MULTILINE | re.DOTALL,
).group()
MARKER = "Sudo ProcMon root-process trace review candidate:"


def scan(policy, env=None):
    result = subprocess.run(
        ["sh", "-c", FUNCTION + '\nsudo_procmon_trace_review "$1"',
         "sh", policy],
        env=env, text=True, capture_output=True, check=True, timeout=3,
    )
    if result.stderr:
        raise AssertionError(result.stderr)
    return result.stdout


class SudoProcmonTraceTests(unittest.TestCase):
    def test_unrestricted_root_capable_grants(self):
        for runas in ("root", "ALL", "ALL : ALL", "#0"):
            with self.subTest(runas=runas):
                self.assertIn(MARKER,
                              scan(f"    ({runas}) NOPASSWD: /usr/bin/procmon\n"))
        self.assertIn(MARKER, scan("    (root) /opt/tools/procmon *\n"))

    def test_never_claims_nonroot_or_restricted_grants(self):
        for policy in (
            "    (builder) /usr/bin/procmon\n",
            "    (ALL, !root) /usr/bin/procmon\n",
            "    (root, !root) /usr/bin/procmon\n",
            "    (root) /usr/bin/procmon --help\n",
            '    (root) /usr/bin/procmon ""\n',
            "    (root) NOEXEC: /usr/bin/procmon\n",
            "    (root) NOEXEC: /usr/bin/true, /usr/bin/procmon\n",
            "    (root) /usr/bin/procmon-helper\n",
        ):
            with self.subTest(policy=policy):
                self.assertEqual("", scan(policy))

    def test_denial_and_ambiguous_policy_suppress_output(self):
        allowed = "    (root) /usr/bin/procmon\n"
        for policy in (
            allowed + "    (root) ! /usr/bin/procmon -p *\n",
            allowed + "    (root) !ALL\n",
            allowed + "    (root) NOEXEC: /usr/bin/procmon\n",
            allowed + "    --fixed-option /tmp/file\n",
            "x" * 2049 + "\n" + allowed,
            "x\n" * 3001 + allowed,
        ):
            with self.subTest(policy=policy[:70]):
                self.assertEqual("", scan(policy))

    def test_deduplicates_and_does_not_invoke_procmon(self):
        with tempfile.TemporaryDirectory() as tmp:
            trap = Path(tmp) / "procmon"
            marker = Path(tmp) / "invoked"
            trap.write_text(f"#!/bin/sh\nprintf called > '{marker}'\n")
            trap.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = tmp + os.pathsep + env["PATH"]
            rule = "    (root) /usr/bin/procmon\n"
            self.assertEqual(scan(rule * 3, env).count(MARKER), 1)
            self.assertFalse(marker.exists())

    def test_module_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
