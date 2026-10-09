import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class MotionConfigurationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.module = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "7_software_information"
            / "Motion_Configuration.sh"
        )

    def _run_check(self, config, simulate_unreadable=False):
        script = "\n".join(
            [
                "SEARCH_IN_FOLDER=1",
                'print_2title() { printf "TITLE: %s\\n" "$1"; }',
                f". {shlex.quote(str(self.module))}",
                "lp_motion_readable() { return 1; }" if simulate_unreadable else ":",
                f"lp_motion_check_config {shlex.quote(str(config))}",
            ]
        )
        return subprocess.run(
            ["sh", "-c", script],
            cwd=self.repo_root,
            capture_output=True,
            text=True,
            check=False,
        )

    def test_absent_config_adds_no_output(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            result = self._run_check(Path(temp_dir) / "motion.conf")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout, "")

    def test_unreadable_config_reports_only_access_state(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            config = Path(temp_dir) / "motion.conf"
            config.write_text("# @admin_password secret-never-print\n", encoding="utf-8")
            result = self._run_check(config, simulate_unreadable=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("not readable", result.stdout)
        self.assertNotIn("secret-never-print", result.stdout)
        self.assertNotIn("admin hash: present", result.stdout)

    def test_empty_hash_and_disabled_control_do_not_raise_review(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            config = Path(temp_dir) / "motion.conf"
            config.write_text(
                "# @admin_password\nwebcontrol_port 0\nwebcontrol_parms 0\n",
                encoding="utf-8",
            )
            result = self._run_check(config)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("admin hash: empty", result.stdout)
        self.assertIn("Motion webcontrol port: 0", result.stdout)
        self.assertNotIn("Review:", result.stdout)

    def test_positive_config_redacts_credentials_and_hook_values(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            config_dir = Path(temp_dir)
            config = config_dir / "motion.conf"
            config.write_text(
                "# @admin_password secret-never-print\n"
                "webcontrol_port 7999\n"
                "webcontrol_parms 2\n"
                "webcontrol_localhost on\n"
                "webcontrol_authentication user:secret-never-print\n",
                encoding="utf-8",
            )
            (config_dir / "camera-1.conf").write_text(
                'on_picture_save /bin/sh -c "secret-never-print"\n'
                "picture_filename secret-never-print\n",
                encoding="utf-8",
            )
            result = self._run_check(config)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("admin hash: present (redacted)", result.stdout)
        self.assertIn("Review: Motion webcontrol may expose advanced settings", result.stdout)
        self.assertIn("Camera picture-save event hook: configured", result.stdout)
        self.assertIn("Camera picture filename: configured", result.stdout)
        self.assertNotIn("secret-never-print", result.stdout)

    def test_authenticated_control_is_not_flagged_as_unauthenticated(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            config = Path(temp_dir) / "motion.conf"
            config.write_text(
                "webcontrol_port 9000\nwebcontrol_parms 2\nwebcontrol_auth_method 2\n",
                encoding="utf-8",
            )
            result = self._run_check(config)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Motion webcontrol authentication: enabled", result.stdout)
        self.assertNotIn("Review:", result.stdout)

    def test_limited_control_is_not_flagged_as_advanced(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            config = Path(temp_dir) / "motion.conf"
            config.write_text(
                "webcontrol_port 9000\nwebcontrol_parms 1\n",
                encoding="utf-8",
            )
            result = self._run_check(config)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Motion webcontrol parameter level: 1", result.stdout)
        self.assertNotIn("Review:", result.stdout)

    def test_string_auth_methods_used_by_newer_motion(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            config = Path(temp_dir) / "motion.conf"
            config.write_text(
                "webcontrol_port 9000\nwebcontrol_parms 3\nwebcontrol_auth_method basic\n",
                encoding="utf-8",
            )
            authenticated = self._run_check(config)
            config.write_text(
                "webcontrol_port 9000\nwebcontrol_parms 3\nwebcontrol_auth_method none\n",
                encoding="utf-8",
            )
            unauthenticated = self._run_check(config)
        self.assertIn("Motion webcontrol authentication: enabled", authenticated.stdout)
        self.assertNotIn("Review:", authenticated.stdout)
        self.assertIn("Motion webcontrol authentication: disabled", unauthenticated.stdout)
        self.assertIn("Review:", unauthenticated.stdout)


if __name__ == "__main__":
    unittest.main()
