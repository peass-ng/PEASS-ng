import shlex
import subprocess
import unittest
from pathlib import Path

import yaml


class PhpIniDirectivesTests(unittest.TestCase):
    def test_restrictive_directives_and_existing_allow_lines_are_selected(self):
        config = Path(__file__).resolve().parents[2] / "build_lists/sensitive_files.yaml"
        data = yaml.safe_load(config.read_text(encoding="utf-8"))
        section = next(item for item in data["search"] if item["name"] == "Apache-Nginx")
        php_ini = next(item["value"] for item in section["value"]["files"] if item["name"] == "php.ini")
        pattern = shlex.split(php_ini["line_grep"])
        self.assertEqual(len(pattern), 1, "builder must pass one grep expression")
        fixture = (
            "allow_url_fopen=Off\n"
            "allow_url_include=On\n"
            "disable_functions=exec,shell_exec\n"
            "open_basedir=/srv/app\n"
            ";disable_functions=commented\n"
            "app_secret=do-not-print-allow_url_fopen\n"
            "memory_limit=32M\n"
        )
        selected = subprocess.run(
            ["grep", "-E", pattern[0]], input=fixture, text=True, capture_output=True
        )
        self.assertEqual(selected.returncode, 0, selected.stderr)
        retained = subprocess.run(
            ["grep", "-Ev", php_ini["remove_regex"]],
            input=selected.stdout,
            text=True,
            capture_output=True,
        )
        self.assertEqual(retained.returncode, 0, retained.stderr)
        self.assertEqual(
            retained.stdout.splitlines(),
            [
                "allow_url_fopen=Off",
                "allow_url_include=On",
                "disable_functions=exec,shell_exec",
                "open_basedir=/srv/app",
            ],
        )
        self.assertTrue(php_ini["bad_regex"].startswith("^[[:space:]]*allow_"))
        self.assertEqual(php_ini["search_in"], ["common"])


if __name__ == "__main__":
    unittest.main()
