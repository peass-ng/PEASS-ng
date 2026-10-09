"""The autologin password lead must remain exact-path and metadata-only."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Autologin"
STORAGE = "PSTORAGE_AUTOLOGIN"


class AutologinPasswordCatalogTests(unittest.TestCase):
    def test_exact_path_preserves_older_names_and_hides_password(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [item for item in catalog["search"] if item["name"] == TITLE]
        self.assertEqual(record["value"]["disable"], ["winpeas"])
        files = {item["name"]: item["value"] for item in record["value"]["files"]}
        self.assertEqual(set(files), {"autologin", "autologin.conf", "passwd"})
        entry = files["passwd"]
        self.assertTrue(entry["just_list_file"])
        self.assertEqual(entry["type"], "f")
        self.assertEqual(entry["search_in"], ["${ROOT_FOLDER}etc", "${ROOT_FOLDER}mnt"])
        pattern = entry["check_extra_path"]
        for path in ("/etc/autologin/passwd",
                     "/mnt/stateful_partition/etc/autologin/passwd",
                     "/opt/autologin.conf", "/var/lib/app/autologin",
                     "/opt/autologin"):
            self.assertIsNotNone(re.search(pattern, path), path)
        for path in ("/etc/passwd", "/tmp/etc/autologin/passwd",
                     "/etc/autologin/passwd.bak", "/etc/other/passwd",
                     "/mnt/other/etc/autologin/passwd"):
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
        self.assertIn(pattern, storage)
        self.assertIn("$FIND_ETC", storage)
        self.assertIn("$FIND_MNT", storage)
        self.assertIn("$FIND_VAR", storage)
        selected = subprocess.run(
            ["sh", "-c", storage + f'\nprintf "%s\n" "${STORAGE}"'],
            env={**os.environ, "ROOT_FOLDER": "/", "FIND_ETC": "\n".join((
                "/etc/autologin/passwd", "/etc/passwd", "/etc/autologin/passwd.bak",
                "/etc/autologin.conf", "/opt/autologin")),
                "FIND_MNT": "\n".join((
                    "/mnt/stateful_partition/etc/autologin/passwd",
                    "/mnt/other/etc/autologin/passwd")),
                "FIND_VAR": "/var/lib/app/autologin.conf"},
            text=True, capture_output=True, timeout=2,
        )
        self.assertEqual(selected.returncode, 0, selected.stderr)
        self.assertEqual(set(selected.stdout.splitlines()),
                         {"/etc/autologin/passwd", "/mnt/stateful_partition/etc/autologin/passwd",
                          "/etc/autologin.conf", "/opt/autologin",
                          "/var/lib/app/autologin.conf"})

        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        passwd_line, = [line for line in section.splitlines()
                        if 'echo_not_found "passwd"' in line]
        self.assertNotIn("cat ", passwd_line)
        self.assertEqual(subprocess.run(["sh", "-n"], input=section, text=True,
                                        capture_output=True, timeout=2).returncode, 0)
        with tempfile.TemporaryDirectory() as tmp:
            password = Path(tmp) / "passwd"
            password.write_text("SECRET_MUST_STAY_HIDDEN")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + passwd_line],
                env={**os.environ, STORAGE: str(password), "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn(str(password), result.stdout)
            self.assertNotIn("SECRET_MUST_STAY_HIDDEN", result.stdout)


if __name__ == "__main__":
    unittest.main()
