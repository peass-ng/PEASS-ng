"""Monit control files should be listed from the existing cache without secrets."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Monit configuration candidates"


class MonitCatalogTests(unittest.TestCase):
    def test_config_paths_and_generated_output(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        records = [item["value"] for item in catalog["search"] if item["name"] == TITLE]
        self.assertEqual(1, len(records))
        group = records[0]
        self.assertEqual(["winpeas"], group["disable"])
        self.assertTrue(group["config"]["auto_check"])
        entries = {item["name"]: item["value"] for item in group["files"]}
        self.assertEqual({".monitrc", "monitrc"}, entries.keys())
        for value in entries.values():
            self.assertTrue(value["just_list_file"])
            self.assertEqual("f", value["type"])
            self.assertEqual(["common"], value["search_in"])
            self.assertNotIn("line_grep", value)
        pattern = entries["monitrc"]["check_extra_path"]
        for path in ("/etc/monitrc", "/usr/local/etc/monitrc", "/etc/monit/monitrc"):
            self.assertIsNotNone(re.search(pattern, path))
        for path in ("/tmp/monitrc", "/tmp/monitrc.bak", "/etc/monitrc.old"):
            self.assertIsNone(re.search(pattern, path))

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
        builder._LinpeasBuilder__generate_finds()
        builder._LinpeasBuilder__generate_storages()
        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        self.assertEqual(0, subprocess.run(["sh", "-n"], input=section, text=True,
                                           capture_output=True, timeout=2).returncode)
        self.assertNotIn("cat ", section)
        self.assertNotIn("ls -lRA", section)
        with tempfile.TemporaryDirectory() as tmp:
            control = Path(tmp) / ".monitrc"
            control.write_text("allow operator:DO_NOT_PRINT_PASSWORD")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_MONIT_CONFIGURATION_CANDIDATES": str(control),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(control), result.stdout)
            self.assertNotIn("DO_NOT_PRINT_PASSWORD", result.stdout)


if __name__ == "__main__":
    unittest.main()
