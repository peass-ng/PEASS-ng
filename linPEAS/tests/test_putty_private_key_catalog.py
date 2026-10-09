"""PuTTY private keys are path-only SSH artifacts in the shared file catalog."""

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]


class PuttyPrivateKeyCatalogTests(unittest.TestCase):
    def test_ppk_selector_uses_existing_ssh_section_without_content_reads(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        records = [item for item in catalog["search"] if item["name"] == "SSH"]
        self.assertEqual(1, len(records))
        files = {item["name"]: item["value"] for item in records[0]["value"]["files"]}
        self.assertIn("*.ppk", files)
        options = files["*.ppk"]
        self.assertTrue(options["just_list_file"])
        self.assertEqual("f", options["type"])
        self.assertEqual(["common"], options["search_in"])
        for key in ("bad_regex", "good_regex", "line_grep", "only_bad_lines"):
            self.assertNotIn(key, options)

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
        self.assertTrue(any("*.ppk" in line and " -name " in line for line in finds))
        section = builder._LinpeasBuilder__generate_sections()["SSH"]
        syntax = subprocess.run(["sh", "-n"], input=section, text=True,
                                capture_output=True, timeout=2)
        self.assertEqual(0, syntax.returncode, syntax.stderr)

        with tempfile.TemporaryDirectory() as tmp:
            key = Path(tmp) / "private.ppk"
            backup = Path(tmp) / "private.ppk.bak"
            for path in (key, backup):
                path.write_text("DO_NOT_PRINT_PRIVATE_KEY")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_SSH": "\n".join((str(key), str(backup))),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(key), result.stdout)
            self.assertNotIn(str(backup), result.stdout)
            self.assertNotIn("DO_NOT_PRINT_PRIVATE_KEY", result.stdout)


if __name__ == "__main__":
    unittest.main()
