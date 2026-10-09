"""Read-only review of Bee sudo grants and Backdrop configuration paths."""

import os
import re
import shlex
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
MODULE = ROOT / "linPEAS/builder/linpeas_parts/6_users_information/7_Sudo_l.sh"
SIGNATURES = ROOT / "build_lists/sensitive_files.yaml"
MARKER = "Sudo Bee PHP CLI review candidate"


class SudoBeeBackdropTests(unittest.TestCase):
    def run_policy(self, policy):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            bindir = base / "bin"
            bindir.mkdir()
            rules = base / "rules"
            rules.write_text(policy, encoding="utf-8")
            calls = base / "calls"
            sudo = bindir / "sudo"
            sudo.write_text(
                '#!/bin/sh\n'
                'printf "%s\\n" "$*" >> "$FAKE_SUDO_CALLS"\n'
                '[ "$1 $2" = "-n -l" ] || exit 99\n'
                'cat "$FAKE_SUDO_RULES"\n',
                encoding="utf-8",
            )
            sudo.chmod(0o755)
            env = os.environ.copy()
            env.update(
                PATH=f"{bindir}:{env['PATH']}",
                FAKE_SUDO_RULES=str(rules),
                FAKE_SUDO_CALLS=str(calls),
            )
            script = "\n".join((
                "E=E", "sudoB=__unused__", "sudoG=__unused__",
                "sudoVB1=__unused__", "sudoVB2=__unused__",
                "SED_RED='&'", "SED_RED_YELLOW='&'", "SED_GREEN='&'",
                "ROOT_FOLDER=/", "PASSWORD=", "TIMEOUT=",
                "print_2title() { :; }", "print_info() { :; }",
                "echo_not_found() { :; }",
                f". {shlex.quote(str(MODULE))}",
            ))
            result = subprocess.run(
                ["sh", "-c", script], env=env, capture_output=True,
                text=True, timeout=15,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(calls.read_text(encoding="utf-8").splitlines(),
                             ["-n -l", "-n -l"])
            return result.stdout

    def test_root_grant_reports_once_without_executing_bee(self):
        for runas in ("root", "#0", "ALL"):
            with self.subTest(runas=runas):
                output = self.run_policy(
                    f"    ({runas} : ALL) NOPASSWD: /usr/local/bin/bee\n"
                )
                self.assertEqual(output.count(MARKER), 1)
                self.assertIn("site bootstrap", output)
        output = self.run_policy("    (root) /opt/bee/bee.php\n")
        self.assertEqual(output.count(MARKER), 1)
        output = self.run_policy("    (root) /bin/true, /usr/local/bin/bee\n")
        self.assertEqual(output.count(MARKER), 1)

    def test_restricted_nonroot_negated_and_other_binary_are_not_candidates(self):
        policies = (
            "    (daemon) /usr/local/bin/bee\n",
            "    (ALL, !root) /usr/local/bin/bee\n",
            "    (root) /usr/local/bin/bee status\n",
            '    (root) /usr/local/bin/bee ""\n',
            "    (root) !/usr/local/bin/bee\n",
            "    (root) /usr/local/bin/bee, !/usr/local/bin/bee\n",
            "    (root) /usr/local/bin/bee\n    (root) !/usr/local/bin/bee\n",
            "    (root) /usr/local/bin/bee, !/usr/local/bin/bee eval *\n",
            "    (root) /usr/local/bin/beekeeper\n",
            "    (root) /usr/bin/php /opt/bee/bee.php\n",
            "    (root) BEE_ALIAS\n",
            "    (root) /usr/local/bin/bee\n        --root=/fixed\n",
            "log: bee eval\n",
        )
        for policy in policies:
            with self.subTest(policy=policy):
                self.assertNotIn(MARKER, self.run_policy(policy))

    def test_policy_limits_suppress_ambiguous_results(self):
        rule = "    (root) /usr/local/bin/bee\n"
        self.assertNotIn(MARKER, self.run_policy(rule + "x\n" * 3001))
        self.assertNotIn(MARKER, self.run_policy(rule + "x" * 2049 + "\n"))

    def test_backdrop_selector_is_path_only_and_preserves_drupal(self):
        data = yaml.safe_load(SIGNATURES.read_text(encoding="utf-8"))
        backdrop = next(item for item in data["search"]
                        if item["name"] == "Backdrop CMS settings candidates")
        drupal = next(item for item in data["search"]
                      if item["name"] == "Drupal")
        self.assertTrue(backdrop["value"]["config"]["auto_check"])
        selector = backdrop["value"]["files"][0]["value"]
        self.assertTrue(selector["just_list_file"])
        self.assertTrue(re.search(selector["check_extra_path"],
                                  "/var/www/html/settings.php"))
        self.assertTrue(re.search(selector["remove_path"],
                                  "/var/www/html/sites/default/settings.php"))
        self.assertEqual(drupal["value"]["files"][0]["value"]
                         ["check_extra_path"], "/default/settings.php")

        sys.path.insert(0, str(ROOT / "linPEAS"))
        from builder.src.linpeasBuilder import LinpeasBuilder
        from builder.src.peasLoaded import PEASLoaded

        builder = LinpeasBuilder.__new__(LinpeasBuilder)
        builder.ploaded = PEASLoaded()
        section = builder._LinpeasBuilder__generate_sections()[
            "Backdrop CMS settings candidates"
        ]
        self.assertIn("PSTORAGE_BACKDROP_CMS_SETTINGS_CANDIDATES", section)
        self.assertNotIn('cat "$f"', section)
        with tempfile.TemporaryDirectory() as tmp:
            site = Path(tmp) / "settings.php"
            site.write_text("<?php $database = 'mysql://secret-never-print';\n",
                            encoding="utf-8")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ,
                     "PSTORAGE_BACKDROP_CMS_SETTINGS_CANDIDATES": str(site),
                     "E": "E", "SED_RED": "settings.php"},
                capture_output=True, text=True, timeout=5,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn(str(site), result.stdout)
            self.assertNotIn("secret-never-print", result.stdout)


if __name__ == "__main__":
    unittest.main()
