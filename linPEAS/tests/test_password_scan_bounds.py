"""Password scans do not block on special files and retain file hits."""

import os
import subprocess
import tempfile
import unittest
from pathlib import Path


PARTS = Path(__file__).resolve().parents[1] / "builder" / "linpeas_parts"


class PasswordScanBoundsTests(unittest.TestCase):
    def test_gtimeout_is_accepted_when_timeout_is_unavailable(self):
        with tempfile.TemporaryDirectory() as temporary:
            stub = Path(temporary) / "gtimeout"
            stub.write_text("#!/bin/sh\nexit 0\n")
            stub.chmod(0o755)
            result = subprocess.run(
                ["/bin/sh", "-c", '. "$1"; printf "%s" "$TIMEOUT"', "sh",
                 str(PARTS / "variables" / "TIMEOUT.sh")],
                env={"PATH": temporary}, capture_output=True, text=True, timeout=5,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stdout, str(stub))

    @unittest.skipUnless(hasattr(os, "mkfifo"), "requires POSIX FIFOs")
    def test_password_scan_skips_fifo_and_keeps_regular_and_symlinked_files(self):
        with tempfile.TemporaryDirectory(prefix="peas-password-") as temporary, \
                tempfile.TemporaryDirectory(prefix="peas-password-linked-") as linked:
            root = Path(temporary)
            config = root / "settings.conf"
            config.write_text("password=fixture_value\n")
            (root / "settings-link.conf").symlink_to(config)
            os.mkfifo(root / "blocked.conf")
            (root / "blocked-link.conf").symlink_to(root / "blocked.conf")
            (Path(linked) / "linked.conf").write_text("password=linked_fixture\n")
            (root / "linked-dir").symlink_to(linked, target_is_directory=True)
            (root / "loop").symlink_to(root, target_is_directory=True)
            environment = os.environ.copy()
            environment.update({
                "SEARCH_IN_FOLDER": temporary,
                "TIMEOUT": subprocess.check_output(
                    ["/bin/sh", "-c", "command -v timeout || command -v gtimeout"],
                    text=True,
                ).strip(),
                "FAST": "", "SUPERFAST": "", "E": "E", "SED_RED": "&",
                "pwd_in_variables1": "password",
            })
            for index in range(2, 12):
                environment[f"pwd_in_variables{index}"] = "unlikely_fixture_pattern"
            result = subprocess.run(
                ["/bin/sh", "-c", 'print_2title() { :; }; . "$1"', "sh",
                 str(PARTS / "9_interesting_files" / "28_Files_with_passwords.sh")],
                env=environment, capture_output=True, text=True, timeout=15,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("settings.conf", result.stdout)
            self.assertIn("settings-link.conf", result.stdout)
            self.assertIn("fixture_value", result.stdout)
            self.assertIn("linked-dir", result.stdout)
            self.assertIn("linked_fixture", result.stdout)


if __name__ == "__main__":
    unittest.main()
