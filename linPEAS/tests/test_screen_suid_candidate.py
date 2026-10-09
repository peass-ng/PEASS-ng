"""Exact, passive SUID candidate highlighting for a versioned executable."""

import subprocess
import unittest
from pathlib import Path


RULES = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/variables/sidB.sh"


class ScreenSuidCandidateTests(unittest.TestCase):
    def test_versioned_name_is_scoped_and_sed_compatible(self):
        entries = [line.strip() for line in RULES.read_text().splitlines()
                   if line.strip().startswith("/screen-4")]
        self.assertEqual(1, len(entries))
        pattern, label = entries[0].rstrip("\\").split("%", 1)
        self.assertEqual(r"/screen-4\.5\.0$", pattern)
        self.assertIn("candidate", label)
        self.assertIn("vendor_patch", label)

        positive = ("/usr/bin/screen-4.5.0", "/opt/terminal/screen-4.5.0")
        negative = ("/usr/bin/screen", "/usr/bin/screen-4.5.1",
                    "/usr/bin/screen-4x5x0", "/usr/bin/screen-4.5.0-patched",
                    "/usr/bin/screen-4.5.0-helper")
        for path in positive + negative:
            grep = subprocess.run(["grep", "-q", pattern], input=path + "\n",
                                  text=True, timeout=2)
            self.assertEqual(path in positive, grep.returncode == 0, path)
            sed = subprocess.run(["sed", "-E", "s," + pattern + ",[candidate],"],
                                 input=path + "\n", capture_output=True,
                                 text=True, timeout=2, check=True)
            self.assertEqual(path in positive, "[candidate]" in sed.stdout, path)


if __name__ == "__main__":
    unittest.main()
