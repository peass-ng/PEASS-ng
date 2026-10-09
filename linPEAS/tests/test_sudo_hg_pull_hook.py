"""Fixed Mercurial pull sources can still use the caller's local hooks."""

import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1] /
          "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
MARKER = "Sudo Mercurial receiving-repo hook review candidate"


class SudoHgPullHookTests(unittest.TestCase):
    def run_policy(self, policy):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            bindir = base / "bin"
            bindir.mkdir()
            sudo = bindir / "sudo"
            sudo.write_text(
                '#!/bin/sh\n'
                '[ "$1 $2" = "-n -l" ] || exit 99\n'
                'cat "$FAKE_SUDO_RULES"\n'
            )
            sudo.chmod(0o755)
            rules = base / "rules"
            rules.write_text(policy)
            env = os.environ.copy()
            env.update(PATH=f"{bindir}:{env['PATH']}", FAKE_SUDO_RULES=str(rules))
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
            return result.stdout

    def test_fixed_source_under_different_runas_is_conditional_cue(self):
        output = self.run_policy(
            '    (service : service) /usr/bin/hg pull /srv/source/\n'
        )
        self.assertEqual(output.count(MARKER), 1, output)
        self.assertIn("verify Mercurial trusted users/groups", output)

    def test_other_commands_and_exclusions_do_not_report(self):
        for policy in (
            '    (service) /usr/bin/hg log /srv/source/\n',
            '    (service) /usr/bin/hg -R /tmp/repo pull /srv/source/\n',
            '    (service) /usr/bin/hg pull /srv/source/, !/usr/bin/hg\n',
            '    (service, !root) /usr/bin/hg pull /srv/source/\n',
            '    (service) /usr/bin/hg pull\n',
        ):
            with self.subTest(policy=policy):
                self.assertNotIn(MARKER, self.run_policy(policy))

    def test_module_syntax(self):
        result = subprocess.run(["sh", "-n", str(MODULE)],
                                capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
