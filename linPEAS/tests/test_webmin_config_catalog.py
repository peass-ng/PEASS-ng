"""Webmin administration leads use exact cached paths and metadata only."""

import os
import re
import subprocess
import sys
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Webmin administration configuration"
STORAGE = "PSTORAGE_WEBMIN_ADMINISTRATION_CONFIGURATION"


class WebminConfigCatalogTests(unittest.TestCase):
    def test_exact_cached_paths_and_metadata_only_output(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [entry for entry in catalog["search"] if entry["name"] == TITLE]
        self.assertTrue(record["value"]["config"]["auto_check"])
        entries = {entry["name"]: entry["value"] for entry in record["value"]["files"]}
        exact_paths = ("/etc/webmin/webmin.acl", "/etc/webmin/miniserv.conf")
        self.assertEqual(set(entries), {"webmin.acl", "miniserv.conf"})
        for name, exact in zip(entries, exact_paths):
            value = entries[name]
            self.assertEqual("f", value["type"])
            self.assertEqual(["${ROOT_FOLDER}etc"], value["search_in"])
            self.assertTrue(value["just_list_file"])
            self.assertIsNotNone(re.search(value["check_extra_path"], exact))
            self.assertIsNotNone(re.search(value["check_extra_path"], "/mnt/image" + exact))
            for lookalike in (f"/etc/other/{name}", f"/etc/webmin/{name}.bak"):
                self.assertIsNone(re.search(value["check_extra_path"], lookalike))

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
        self.assertTrue(any("webmin.acl" in line and "${ROOT_FOLDER}etc" in line
                            for line in finds))
        storages = builder._LinpeasBuilder__generate_storages()
        storage = next(line for line in storages if line.startswith(STORAGE + "="))
        self.assertIn("$FIND_ETC", storage)
        for root_prefix in ("", "/mnt/image"):
            with self.subTest(root_prefix=root_prefix):
                expected = {root_prefix + path for path in exact_paths}
                candidates = (*expected, root_prefix + "/etc/other/webmin.acl",
                              root_prefix + "/etc/webmin/webmin.acl.bak",
                              "/tmp/etc/webmin/miniserv.conf")
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
        self.assertNotIn("grep -E 'package-updates'", section)
        syntax = subprocess.run(["sh", "-n"], input=section, text=True,
                                capture_output=True, timeout=2)
        self.assertEqual(0, syntax.returncode, syntax.stderr)


if __name__ == "__main__":
    unittest.main()
