"""The existing Supervisord content pass should expose include paths."""

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]


class SupervisordIncludeCatalogTests(unittest.TestCase):
    def test_include_path_and_existing_fields_are_shown(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        groups = [item["value"] for item in catalog["search"] if item["name"] == "Supervisord"]
        self.assertEqual(1, len(groups))
        entry = next(item["value"] for item in groups[0]["files"]
                     if item["name"] == "supervisord.conf")
        self.assertEqual(["common"], entry["search_in"])
        self.assertTrue(entry["only_bad_lines"])
        pattern = entry["bad_regex"]
        def matches(line):
            return subprocess.run(["grep", "-E", pattern], input=line + "\n", text=True,
                                  capture_output=True, timeout=2).returncode == 0
        for line in ("files = /home/operator/*.ini", "  files=/etc/supervisor/conf.d/*.conf",
                     "port=127.0.0.1:9001", "username=operator", "password=synthetic"):
            self.assertTrue(matches(line), line)
        for line in ("# files = /tmp/untrusted/*.ini", "other_files = /tmp/*.ini",
                     "files_without_equals /tmp/*.ini"):
            self.assertFalse(matches(line), line)

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
        builder._LinpeasBuilder__generate_storages()
        section = builder._LinpeasBuilder__generate_sections()["Supervisord"]
        self.assertEqual(0, subprocess.run(["sh", "-n"], input=section, text=True,
                                           capture_output=True, timeout=2).returncode)

        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "supervisord.conf"
            config.write_text("[include]\nfiles = /home/operator/*.ini\n"
                              "# files = /tmp/untrusted/*.ini\nother_files = /tmp/*.ini\n"
                              "[inet_http_server]\nport=127.0.0.1:9001\n"
                              "username=operator\npassword=synthetic\n")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_SUPERVISORD": str(config),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn("files = /home/operator/*.ini", result.stdout)
            self.assertIn("port=127.0.0.1:9001", result.stdout)
            self.assertIn("username=operator", result.stdout)
            self.assertIn("password=synthetic", result.stdout)
            self.assertNotIn("# files =", result.stdout)
            self.assertNotIn("other_files =", result.stdout)


if __name__ == "__main__":
    unittest.main()
