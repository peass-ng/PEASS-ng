import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class AIStoragePermissionsTests(unittest.TestCase):
    module = (Path(__file__).resolve().parents[1] / "builder" / "linpeas_parts"
              / "9_interesting_files" / "30_AI_storage_permissions.sh")

    def run_audit(self, home, search_in_folder=""):
        script = ("print_2title() { printf '%s\\n' \"$1\"; }\n"
                  + ". " + shlex.quote(str(self.module)))
        env = dict(os.environ, HOME=str(home), SEARCH_IN_FOLDER=search_in_folder)
        return subprocess.run(["sh", "-c", script], env=env, capture_output=True,
                              text=True, timeout=5, check=True).stdout

    def test_private_and_shared_directories_without_content_disclosure(self):
        with tempfile.TemporaryDirectory() as tmp:
            home = Path(tmp).resolve() / "home with spaces"
            private = home / ".codex"
            shared = home / ".grok"
            private.mkdir(parents=True)
            shared.mkdir()
            private.chmod(0o700)
            shared.chmod(0o770)
            (shared / "private-data.txt").write_text("SECRET_FIXTURE_DO_NOT_PRINT")
            # Opening this pipe would block: the audit must never enumerate/read bodies.
            os.mkfifo(shared / "stored-data-pipe")
            output = self.run_audit(home)
            self.assertIn(str(private), output)
            self.assertIn(str(shared), output)
            self.assertEqual(output.count("Review group/other access"), 1)
            self.assertNotIn("SECRET_FIXTURE_DO_NOT_PRINT", output)
            self.assertNotIn("private-data.txt", output)
            self.assertNotIn("stored-data-pipe", output)

    def test_symlinked_root_and_ancestor_are_skipped(self):
        with tempfile.TemporaryDirectory() as tmp:
            home = Path(tmp).resolve() / "home"
            outside = Path(tmp).resolve() / "outside"
            home.mkdir()
            (outside / "antigravity-cli").mkdir(parents=True)
            (home / ".codex").symlink_to(outside, target_is_directory=True)
            (home / ".gemini").symlink_to(outside, target_is_directory=True)
            output = self.run_audit(home)
            self.assertEqual(output.count("skipped symbolic link"), 2)
            self.assertNotIn(str(outside), output)

    def test_folder_scoped_scan_does_not_inspect_home(self):
        with tempfile.TemporaryDirectory() as tmp:
            home = Path(tmp).resolve()
            (home / ".claude").mkdir()
            self.assertEqual(self.run_audit(home, str(home)), "")

    def test_missing_roots_are_reported(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertIn("No supported storage directories found", self.run_audit(Path(tmp).resolve()))


if __name__ == "__main__":
    unittest.main()
