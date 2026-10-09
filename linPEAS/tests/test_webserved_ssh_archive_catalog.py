"""Web-served SSH archives are listed by path, without extraction or content reads."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Web-served SSH identity archives"


class WebservedSshArchiveCatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        entries = [item["value"] for item in catalog["search"] if item["name"] == TITLE]
        if len(entries) != 1:
            raise AssertionError("expected one SSH archive category")
        cls.group = entries[0]

    def test_exact_linux_path_only_selectors(self):
        self.assertEqual(["winpeas"], self.group["disable"])
        self.assertTrue(self.group["config"]["auto_check"])
        self.assertEqual(["*ssh*.tgz", "*ssh*.tar.gz"],
                         [item["name"] for item in self.group["files"]])
        for entry in self.group["files"]:
            value = entry["value"]
            self.assertEqual("f", value["type"])
            self.assertEqual(["common"], value["search_in"])
            self.assertTrue(value["just_list_file"])
            self.assertNotIn("bad_regex", value)
            self.assertNotIn("line_grep", value)

            ext = ".tgz" if entry["name"].endswith(".tgz") else ".tar.gz"
            pattern = value["check_extra_path"]
            positives = (
                "/home/alice/public_www/backup-ssh-identity-files" + ext,
                "/offline/home/alice/public_www/protected/ssh-keys" + ext,
            )
            negatives = (
                "/tmp/ssh-keys" + ext,
                "/home/alice/ssh-keys" + ext,
                "/home/alice/public_www/a/b/c/ssh-keys" + ext,
                "/home/alice/public_www/backup-ssh-identity-files" + ext + ".old",
                "/home/alice/public_www/backup-keys" + ext,
                "C:/Users/alice/public_www/ssh-keys" + ext,
            )
            for path in positives:
                self.assertIsNotNone(re.search(pattern, path), path)
            for path in negatives:
                self.assertIsNone(re.search(pattern, path), path)
            result = subprocess.run(
                ["grep", "-E", pattern], input="\n".join((*positives, *negatives)) + "\n",
                text=True, capture_output=True, check=True, timeout=2,
            )
            self.assertEqual("\n".join(positives) + "\n", result.stdout)

    def test_generated_cached_inventory_lists_only_paths(self):
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
            "PSTORAGE_WEB_SERVED_SSH_IDENTITY_ARCHIVES="))
        self.assertIn("FIND_VAR", assignment)
        self.assertIn("head -n 70", assignment)
        self.assertNotIn("cat ", section)
        self.assertNotIn("tar ", section)
        self.assertNotIn("unzip", section)
        self.assertEqual(0, subprocess.run(
            ["sh", "-n"], input=section, text=True, capture_output=True, timeout=2,
        ).returncode)

        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            webroot = root / "home/alice/public_www/protected"
            webroot.mkdir(parents=True)
            valid = [webroot / "backup-ssh-identity-files.tgz",
                     webroot / "ssh-keys.tar.gz"]
            for path in valid:
                path.write_text("PRIVATE_ARCHIVE_CONTENT")
            invalid = root / "tmp/ssh-keys.tgz"
            invalid.parent.mkdir()
            invalid.write_text("UNRELATED_CONTENT")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + assignment + "\n" + section],
                env={**os.environ, "FIND_VAR": "\n".join(map(str, (*valid, invalid))),
                     "ROOT_FOLDER": str(root) + "/", "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=3,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            for path in valid:
                self.assertIn(str(path), result.stdout)
            self.assertNotIn(str(invalid), result.stdout)
            self.assertNotIn("PRIVATE_ARCHIVE_CONTENT", result.stdout)
            self.assertNotIn("UNRELATED_CONTENT", result.stdout)


if __name__ == "__main__":
    unittest.main()
