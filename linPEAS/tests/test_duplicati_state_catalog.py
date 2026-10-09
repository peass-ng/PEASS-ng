"""Keep Duplicati state-file discovery scoped, fast, and metadata-only."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]


class DuplicatiStateCatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        records = [item for item in catalog["search"] if item["name"] == "Duplicati server state"]
        assert len(records) == 1
        cls.record = records[0]
        cls.entry = cls.record["value"]["files"][0]
        cls.options = cls.entry["value"]

    def test_exact_name_and_metadata_only(self):
        self.assertTrue(self.record["value"]["config"]["auto_check"])
        self.assertEqual(self.entry["name"], "Duplicati-server.sqlite")
        self.assertEqual(len(self.record["value"]["files"]), 1)
        self.assertEqual(self.options["type"], "f")
        self.assertEqual(self.options["search_in"], ["common"])
        self.assertTrue(self.options["just_list_file"])
        self.assertNotIn("bad_regex", self.options)
        self.assertNotIn("line_grep", self.options)

    def test_product_paths_and_unrelated_paths(self):
        pattern = self.options["check_extra_path"]
        positives = (
            "/opt/duplicati/config/Duplicati-server.sqlite",
            "/var/lib/Duplicati/Duplicati-server.sqlite",
            "/home/alice/.config/Duplicati/Duplicati-server.sqlite",
            "/Duplicati/Duplicati-server.sqlite",
        )
        negatives = (
            "/opt/other/config/Duplicati-server.sqlite",
            "/opt/duplicati/config/Duplicati-server.sqlite.bak",
            "/opt/duplicati/config/other.sqlite",
            "/opt/notduplicati/config/Duplicati-server.sqlite",
            "/tmp/Duplicati-server.sqlite",
            "/config/Duplicati-server.sqlite",
        )
        for path in positives:
            with self.subTest(path=path):
                self.assertIsNotNone(re.search(pattern, path))
        for path in negatives:
            with self.subTest(path=path):
                self.assertIsNone(re.search(pattern, path))

        result = subprocess.run(
            ["grep", "-E", pattern],
            input="\n".join(positives + negatives) + "\n",
            text=True,
            capture_output=True,
            timeout=2,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.splitlines(), list(positives))

    def test_cached_builder_selector_and_section_do_not_read_content(self):
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
        self.assertTrue(any('Duplicati-server.sqlite' in line and ' -name ' in line
                            for line in finds))
        storage = next(
            line for line in builder._LinpeasBuilder__generate_storages()
            if line.startswith("PSTORAGE_DUPLICATI_SERVER_STATE=")
        )
        self.assertIn("Duplicati-server", storage)
        self.assertIn("grep -E '", storage)
        section = builder._LinpeasBuilder__generate_sections()["Duplicati server state"]
        syntax = subprocess.run(["sh", "-n"], input=section, text=True, capture_output=True)
        self.assertEqual(syntax.returncode, 0, syntax.stderr)
        self.assertNotIn('cat "$f"', section)

        with tempfile.TemporaryDirectory() as temp:
            db = Path(temp) / "Duplicati-server.sqlite"
            db.write_text("NEVER_PRINT_DATABASE_CONTENT")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_DUPLICATI_SERVER_STATE": str(db),
                     "E": "E", "SED_RED": "Duplicati-server.sqlite"},
                text=True,
                capture_output=True,
                timeout=2,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn(str(db), result.stdout)
            self.assertNotIn("NEVER_PRINT_DATABASE_CONTENT", result.stdout)


if __name__ == "__main__":
    unittest.main()
