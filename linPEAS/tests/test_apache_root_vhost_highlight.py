"""Apache's existing cached vhost display highlights only exact root identities."""

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
        cls.nested = files[0]["value"]["files"][0]["value"]["bad_regex"]
        cls.direct = next(item["value"]["bad_regex"] for item in files
                          if item["name"] == "000-default.conf")

    def colorized(self, pattern, line):
        result = subprocess.run(
            ["sed", "-E", "s," + pattern + ",[ROOT-CUE],g"],
            input=line + "\n", text=True, capture_output=True, timeout=2,
        )
        self.assertEqual(0, result.returncode, result.stderr)
        return result.stdout.strip()

    def test_root_identity_boundaries_and_existing_highlight(self):
        for pattern in (self.nested, self.direct):
            for line in ("AssignUserId root root", "AssignUserID 0 0",
                         "assignuserid root 0", "  ASSIGNUSERID 0 root  "):
                with self.subTest(line=line):
                    self.assertIn("[ROOT-CUE]", self.colorized(pattern, line))
            for line in ("AssignUserId root staff", "AssignUserId app root",
                         "AssignUserId root rootish", "AssignUserId root 00",
                         "NotAssignUserId root root"):
                with self.subTest(line=line):
                    self.assertNotIn("[ROOT-CUE]", self.colorized(pattern, line))
            self.assertIn("[ROOT-CUE]", self.colorized(pattern, "ServerName local.example"))

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
        self.assertIn(self.nested, section)
        self.assertIn(self.direct, section)
        syntax = subprocess.run(["sh", "-n"], input=section, text=True,
                                capture_output=True, timeout=2)
        self.assertEqual(0, syntax.returncode, syntax.stderr)


if __name__ == "__main__":
    unittest.main()
