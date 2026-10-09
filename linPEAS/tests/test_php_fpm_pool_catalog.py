"""PHP-FPM pool discovery must reuse cached paths and keep content private."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "PHP-FPM pool configuration candidates"
STORAGE = "PSTORAGE_PHP_FPM_POOL_CONFIGURATION_CANDIDATES"


class PhpFpmPoolCatalogTests(unittest.TestCase):
    def test_exact_pool_paths_and_metadata_only_output(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [item for item in catalog["search"] if item["name"] == TITLE]
        self.assertEqual(record["value"]["disable"], ["winpeas"])
        entry, = record["value"]["files"]
        self.assertEqual(entry["name"], "*.conf")
        self.assertEqual(entry["value"]["type"], "f")
        self.assertTrue(entry["value"]["just_list_file"])
        self.assertEqual(entry["value"]["search_in"],
                         ["${ROOT_FOLDER}etc", "${ROOT_FOLDER}usr"])
        pattern = entry["value"]["check_extra_path"].replace("${ROOT_FOLDER}", "/")
        accepted = ("/etc/php/8.1/fpm/pool.d/www.conf",
                    "/etc/php-fpm.d/site.conf",
                    "/usr/local/etc/php-fpm.d/site.conf")
        rejected = ("/tmp/etc/php/8.1/fpm/pool.d/www.conf",
                    "/etc/php/8.1/fpm/pool.d/www.conf.bak",
                    "/etc/php/8.1/fpm/php-fpm.conf",
                    "/usr/local/etc/other/site.conf")
        for path in accepted:
            self.assertIsNotNone(re.search(pattern, path), path)
        for path in rejected:
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
        self.assertIn("$FIND_ETC", storage)
        self.assertIn("$FIND_USR", storage)
        self.assertIn("head -n 70", storage)
        self.assertNotIn("find ", storage)
        selected = subprocess.run(
            ["sh", "-c", storage + f'\nprintf "%s\\n" "${STORAGE}"'],
            env={**os.environ, "ROOT_FOLDER": "/",
                 "FIND_ETC": "\n".join((accepted[0], rejected[1], rejected[2])),
                 "FIND_USR": accepted[2]},
            text=True, capture_output=True, timeout=2,
        )
        self.assertEqual(selected.returncode, 0, selected.stderr)
        self.assertEqual(selected.stdout.strip().splitlines(), [accepted[0], accepted[2]])

        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        self.assertNotIn("cat ", section)
        self.assertNotIn("head -c", section)
        self.assertEqual(subprocess.run(["sh", "-n"], input=section, text=True,
                                        capture_output=True, timeout=2).returncode, 0)
        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "site.conf"
            config.write_text("env[PRIVATE_KEY] = SECRET_MUST_STAY_HIDDEN\n")
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
