"""Scoped CMS credential-store candidates must remain path-only."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


CATALOG = Path(__file__).resolve().parents[2] / "build_lists/sensitive_files.yaml"


class CmsFileStoreCatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        groups = yaml.safe_load(CATALOG.read_text())["search"]
        cls.groups = {group["name"]: group["value"] for group in groups}

    def assert_scoped_path_only(self, group_name, filename, accepted, rejected):
        group = self.groups[group_name]
        self.assertTrue(group["config"]["auto_check"])
        self.assertEqual(1, len(group["files"]))
        self.assertEqual(filename, group["files"][0]["name"])
        entry = group["files"][0]["value"]
        self.assertTrue(entry["just_list_file"])
        self.assertEqual("f", entry["type"])
        self.assertEqual(["common"], entry["search_in"])
        self.assertNotIn("line_grep", entry)
        pattern = entry["check_extra_path"]
        for path in accepted:
            self.assertIsNotNone(re.search(pattern, path), path)
        for path in rejected:
            self.assertIsNone(re.search(pattern, path), path)
        result = subprocess.run(
            ["grep", "-E", pattern],
            input="\n".join(accepted + rejected) + "\n",
            capture_output=True, text=True, timeout=2, check=True,
        )
        self.assertEqual(accepted, result.stdout.splitlines())

    def test_json_backed_store_requires_data_path(self):
        self.assert_scoped_path_only(
            "WonderCMS", "database.js",
            ["/var/www/site/data/database.js", "/srv/www/site with space/data/database.js"],
            ["/var/www/site/database.js", "/tmp/database.js", "/var/www/site/data/database.jsx"],
        )

    def test_php_password_store_requires_settings_path(self):
        self.assert_scoped_path_only(
            "Pluck CMS", "pass.php",
            ["/var/www/site/pluck/data/settings/pass.php", "/var/www/site/data/settings/pass.php"],
            ["/var/www/site/pass.php", "/var/www/site/data/pass.php", "/var/www/site/data/settings/pass.php.bak"],
        )

    def test_existing_php_catalog_still_contains_generic_files(self):
        files = {entry["name"] for entry in self.groups["PHP_files"]["files"]}
        self.assertTrue({"database.php", "settings.php", "*config*.php"} <= files)

    def test_generated_sections_list_scoped_paths_without_file_content(self):
        sys.path.insert(0, str(CATALOG.parent.parent / "linPEAS"))
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
        storage = builder._LinpeasBuilder__generate_storages()
        sections = builder._LinpeasBuilder__generate_sections()

        with tempfile.TemporaryDirectory(prefix="peas-cms-", dir="/tmp") as temp:
            root = Path(temp)
            cases = (
                ("WonderCMS", "PSTORAGE_WONDERCMS", "data/database.js", "other/database.js"),
                ("Pluck CMS", "PSTORAGE_PLUCK_CMS", "pluck/data/settings/pass.php", "other/pass.php"),
            )
            for title, variable, included, excluded in cases:
                positive = root / included
                negative = root / excluded
                positive.parent.mkdir(parents=True, exist_ok=True)
                negative.parent.mkdir(parents=True, exist_ok=True)
                positive.write_text("HIDDEN_CMS_SECRET_VALUE")
                negative.write_text("HIDDEN_OTHER_VALUE")
                assignment = next(item for item in storage if item.startswith(variable + "="))
                script = "print_2title() { :; }; " + assignment + "\n" + sections[title]
                self.assertEqual(0, subprocess.run(
                    ["sh", "-n"], input=script, text=True,
                    capture_output=True, timeout=2,
                ).returncode)
                result = subprocess.run(
                    ["sh", "-c", script],
                    env={**os.environ, "FIND_VAR": f"{positive}\n{negative}",
                         "ROOT_FOLDER": "/", "E": "E", "SED_RED": "&"},
                    capture_output=True, text=True, timeout=5,
                )
                self.assertEqual(0, result.returncode, result.stderr)
                self.assertIn(str(positive), result.stdout)
                self.assertNotIn(str(negative), result.stdout)
                self.assertNotIn("HIDDEN_CMS_SECRET_VALUE", result.stdout)
                self.assertNotIn("HIDDEN_OTHER_VALUE", result.stdout)


if __name__ == "__main__":
    unittest.main()
