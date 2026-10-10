import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]


class PswmVaultCatalogTests(unittest.TestCase):
    def test_default_vault_path_is_metadata_only(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record = next(item for item in catalog["search"] if item["name"] == "PSWM vault candidates")
        self.assertTrue(record["value"]["config"]["auto_check"])
        entry = record["value"]["files"][0]
        self.assertEqual(entry["name"], "pswm")
        self.assertTrue(entry["value"]["just_list_file"])
        self.assertEqual(entry["value"]["search_in"], ["$HOMESEARCH"])
        pattern = entry["value"]["check_extra_path"]
        self.assertIsNotNone(re.search(pattern, "/home/user/.local/share/pswm/pswm"))
        for unrelated in ("/usr/bin/pswm", "/home/user/pswm", "/home/user/.local/share/pswm/pswm.bak"):
            self.assertIsNone(re.search(pattern, unrelated))

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
        home_find = next(line for line in finds if line.startswith("cache_find FIND_HOMESEARCH "))
        self.assertIn('-name "pswm"', home_find)
        storages = builder._LinpeasBuilder__generate_storages()
        vault_storage = next(line for line in storages if line.startswith("PSTORAGE_PSWM_VAULT_CANDIDATES="))
        self.assertIn("grep -E '/\\.local/share/pswm/pswm$'", vault_storage)
        self.assertIn('grep -E "^$GREPHOMESEARCH"', vault_storage)
        section = builder._LinpeasBuilder__generate_sections()["PSWM vault candidates"]
        syntax = subprocess.run(["sh", "-n"], input=section, text=True, capture_output=True)
        self.assertEqual(syntax.returncode, 0, syntax.stderr)

        with tempfile.TemporaryDirectory() as tmp:
            vault = Path(tmp) / ".local/share/pswm/pswm"
            vault.parent.mkdir(parents=True)
            vault.write_text("SENSITIVE_ENCRYPTED_VAULT_FIXTURE")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_PSWM_VAULT_CANDIDATES": str(vault),
                     "E": "E", "SED_RED": "pswm"},
                text=True, capture_output=True,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn(str(vault), result.stdout)
            self.assertIn("readability", result.stdout)
            self.assertNotIn("SENSITIVE_ENCRYPTED_VAULT_FIXTURE", result.stdout)


if __name__ == "__main__":
    unittest.main()
