"""A PHP database settings filename is listed from cached paths without reading it."""

import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "PHP database settings path candidates"


class PhpDatabasePathCatalogTests(unittest.TestCase):
    def test_exact_bounded_webroot_selector_is_path_only(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [item for item in catalog["search"] if item["name"] == TITLE]
        self.assertEqual(["winpeas"], record["value"]["disable"])
        self.assertTrue(record["value"]["config"]["auto_check"])
        entry, = record["value"]["files"]
        self.assertEqual("db.php5", entry["name"])
        value = entry["value"]
        self.assertEqual("f", value["type"])
        self.assertEqual(["common"], value["search_in"])
        self.assertTrue(value["just_list_file"])
        self.assertNotIn("line_grep", value)

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
        self.assertTrue(any('db.php5' in line and ' -name ' in line for line in finds))

        storages = builder._LinpeasBuilder__generate_storages()
        storage = next(line for line in storages
                       if line.startswith("PSTORAGE_PHP_DATABASE_SETTINGS_PATH_CANDIDATES="))
        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        self.assertNotIn('cat "$f"', section)
        self.assertNotIn("grep -i", section)
        self.assertEqual(0, subprocess.run(["sh", "-n"], input=storage + "\n" + section,
                                           text=True, capture_output=True, timeout=2).returncode)

        accepted = "/var/www/site/db.php5"
        offline_accepted = "/mnt/offline/root/var/www/site/db.php5"
        rejected = ("/home/user/db.php5", "/var/www/site/db.php5.bak",
                    "/var/www/site/db.php", "/var/www/a/b/c/d/e/f/db.php5",
                    "/mnt/offline/root/var/www/a/b/c/d/e/f/db.php5",
                    "/mnt/offline/root/var/wwww/site/db.php5")
        selected = subprocess.run(
            ["sh", "-c", storage + '\nprintf "%s\\n" "$PSTORAGE_PHP_DATABASE_SETTINGS_PATH_CANDIDATES"'],
            env={**os.environ, "ROOT_FOLDER": "/", "GREPHOMESEARCH": "/home/",
                 "FIND_VAR": "\n".join((accepted, *rejected))},
            capture_output=True, text=True, timeout=2,
        )
        self.assertEqual(0, selected.returncode, selected.stderr)
        selected_paths = selected.stdout.splitlines()
        self.assertIn(accepted, selected_paths)
        for path in rejected:
            self.assertNotIn(path, selected_paths)

        offline_selected = subprocess.run(
            ["sh", "-c", storage + '\nprintf "%s\\n" "$PSTORAGE_PHP_DATABASE_SETTINGS_PATH_CANDIDATES"'],
            env={**os.environ, "ROOT_FOLDER": "/mnt/offline/root/",
                 "GREPHOMESEARCH": "/mnt/offline/root/home/",
                 "FIND_VAR": "\n".join((accepted, offline_accepted, *rejected))},
            capture_output=True, text=True, timeout=2,
        )
        self.assertEqual(0, offline_selected.returncode, offline_selected.stderr)
        offline_paths = offline_selected.stdout.splitlines()
        self.assertIn(offline_accepted, offline_paths)
        self.assertNotIn(accepted, offline_paths)
        for path in rejected:
            self.assertNotIn(path, offline_paths)

        with tempfile.TemporaryDirectory() as temporary:
            candidate = Path(temporary) / "db.php5"
            candidate.write_text("PRIVATE_PASSWORD_MUST_NOT_PRINT")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_PHP_DATABASE_SETTINGS_PATH_CANDIDATES": str(candidate),
                     "E": "E", "SED_RED": "&"},
                capture_output=True, text=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(candidate), result.stdout)
            self.assertNotIn("PRIVATE_PASSWORD_MUST_NOT_PRINT", result.stdout)


if __name__ == "__main__":
    unittest.main()
