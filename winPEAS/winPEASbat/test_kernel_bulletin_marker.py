"""Check the legacy kernel-update marker and its conservative failure wording."""

import unittest
from pathlib import Path


BAT = Path(__file__).with_name("winPEAS.bat")


class KernelBulletinMarkerTests(unittest.TestCase):
    def test_update_kb_and_qfe_failure_caveat(self):
        data = BAT.read_bytes()
        self.assertEqual(data.count(b"\n"), data.count(b"\r\n"))
        lines = data.decode("utf-8", errors="replace").splitlines()
        matches = [i for i, line in enumerate(lines) if "ECHO.MS15-051" in line]
        self.assertEqual(1, len(matches))
        index = matches[0]
        self.assertIn('findstr /C:"KB3045171"', lines[index - 1])
        self.assertIn("MS15-051 (KB3045171) not found in WMIC QFE output", lines[index])
        self.assertIn("may be unavailable or incomplete", lines[index])
        self.assertIn("verify supported OS/service pack and superseding updates", lines[index])
        self.assertNotIn("patch is NOT installed", lines[index])
        self.assertNotIn("KB3057191", lines[index - 1])


if __name__ == "__main__":
    unittest.main()
