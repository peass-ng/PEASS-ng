"""The sudo Nmap review must inspect a small wrapper without running it."""

import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1] /
          "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
MARKER = "Sudo Nmap wrapper NSE data-loader review candidate"


class SudoNmapWrapperTests(unittest.TestCase):
    def run_policy(self, policy, wrapper=None):
        # The probe intentionally rejects symlink path components (macOS /tmp
        # and /var are symlinks), so keep fixtures in the resolved worktree.
        with tempfile.TemporaryDirectory(dir=MODULE.parent) as tmp:
            base = Path(tmp)
            bindir = base / "bin"
            bindir.mkdir()
            nmap = bindir / "nmap"
            if wrapper is not None:
                nmap.write_text(wrapper)
                nmap.chmod(0o755)
            sudo = bindir / "sudo"
            sudo.write_text(
                '#!/bin/sh\n'
                'printf "%s\\n" "$*" >> "$FAKE_SUDO_CALLS"\n'
                '[ "$1 $2" = "-n -l" ] || exit 99\n'
                'cat "$FAKE_SUDO_RULES"\n'
            )
            sudo.chmod(0o755)
            rules = base / "rules"
            rules.write_text(policy.replace("@NMAP@", str(nmap)))
            calls = base / "calls"
            env = os.environ.copy()
            env.update(PATH=f"{bindir}:{env['PATH']}",
                       FAKE_SUDO_RULES=str(rules), FAKE_SUDO_CALLS=str(calls))
            body = "\n".join((
                "E=E", "sudoB=__unused__", "sudoG=__unused__",
                "sudoVB1=__unused__", "sudoVB2=__unused__",
                "SED_RED='&'", "SED_RED_YELLOW='&'", "SED_GREEN='&'",
                "PASSWORD=", "TIMEOUT=", "ROOT_FOLDER=",
                "print_2title() { :; }", "print_info() { :; }",
                "echo_not_found() { :; }",
                f". {shlex.quote(str(MODULE))}",
            ))
            result = subprocess.run(["sh", "-c", body], env=env,
                                    capture_output=True, text=True, timeout=15)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(calls.read_text().splitlines(), ["-n -l", "-n -l"])
            return result.stdout

    def test_incomplete_filter_and_forwarding_report_once(self):
        wrapper = ('#!/bin/bash\n'
                   'case "$*" in *--script*) exit 1;; esac\n'
                   'exec /usr/bin/nmap.original "$@"\n')
        output = self.run_policy('    (ALL : ALL) NOPASSWD: @NMAP@ *\n', wrapper)
        self.assertEqual(output.count(MARKER), 1, output)
        self.assertIn("--datadir filter not seen", output)

    def test_fixed_args_or_exclusions_do_not_report(self):
        wrapper = ('#!/bin/bash\n'
                   'case "$*" in *--script*) exit 1;; esac\n'
                   'exec /usr/bin/nmap.original "$@"\n')
        for policy in (
            '    (root) NOPASSWD: @NMAP@ -sT 127.0.0.1\n',
            '    (daemon) NOPASSWD: @NMAP@ *\n',
            '    (ALL, !root) NOPASSWD: @NMAP@ *\n',
            '    (root) NOPASSWD: @NMAP@ *, !@NMAP@ *\n',
        ):
            with self.subTest(policy=policy):
                self.assertNotIn(MARKER, self.run_policy(policy, wrapper))

    def test_binary_or_filtered_datadir_do_not_report(self):
        rule = '    (root) NOPASSWD: @NMAP@ *\n'
        for wrapper in (
            '\x7fELFbinary --script exec /usr/bin/nmap.original "$@"',
            '#!/bin/bash\n# --script --datadir\nexec /usr/bin/nmap.original "$@"\n',
            '#!/bin/bash\n# --script\nexec /usr/bin/nmap.original "$@"\n',
            '#!/bin/bash\necho --script\nexec /usr/bin/nmap.original "$@"\n',
            '#!/bin/bash\n# --script\necho no forwarding\n',
        ):
            with self.subTest(wrapper=wrapper):
                self.assertNotIn(MARKER, self.run_policy(rule, wrapper))

    def test_file_and_policy_caps_are_explicit(self):
        rule = '    (root) NOPASSWD: @NMAP@ *\n'
        oversized = '#!/bin/bash\n# --script\nexec /usr/bin/nmap.original "$@"\n' + '#' * 66000
        self.assertIn("64 KiB file limit", self.run_policy(rule, oversized))
        small = '#!/bin/bash\n# --script\nexec /usr/bin/nmap.original "$@"\n'
        self.assertIn("policy limit", self.run_policy(rule + "x\n" * 3001, small))

    def test_module_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
