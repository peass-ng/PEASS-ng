import os
import subprocess
import tempfile
import unittest
from pathlib import Path

import yaml


class BackupFoldersTests(unittest.TestCase):
    def setUp(self):
        self.repo_root = Path(__file__).resolve().parents[2]
        self.module = self.repo_root / "linPEAS/builder/linpeas_parts/9_interesting_files/13_Backup_folders.sh"

    def test_backup_names_keep_file_and_directory_searches(self):
        config = yaml.safe_load((self.repo_root / "build_lists/sensitive_files.yaml").read_text())
        backup = next(record for record in config["search"] if record["name"] == "Backups")
        types = {}
        for record in backup["value"]["files"]:
            types.setdefault(record["name"], set()).add(record["value"]["type"])
        self.assertEqual(types["backup"], {"d", "f"})
        self.assertEqual(types["backups"], {"d", "f"})
        self.assertEqual(types["npbackup.conf"], {"f"})

    def test_builder_emits_both_find_paths_for_backup_names(self):
        with tempfile.TemporaryDirectory() as temp:
            output_path = Path(temp) / "linpeas-backup.sh"
            subprocess.run(
                ["python3", "-m", "builder.linpeas_builder", "--include",
                 "IF_Backup_folders", "--output", str(output_path)],
                cwd=self.repo_root / "linPEAS", capture_output=True, text=True,
                check=True,
            )
            lines = output_path.read_text().splitlines()
            directory_find = next(line for line in lines if "FIND_DIR_VAR=" in line)
            file_find = next(line for line in lines if "FIND_VAR=" in line)
            storage = next(line for line in lines if "PSTORAGE_BACKUPS=" in line)
            for name in ("backup", "backups"):
                self.assertIn(f'-name \\"{name}\\"', directory_find)
                self.assertIn(f'-name \\"{name}\\"', file_find)
            self.assertIn("$FIND_DIR_VAR", storage)
            self.assertIn("$FIND_VAR", storage)

    def run_module(self, paths):
        script = 'print_2title() { :; }; . "$BACKUP_MODULE"'
        env = dict(os.environ, BACKUP_MODULE=str(self.module),
                   PSTORAGE_BACKUPS="\n".join(map(str, paths)),
                   SEARCH_IN_FOLDER="", DEBUG="", E="E", SED_RED="")
        result = subprocess.run(["sh", "-c", script], env=env,
                                capture_output=True, text=True, check=True)
        return result.stdout

    def test_lists_immediate_metadata_and_skips_exact_name_file(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            backup_dir = root / "backup"
            backup_dir.mkdir()
            archive = backup_dir / "opaque.zip.aes"
            archive.write_text("private fixture contents")
            nested = backup_dir / "nested"
            nested.mkdir()
            (nested / "deep.txt").write_text("nested fixture contents")
            exact_file = root / "backups"
            exact_file.write_text("exact-name file contents")

            output = self.run_module([backup_dir, exact_file])
            self.assertEqual(output.count(str(archive)), 1)
            self.assertIn(str(nested), output)
            self.assertNotIn("deep.txt", output)
            self.assertNotIn(str(exact_file), output)
            self.assertNotIn("fixture contents", output)

    def test_caps_each_directory_at_thirty_entries(self):
        with tempfile.TemporaryDirectory() as temp:
            backups = Path(temp) / "backups"
            backups.mkdir()
            for index in range(40):
                (backups / f"item{index:02d}").touch()

            output = self.run_module([backups])
            entries = [line for line in output.splitlines()
                       if f"{backups}/item" in line]
            self.assertEqual(len(entries), 30)


if __name__ == "__main__":
    unittest.main()
