import os
import shutil
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

    def test_readable_root_archives_report_only_actual_group_or_world_access(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            backups = root / "backups"
            backups.mkdir()
            for name in ("group copy.tar.gz", "locked.tar.gz", "other-group.zip",
                         "world-readable.txz", "ordinary.txt", "nested.tar"):
                (backups / name).write_text("fixture contents")
            (backups / "group copy.tar.gz").chmod(0o640)
            (backups / "locked.tar.gz").chmod(0o600)
            (backups / "other-group.zip").chmod(0o640)
            nested = backups / "subdir"
            nested.mkdir()
            (nested / "deep.tar.gz").write_text("fixture contents")
            (backups / "link.tar.gz").symlink_to(backups / "group copy.tar.gz")

            bin_dir = root / "bin"
            bin_dir.mkdir()
            for name in ("ls", "find", "head", "awk", "cut", "sed"):
                (bin_dir / name).symlink_to(shutil.which(name))
            self.assertFalse((bin_dir / "stat").exists())

            script = r'''
print_2title() { :; }
id() {
  case "$1" in
    -u) printf '%s\n' 1000 ;;
    -G) printf '%s\n' '1000 4242' ;;
    *) return 1 ;;
  esac
}
ls() {
  if [ "$1" = -ldn ]; then
    case "$2" in
      *'group copy.tar.gz') printf '%s\n' '-rw-r----- 1 0 4242 123 fixture' ;;
      *'locked.tar.gz') printf '%s\n' '-rw------- 1 0 4242 123 fixture' ;;
      *'other-group.zip') printf '%s\n' '-rw-r----- 1 0 8888 123 fixture' ;;
      *'world-readable.txz') printf '%s\n' '-rw-r--r-- 1 0 8888 123 fixture' ;;
      *'ordinary.txt') printf '%s\n' '-rw-r--r-- 1 0 8888 123 fixture' ;;
      *) return 1 ;;
    esac
  else
    command ls "$@"
  fi
}
stat() { return 127; }
. "$BACKUP_MODULE"
'''
            env = dict(os.environ, BACKUP_MODULE=str(self.module),
                       PSTORAGE_BACKUPS=str(backups), SEARCH_IN_FOLDER="", DEBUG="",
                       E="E", SED_RED="", PATH=str(bin_dir))
            result = subprocess.run(["/bin/sh", "-c", script], env=env,
                                    capture_output=True, text=True, check=True)
            findings = [line for line in result.stdout.splitlines()
                        if "Possible exposure:" in line]
            self.assertEqual(len(findings), 2, result.stdout)
            self.assertTrue(any("group copy.tar.gz" in line and
                                "access=group-readable" in line for line in findings))
            self.assertTrue(any("world-readable.txz" in line and
                                "access=world-readable" in line for line in findings))
            for name in ("locked.tar.gz", "other-group.zip", "ordinary.txt",
                         "nested.tar", "deep.tar.gz", "link.tar.gz"):
                self.assertFalse(any(name in line for line in findings), name)
            self.assertNotIn("fixture contents", result.stdout)


if __name__ == "__main__":
    unittest.main()
