"""Samba policy previews and rclone paths should use cached file discovery."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]


class SambaRcloneCatalogTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        records = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())["search"]
        cls.records = {record["name"]: record["value"] for record in records}

    def test_samba_includes_and_policy_preview(self):
        samba, = self.records["Samba"]["files"]
        for directive in ("include", "force user", "wide links", "allow insecure wide links"):
            self.assertIn(directive, samba["value"]["line_grep"])

        group = self.records["Samba included configuration policies"]
        self.assertEqual(group["disable"], ["winpeas"])
        entry, = group["files"]
        self.assertEqual(entry["name"], "*.conf")
        selector = entry["value"]["check_extra_path"]
        self.assertIsNotNone(re.search(selector, "/etc/samba/shares.conf"))
        self.assertIsNotNone(re.search(selector, "/usr/local/etc/samba/extra.conf"))
        self.assertIsNone(re.search(selector, "/tmp/samba/shares.conf"))
        self.assertIsNone(re.search(selector, "/etc/samba/shares.conf.bak"))

        sys.path.insert(0, str(ROOT / "linPEAS"))
        from builder.src.linpeasBuilder import LinpeasBuilder
        from builder.src.peasLoaded import PEASLoaded

        builder = LinpeasBuilder.__new__(LinpeasBuilder)
        builder.ploaded = PEASLoaded()
        section = builder._LinpeasBuilder__generate_sections()["Samba included configuration policies"]
        main_section = builder._LinpeasBuilder__generate_sections()["Samba"]
        self.assertEqual(subprocess.run(["sh", "-n"], input=section, text=True,
                                        capture_output=True, timeout=2).returncode, 0)
        self.assertEqual(subprocess.run(["sh", "-n"], input=main_section, text=True,
                                        capture_output=True, timeout=2).returncode, 0)
        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "shares.conf"
            config.write_text("force user = backup\npassword = SECRET_VALUE\nwide links = yes\n")
            output = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_SAMBA_INCLUDED_CONFIGURATION_POLICIES": str(config),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(output.returncode, 0, output.stderr)
            self.assertIn("force user = backup", output.stdout)
            self.assertIn("wide links = yes", output.stdout)
            self.assertNotIn("SECRET_VALUE", output.stdout)

            main_config = Path(tmp) / "smb.conf"
            main_config.write_text("INCLUDE = /etc/samba/shares.conf\npassword = include-SECRET_VALUE\n")
            main_output = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; smbstatus() { :; }; " + main_section],
                env={**os.environ, "PSTORAGE_SAMBA": str(main_config), "E": "E", "SED_RED": "&",
                     "SED_GOOD": "&", "SED_RED_YELLOW": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(main_output.returncode, 0, main_output.stderr)
            self.assertIn("INCLUDE = /etc/samba/shares.conf", main_output.stdout)
            self.assertNotIn("SECRET_VALUE", main_output.stdout)

    def test_rclone_is_path_only(self):
        group = self.records["Rclone configuration candidates"]
        self.assertEqual(group["disable"], ["winpeas"])
        entry, = group["files"]
        self.assertEqual(entry["name"], "rclone.conf")
        self.assertTrue(entry["value"]["just_list_file"])
        self.assertEqual(entry["value"]["search_in"], ["common"])


if __name__ == "__main__":
    unittest.main()
