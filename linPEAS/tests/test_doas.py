import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class DoasCheckTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.function_file = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "functions"
            / "checkDoas.sh"
        )
        cls.module_file = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "6_users_information"
            / "9_Doas.sh"
        )

    def _run_shell(self, body, env=None):
        script = f". {shlex.quote(str(self.function_file))}\n{body}"
        return subprocess.run(
            ["sh", "-c", script],
            cwd=str(self.repo_root),
            env=env,
            capture_output=True,
            text=True,
            check=False,
        )

    def test_version_parsing_and_comparison(self):
        result = self._run_shell(
            """
printf 'parsed=%s\n' "$(doas_extract_upstream_version '1:6.8.2-1ubuntu1')"
doas_version_lt 6.8 6.8.1 && echo lt=yes
doas_version_ge 6.8.1 6.8.1 && echo ge=yes
doas_version_le 6.8.2 6.8.2 && echo le=yes
! doas_version_lt 6.9 6.8.1 && echo newer=yes
"""
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("parsed=6.8.2", result.stdout)
        self.assertIn("lt=yes", result.stdout)
        self.assertIn("ge=yes", result.stdout)
        self.assertIn("le=yes", result.stdout)
        self.assertIn("newer=yes", result.stdout)

    def test_soccer_rule_matches_current_user_and_dstat(self):
        result = self._run_shell(
            """
doas_current_user=player
doas_current_uid=1000
doas_current_groups='player users'
doas_current_gids='1000 100'
sudoVB1='dstat$'
sudoVB2=''
rule='permit nopass player as root cmd /usr/bin/dstat'
printf 'identity=%s\n' "$(doas_rule_identity "$rule")"
printf 'command=%s\n' "$(doas_rule_command "$rule")"
doas_rule_applies_to_current_user "$rule" && echo applies=yes
doas_rule_targets_root "$rule" && echo root=yes
doas_rule_has_option "$rule" nopass && echo nopass=yes
doas_command_is_dangerous "$(doas_rule_command "$rule")" && echo dangerous=yes
"""
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("identity=player", result.stdout)
        self.assertIn("command=/usr/bin/dstat", result.stdout)
        self.assertIn("applies=yes", result.stdout)
        self.assertIn("root=yes", result.stdout)
        self.assertIn("nopass=yes", result.stdout)
        self.assertIn("dangerous=yes", result.stdout)

    def test_group_rule_and_setenv_options_are_parsed(self):
        result = self._run_shell(
            """
doas_current_user=alice
doas_current_uid=1001
doas_current_groups='alice wheel'
doas_current_gids='1001 10'
rule='permit setenv { PATH=/tmp LD_PRELOAD } nopass :wheel as root cmd /usr/bin/env'
printf 'identity=%s\n' "$(doas_rule_identity "$rule")"
doas_rule_applies_to_current_user "$rule" && echo applies=yes
doas_rule_has_option "$rule" setenv && echo setenv=yes
doas_rule_has_dangerous_environment "$rule" && echo environment=dangerous
"""
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("identity=:wheel", result.stdout)
        self.assertIn("applies=yes", result.stdout)
        self.assertIn("setenv=yes", result.stdout)
        self.assertIn("environment=dangerous", result.stdout)

    def test_rule_reader_ignores_comments_but_preserves_quoted_hashes(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            config = Path(tmpdir) / "doas.conf"
            config.write_text(
                "# ignored\n"
                "permit nopass player as root cmd /usr/bin/dstat # Soccer\n"
                'permit player cmd /usr/bin/printf args "#kept"\n'
                "deny :blocked\n",
                encoding="utf-8",
            )
            result = self._run_shell(
                f"doas_read_rules {shlex.quote(str(config))}"
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn("ignored", result.stdout)
        self.assertNotIn("Soccer", result.stdout)
        self.assertIn("permit nopass player as root cmd /usr/bin/dstat", result.stdout)
        self.assertIn('args "#kept"', result.stdout)
        self.assertIn("deny :blocked", result.stdout)

    def test_doas_c_evaluation_never_runs_the_requested_command(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            tmp_path = Path(tmpdir)
            fake_doas = tmp_path / "doas"
            config = tmp_path / "doas.conf"
            executed = tmp_path / "executed"
            config.write_text(
                "permit nopass player as root cmd /usr/bin/dstat\n",
                encoding="utf-8",
            )
            fake_doas.write_text(
                "#!/bin/sh\n"
                "[ \"$1\" = -C ] || exit 20\n"
                "[ -f \"$2\" ] || exit 21\n"
                "if [ $# -eq 2 ]; then exit 0; fi\n"
                "[ \"$3\" = /usr/bin/dstat ] || { echo deny; exit 1; }\n"
                "echo 'permit nopass'\n",
                encoding="utf-8",
            )
            fake_doas.chmod(0o755)

            result = self._run_shell(
                "\n".join(
                    [
                        "TIMEOUT=",
                        f"doas_config_syntax_valid {shlex.quote(str(fake_doas))} {shlex.quote(str(config))} && echo syntax=ok",
                        f"doas_check_command {shlex.quote(str(fake_doas))} {shlex.quote(str(config))} /usr/bin/dstat",
                        f"[ ! -e {shlex.quote(str(executed))} ] && echo executed=no",
                    ]
                )
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("syntax=ok", result.stdout)
        self.assertIn("permit nopass", result.stdout)
        self.assertIn("executed=no", result.stdout)

    def test_module_reports_the_soccer_configuration(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            tmp_path = Path(tmpdir)
            fake_doas = tmp_path / "doas"
            config = tmp_path / "doas.conf"
            fake_doas.write_text("#!/bin/sh\nexit 1\n", encoding="utf-8")
            fake_doas.chmod(0o755)
            config.write_text(
                "permit nopass player as root cmd /usr/bin/dstat\n",
                encoding="utf-8",
            )

            body = "\n".join(
                [
                    f"PATH={shlex.quote(str(tmp_path))}:$PATH",
                    "export PATH",
                    "E=E",
                    "SED_RED='&'",
                    "SED_RED_YELLOW='&'",
                    "SED_LIGHT_CYAN='&'",
                    "SED_GREEN='&'",
                    "TIMEOUT=",
                    "sudoVB1='dstat$'",
                    "sudoVB2=''",
                    "print_2title() { echo \"TITLE: $1\"; }",
                    "print_3title() { echo \"SUBTITLE: $1\"; }",
                    "print_info() { :; }",
                    "echo_not_found() { echo \"NOT FOUND: $1\"; }",
                    "id() {",
                    "  case \"$1\" in",
                    "    -un) echo player ;;",
                    "    -u) echo 1000 ;;",
                    "    -Gn) echo 'player users' ;;",
                    "    -G) echo '1000 100' ;;",
                    "    *) command id \"$@\" ;;",
                    "  esac",
                    "}",
                    f". {shlex.quote(str(self.module_file))}",
                ]
            )
            result = self._run_shell(body)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn(
            "matching permit rule allows GTFOBins-capable command /usr/bin/dstat as root without a password",
            result.stdout,
        )
        self.assertIn("HTB Soccer privilege-escalation pattern", result.stdout)

    def test_symlinked_binary_uses_target_permissions(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            tmp_path = Path(tmpdir)
            real_doas = tmp_path / "doas-real"
            linked_doas = tmp_path / "doas"
            real_doas.write_text("#!/bin/sh\nexit 1\n", encoding="utf-8")
            real_doas.chmod(0o755)
            linked_doas.symlink_to(real_doas)

            body = "\n".join(
                [
                    f"PATH={shlex.quote(str(tmp_path))}:$PATH",
                    "export PATH",
                    "E=E",
                    "SED_RED='&'",
                    "SED_RED_YELLOW='&'",
                    "SED_LIGHT_CYAN='&'",
                    "SED_GREEN='&'",
                    "TIMEOUT=",
                    "sudoVB1=''",
                    "sudoVB2=''",
                    "print_2title() { :; }",
                    "print_3title() { :; }",
                    "print_info() { :; }",
                    "echo_not_found() { :; }",
                    "id() {",
                    "  case \"$1\" in",
                    "    -un) echo root ;;",
                    "    -u) echo 0 ;;",
                    "    -Gn) echo wheel ;;",
                    "    -G) echo 0 ;;",
                    "    *) command id \"$@\" ;;",
                    "  esac",
                    "}",
                    f". {shlex.quote(str(self.module_file))}",
                ]
            )
            result = self._run_shell(body)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn("CRITICAL: doas is writable", result.stdout)

    def test_module_replaces_invalid_doas_l_with_config_evaluation(self):
        content = self.module_file.read_text(encoding="utf-8")
        self.assertNotRegex(content, r"\bdoas\s+-l\b")
        self.assertIn("doas_config_syntax_valid", content)
        self.assertIn("doas_check_command", content)
        self.assertIn("HTB Soccer", content)


if __name__ == "__main__":
    unittest.main()
