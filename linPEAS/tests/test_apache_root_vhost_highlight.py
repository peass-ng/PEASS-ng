"""Apache's cached vhost display highlights explicit process identities."""

import subprocess
import sys
import unittest
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[2]


class ApacheRootVhostHighlightTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        catalog = yaml.safe_load((ROOT / "build_lists/sensitive_files.yaml").read_text())
        record, = [item for item in catalog["search"] if item["name"] == "Apache-Nginx"]
        files = record["value"]["files"]
        nested = files[0]["value"]["files"][0]["value"]
        direct = next(item["value"] for item in files
                      if item["name"] == "000-default.conf")
        cls.records = ((nested["bad_regex"], nested["remove_regex"]),
                       (direct["bad_regex"], direct["remove_regex"]))

    def colorized(self, pattern, remove, line):
        # The generator places this YAML regex inside shell double quotes,
        # which reduces its doubled backslash before grep receives it.
        remove = remove.replace("\\\\", "\\")
        filtered = subprocess.run(
            ["grep", "-Ev", remove], input=line + "\n", text=True,
            capture_output=True, timeout=2,
        )
        self.assertIn(filtered.returncode, (0, 1), filtered.stderr)
        result = subprocess.run(
            ["sed", "-E", "s," + pattern + ",[IDENTITY-CUE],g"],
            input=filtered.stdout, text=True, capture_output=True, timeout=2,
        )
        self.assertEqual(0, result.returncode, result.stderr)
        return result.stdout.strip()

    def test_active_identity_directives_and_existing_highlight(self):
        for pattern, remove in self.records:
            for line in ("AssignUserId root root", "AssignUserID 0 0",
                         "assignuserid root 0", "  ASSIGNUSERID 0 root  ",
                         "AssignUserId root staff", "AssignUserId app root",
                         "AssignUserID app_user app-group"):
                with self.subTest(line=line):
                    self.assertIn("[IDENTITY-CUE]", self.colorized(pattern, remove, line))
            for line in ("# AssignUserId app staff", "  # AssignUserID 0 0",
                         "NotAssignUserId root root", "AssignUserID app",
                         "AssignUserID app/group staff"):
                with self.subTest(line=line):
                    self.assertNotIn("[IDENTITY-CUE]", self.colorized(pattern, remove, line))
            self.assertEqual("", self.colorized(pattern, remove, "# AssignUserID app staff"))
            self.assertIn("[IDENTITY-CUE]", self.colorized(pattern, remove, "ServerName local.example"))

    def test_generated_section_preserves_cached_vhost_inventory(self):
        sys.path.insert(0, str(ROOT / "linPEAS"))
        from builder.src.linpeasBuilder import LinpeasBuilder
        from builder.src.peasLoaded import PEASLoaded

        builder = LinpeasBuilder.__new__(LinpeasBuilder)
        builder.ploaded = PEASLoaded()
        section = builder._LinpeasBuilder__generate_sections()["Apache-Nginx"]
        self.assertIn("PSTORAGE_APACHE_NGINX", section)
        self.assertIn('grep -E "sites-enabled$"', section)
        self.assertIn('grep -E "000-default\\.conf$"', section)
        for pattern, remove in self.records:
            self.assertIn(pattern, section)
            self.assertIn('grep -Ev "' + remove + '" | sed -${E} ', section)
        syntax = subprocess.run(["sh", "-n"], input=section, text=True,
                                capture_output=True, timeout=2)
        self.assertEqual(0, syntax.returncode, syntax.stderr)


if __name__ == "__main__":
    unittest.main()
