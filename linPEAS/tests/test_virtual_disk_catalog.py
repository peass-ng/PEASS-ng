"""VirtualBox artifacts are listed through the existing virtual-disk inventory."""

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]


class VirtualDiskCatalogTests(unittest.TestCase):
    def test_virtualbox_files_are_path_only_and_exact_extension(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        records = [item for item in catalog["search"] if item["name"] == "Virtual Disks"]
        self.assertEqual(1, len(records))
        files = {item["name"]: item["value"] for item in records[0]["value"]["files"]}
        for name in ("*.vbox", "*.vdi"):
            with self.subTest(name=name):
                self.assertIn(name, files)
                self.assertTrue(files[name]["just_list_file"])
                self.assertEqual("f", files[name]["type"])
                self.assertEqual(["common"], files[name]["search_in"])
                for key in ("bad_regex", "good_regex", "line_grep", "only_bad_lines"):
                    self.assertNotIn(key, files[name])

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
        for name in ("*.vbox", "*.vdi"):
            self.assertTrue(any(name in line and " -name " in line for line in finds))
        section = builder._LinpeasBuilder__generate_sections()["Virtual Disks"]
        syntax = subprocess.run(["sh", "-n"], input=section, text=True,
                                capture_output=True, timeout=2)
        self.assertEqual(0, syntax.returncode, syntax.stderr)

        with tempfile.TemporaryDirectory() as tmp:
            paths = [Path(tmp) / name for name in ("guest.vbox", "guest.vdi", "guest.vdi.bak")]
            for path in paths:
                path.write_text("DO_NOT_PRINT_DISK_CONTENT")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_VIRTUAL_DISKS": "\n".join(map(str, paths)),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(paths[0]), result.stdout)
            self.assertIn(str(paths[1]), result.stdout)
            self.assertNotIn(str(paths[2]), result.stdout)
            self.assertNotIn("DO_NOT_PRINT_DISK_CONTENT", result.stdout)


if __name__ == "__main__":
    unittest.main()
