"""The NAS configuration catalog must stay exact-path and metadata-only."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "OpenMediaVault configuration candidate"
STORAGE = "PSTORAGE_OPENMEDIAVAULT_CONFIGURATION_CANDIDATE"


class OpenMediaVaultConfigCatalogTests(unittest.TestCase):
    def test_exact_path_in_existing_cache_and_no_content_output(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [item for item in catalog["search"] if item["name"] == TITLE]
        self.assertEqual(record["value"]["disable"], ["winpeas"])
        self.assertTrue(record["value"]["config"]["auto_check"])
        entry, = record["value"]["files"]
        self.assertEqual(entry["name"], "config.xml")
        self.assertEqual(entry["value"]["type"], "f")
        self.assertTrue(entry["value"]["just_list_file"])
        self.assertEqual(entry["value"]["search_in"], ["${ROOT_FOLDER}etc"])
        pattern = entry["value"]["check_extra_path"]
        self.assertIsNotNone(re.search(pattern, "/etc/openmediavault/config.xml"))
        for path in ("/etc/other/config.xml", "/tmp/etc/openmediavault/config.xml",
                     "/etc/openmediavault/config.xml.old"):
            self.assertIsNone(re.search(pattern, path))

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
        self.assertIn(pattern, storage)
        self.assertIn("$FIND_ETC", storage)
        candidates = ("/etc/openmediavault/config.xml", "/etc/other/config.xml",
                      "/tmp/etc/openmediavault/config.xml")
        selected = subprocess.run(
            ["sh", "-c", storage + f'\nprintf "%s\\n" "${STORAGE}"'],
            env={**os.environ, "ROOT_FOLDER": "/", "FIND_ETC": "\n".join(candidates)},
            text=True, capture_output=True, timeout=2,
        )
        self.assertEqual(selected.returncode, 0, selected.stderr)
        self.assertEqual(selected.stdout.strip(), candidates[0])

        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        self.assertNotIn("cat ", section)
        self.assertEqual(subprocess.run(["sh", "-n"], input=section, text=True,
                                        capture_output=True, timeout=2).returncode, 0)
        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "config.xml"
            config.write_text("SECRET_MUST_STAY_HIDDEN")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, STORAGE: str(config), "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn(str(config), result.stdout)
            self.assertNotIn("SECRET_MUST_STAY_HIDDEN", result.stdout)


if __name__ == "__main__":
    unittest.main()
