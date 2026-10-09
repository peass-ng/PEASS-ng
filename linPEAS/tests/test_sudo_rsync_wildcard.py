"""Passive sudoers argument-wildcard correlation for rsync archive copies."""

import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1] /
          "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
MARKER = "Sudo rsync argument wildcard review candidate"


class SudoRsyncWildcardTests(unittest.TestCase):
    def run_policy(self, policy, owned_dest=False, linked_dest=False):
        with tempfile.TemporaryDirectory(dir=MODULE.parent) as tmp:
            base = Path(tmp)
            source = base / "source"
            source.mkdir()
            dest = Path('/usr')
            if owned_dest or linked_dest:
                dest = base / 'dest'
                if linked_dest:
                    dest.symlink_to('/usr', target_is_directory=True)
                else:
                    dest.mkdir()
            bindir = base / "bin"
            bindir.mkdir()
            sudo = bindir / "sudo"
            sudo.write_text(
                '#!/bin/sh\n'
                'printf "%s\\n" "$*" >> "$FAKE_SUDO_CALLS"\n'
                '[ "$1 $2" = "-n -l" ] || exit 99\n'
                'cat "$FAKE_SUDO_RULES"\n'
            )
            sudo.chmod(0o755)
            rules = base / "rules"
            rules.write_text(policy.replace("@SOURCE@", str(source))
                             .replace("@DEST@", str(dest)))
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

    @unittest.skipIf(os.geteuid() == 0, "destination ownership requires an unprivileged user")
    def test_archive_rule_with_argument_wildcard_reports_once(self):
        policy = ('    (root : root) NOPASSWD: /usr/bin/rsync -a '
                  '--exclude\\=.hg @SOURCE@/* @DEST@/\n')
        output = self.run_policy(policy)
        self.assertEqual(output.count(MARKER), 1, output)
        self.assertIn("writable source directory", output)

    def test_owned_or_symlinked_destination_does_not_report(self):
        policy = '    (root) NOPASSWD: /usr/bin/rsync -a @SOURCE@/* @DEST@/\n'
        self.assertNotIn(MARKER, self.run_policy(policy, owned_dest=True))
        self.assertNotIn(MARKER, self.run_policy(policy, linked_dest=True))

    def test_fixed_arguments_and_unprivileged_runas_do_not_report(self):
        policies = (
            '    (root) NOPASSWD: /usr/bin/rsync -a @SOURCE@/file @DEST@/\n',
            '    (root) NOPASSWD: /usr/bin/rsync -r @SOURCE@/* @DEST@/\n',
            '    (root) NOPASSWD: /usr/bin/rsync -a -- @SOURCE@/* @DEST@/\n',
            '    (root) NOPASSWD: /usr/bin/rsync -a --no-perms @SOURCE@/* @DEST@/\n',
            '    (daemon) NOPASSWD: /usr/bin/rsync -a @SOURCE@/* @DEST@/\n',
            '    (ALL, !root) NOPASSWD: /usr/bin/rsync -a @SOURCE@/* @DEST@/\n',
            '    (root) NOPASSWD: /usr/bin/rsync -a @SOURCE@/* @DEST@/, !/usr/bin/rsync\n',
        )
        for policy in policies:
            with self.subTest(policy=policy):
                self.assertNotIn(MARKER, self.run_policy(policy))

    def test_policy_cap_is_explicit(self):
        policy = '    (root) NOPASSWD: /usr/bin/rsync -a @SOURCE@/* @DEST@/\n'
        output = self.run_policy(policy + "x\n" * 3001)
        self.assertNotIn(MARKER, output)
        self.assertIn("Sudo rsync wildcard review incomplete", output)

    def test_module_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
