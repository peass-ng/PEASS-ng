"""HTTP archive discovery should reuse the file cache without exposing captures."""

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "HTTP archive candidates"


class HTTPArchiveCatalogTests(unittest.TestCase):
    def test_generated_selector_lists_only_archive_paths(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        records = [item for item in catalog["search"] if item["name"] == TITLE]
        self.assertEqual(len(records), 1)
        record = records[0]
        self.assertTrue(record["value"]["config"]["auto_check"])
        self.assertEqual(len(record["value"]["files"]), 1)
        entry = record["value"]["files"][0]
        self.assertEqual(entry["name"], "*.har")
        self.assertEqual(entry["value"]["search_in"], ["common"])
        self.assertEqual(entry["value"]["type"], "f")
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
        self.assertTrue(any('*.har' in item and ' -name ' in item for item in finds))
        storage = next(item for item in builder._LinpeasBuilder__generate_storages()
                       if item.startswith("PSTORAGE_HTTP_ARCHIVE_CANDIDATES="))
        self.assertIn('grep -E ".*\\.har$"', storage)
        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        self.assertEqual(subprocess.run(["sh", "-n"], input=section, text=True,
                                        capture_output=True, timeout=2).returncode, 0)

        with tempfile.TemporaryDirectory() as tmp:
            archive = Path(tmp) / "support.har"
            unrelated = Path(tmp) / "support.har.json"
            archive.write_text("SECRET_CAPTURE_VALUE")
            unrelated.write_text("SECRET_OTHER_VALUE")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_HTTP_ARCHIVE_CANDIDATES":
                     f"{archive}\n{unrelated}", "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn(str(archive), result.stdout)
            self.assertNotIn(str(unrelated), result.stdout)
            self.assertNotIn("SECRET_CAPTURE_VALUE", result.stdout)
            self.assertNotIn("SECRET_OTHER_VALUE", result.stdout)


if __name__ == "__main__":
    unittest.main()
