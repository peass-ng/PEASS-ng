"""Dolibarr's credential config is a scoped, path-only catalog finding."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
CATALOG = ROOT / "build_lists/sensitive_files.yaml"
SIDB = ROOT / "linPEAS/builder/linpeas_parts/variables/sidB.sh"


class DolibarrCatalogTests(unittest.TestCase):
    def test_config_path_is_scoped_and_content_is_not_printed(self):
        groups = {
            group["name"]: group["value"]
            for group in yaml.safe_load(CATALOG.read_text())["search"]
        }
        group = groups["Dolibarr database settings candidates"]
        self.assertEqual(["winpeas"], group["disable"])
        self.assertTrue(group["config"]["auto_check"])
        self.assertEqual(1, len(group["files"]))
        item = group["files"][0]
        self.assertEqual("conf.php", item["name"])
        value = item["value"]
        self.assertTrue(value["just_list_file"])
        self.assertEqual(["common"], value["search_in"])
        self.assertNotIn("line_grep", value)
        pattern = value["check_extra_path"]
        accepted = ["/var/www/erp/htdocs/conf/conf.php", "/opt/erp site/htdocs/conf/conf.php"]
        rejected = ["/var/www/erp/conf/conf.php", "/tmp/conf.php",
                    "/var/www/erp/htdocs/conf/conf.php.old"]
        for path in accepted:
            self.assertIsNotNone(re.search(pattern, path), path)
        for path in rejected:
            self.assertIsNone(re.search(pattern, path), path)
        result = subprocess.run(
            ["grep", "-E", pattern], input="\n".join(accepted + rejected) + "\n",
            capture_output=True, text=True, timeout=2, check=True,
        )
        self.assertEqual(accepted, result.stdout.splitlines())

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
        storage = builder._LinpeasBuilder__generate_storages()
        sections = builder._LinpeasBuilder__generate_sections()
        assignment = next(
            line for line in storage if line.startswith("PSTORAGE_DOLIBARR_DATABASE_SETTINGS_CANDIDATES=")
        )
        script = "print_2title() { :; }; " + assignment + "\n" + sections[
            "Dolibarr database settings candidates"
        ]
        self.assertEqual(0, subprocess.run(["sh", "-n"], input=script, text=True,
                                           capture_output=True, timeout=2).returncode)
        with tempfile.TemporaryDirectory(prefix="peas-dolibarr-", dir="/tmp") as temp:
            root = Path(temp)
            present = root / "htdocs/conf/conf.php"
            unrelated = root / "other/conf.php"
            present.parent.mkdir(parents=True)
            unrelated.parent.mkdir(parents=True)
            present.write_text("SENSITIVE_DB_PASSWORD")
            unrelated.write_text("SENSITIVE_UNRELATED_VALUE")
            result = subprocess.run(
                ["sh", "-c", script],
                env={**os.environ, "FIND_VAR": f"{present}\n{unrelated}",
                     "ROOT_FOLDER": "/", "E": "E", "SED_RED": "&"},
                capture_output=True, text=True, timeout=5,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(present), result.stdout)
            self.assertNotIn(str(unrelated), result.stdout)
            self.assertNotIn("SENSITIVE_DB_PASSWORD", result.stdout)
            self.assertNotIn("SENSITIVE_UNRELATED_VALUE", result.stdout)

    def test_enlightenment_cve_cue_names_only_affected_helper(self):
        entries = [line for line in SIDB.read_text().splitlines() if "CVE-2022-37706" in line]
        self.assertEqual(1, len(entries))
        self.assertIn("/enlightenment_sys$", entries[0])


if __name__ == "__main__":
    unittest.main()
