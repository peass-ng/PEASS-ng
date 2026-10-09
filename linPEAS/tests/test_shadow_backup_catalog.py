"""A shadow backup is a path-only inventory lead, never a hash read."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Shadow backup candidates"


class ShadowBackupCatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        groups = [item["value"] for item in catalog["search"] if item["name"] == TITLE]
        if len(groups) != 1:
            raise AssertionError("expected one shadow backup group")
        cls.group = groups[0]

    def test_exact_path_only_linux_selector(self):
        self.assertEqual(["winpeas"], self.group["disable"])
        self.assertTrue(self.group["config"]["auto_check"])
        self.assertEqual(1, len(self.group["files"]))
        selector = self.group["files"][0]
        self.assertEqual("shadow.bak", selector["name"])
        value = selector["value"]
        self.assertEqual("f", value["type"])
        self.assertEqual(["${ROOT_FOLDER}var"], value["search_in"])
        self.assertTrue(value["just_list_file"])
        self.assertNotIn("line_grep", value)
        pattern = value["check_extra_path"]
        positives = (
            "/var/backups/shadow.bak",
            "/offline/root/var/backups/shadow.bak",
        )
        negatives = (
            "/etc/shadow.bak",
            "/var/backups/gshadow.bak",
            "/var/backups/shadow.bak.old",
            "/var/backups/shadow.bak2",
            "/var/backups/sub/shadow.bak",
        )
        for path in positives:
            self.assertIsNotNone(re.search(pattern, path), path)
        for path in negatives:
            self.assertIsNone(re.search(pattern, path), path)

    def test_generated_section_lists_only_matching_path(self):
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
            "PSTORAGE_SHADOW_BACKUP_CANDIDATES="))
        self.assertEqual(0, subprocess.run(
            ["sh", "-n"], input=section, text=True, capture_output=True, timeout=2,
        ).returncode)
        self.assertNotIn("cat ", section)
        live = subprocess.run(
            ["sh", "-c", assignment + "\nprintf '%s\\n' \"$PSTORAGE_SHADOW_BACKUP_CANDIDATES\""],
            env={**os.environ, "FIND_VAR": "/var/backups/shadow.bak\n/home/user/var/backups/shadow.bak",
                 "ROOT_FOLDER": "/", "E": "E", "SED_RED": "&"},
            text=True, capture_output=True, timeout=2,
        )
        self.assertEqual(0, live.returncode, live.stderr)
        self.assertIn("/var/backups/shadow.bak", live.stdout)
        self.assertNotIn("/home/user/var/backups/shadow.bak", live.stdout)
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            target = root / "var/backups/shadow.bak"
            lookalike = root / "var/backups/shadow.bak.old"
            outside = root / "etc/shadow.bak"
            nested = root / "home/user/var/backups/shadow.bak"
            target.parent.mkdir(parents=True)
            outside.parent.mkdir(parents=True)
            nested.parent.mkdir(parents=True)
            target.write_text("PRIVATE_HASH_CONTENT")
            lookalike.write_text("LOOKALIKE_CONTENT")
            outside.write_text("OUTSIDE_CONTENT")
            nested.write_text("NESTED_CONTENT")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + assignment + "\n" + section],
                env={**os.environ, "FIND_VAR": "\n".join(map(str, (target, lookalike, outside, nested))),
                     "ROOT_FOLDER": str(root) + "/", "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(target), result.stdout)
            self.assertNotIn(str(lookalike), result.stdout)
            self.assertNotIn(str(outside), result.stdout)
            self.assertNotIn(str(nested), result.stdout)
            for secret in ("PRIVATE_HASH_CONTENT", "LOOKALIKE_CONTENT", "OUTSIDE_CONTENT", "NESTED_CONTENT"):
                self.assertNotIn(secret, result.stdout)


if __name__ == "__main__":
    unittest.main()
