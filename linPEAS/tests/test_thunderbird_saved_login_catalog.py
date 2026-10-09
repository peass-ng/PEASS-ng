"""Thunderbird login artifacts use the cached filename inventory and path-only output."""

import os
import re
import subprocess
import sys
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Thunderbird saved-login files"
STORAGE = "PSTORAGE_THUNDERBIRD_SAVED_LOGIN_FILES"


class ThunderbirdSavedLoginCatalogTests(unittest.TestCase):
    def test_exact_profile_artifacts_and_nearby_negatives(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        group, = [item["value"] for item in catalog["search"] if item["name"] == TITLE]
        self.assertEqual(group["disable"], ["winpeas"])
        self.assertTrue(group["config"]["auto_check"])
        files = {item["name"]: item["value"] for item in group["files"]}
        self.assertEqual(set(files), {"logins.json", "key3.db", "key4.db"})
        for name, value in files.items():
            self.assertEqual(value["type"], "f")
            self.assertEqual(value["search_in"], ["$HOMESEARCH"])
            self.assertTrue(value["just_list_file"])
            self.assertNotIn("bad_regex", value)
            pattern = value["check_extra_path"]
            for path in (f"/home/alice/.thunderbird/abcd.default/{name}",
                         f"/offline/home/alice/.thunderbird/work.profile/{name}"):
                self.assertIsNotNone(re.search(pattern, path), path)
            for path in (f"/home/alice/.mozilla/firefox/abcd.default/{name}",
                         f"/home/alice/.thunderbird/{name}",
                         f"/home/alice/.thunderbird/abcd.default/{name}.bak",
                         f"/home/alice/.thunderbird/abcd.default/nested/{name}"):
                self.assertIsNone(re.search(pattern, path), path)

    def test_generated_storage_reuses_cached_files_without_reading_content(self):
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
        assignment, = [line for line in builder._LinpeasBuilder__generate_storages()
                       if line.startswith(STORAGE + "=")]
        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        self.assertIn("$FIND_HOMESEARCH", assignment)
        self.assertIn("head -n 70", assignment)
        self.assertNotIn("cat ", section)
        self.assertNotIn("sqlite", section.lower())
        self.assertEqual(subprocess.run(["sh", "-n"], input=section, text=True,
                                        capture_output=True, timeout=2).returncode, 0)

        selected = (
            "/home/alice/.thunderbird/abcd.default/logins.json",
            "/offline/home/alice/.thunderbird/work.profile/key3.db",
        )
        rejected = (
            "/home/alice/.mozilla/firefox/abcd.default/logins.json",
            "/home/alice/.thunderbird/key4.db",
            "/home/alice/.thunderbird/abcd.default/logins.json.bak",
            "/home/alice/.thunderbird/abcd.default/nested/key3.db",
        )
        result = subprocess.run(
            ["sh", "-c", assignment + f'\nprintf "%s\\n" "${STORAGE}"'],
            env={**os.environ, "FIND_HOMESEARCH": "\n".join((*selected, *rejected)),
                 "GREPHOMESEARCH": "/home|/offline/home", "ROOT_FOLDER": "/",
                 "E": "E", "SED_RED": "&"},
            text=True, capture_output=True, timeout=3,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(set(result.stdout.splitlines()), set(selected))


if __name__ == "__main__":
    unittest.main()
