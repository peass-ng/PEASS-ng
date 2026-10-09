"""Regression fixtures for the bounded SQLite table preview."""

import os
import shutil
import sqlite3
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/9_interesting_files/15_Db_files.sh"


class DatabasePreviewTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.database = self.root / "user's vault.sqlite"
        connection = sqlite3.connect(str(self.database))
        connection.execute('CREATE TABLE "user\'s ""vault""" (password TEXT, note TEXT)')
        connection.executemany(
            'INSERT INTO "user\'s ""vault""" VALUES (?, ?)',
            [(f"secret-{index:02d}", "ordinary") for index in range(11)],
        )
        connection.commit()
        connection.close()

    def run_module(self, python_fallback):
        env = os.environ.copy()
        env.update({
            "MACPEAS": "", "DEBUG": "", "PSTORAGE_DATABASE": str(self.database),
            "E": "E", "GREEN": "", "NC": "", "DG": "", "BLUE": "", "SED_RED": "X",
        })
        if python_fallback:
            tools = self.root / "tools"
            tools.mkdir()
            for name in ("file", "grep", "sed", "head", "python3"):
                source = shutil.which(name)
                if not source:
                    self.skipTest(f"{name} is unavailable")
                (tools / name).symlink_to(source)
            env["PATH"] = str(tools)
        elif not shutil.which("sqlite3"):
            self.skipTest("sqlite3 is unavailable")
        command = 'print_2title() { :; }; . "$1"'
        return subprocess.run(
            ["/bin/sh", "-c", command, "sh", str(MODULE)],
            env=env, capture_output=True, text=True, timeout=10, check=True,
        )

    def assert_bounded_preview(self, result):
        self.assertIn("secret-00", result.stdout)
        self.assertIn("secret-09", result.stdout)
        self.assertNotIn("secret-10", result.stdout)
        self.assertIn("user's", result.stdout)
        self.assertNotIn("SyntaxError", result.stderr)

    def test_sqlite_cli_quotes_paths_and_limits_rows(self):
        self.assert_bounded_preview(self.run_module(python_fallback=False))

    def test_python_fallback_quotes_paths_and_limits_rows(self):
        self.assert_bounded_preview(self.run_module(python_fallback=True))


if __name__ == "__main__":
    unittest.main()
