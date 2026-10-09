"""The embedded Derby cue uses one path-only marker and no database reads."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "OFBiz embedded Derby database"


class OfbizEmbeddedDerbyCatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        matches = [item["value"] for item in catalog["search"] if item["name"] == TITLE]
        if len(matches) != 1:
            raise AssertionError("expected one OFBiz Derby group")
        cls.group = matches[0]

    def test_one_scoped_file_marker(self):
        self.assertEqual(["winpeas"], self.group["disable"])
        self.assertTrue(self.group["config"]["auto_check"])
        self.assertEqual(1, len(self.group["files"]))
        marker = self.group["files"][0]
        self.assertEqual("service.properties", marker["name"])
        value = marker["value"]
        self.assertEqual("f", value["type"])
        self.assertEqual(["common"], value["search_in"])
        self.assertTrue(value["just_list_file"])
        self.assertNotIn("bad_regex", value)
        pattern = value["check_extra_path"]
        self.assertIsNotNone(re.search(pattern,
            "/opt/ofbiz/runtime/data/derby/ofbiz/service.properties"))
        for path in (
            "/opt/ofbiz/runtime/data/derby/ofbiz/seg0/service.properties",
            "/opt/ofbiz/runtime/data/derby/ofbiz/service.properties.bak",
            "/opt/anotherapp/service.properties",
        ):
            self.assertIsNone(re.search(pattern, path), path)
        result = subprocess.run(["grep", "-E", pattern],
            input="/opt/ofbiz/runtime/data/derby/ofbiz/service.properties\n"
                  "/opt/anotherapp/service.properties\n",
            text=True, capture_output=True, check=True, timeout=2)
        self.assertEqual("/opt/ofbiz/runtime/data/derby/ofbiz/service.properties\n",
                         result.stdout)

    def test_generated_section_lists_marker_without_reading_database(self):
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
        storage = builder._LinpeasBuilder__generate_storages()
        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        assignment = next(item for item in storage if item.startswith(
            "PSTORAGE_OFBIZ_EMBEDDED_DERBY_DATABASE="))
        self.assertEqual(0, subprocess.run(["sh", "-n"], input=section,
            text=True, capture_output=True, timeout=2).returncode)
        for forbidden in ("cat ", "ls -lRA", "find \"$f\"", "ij "):
            self.assertNotIn(forbidden, section)
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            marker = root / "opt/ofbiz/runtime/data/derby/ofbiz/service.properties"
            ignored = root / "opt/other/service.properties"
            marker.parent.mkdir(parents=True)
            ignored.parent.mkdir(parents=True)
            marker.write_text("PRIVATE_DATABASE_MARKER")
            ignored.write_text("UNRELATED")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + assignment + "\n" + section],
                env={**os.environ, "FIND_VAR": str(marker) + "\n" + str(ignored),
                     "ROOT_FOLDER": str(root) + "/", "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(marker), result.stdout)
            self.assertNotIn(str(ignored), result.stdout)
            self.assertNotIn("PRIVATE_DATABASE_MARKER", result.stdout)
            self.assertNotIn("UNRELATED", result.stdout)


if __name__ == "__main__":
    unittest.main()
