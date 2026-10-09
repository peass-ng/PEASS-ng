"""Printer configuration paths use the cached inventory without reading URIs."""

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]


class CupsPrinterConfigCatalogTests(unittest.TestCase):
    def test_exact_path_only_selector(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [item for item in catalog["search"] if item["name"] == "Other Interesting"]
        files = {item["name"]: item["value"] for item in record["value"]["files"]}
        entry = files["printers.conf"]
        self.assertEqual(entry["search_in"], ["common"])
        self.assertEqual(entry["type"], "f")
        self.assertTrue(entry["just_list_file"])
        for key in ("bad_regex", "good_regex", "line_grep", "only_bad_lines"):
            self.assertNotIn(key, entry)

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
        self.assertTrue(any("printers.conf" in line and " -name " in line for line in finds))
        section = builder._LinpeasBuilder__generate_sections()["Other Interesting"]
        syntax = subprocess.run(["sh", "-n"], input=section, text=True,
                                capture_output=True, timeout=2)
        self.assertEqual(0, syntax.returncode, syntax.stderr)

        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "printers.conf"
            unrelated = Path(tmp) / "printers.conf.bak"
            config.write_text("DeviceURI https://user:DO_NOT_PRINT_PASSWORD@example.invalid/print")
            unrelated.write_text("DO_NOT_PRINT_BACKUP")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_OTHER_INTERESTING": "\n".join((str(config), str(unrelated))),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(config), result.stdout)
            self.assertNotIn(str(unrelated), result.stdout)
            self.assertNotIn("DO_NOT_PRINT_PASSWORD", result.stdout)


if __name__ == "__main__":
    unittest.main()
