"""The Fail2Ban action-directory cue reads only exact directory metadata."""

import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/1_system_information/16_Protections.sh"


class Fail2banActionDirectoryCandidateTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        source = MODULE.read_text()
        start = source.index("fail2ban_action_dir_review() {")
        end = source.index("\n}", start) + 2
        cls.helper = source[start:end]
        assert "fail2ban_action_dir_review /etc/fail2ban/action.d" in source
        assert "cat " not in cls.helper and "find " not in cls.helper

    def check_path(self, path, root=""):
        script = self.helper + "\nIAMROOT=" + shlex.quote(root) + "\nfail2ban_action_dir_review \"$1\"\n"
        result = subprocess.run(
            ["sh", "-c", script, "--", str(path)],
            capture_output=True, text=True, timeout=3,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_existing_writable_searchable_directory_is_candidate(self):
        with tempfile.TemporaryDirectory() as base:
            directory = Path(base) / "action.d"
            directory.mkdir(mode=0o700)
            output = self.check_path(directory)
            self.assertIn("review candidate", output)
            self.assertIn(str(directory), output)
            self.assertIn("service identity", output)
            self.assertIn("event trigger unverified", output)

    def test_absent_and_final_component_symlink_are_silent(self):
        with tempfile.TemporaryDirectory() as base:
            directory = Path(base) / "action.d"
            directory.mkdir()
            link = Path(base) / "action-link"
            link.symlink_to(directory, target_is_directory=True)
            self.assertEqual("", self.check_path(Path(base) / "absent"))
            self.assertEqual("", self.check_path(link))

    def test_unwritable_directory_is_silent(self):
        if os.geteuid() == 0:
            self.skipTest("root bypasses ordinary mode-bit write restrictions")
        with tempfile.TemporaryDirectory() as base:
            directory = Path(base) / "action.d"
            directory.mkdir(mode=0o500)
            self.assertEqual("", self.check_path(directory))

    def test_root_run_is_silent(self):
        with tempfile.TemporaryDirectory() as base:
            directory = Path(base) / "action.d"
            directory.mkdir(mode=0o700)
            self.assertEqual("", self.check_path(directory, root="1"))


if __name__ == "__main__":
    unittest.main()
