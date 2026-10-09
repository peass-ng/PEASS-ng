"""Kerberos target-account authorization files should be surfaced without reading them."""

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
MODULE = ROOT / "linPEAS/builder/linpeas_parts/7_software_information/Kerberos.sh"


class KerberosTargetAuthorizationCatalogTests(unittest.TestCase):
    def test_k5users_reuses_cached_selector_and_prints_metadata_only(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [item["value"] for item in catalog["search"] if item["name"] == "Kerberos"]
        files = {item["name"]: item["value"] for item in record["files"]}
        self.assertFalse(record["config"]["auto_check"])
        for name in (".k5login", ".k5users"):
            self.assertEqual("f", files[name]["type"])
            self.assertEqual(["common"], files[name]["search_in"])

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
                       if line.startswith("PSTORAGE_KERBEROS="))
        self.assertIn('\\.k5login$', storage)
        self.assertIn('\\.k5users$', storage)

        with tempfile.TemporaryDirectory() as tmp:
            target = Path(tmp) / ".k5users"
            target.write_text("SENSITIVE_PRINCIPAL_DO_NOT_PRINT\n")
            other = Path(tmp) / ".k5users.bak"
            other.write_text("irrelevant\n")
            selected = subprocess.run(
                ["sh", "-c", storage + '\nprintf "%s\\n" "$PSTORAGE_KERBEROS"'],
                env={**os.environ, "ROOT_FOLDER": "", "GREPHOMESEARCH": tmp,
                     "FIND_HOMESEARCH": "\n".join((str(target), str(other)))},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, selected.returncode, selected.stderr)
            self.assertEqual(str(target), selected.stdout.strip())

            module = MODULE.read_text()
            self.assertIn('while IFS= read -r f; do', module)
            self.assertIn('if [ "${f##*/}" = ".k5users" ] && [ -f "$f" ]; then', module)
            self.assertIn('if [ "${f##*/}" = ".k5login" ]; then', module)
            self.assertEqual(0, subprocess.run(["sh", "-n", str(MODULE)],
                           capture_output=True, timeout=2).returncode)
            output = subprocess.run(
                ["sh", "-c", 'print_2title() { :; }; print_info() { :; }; . "$MODULE"'],
                env={**os.environ, "MODULE": str(MODULE), "PSTORAGE_KERBEROS": str(target),
                     "DEBUG": "", "E": "E", "SED_RED": "&", "SED_GREEN": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, output.returncode, output.stderr)
            self.assertIn(str(target), output.stdout)
            self.assertNotIn("SENSITIVE_PRINCIPAL_DO_NOT_PRINT", output.stdout)

            nested = Path(tmp) / ".k5login"
            nested.mkdir()
            nested_users = nested / ".k5users"
            nested_users.write_text("NESTED_PRINCIPAL_DO_NOT_PRINT\n")
            nested_output = subprocess.run(
                ["sh", "-c", 'print_2title() { :; }; print_info() { :; }; . "$MODULE"'],
                env={**os.environ, "MODULE": str(MODULE),
                     "PSTORAGE_KERBEROS": str(nested_users),
                     "DEBUG": "", "E": "E", "SED_RED": "&", "SED_GREEN": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, nested_output.returncode, nested_output.stderr)
            self.assertIn(str(nested_users), nested_output.stdout)
            self.assertNotIn("NESTED_PRINCIPAL_DO_NOT_PRINT", nested_output.stdout)

            backup = Path(tmp) / ".k5login.bak"
            backup.write_text("BACKUP_PRINCIPAL_DO_NOT_PRINT\n")
            backup_output = subprocess.run(
                ["sh", "-c", 'print_2title() { :; }; print_info() { :; }; . "$MODULE"'],
                env={**os.environ, "MODULE": str(MODULE),
                     "PSTORAGE_KERBEROS": str(backup),
                     "DEBUG": "", "E": "E", "SED_RED": "&", "SED_GREEN": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, backup_output.returncode, backup_output.stderr)
            self.assertNotIn("BACKUP_PRINCIPAL_DO_NOT_PRINT", backup_output.stdout)

            login_dir = Path(tmp) / "account"
            login_dir.mkdir()
            login = login_dir / ".k5login"
            login.write_text("EXPECTED_LOGIN_PRINCIPAL\n")
            login_output = subprocess.run(
                ["sh", "-c", 'print_2title() { :; }; print_info() { :; }; . "$MODULE"'],
                env={**os.environ, "MODULE": str(MODULE),
                     "PSTORAGE_KERBEROS": str(login),
                     "DEBUG": "", "E": "E", "SED_RED": "&", "SED_GREEN": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, login_output.returncode, login_output.stderr)
            self.assertIn("EXPECTED_LOGIN_PRINCIPAL", login_output.stdout)

            spaced_dir = Path(tmp) / "account with space"
            spaced_dir.mkdir()
            spaced_users = spaced_dir / ".k5users"
            spaced_users.write_text("SPACED_PRINCIPAL_DO_NOT_PRINT\n")
            spaced_output = subprocess.run(
                ["sh", "-c", 'print_2title() { :; }; print_info() { :; }; . "$MODULE"'],
                env={**os.environ, "MODULE": str(MODULE),
                     "PSTORAGE_KERBEROS": str(spaced_users),
                     "DEBUG": "", "E": "E", "SED_RED": "&", "SED_GREEN": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, spaced_output.returncode, spaced_output.stderr)
            self.assertIn(str(spaced_users), spaced_output.stdout)
            self.assertNotIn("SPACED_PRINCIPAL_DO_NOT_PRINT", spaced_output.stdout)

            missing_output = subprocess.run(
                ["sh", "-c", 'print_2title() { :; }; print_info() { :; }; . "$MODULE"'],
                env={**os.environ, "MODULE": str(MODULE),
                     "PSTORAGE_KERBEROS": str(Path(tmp) / "missing" / ".k5users"),
                     "DEBUG": "", "E": "E", "SED_RED": "&", "SED_GREEN": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, missing_output.returncode, missing_output.stderr)
            self.assertNotIn(".k5users authorization file", missing_output.stdout)


if __name__ == "__main__":
    unittest.main()
