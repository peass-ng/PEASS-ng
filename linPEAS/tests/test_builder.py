import os
import re
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

    def test_threads_flag_present_in_getopts(self):
        """Regression: -z must appear in the getopts string so it is actually parsed."""
        with tempfile.TemporaryDirectory() as tmpdir:
            output_path = Path(tmpdir) / "linpeas.sh"
            self._run_builder(["--all-no-fat"], output_path)
            content = output_path.read_text(encoding="utf-8", errors="ignore")
            # Match the actual option-parsing line: 'while getopts' followed by
            # either a single or double quoted option string, to avoid matching
            # comments or help text that happen to contain 'getopts'.
            getopts_line = next(
                (l for l in content.splitlines()
                 if re.match(r'\s*while\s+getopts\s+[\'"]', l)),
                None
            )
            self.assertIsNotNone(getopts_line,
                                 "'while getopts' line not found in built script.")
            self.assertIn("z:", getopts_line,
                          "-z: option is missing from the getopts string in the built script.")

    def test_threads_flag_present_in_help_text(self):
        """Regression: -z must be documented in the help text of the built script."""
        with tempfile.TemporaryDirectory() as tmpdir:
            output_path = Path(tmpdir) / "linpeas.sh"
            self._run_builder(["--all-no-fat"], output_path)
            content = output_path.read_text(encoding="utf-8", errors="ignore")
            self.assertIn("-z <N>", content,
                          "-z <N> help entry is missing from the built script.")

    def test_mitre_flag_present_in_getopts(self):
        """The -T flag must appear in the getopts string so it is actually parsed."""
        with tempfile.TemporaryDirectory() as tmpdir:
            output_path = Path(tmpdir) / "linpeas.sh"
            self._run_builder(["--all-no-fat"], output_path)
            content = output_path.read_text(encoding="utf-8", errors="ignore")
            getopts_line = next(
                (l for l in content.splitlines()
                 if re.match(r'\s*while\s+getopts\s+[\'"]', l)),
                None
            )
            self.assertIsNotNone(getopts_line,
                                 "'while getopts' line not found in built script.")
            self.assertIn("T:", getopts_line,
                          "-T: option is missing from the getopts string in the built script.")

    def test_mitre_flag_present_in_help_text(self):
        """The -T flag must be documented in the help text of the built script."""
        with tempfile.TemporaryDirectory() as tmpdir:
            output_path = Path(tmpdir) / "linpeas.sh"
            self._run_builder(["--all-no-fat"], output_path)
            content = output_path.read_text(encoding="utf-8", errors="ignore")
            self.assertIn("-T", content,
                          "-T help entry is missing from the built script.")

    def test_mitre_filter_function_present(self):
        """check_mitre_filter() must be emitted into the built script."""
        with tempfile.TemporaryDirectory() as tmpdir:
            output_path = Path(tmpdir) / "linpeas.sh"
            self._run_builder(["--all-no-fat"], output_path)
            content = output_path.read_text(encoding="utf-8", errors="ignore")
            self.assertIn("check_mitre_filter", content,
                          "check_mitre_filter function is missing from the built script.")

    def _run_base_mitre_filter(self, mitre_filter, check_ids):
        base_file = self.linpeas_dir / "builder" / "linpeas_parts" / "linpeas_base" / "0_variables_base.sh"
        result = subprocess.run(
            [
                "bash",
                "-lc",
                (
                    f'source "{base_file}" >/dev/null 2>&1 || true; '
                    f'MITRE_FILTER="{mitre_filter}"; '
                    f'check_mitre_filter "{check_ids}"; '
                    'echo $?'
                ),
            ],
            capture_output=True,
            text=True,
            cwd=str(self.repo_root),
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout.strip().splitlines()[-1]

    def test_mitre_parent_filter_matches_subtechnique(self):
        """Regression: filtering by a base technique must include child sub-techniques."""
        exit_code = self._run_base_mitre_filter("T1552", "T1552.001")
        self.assertEqual("0", exit_code,
                         "Parent MITRE filter T1552 should match sub-technique T1552.001.")

    def test_mitre_subtechnique_filter_does_not_match_parent(self):
        """Regression: filtering by a sub-technique must not include a parent-only tag."""
        exit_code = self._run_base_mitre_filter("T1552.001", "T1552")
        self.assertEqual("1", exit_code,
                         "Sub-technique MITRE filter T1552.001 should not match parent tag T1552.")


if __name__ == "__main__":
    unittest.main()
