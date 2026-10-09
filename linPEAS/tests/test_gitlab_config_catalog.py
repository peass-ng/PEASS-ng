"""GitLab config discovery and preview stay cached, bounded, and passive."""

import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
MODULE = ROOT / "linPEAS/builder/linpeas_parts/7_software_information/Gitlab.sh"


class GitLabConfigCatalogTests(unittest.TestCase):
    def test_cached_selector_keeps_existing_names_and_finds_rb(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [item for item in catalog["search"] if item["name"] == "GitLab"]
        self.assertFalse(record["value"]["config"]["auto_check"])
        names = {entry["name"] for entry in record["value"]["files"]}
        self.assertEqual({"secrets.yml", "gitlab.yml", "gitlab.rb"}, names)

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
                       if line.startswith("PSTORAGE_GITLAB="))
        self.assertIn("$FIND_OPT", storage)
        candidates = ["/opt/backup/gitlab.rb", "/etc/gitlab/secrets.yml",
                      "/etc/gitlab/gitlab.yml", "/opt/backup/gitlab.rm",
                      "/opt/backup/gitlab.rb.old"]
        result = subprocess.run(
            ["sh", "-c", storage + '\nprintf "%s\\n" "$PSTORAGE_GITLAB"'],
            env={**os.environ, "ROOT_FOLDER": "/", "FIND_OPT": "\n".join(candidates),
                 "FIND_ETC": "\n".join(candidates)},
            capture_output=True, text=True, timeout=3,
        )
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual(set(candidates[:3]), set(result.stdout.splitlines()))

    def run_module(self, path):
        return subprocess.run(
            ["sh", "-c", 'print_2title() { :; }; . "$GITLAB_MODULE"'],
            env={**os.environ, "GITLAB_MODULE": str(MODULE),
                 "PSTORAGE_GITLAB": str(path), "DEBUG": "", "E": "E",
                 "SED_RED": "&"},
            capture_output=True, text=True, timeout=3,
        )

    def test_regular_preview_is_line_and_width_bounded(self):
        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "gitlab.rb"
            config.write_text("# hidden\n" + "# common setting\n" * 9000 +
                              "unrelated = 1\n" * 45 +
                              "smtp_password = 'CANDIDATE'\n" +
                              "user = '" + "x" * 300 + "'\n" +
                              "smtp_port = 25\n" * 45)
            result = self.run_module(config)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn(str(config), result.stdout)
            self.assertIn("CANDIDATE", result.stdout)
            self.assertNotIn("# hidden", result.stdout)
            self.assertNotIn("x" * 300, result.stdout)
            self.assertNotIn("unrelated = 1", result.stdout)
            self.assertLessEqual(result.stdout.count("smtp_port = 25"), 39)

    def test_yaml_repository_preview_uses_the_file_and_stays_bounded(self):
        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "gitlab.yml"
            config.write_text(
                "unrelated: hidden\nrepositories:\n"
                "  storages:\n    default:\n      path: /var/opt/gitlab-data\n"
                "  more: " + "x" * 300 + "\n"
                "private: DO_NOT_PRINT\n")
            result = self.run_module(config)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn("repositories:", result.stdout)
            self.assertIn("/var/opt/gitlab-data", result.stdout)
            self.assertNotIn("DO_NOT_PRINT", result.stdout)
            self.assertNotIn("x" * 300, result.stdout)

            config.write_text("SECRET_OVERSIZE\n" + "x" * 262145)
            result = self.run_module(config)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn("larger than 256 KiB", result.stdout)
            self.assertNotIn("SECRET_OVERSIZE", result.stdout)

            config.unlink()
            target = Path(tmp) / "target"
            target.write_text("repositories:\n  SECRET_SYMLINK\n")
            config.symlink_to(target)
            result = self.run_module(config)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn("symlink", result.stdout)
            self.assertNotIn("SECRET_SYMLINK", result.stdout)

    def test_oversize_and_symlink_contents_are_not_read(self):
        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "gitlab.rb"
            config.write_text("SECRET_OVERSIZE\n" + "x" * 262145)
            result = self.run_module(config)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn("larger than 256 KiB", result.stdout)
            self.assertNotIn("SECRET_OVERSIZE", result.stdout)

            config.unlink()
            target = Path(tmp) / "target"
            target.write_text("SECRET_SYMLINK")
            config.symlink_to(target)
            result = self.run_module(config)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertIn("symlink", result.stdout)
            self.assertNotIn("SECRET_SYMLINK", result.stdout)


if __name__ == "__main__":
    unittest.main()
