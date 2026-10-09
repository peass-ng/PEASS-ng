"""Crash reports are surfaced by path and metadata without reading core data."""

import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Crash report candidates"


class CrashReportCatalogTests(unittest.TestCase):
    def test_scoped_cached_path_only_selector(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [item for item in catalog["search"] if item["name"] == TITLE]
        self.assertIn("winpeas", record["value"]["disable"])
        entry, = record["value"]["files"]
        self.assertEqual("*.crash", entry["name"])
        self.assertEqual("f", entry["value"]["type"])
        self.assertEqual(["common"], entry["value"]["search_in"])
        self.assertTrue(entry["value"]["just_list_file"])

        sys.path.insert(0, str(ROOT / "linPEAS"))
        from builder.src.linpeasBuilder import LinpeasBuilder
        from builder.src.peasLoaded import PEASLoaded

        builder = LinpeasBuilder.__new__(LinpeasBuilder)
        builder.ploaded = PEASLoaded()
        builder.hidden_files = set()
        builder.bash_find_f_vars = set()
        builder.bash_find_d_vars = set()
        builder.bash_storages = set()
        builder._LinpeasBuilder__get_files_to_search()
        finds, _ = builder._LinpeasBuilder__generate_finds()
        self.assertTrue(any('*.crash' in line and ' -name ' in line for line in finds))

        storages = builder._LinpeasBuilder__generate_storages()
        storage = next(line for line in storages
                       if line.startswith("PSTORAGE_CRASH_REPORT_CANDIDATES="))
        self.assertIn("/var/crash/[^/]+\\.crash$", storage)
        selected = subprocess.run(
            ["sh", "-c", storage + '\nprintf "%s\\n" "$PSTORAGE_CRASH_REPORT_CANDIDATES"'],
            env={**os.environ, "ROOT_FOLDER": "/", "FIND_VAR": "\n".join((
                "/var/crash/_usr_bin_tool.1000.crash",
                "/home/alice/tool.crash",
                "/var/crash/nested/tool.crash",
                "/var/crash/tool.crash.old",
            ))},
            text=True, capture_output=True, timeout=2,
        )
        self.assertEqual(0, selected.returncode, selected.stderr)
        self.assertIn("/var/crash/_usr_bin_tool.1000.crash", selected.stdout)
        self.assertNotIn("/home/alice/tool.crash", selected.stdout)
        self.assertNotIn("/var/crash/nested/tool.crash", selected.stdout)
        self.assertNotIn("/var/crash/tool.crash.old", selected.stdout)

        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        self.assertEqual(0, subprocess.run(["sh", "-n"], input=section, text=True,
                                           capture_output=True, timeout=2).returncode)
        self.assertNotIn('cat "$f"', section)
        self.assertNotIn("ls -lRA", section)

        with tempfile.TemporaryDirectory() as temporary:
            candidate = Path(temporary) / "candidate.crash"
            candidate.write_text("PRIVATE_MEMORY_MUST_NOT_PRINT")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ,
                     "PSTORAGE_CRASH_REPORT_CANDIDATES": str(candidate),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(candidate), result.stdout)
            self.assertNotIn("PRIVATE_MEMORY_MUST_NOT_PRINT", result.stdout)

        # A new path filter must not narrow the existing mixed-log section.
        logs = builder._LinpeasBuilder__generate_sections()["Interesting logs"]
        self.assertIn("auth.log", logs)
        self.assertIn("access.log", logs)
        self.assertIn("error.log", logs)


if __name__ == "__main__":
    unittest.main()
