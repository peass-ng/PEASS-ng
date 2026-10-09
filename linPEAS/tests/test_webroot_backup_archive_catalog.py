"""Webroot backup archives are bounded path-only inventory leads."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Webroot backup archive candidates"


class WebrootBackupArchiveCatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        matches = [item["value"] for item in catalog["search"] if item["name"] == TITLE]
        if len(matches) != 1:
            raise AssertionError("expected one webroot backup archive group")
        cls.group = matches[0]

    def test_narrow_linux_path_only_selector(self):
        self.assertEqual(["winpeas"], self.group["disable"])
        self.assertTrue(self.group["config"]["auto_check"])
        self.assertEqual(1, len(self.group["files"]))
        entry = self.group["files"][0]
        self.assertEqual("*backup*.zip", entry["name"])
        value = entry["value"]
        self.assertEqual("f", value["type"])
        self.assertEqual(["common"], value["search_in"])
        self.assertTrue(value["just_list_file"])
        self.assertNotIn("bad_regex", value)
        self.assertNotIn("line_grep", value)

        pattern = value["check_extra_path"]
        positives = (
            "/var/www/html/files/16162020_backup.zip",
            "/var/www/site/backup.zip",
        )
        negatives = (
            "/tmp/16162020_backup.zip",
            "/var/www/html/files/data.zip",
            "/var/www/html/files/backup.zip.old",
            "/var/www/html/assets/deeper/backup.zip",
            "C:/inetpub/wwwroot/backup.zip",
        )
        for path in positives:
            self.assertIsNotNone(re.search(pattern, path), path)
        for path in negatives:
            self.assertIsNone(re.search(pattern, path), path)
        result = subprocess.run(
            ["grep", "-E", pattern],
            input="\n".join((*positives, *negatives)) + "\n",
            text=True, capture_output=True, check=True, timeout=2,
        )
        self.assertEqual("\n".join(positives) + "\n", result.stdout)

    def test_generated_section_is_capped_and_does_not_read_archives(self):
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
        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        assignment = next(item for item in storage if item.startswith(
            "PSTORAGE_WEBROOT_BACKUP_ARCHIVE_CANDIDATES="))
        self.assertEqual(0, subprocess.run(
            ["sh", "-n"], input=section, text=True, capture_output=True, timeout=2,
        ).returncode)
        self.assertIn("head -n 70", assignment)
        self.assertNotIn("cat ", section)
        self.assertNotIn("unzip", section)

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            webroot = root / "var/www/html/files"
            webroot.mkdir(parents=True)
            archives = [webroot / f"{index:03}_backup.zip" for index in range(75)]
            for archive in archives:
                archive.write_text("SECRET_INSIDE_ARCHIVE")
            ignored = root / "tmp/backup.zip"
            ignored.parent.mkdir()
            ignored.write_text("UNRELATED_CONTENT")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + assignment + "\n" + section],
                env={**os.environ, "FIND_VAR": "\n".join(map(str, (*archives, ignored))),
                     "ROOT_FOLDER": str(root) + "/", "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=3,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual(70, result.stdout.count("_backup.zip"))
            self.assertNotIn(str(ignored), result.stdout)
            self.assertNotIn("SECRET_INSIDE_ARCHIVE", result.stdout)
            self.assertNotIn("UNRELATED_CONTENT", result.stdout)


if __name__ == "__main__":
    unittest.main()
