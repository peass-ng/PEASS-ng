"""The default encrypted credential database is a path-only lead."""

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]


class PasspieCatalogTests(unittest.TestCase):
    def test_default_database_directory_is_metadata_only(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        records = {item["name"]: item["value"] for item in catalog["search"]}
        record = records["Passpie database candidate"]
        self.assertTrue(record["config"]["auto_check"])
        self.assertEqual(["winpeas"], record["disable"])
        self.assertFalse(record["config"].get("exec"))
        self.assertEqual(1, len(record["files"]))
        entry = record["files"][0]
        self.assertEqual(".passpie", entry["name"])
        self.assertEqual("d", entry["value"]["type"])
        self.assertEqual(["$HOMESEARCH"], entry["value"]["search_in"])
        self.assertFalse(entry["value"].get("just_list_file", False))
        self.assertEqual(".password-store", records["Pass Store Directories"]["files"][0]["name"])

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
        home_find = next(line for line in finds if line.startswith("cache_find FIND_DIR_HOMESEARCH "))
        self.assertIn('-name ".passpie"', home_find)
        section = builder._LinpeasBuilder__generate_sections()["Passpie database candidate"]
        for unsafe in ("cat ", "ls -lRA", "find \"$f\""):
            self.assertNotIn(unsafe, section)
        self.assertIn('ls -ld "$f"', section)
        syntax = subprocess.run(["sh", "-n"], input=section, text=True,
                                capture_output=True, timeout=2)
        self.assertEqual(0, syntax.returncode, syntax.stderr)

        with tempfile.TemporaryDirectory() as tmp:
            database = Path(tmp) / ".passpie"
            database.mkdir()
            (database / ".keys").write_text("DO_NOT_PRINT_KEY_MATERIAL")
            group = database / "account"
            group.mkdir()
            (group / "privileged.pass").write_text("DO_NOT_PRINT_ENCRYPTED_ENTRY")
            backup = Path(tmp) / ".passpie.bak"
            backup.mkdir()
            found = subprocess.run(
                ["find", tmp, "-type", "d", "-name", ".passpie"],
                capture_output=True, text=True, check=True, timeout=2,
            ).stdout.splitlines()
            self.assertEqual([str(database)], found)

            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_PASSPIE_DATABASE_CANDIDATE": str(database),
                     "E": "E", "SED_RED": "&"},
                capture_output=True, text=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(database), result.stdout)
            for secret in ("DO_NOT_PRINT_KEY_MATERIAL", "DO_NOT_PRINT_ENCRYPTED_ENTRY", "privileged.pass"):
                self.assertNotIn(secret, result.stdout)


if __name__ == "__main__":
    unittest.main()
