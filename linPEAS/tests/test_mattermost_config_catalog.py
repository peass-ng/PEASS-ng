"""The Mattermost configuration lead uses an exact cached path, not contents."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Mattermost database configuration candidate"
STORAGE = "PSTORAGE_MATTERMOST_DATABASE_CONFIGURATION_CANDIDATE"


class MattermostConfigCatalogTests(unittest.TestCase):
    def test_exact_cached_path_and_metadata_only_section(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [item for item in catalog["search"] if item["name"] == TITLE]
        self.assertEqual(["winpeas"], record["value"]["disable"])
        self.assertTrue(record["value"]["config"]["auto_check"])
        entry, = record["value"]["files"]
        self.assertEqual("config.json", entry["name"])
        self.assertEqual("f", entry["value"]["type"])
        self.assertEqual(["common"], entry["value"]["search_in"])
        self.assertTrue(entry["value"]["just_list_file"])
        self.assertNotIn("line_grep", entry["value"])
        pattern = entry["value"]["check_extra_path"]
        exact = "/opt/mattermost/config/config.json"
        self.assertIsNotNone(re.search(pattern, exact))
        for path in ("/opt/mattermost/config/config.json.old",
                     "/opt/other/config/config.json",
                     "/tmp/opt/mattermost/config/config.json"):
            self.assertIsNone(re.search(pattern, path), path)

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
        storage = next(line for line in builder._LinpeasBuilder__generate_storages()
                       if line.startswith(STORAGE + "="))
        self.assertIn("$FIND_OPT", storage)
        self.assertIn(pattern, storage)
        selected = subprocess.run(
            ["sh", "-c", storage + f'\nprintf "%s\\n" "${STORAGE}"'],
            env={**os.environ, "ROOT_FOLDER": "/", "FIND_OPT": "\n".join(
                (exact, "/opt/other/config/config.json"))},
            text=True, capture_output=True, timeout=2,
        )
        self.assertEqual(0, selected.returncode, selected.stderr)
        self.assertEqual(exact, selected.stdout.strip())

        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        self.assertNotIn("cat ", section)
        syntax = subprocess.run(["sh", "-n"], input=section, text=True,
                                capture_output=True, timeout=2)
        self.assertEqual(0, syntax.returncode, syntax.stderr)
        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "config.json"
            config.write_text("SECRET_DATABASE_CREDENTIAL")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, STORAGE: str(config), "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(config), result.stdout)
            self.assertNotIn("SECRET_DATABASE_CREDENTIAL", result.stdout)


if __name__ == "__main__":
    unittest.main()
