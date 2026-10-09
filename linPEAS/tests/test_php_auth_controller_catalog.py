"""PHP authentication-controller inventory stays scoped and path-only."""

import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "PHP authentication controller candidates"


class PhpAuthControllerCatalogTests(unittest.TestCase):
    def test_generated_selector_and_output_are_scoped(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [entry for entry in catalog["search"] if entry["name"] == TITLE]
        self.assertEqual(["winpeas"], record["value"]["disable"])
        self.assertTrue(record["value"]["config"]["auto_check"])
        file_record, = record["value"]["files"]
        self.assertEqual("AuthController.php", file_record["name"])
        self.assertEqual("f", file_record["value"]["type"])
        self.assertEqual(["common"], file_record["value"]["search_in"])
        self.assertTrue(file_record["value"]["just_list_file"])
        self.assertNotIn("line_grep", file_record["value"])

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
        self.assertTrue(any('AuthController.php' in line and ' -name ' in line
                            for line in finds))
        storage = next(line for line in builder._LinpeasBuilder__generate_storages()
                       if line.startswith("PSTORAGE_PHP_AUTHENTICATION_CONTROLLER_CANDIDATES="))
        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        self.assertNotIn("ls -lRA", section)
        self.assertNotIn('cat "$f"', section)
        self.assertEqual(0, subprocess.run(["sh", "-n"], input=section, text=True,
                                           capture_output=True, timeout=2).returncode)

        with tempfile.TemporaryDirectory(prefix="peas-php-controller-", dir="/tmp") as directory:
            base = Path(directory)
            accepted = base / "app/Http/Controllers/AuthController.php"
            rejected = [base / "app/Http/Controllers/AuthController.php.bak",
                        base / "app/Http/Controllers/OtherController.php",
                        base / "other/AuthController.php"]
            for path in [accepted, *rejected]:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("DO_NOT_PRINT_CONTROLLER_PASSWORD")
            script = "print_2title() { :; }; " + storage + "\n" + section
            result = subprocess.run(
                ["sh", "-c", script],
                env={**os.environ, "FIND_VAR": "\n".join(map(str, [accepted, *rejected])),
                     "ROOT_FOLDER": "/", "E": "E", "SED_RED": "&"},
                capture_output=True, text=True, timeout=5,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(accepted), result.stdout)
            for path in rejected:
                self.assertNotIn(str(path), result.stdout)
            self.assertNotIn("DO_NOT_PRINT_CONTROLLER_PASSWORD", result.stdout)


if __name__ == "__main__":
    unittest.main()
