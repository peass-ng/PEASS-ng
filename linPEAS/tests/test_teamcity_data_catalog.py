"""TeamCity data candidates should use the shared cache and disclose paths only."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "TeamCity data candidates"


class TeamCityDataCatalogTests(unittest.TestCase):
    def test_paths_are_scoped_and_content_is_not_read(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        records = [item["value"] for item in catalog["search"] if item["name"] == TITLE]
        self.assertEqual(1, len(records))
        group = records[0]
        self.assertEqual(["winpeas"], group["disable"])
        self.assertTrue(group["config"]["auto_check"])
        cases = {
            "buildserver.script": ("f", "/data/.BuildServer/system/buildserver.script",
                                   "/tmp/buildserver.script"),
            "buildserver.log": ("f", "/data/.BuildServer/system/buildserver.log",
                                "/tmp/buildserver.log"),
            "TeamCity_Backup_*.zip": ("f", "/data/.BuildServer/backup/TeamCity_Backup_20241009_132300.zip",
                                       "/tmp/TeamCity_Backup_latest.zip"),
        }
        self.assertEqual(cases.keys(), {item["name"] for item in group["files"]})
        for item in group["files"]:
            expected_type, accepted, rejected = cases[item["name"]]
            value = item["value"]
            self.assertEqual(expected_type, value["type"])
            self.assertEqual(["common"], value["search_in"])
            self.assertTrue(value["just_list_file"])
            self.assertNotIn("line_grep", value)
            self.assertNotIn("exec", value)
            pattern = value["check_extra_path"]
            self.assertIsNotNone(re.search(pattern, accepted))
            self.assertIsNone(re.search(pattern, rejected))
            result = subprocess.run(["grep", "-E", pattern], input=accepted + "\n" + rejected + "\n",
                                    text=True, capture_output=True, timeout=2)
            self.assertEqual(accepted + "\n", result.stdout)

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
        self.assertNotIn("grep -n", section)
        self.assertNotIn("ls -lRA", section)

        with tempfile.TemporaryDirectory() as tmp:
            db = Path(tmp) / "buildserver.script"
            db.write_text("DO_NOT_PRINT_DATABASE_ROW")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_TEAMCITY_DATA_CANDIDATES": str(db),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(db), result.stdout)
            self.assertNotIn("DO_NOT_PRINT_DATABASE_ROW", result.stdout)


if __name__ == "__main__":
    unittest.main()
