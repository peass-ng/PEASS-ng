"""Authentication logs are cached by exact name and listed without contents."""

import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Interesting logs"


class AuthLogCatalogTests(unittest.TestCase):
    def test_exact_cached_path_only_selector(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [item for item in catalog["search"] if item["name"] == TITLE]
        options = {item["name"]: item["value"] for item in record["value"]["files"]}
        self.assertEqual({"access.log", "error.log", "auth.log"}, options.keys())
        self.assertEqual(
            {"just_list_file": True, "type": "f", "search_in": ["common"]},
            options["auth.log"],
        )

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
        self.assertTrue(any('-name \\"auth.log\\"' in line for line in finds))
        builder._LinpeasBuilder__generate_storages()
        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        self.assertNotIn('cat "$f"', section)
        self.assertNotIn("ls -lRA", section)
        self.assertEqual(0, subprocess.run(["sh", "-n"], input=section, text=True,
                                           capture_output=True, timeout=2).returncode)

        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            exact = root / "auth.log"
            exact.write_text("SECRET_DO_NOT_PRINT\n")
            rotated = root / "auth.log.1"
            rotated.write_text("OTHER_SECRET_DO_NOT_PRINT\n")
            unrelated = root / "authentication.log"
            unrelated.write_text("UNRELATED_SECRET_DO_NOT_PRINT\n")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ,
                     "PSTORAGE_INTERESTING_LOGS": "\n".join(map(str, (exact, rotated, unrelated))),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(exact), result.stdout)
            self.assertNotIn(str(rotated), result.stdout)
            self.assertNotIn(str(unrelated), result.stdout)
            self.assertNotIn("SECRET_DO_NOT_PRINT", result.stdout)


if __name__ == "__main__":
    unittest.main()
