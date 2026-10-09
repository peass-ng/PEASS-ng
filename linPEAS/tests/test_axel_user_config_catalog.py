"""Axel user configuration is a path-only lead in the existing file cache."""

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
SECTION_NAME = "Axel download configuration candidates"


class AxelUserConfigCatalogTests(unittest.TestCase):
    def test_exact_path_only_selector_and_generated_output(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        records = [item for item in catalog["search"] if item["name"] == SECTION_NAME]
        self.assertEqual(1, len(records))
        options = records[0]["value"]
        self.assertEqual(["winpeas"], options["disable"])
        self.assertTrue(options["config"]["auto_check"])
        self.assertEqual(1, len(options["files"]))
        self.assertEqual(".axelrc", options["files"][0]["name"])
        selector = options["files"][0]["value"]
        self.assertEqual(["common"], selector["search_in"])
        self.assertEqual("f", selector["type"])
        self.assertTrue(selector["just_list_file"])
        for key in ("bad_regex", "good_regex", "line_grep", "only_bad_lines"):
            self.assertNotIn(key, selector)

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
        self.assertTrue(any('.axelrc' in line and ' -name ' in line for line in finds))
        section = builder._LinpeasBuilder__generate_sections()[SECTION_NAME]
        syntax = subprocess.run(["sh", "-n"], input=section, text=True,
                                capture_output=True, timeout=2)
        self.assertEqual(0, syntax.returncode, syntax.stderr)

        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / ".axelrc"
            backup = Path(tmp) / ".axelrc.bak"
            config.write_text("DO_NOT_PRINT_CONFIG_SECRET")
            backup.write_text("DO_NOT_PRINT_CONFIG_SECRET")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ,
                     "PSTORAGE_AXEL_DOWNLOAD_CONFIGURATION_CANDIDATES":
                         "\n".join((str(config), str(backup))),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(config), result.stdout)
            self.assertNotIn(str(backup), result.stdout)
            self.assertNotIn("DO_NOT_PRINT_CONFIG_SECRET", result.stdout)


if __name__ == "__main__":
    unittest.main()
