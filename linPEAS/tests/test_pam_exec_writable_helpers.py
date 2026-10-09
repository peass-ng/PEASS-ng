"""Focused execution tests for bounded PAM helper metadata review."""

import os
from pathlib import Path
import subprocess
import tempfile
import unittest


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/7_software_information/Pamd.sh"


class PamExecWritableHelperTests(unittest.TestCase):
    def run_module(self, root):
        env = os.environ.copy()
        env.update(ROOT_FOLDER=str(root) + "/", SEARCH_IN_FOLDER="1", DEBUG="", SED_RED="")
        result = subprocess.run(
            ["sh", "-c", '. "$1"', "sh", str(MODULE)],
            env=env,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            check=True,
        )
        return result.stdout

    def test_existing_password_line_is_still_printed_once(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            policy = root / "etc/pam.d/sshd"
            policy.parent.mkdir(parents=True)
            policy.write_text("auth required pam_unix.so passwd=fixture-marker\n")

            output = self.run_module(root)

            self.assertEqual(1, output.count("fixture-marker"))
            self.assertIn(str(policy), output)

    def test_exact_executable_writable_helper_is_reported_without_contents(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            policy = root / "etc/pam.d/sshd"
            policy.parent.mkdir(parents=True)
            helper = root / "usr/local/sbin/session-helper.sh"
            helper.parent.mkdir(parents=True)
            helper.write_text("SECRET_DO_NOT_PRINT\n")
            helper.chmod(0o700)
            policy.write_text("session required pam_exec.so quiet /usr/local/sbin/session-helper.sh\n")

            output = self.run_module(root)

            self.assertIn(str(policy), output)
            self.assertIn(str(helper), output)
            self.assertEqual(1, output.count("PAM exec writable helper candidate:"))
            self.assertNotIn("SECRET_DO_NOT_PRINT", output)

    def test_comments_relative_nonexecutable_and_symlink_targets_are_ignored(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            policy = root / "etc/pam.d/sshd"
            policy.parent.mkdir(parents=True)
            helper = root / "usr/local/sbin/session-helper.sh"
            helper.parent.mkdir(parents=True)
            helper.write_text("fixture")
            helper.chmod(0o600)
            (helper.parent / "linked-helper.sh").symlink_to(helper)
            policy.write_text(
                "# session required pam_exec.so /usr/local/sbin/session-helper.sh\n"
                "session required pam_unix.so # pam_exec.so /usr/local/sbin/session-helper.sh\n"
                "session required pam_exec.so relative-helper.sh\n"
                "session required pam_exec.so /usr/local/sbin/session-helper.sh\n"
                "session required pam_exec.so /usr/local/sbin/linked-helper.sh\n"
            )

            self.assertNotIn("PAM exec writable helper candidate:", self.run_module(root))

    def test_oversized_or_symlinked_pam_configuration_is_ignored(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            policy_dir = root / "etc/pam.d"
            policy_dir.mkdir(parents=True)
            helper = root / "usr/local/sbin/session-helper.sh"
            helper.parent.mkdir(parents=True)
            helper.write_text("fixture")
            helper.chmod(0o700)
            line = "session required pam_exec.so /usr/local/sbin/session-helper.sh\n"
            (policy_dir / "oversized").write_text(line + "#" * 8192)
            outside = root / "outside-policy"
            outside.write_text(line)
            (policy_dir / "linked").symlink_to(outside)

            self.assertNotIn("PAM exec writable helper candidate:", self.run_module(root))

    def test_config_file_limit_skips_later_entries(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            policy_dir = root / "etc/pam.d"
            policy_dir.mkdir(parents=True)
            for index in range(128):
                (policy_dir / f"config-{index:03d}").write_text("auth required pam_unix.so\n")
            helper = root / "usr/local/sbin/session-helper.sh"
            helper.parent.mkdir(parents=True)
            helper.write_text("fixture")
            helper.chmod(0o700)
            (policy_dir / "zz-later").write_text(
                "session required pam_exec.so /usr/local/sbin/session-helper.sh\n"
            )

            self.assertNotIn("PAM exec writable helper candidate:", self.run_module(root))


if __name__ == "__main__":
    unittest.main()
