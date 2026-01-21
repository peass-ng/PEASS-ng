import os
import stat
import subprocess
import tempfile
import unittest
from pathlib import Path


class LinpeasBuilderTests(unittest.TestCase):
    def setUp(self):
        self.repo_root = Path(__file__).resolve().parents[2]
        self.linpeas_dir = self.repo_root / "linPEAS"

    def _run_builder(self, args, output_path):
        cmd = ["python3", "-m", "builder.linpeas_builder"] + args + ["--output", str(output_path)]
        result = subprocess.run(cmd, cwd=str(self.linpeas_dir), capture_output=True, text=True)
        if result.returncode != 0:
            raise AssertionError(
                f"linpeas_builder failed:\nstdout:\n{result.stdout}\nstderr:\n{result.stderr}"
            )

    def test_small_build_creates_executable(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            output_path = Path(tmpdir) / "linpeas_small.sh"
            self._run_builder(["--small"], output_path)
            self.assertTrue(output_path.exists(), "linpeas_small.sh was not created.")
            mode = output_path.stat().st_mode
            self.assertTrue(mode & stat.S_IXUSR, "linpeas_small.sh is not executable.")

    def test_include_exclude_modules(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            output_path = Path(tmpdir) / "linpeas_include.sh"
            self._run_builder(["--include", "system_information,container", "--exclude", "container"], output_path)
            content = output_path.read_text(encoding="utf-8", errors="ignore")
            self.assertIn("Operative system", content)
            self.assertNotIn("Am I Containered?", content)


if __name__ == "__main__":
    unittest.main()
