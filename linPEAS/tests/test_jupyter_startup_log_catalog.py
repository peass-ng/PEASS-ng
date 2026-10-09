"""Jupyter startup logs should be listed from the shared cache, never read."""

import os
import re
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]
TITLE = "Jupyter startup log candidates"


class JupyterStartupLogCatalogTests(unittest.TestCase):
    def test_scoped_path_only_selector(self):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [item for item in catalog["search"] if item["name"] == TITLE]
        value = record["value"]
        self.assertEqual(value["disable"], ["winpeas"])
        self.assertTrue(value["config"]["auto_check"])
        entry, = value["files"]
        self.assertEqual(entry["name"], "jupyter-*.log")
        self.assertEqual(entry["value"]["search_in"], ["common"])
        self.assertEqual(entry["value"]["type"], "f")
        self.assertTrue(entry["value"]["just_list_file"])
        pattern = entry["value"]["check_extra_path"]
        self.assertIsNotNone(re.search(pattern, "/opt/notebooks/logs/jupyter-2024-01-01.log"))
        for path in ("/opt/logs/jupyter.log", "/tmp/jupyter-2024-01-01.log",
                     "/opt/logs/other-2024-01-01.log"):
            self.assertIsNone(re.search(pattern, path))

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
        self.assertTrue(any("jupyter-*.log" in find for find in finds))
        storage = next(line for line in builder._LinpeasBuilder__generate_storages()
                       if line.startswith("PSTORAGE_JUPYTER_STARTUP_LOG_CANDIDATES="))
        self.assertIn(pattern, storage)
        candidates = ("/opt/notebooks/logs/jupyter-2024-01-01.log",
                      "/opt/notebooks/logs/jupyter.log",
                      "/opt/notebooks/jupyter-2024-01-01.log",
                      "/opt/notebooks/logs/jupyter-2024-01-01.txt")
        selected = subprocess.run(
            ["sh", "-c", storage + '\nprintf "%s\\n" "$PSTORAGE_JUPYTER_STARTUP_LOG_CANDIDATES"'],
            env={**os.environ, "ROOT_FOLDER": "/", "FIND_OPT": "\n".join(candidates)},
            text=True, capture_output=True, timeout=2,
        )
        self.assertEqual(selected.returncode, 0, selected.stderr)
        self.assertEqual(selected.stdout.strip(), candidates[0])
        section = builder._LinpeasBuilder__generate_sections()[TITLE]
        self.assertEqual(subprocess.run(["sh", "-n"], input=section, text=True,
                                        capture_output=True, timeout=2).returncode, 0)
        self.assertNotIn("cat ", section)

        with tempfile.TemporaryDirectory() as tmp:
            log = Path(tmp) / "jupyter-2024-01-01.log"
            log.write_text("DO_NOT_PRINT_TOKEN")
            result = subprocess.run(
                ["sh", "-c", "print_2title() { :; }; " + section],
                env={**os.environ, "PSTORAGE_JUPYTER_STARTUP_LOG_CANDIDATES": str(log),
                     "E": "E", "SED_RED": "&"},
                text=True, capture_output=True, timeout=2,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn(str(log), result.stdout)
            self.assertNotIn("DO_NOT_PRINT_TOKEN", result.stdout)


if __name__ == "__main__":
    unittest.main()
