"""Keep Grafana database discovery scoped and metadata-only."""

import pathlib
import re
import subprocess
import unittest

import yaml


YAML_PATH = pathlib.Path(__file__).resolve().parents[2] / "build_lists/sensitive_files.yaml"


class GrafanaSelectorTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        listing = yaml.safe_load(YAML_PATH.read_text())
        group = next(entry for entry in listing["search"] if entry["name"] == "Grafana")
        cls.files = {entry["name"]: entry["value"] for entry in group["value"]["files"]}
        cls.pattern = cls.files["grafana.db"]["check_extra_path"]

    def test_config_and_database_paths_are_preserved(self):
        paths = [
            "/etc/grafana/grafana.ini",
            "/var/lib/grafana/grafana.db",
            "/opt/grafana/data/grafana.db",
            "/srv/data/grafana/grafana.db",
            "/opt/data/grafana.db",
        ]
        for path in paths:
            with self.subTest(path=path):
                self.assertRegex(path, self.pattern)

    def test_unrelated_files_do_not_match(self):
        for path in (
            "/opt/data/another.db",
            "/var/lib/other/grafana.db",
            "/srv/app/data/grafana.db",
            "/etc/grafana/grafana.ini.bak",
        ):
            with self.subTest(path=path):
                self.assertIsNone(re.search(self.pattern, path))

    def test_database_entry_only_lists_file_metadata(self):
        db = self.files["grafana.db"]
        self.assertTrue(db["just_list_file"])
        self.assertEqual(db["type"], "f")
        self.assertNotIn("bad_regex", db)
        self.assertNotIn("line_grep", db)

    def test_posix_grep_pattern_matches_new_path(self):
        result = subprocess.run(
            ["grep", "-E", self.pattern],
            input="/opt/data/grafana.db\n/opt/data/another.db\n",
            text=True,
            capture_output=True,
            check=True,
            timeout=2,
        )
        self.assertEqual(result.stdout, "/opt/data/grafana.db\n")


if __name__ == "__main__":
    unittest.main()
