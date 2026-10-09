"""Proxmox host archives are scoped inventory leads, never archive reads."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Proxmox VE host backups"


class ProxmoxHostBackupCatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        matches = [item["value"] for item in catalog["search"] if item["name"] == TITLE]
        if len(matches) != 1:
            raise AssertionError("expected one Proxmox backup group")
        cls.group = matches[0]

    def test_exact_scoped_path_only_selectors(self):
        self.assertEqual(["winpeas"], self.group["disable"])
        self.assertTrue(self.group["config"]["auto_check"])
        cases = {
            "pve-host-*.tar.gz": (
                "/var/backups/pve-host-2023_04_15-16_09_46.tar.gz",
                ("/tmp/pve-host-2023_04_15-16_09_46.tar.gz",
                 "/var/backups/pve-host-latest.tar.gz.old"),
            ),
            "proxmox_backup_*.tar.gz": (
                "/var/backups/proxmox_backup_node_2023-04-15.15.36.28.tar.gz",
                ("/tmp/proxmox_backup_node_2023-04-15.15.36.28.tar.gz",
                 "/var/backups/proxmox_backup_node_2023-04-15.15.36.28.tar.gz.old"),
            ),
        }
        self.assertEqual(set(cases), {item["name"] for item in self.group["files"]})
        for item in self.group["files"]:
            positive, negatives = cases[item["name"]]
            value = item["value"]
            self.assertEqual("f", value["type"])
            self.assertEqual(["common"], value["search_in"])
            self.assertTrue(value["just_list_file"])
            self.assertNotIn("line_grep", value)
            pattern = value["check_extra_path"]
            self.assertIsNotNone(re.search(pattern, positive))
            if item["name"] == "pve-host-*.tar.gz":
                self.assertIsNotNone(re.search(pattern, "/var/backups/pve-host-latest.tar.gz"))
            for negative in negatives:
                self.assertIsNone(re.search(pattern, negative), negative)
            result = subprocess.run(
                ["grep", "-E", pattern], input="\n".join((positive, *negatives)) + "\n",
                text=True, capture_output=True, check=True, timeout=2,
            )
            self.assertEqual(positive + "\n", result.stdout)

    def test_generated_section_lists_only_paths(self):
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
            "PSTORAGE_PROXMOX_VE_HOST_BACKUPS="))
        self.assertEqual(0, subprocess.run(
            ["sh", "-n"], input=section, text=True, capture_output=True, timeout=2,
        ).returncode)
        self.assertNotIn("cat ", section)
        self.assertNotIn("tar ", section)
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            archive = root / "var/backups/pve-host-2023_04_15-16_09_46.tar.gz"
            ignored = root / "tmp/pve-host-2023_04_15-16_09_46.tar.gz"
            archive.parent.mkdir(parents=True)
            ignored.parent.mkdir(parents=True)
            archive.write_text("SECRET_INSIDE_ARCHIVE")
            ignored.write_text("UNRELATED_CONTENT")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + assignment + "\n" + section],
                env={**os.environ, "FIND_VAR": str(archive) + "\n" + str(ignored),
                     "ROOT_FOLDER": str(root) + "/", "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(archive), result.stdout)
            self.assertNotIn(str(ignored), result.stdout)
            self.assertNotIn("SECRET_INSIDE_ARCHIVE", result.stdout)
            self.assertNotIn("UNRELATED_CONTENT", result.stdout)


if __name__ == "__main__":
    unittest.main()
