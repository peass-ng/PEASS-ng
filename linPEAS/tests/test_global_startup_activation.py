"""Fixed-depth, content-free global startup activation review."""

import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/8_interesting_perms_files/7_Files_etc_profile_d.sh"


class GlobalStartupActivationTests(unittest.TestCase):
    def run_check(self, content, shell="sh"):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            activation = root / "venv" / "bin" / "activate"
            activation.parent.mkdir(parents=True)
            activation.write_text("# no execution expected\n")
            startup = root / "startup"
            startup.write_text(content.replace("{activate}", str(activation)))
            result = subprocess.run(
                [shell, "-c", '. "$1"; check_global_venv_activation "$2"',
                 shell, str(MODULE), str(startup)],
                env={"SEARCH_IN_FOLDER": "1", "PATH": "/usr/bin:/bin"},
                capture_output=True, text=True, timeout=3,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            return result.stdout, str(activation)

    def test_literal_source_forms_and_no_content_disclosure(self):
        content = ('source {activate} # hidden-password-value\n'
                   '. "{activate}"\n'
                   "source '{activate}'\n")
        for shell in ("sh", "bash"):
            if not shutil.which(shell):
                continue
            with self.subTest(shell=shell):
                output, path = self.run_check(content, shell)
                self.assertEqual(output.count(" sources " + path), 3)
                self.assertNotIn("hidden-password-value", output)
                self.assertNotIn("no execution expected", output)

    def test_ignores_comments_relative_paths_and_late_content(self):
        content = ('# source {activate}\n'
                   'source ./venv/bin/activate\n'
                   'echo source {activate}\n'
                   + "#" * 65536 + "\nsource {activate}\n")
        output, _ = self.run_check(content)
        self.assertEqual(output, "")

    def test_caps_matching_paths_per_startup_file(self):
        output, path = self.run_check("source {activate}\n" * 12)
        self.assertEqual(output.count(" sources " + path), 8)


if __name__ == "__main__":
    unittest.main()
