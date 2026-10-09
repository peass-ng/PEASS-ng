"""Git repository directories are path-only leads; worktree files remain visible."""

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]


class GitRepositoryMetadataCatalogTests(unittest.TestCase):
    def test_directory_and_worktree_selectors_remain_distinct_and_bounded(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        records = {item["name"]: item["value"] for item in catalog["search"]}
        metadata = records["Git repository metadata"]
        directory = metadata["files"][0]
        self.assertEqual(".git", directory["name"])
        self.assertEqual("d", directory["value"]["type"])
        self.assertEqual(["common"], directory["value"]["search_in"])
        self.assertFalse(directory["value"].get("just_list_file", False))

        github = {item["name"]: item["value"] for item in records["Github"]["files"]}
        self.assertEqual("f", github[".git"]["type"])
        self.assertTrue(github[".git"]["just_list_file"])

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
        self.assertTrue(any(' -type d ' in line and '.git' in line for line in finds))
        self.assertTrue(any(' -type d ' not in line and '.git' in line for line in finds))
        storages = builder._LinpeasBuilder__generate_storages()
        git_storage = next(line for line in storages if line.startswith("PSTORAGE_GIT_REPOSITORY_METADATA="))
        self.assertIn("head -n 70", git_storage)

        section = builder._LinpeasBuilder__generate_sections()["Git repository metadata"]
        self.assertNotIn("ls -lRA", section)
        self.assertNotIn("cat ", section)
        self.assertNotIn("find \"$f\"", section)
        syntax = subprocess.run(["sh", "-n"], input=section, text=True,
                                capture_output=True, timeout=2)
        self.assertEqual(0, syntax.returncode, syntax.stderr)

        with tempfile.TemporaryDirectory() as tmp:
            repo = Path(tmp) / "repo" / ".git"
            repo.mkdir(parents=True)
            (repo / "objects").mkdir()
            (repo / "objects" / "secret").write_text("DO_NOT_PRINT_GIT_OBJECT")
            worktree = Path(tmp) / "worktree"
            worktree.mkdir()
            pointer = worktree / ".git"
            pointer.write_text("gitdir: DO_NOT_PRINT_POINTER")
            directories = subprocess.run(
                ["find", tmp, "-type", "d", "-name", ".git"],
                text=True, capture_output=True, timeout=2, check=True,
            ).stdout.splitlines()
            files = subprocess.run(
                ["find", tmp, "-type", "f", "-name", ".git"],
                text=True, capture_output=True, timeout=2, check=True,
            ).stdout.splitlines()
            self.assertEqual([str(repo)], directories)
            self.assertEqual([str(pointer)], files)

            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_GIT_REPOSITORY_METADATA": str(repo),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(repo), result.stdout)
            self.assertNotIn("DO_NOT_PRINT_GIT_OBJECT", result.stdout)
            self.assertNotIn("objects", result.stdout)

            original = builder._LinpeasBuilder__generate_sections()["Github"]
            worktree_result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + original],
                env={**os.environ, "PSTORAGE_GITHUB": str(pointer),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(0, worktree_result.returncode, worktree_result.stderr)
            self.assertIn(str(pointer), worktree_result.stdout)
            self.assertNotIn("DO_NOT_PRINT_POINTER", worktree_result.stdout)


if __name__ == "__main__":
    unittest.main()
