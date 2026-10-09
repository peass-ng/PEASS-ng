"""Product-specific sensitive-file candidates stay scoped and path-only."""

import re
import subprocess
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]


class ProductFileCatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        cls.groups = {entry["name"]: entry["value"] for entry in catalog["search"]}

    def test_prestashop_paths_are_exact_and_metadata_only(self):
        group = self.groups["PrestaShop database settings candidates"]
        self.assertTrue(group["config"]["auto_check"])
        files = {entry["name"]: entry["value"] for entry in group["files"]}
        self.assertEqual({"parameters.php", "settings.inc.php"}, set(files))
        cases = {
            "parameters.php": (
                "/var/www/shop/app/config/parameters.php",
                "/var/www/shop/config/parameters.php",
            ),
            "settings.inc.php": (
                "/var/www/shop/config/settings.inc.php",
                "/var/www/shop/config-alt/settings.inc.php",
            ),
        }
        for name, (positive, negative) in cases.items():
            with self.subTest(name=name):
                entry = files[name]
                self.assertTrue(entry["just_list_file"])
                self.assertEqual("f", entry["type"])
                self.assertEqual(["common"], entry["search_in"])
                self.assertRegex(positive, entry["check_extra_path"])
                self.assertIsNone(re.search(entry["check_extra_path"], negative))
                self.assertIsNone(re.search(entry["check_extra_path"], positive + ".bak"))
                self.assertNotIn("line_grep", entry)
                self.assertNotIn("bad_regex", entry)

    def test_backup_requires_datastore_and_fixed_name(self):
        group = self.groups["ChangeDetection backup candidates"]
        self.assertTrue(group["config"]["auto_check"])
        self.assertEqual(1, len(group["files"]))
        entry = group["files"][0]
        self.assertEqual("changedetection-backup-*.zip", entry["name"])
        value = entry["value"]
        self.assertTrue(value["just_list_file"])
        self.assertEqual("f", value["type"])
        self.assertEqual(["common", "${ROOT_FOLDER}datastore/Backups"],
                         value["search_in"])
        pattern = value["check_extra_path"]
        for path in (
            "/datastore/Backups/changedetection-backup-20240830202524.zip",
            "/opt/service/datastore/Backups/changedetection-backup-42.zip",
        ):
            self.assertRegex(path, pattern)
        for path in (
            "/home/me/Backups/changedetection-backup-42.zip",
            "/datastore/Other/changedetection-backup-42.zip",
            "/datastore/Backups/changedetection-backup-latest.zip",
            "/datastore/Backups/changedetection-backup-42.zip.bak",
        ):
            self.assertIsNone(re.search(pattern, path))
        result = subprocess.run(
            ["grep", "-E", pattern],
            input=("/datastore/Backups/changedetection-backup-42.zip\n"
                   "/home/me/Backups/changedetection-backup-42.zip\n"),
            text=True, capture_output=True, check=True, timeout=2,
        )
        self.assertEqual(result.stdout,
                         "/datastore/Backups/changedetection-backup-42.zip\n")

    def test_existing_generic_php_selectors_remain(self):
        files = {entry["name"] for entry in self.groups["PHP_files"]["files"]}
        self.assertTrue({"*config*.php", "database.php", "settings.php"} <= files)


if __name__ == "__main__":
    unittest.main()
