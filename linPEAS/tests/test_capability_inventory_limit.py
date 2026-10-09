"""Ensure the existing file-capability listing labels real truncation."""

import os
import shlex
import subprocess
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/8_interesting_perms_files/4_Capabilities.sh"


class CapabilityInventoryLimitTests(unittest.TestCase):
    def list_capabilities(self, count):
        source = MODULE.read_text()
        start = source.index("  binfmt_probe_count=0\n  cap_inventory_count=0")
        end = source.index("\n  checkSnapConfineCVE20268933", start)
        listing = source[start:end]
        records = "\n".join(f"/not-present/cap{i:02d} cap_net_raw=ep" for i in range(count))
        script = (
            "E=E; SED_RED='<R>'; SED_RED_YELLOW='<H>'; capsB='cap_net_raw'; "
            "capsVB=''; IAMROOT=1; STRINGS='';\n"
            f"getcap() {{ printf '%s\\n' {shlex.quote(records)}; }}\n"
            f"{listing}\n"
        )
        result = subprocess.run(
            ["sh", "-c", script], env=os.environ, capture_output=True,
            text=True, timeout=5,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_fifty_records_have_no_partial_cue(self):
        output = self.list_capabilities(50)
        self.assertEqual(output.count("/not-present/cap"), 50)
        self.assertIn("/not-present/cap49", output)
        self.assertNotIn("Capability inventory is partial", output)

    def test_fifty_first_record_triggers_one_partial_cue(self):
        for count in (51, 52):
            with self.subTest(count=count):
                output = self.list_capabilities(count)
                self.assertEqual(output.count("/not-present/cap"), 50)
                self.assertIn("/not-present/cap49", output)
                self.assertNotIn("/not-present/cap50", output)
                self.assertNotIn("/not-present/cap51", output)
                self.assertEqual(output.count("Capability inventory is partial"), 1)


if __name__ == "__main__":
    unittest.main()
