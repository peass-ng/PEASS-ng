"""PostgreSQL NSS configuration leads stay exact and metadata-only."""

import os
import re
import subprocess
import sys
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "PostgreSQL NSS configuration"
STORAGE = "PSTORAGE_POSTGRESQL_NSS_CONFIGURATION"


class NssPgsqlConfigCatalogTests(unittest.TestCase):
    def test_exact_live_and_offline_paths_without_config_content(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [entry for entry in catalog["search"] if entry["name"] == TITLE]
        self.assertTrue(record["value"]["config"]["auto_check"])
        entries = {entry["name"]: entry["value"] for entry in record["value"]["files"]}
        names = {"nss-pgsql.conf", "nss-pgsql-root.conf"}
        self.assertEqual(names, set(entries))
        for name, value in entries.items():
            self.assertEqual("f", value["type"])
            self.assertEqual(["${ROOT_FOLDER}etc"], value["search_in"])
            self.assertTrue(value["just_list_file"])
            self.assertIsNotNone(re.search(value["check_extra_path"], "/etc/" + name))
            self.assertIsNotNone(re.search(value["check_extra_path"], "/mnt/image/etc/" + name))
            for false_path in ("/etc/other/" + name, "/etc/" + name + ".bak"):
                self.assertIsNone(re.search(value["check_extra_path"], false_path))

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
        for name in names:
            self.assertTrue(any(name in line and "${ROOT_FOLDER}etc" in line
                                for line in finds))
        storages = builder._LinpeasBuilder__generate_storages()
        storage = next(line for line in storages if line.startswith(STORAGE + "="))
        self.assertIn("$FIND_ETC", storage)
        for root_prefix in ("", "/mnt/image"):
            with self.subTest(root_prefix=root_prefix):
                expected = {root_prefix + "/etc/" + name for name in names}
                candidates = (*expected,
                              root_prefix + "/etc/other/nss-pgsql.conf",
                              root_prefix + "/etc/nss-pgsql.conf.bak",
                              root_prefix + "/etc/nss-pgsql-root.conf.old",
                              "/tmp/etc/nss-pgsql.conf")
                selected = subprocess.run(
                    ["sh", "-c", storage + f'\nprintf "%s\\n" "${STORAGE}"'],
                    env={**os.environ, "ROOT_FOLDER": root_prefix + "/",
                         "FIND_ETC": "\n".join(candidates)},
                    text=True, capture_output=True, timeout=2,
                )
                self.assertEqual(0, selected.returncode, selected.stderr)
                self.assertEqual(expected, set(selected.stdout.splitlines()))

        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        self.assertNotIn('cat "$f"', section)
        syntax = subprocess.run(["sh", "-n"], input=section, text=True,
                                capture_output=True, timeout=2)
        self.assertEqual(0, syntax.returncode, syntax.stderr)


if __name__ == "__main__":
    unittest.main()
