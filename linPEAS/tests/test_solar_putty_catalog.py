"""Solar-PuTTY file leads stay scoped and path-only in the shared catalog."""

import re
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]


class SolarPuttyCatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record = next(item for item in catalog["search"]
                      if item["name"] == "Solar-PuTTY session stores")
        cls.record = record
        cls.files = {item["name"]: item["value"]
                     for item in record["value"]["files"]}

    def test_artifacts_are_exact_and_path_only(self):
        self.assertTrue(self.record["value"]["config"]["auto_check"])
        self.assertEqual({"sessions-backup.dat", "data.dat"}, set(self.files))
        for value in self.files.values():
            self.assertTrue(value["just_list_file"])
            self.assertEqual("f", value["type"])
            self.assertEqual(["common"], value["search_in"])

    def test_backup_requires_product_directory(self):
        pattern = self.files["sessions-backup.dat"]["check_extra_path"]
        self.assertRegex("/opt/backups/Solar-PuTTY/sessions-backup.dat", pattern)
        self.assertIsNone(re.search(pattern, "/opt/backups/sessions-backup.dat"))
        self.assertIsNone(re.search(pattern, "/opt/backups/Solar-PuTTY/sessions-backup.dat.bak"))

    def test_native_store_requires_app_directory(self):
        pattern = self.files["data.dat"]["check_extra_path"]
        self.assertRegex("/home/a/AppData/Roaming/SolarWinds/FreeTools/Solar-PuTTY/data.dat", pattern)
        self.assertIsNone(re.search(pattern, "/home/a/Documents/data.dat"))


if __name__ == "__main__":
    unittest.main()
