"""Keep legacy encrypted database candidates in the existing path-only search."""

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Keepass"


class KeePassLegacyCatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        records = [item for item in catalog["search"] if item["name"] == TITLE]
        assert len(records) == 1
        cls.record = records[0]
        cls.files = {item["name"]: item["value"]
                     for item in cls.record["value"]["files"]}

    def test_legacy_and_current_selectors_are_metadata_only(self):
        self.assertTrue(self.record["value"]["config"]["auto_check"])
        for extension in ("*.kdb", "*.kdbx"):
            with self.subTest(extension=extension):
                self.assertIn(extension, self.files)
                options = self.files[extension]
                self.assertTrue(options["just_list_file"])
                self.assertEqual(options["type"], "f")
                self.assertEqual(options["search_in"], ["common"])
                self.assertNotIn("bad_regex", options)
                self.assertNotIn("line_grep", options)

    def test_builder_reuses_common_search_and_lists_only_exact_extensions(self):
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
        self.assertTrue(any('*.kdb' in line and '*.kdbx' in line and ' -name ' in line
                            for line in finds))
        storages = builder._LinpeasBuilder__generate_storages()
        storage_name = "PSTORAGE_KEEPASS"
        self.assertEqual(1, sum(line.startswith(storage_name + "=")
                                for line in storages))
        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        syntax = subprocess.run(["sh", "-n"], input=section, text=True,
                                capture_output=True, timeout=2)
        self.assertEqual(syntax.returncode, 0, syntax.stderr)
        self.assertNotIn('cat "$f"', section)

        with tempfile.TemporaryDirectory() as tmp:
            paths = [Path(tmp) / name for name in
                     ("vault.kdb", "vault.kdbx", "vault.kdb.bak")]
            for path in paths:
                path.write_text("DO_NOT_PRINT_VAULT_CONTENT")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, storage_name: "\n".join(map(str, paths)),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn(str(paths[0]), result.stdout)
            self.assertIn(str(paths[1]), result.stdout)
            self.assertNotIn(str(paths[2]), result.stdout)
            self.assertNotIn("DO_NOT_PRINT_VAULT_CONTENT", result.stdout)


if __name__ == "__main__":
    unittest.main()
