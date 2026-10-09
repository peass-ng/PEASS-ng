"""Suricata event logs are discovered without reading potentially sensitive data."""

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Suricata event log candidates"


class SuricataEventCatalogTests(unittest.TestCase):
    def test_selector_is_scoped_and_path_only(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [item for item in catalog["search"] if item["name"] == TITLE]
        self.assertIn("winpeas", record["value"]["disable"])
        self.assertTrue(record["value"]["config"]["auto_check"])
        entry, = record["value"]["files"]
        self.assertEqual(entry["name"], "eve*.json*")
        self.assertEqual(entry["value"]["search_in"], ["${ROOT_FOLDER}var"])
        self.assertEqual(entry["value"]["type"], "f")
        self.assertTrue(entry["value"]["just_list_file"])

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
        self.assertTrue(any("eve*.json*" in item and " -name " in item
                            for item in finds))
        storage = next(item for item in builder._LinpeasBuilder__generate_storages()
                       if item.startswith("PSTORAGE_SURICATA_EVENT_LOG_CANDIDATES="))
        self.assertIn("/var/log/suricata/eve[^/]*\\.json", storage)
        candidates = (
            "/var/log/suricata/eve.json",
            "/var/log/suricata/eve.json.7.gz",
            "/var/log/suricata/eve.7.json",
            "/var/log/suricata/eve-2024-01-01.json.gz",
            "/var/log/other/eve.json",
            "/var/log/suricata/access.json",
        )
        selected = subprocess.run(
            ["sh", "-c", storage + '\nprintf "%s\\n" "$PSTORAGE_SURICATA_EVENT_LOG_CANDIDATES"'],
            env={**os.environ, "ROOT_FOLDER": "/", "FIND_VAR": "\n".join(candidates)},
            text=True, capture_output=True, timeout=2,
        )
        self.assertEqual(selected.returncode, 0, selected.stderr)
        for path in candidates[:4]:
            self.assertIn(path, selected.stdout)
        for path in candidates[4:]:
            self.assertNotIn(path, selected.stdout)
        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        self.assertEqual(subprocess.run(["sh", "-n"], input=section, text=True,
                                        capture_output=True, timeout=2).returncode, 0)

        with tempfile.TemporaryDirectory() as tmp:
            paths = [Path(tmp) / name for name in (
                "eve.json", "eve.json.7.gz", "eve.7.json", "eve-2024-01-01.json.gz",
                "other.json")]
            for path in paths:
                path.write_text("SECRET_EVENT_VALUE")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ,
                     "PSTORAGE_SURICATA_EVENT_LOG_CANDIDATES": "\n".join(map(str, paths)),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            for path in paths[:-1]:
                self.assertIn(str(path), result.stdout)
            self.assertNotIn(str(paths[-1]), result.stdout)
            self.assertNotIn("SECRET_EVENT_VALUE", result.stdout)


if __name__ == "__main__":
    unittest.main()
