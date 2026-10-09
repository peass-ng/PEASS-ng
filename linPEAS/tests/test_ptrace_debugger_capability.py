"""Check the existing file-capability classification at debugger boundaries."""

import os
import shlex
import subprocess
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CAPS_VARIABLE = ROOT / "builder/linpeas_parts/variables/capsVB.sh"
CAPS_MODULE = ROOT / "builder/linpeas_parts/8_interesting_perms_files/4_Capabilities.sh"


class PtraceDebuggerCapabilityTests(unittest.TestCase):
    def classify(self, record):
        source = CAPS_MODULE.read_text()
        start = source.index("  cap_inventory_count=0\n  getcap -r / 2>/dev/null | head -n 51 | while read cb; do")
        end = source.index("\n  checkSnapConfineCVE20268933", start)
        listing = source[start:end]
        script = (
            f". {shlex.quote(str(CAPS_VARIABLE))}\n"
            "E=E; SED_RED='<R>'; SED_RED_YELLOW='<H>'; capsB='cap_sys_ptrace'; IAMROOT=1\n"
            "getcap() { printf '%s\\n' \"$CAP_RECORD\"; }\n"
            f"{listing}\n"
        )
        result = subprocess.run(
            ["sh", "-c", script], env={**os.environ, "CAP_RECORD": record},
            capture_output=True, text=True, timeout=5,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_exact_debugger_with_ptrace_is_emphasized(self):
        for record in (
            "/usr/bin/gdb cap_sys_ptrace=ep",
            "/opt/tools/gdb = cap_sys_ptrace+ep",
        ):
            with self.subTest(record=record):
                self.assertIn("<H>", self.classify(record))

    def test_other_names_and_capabilities_are_not_emphasized(self):
        for record in (
            "/usr/bin/gdb-wrapper cap_sys_ptrace=ep",
            "/usr/bin/gdb.py cap_sys_ptrace=ep",
            "/usr/bin/gdb cap_net_raw=ep",
        ):
            with self.subTest(record=record):
                self.assertNotIn("<H>", self.classify(record))

    def test_existing_ptrace_interpreter_classification_survives(self):
        self.assertIn("<H>", self.classify("/usr/bin/python3 cap_sys_ptrace=ep"))


if __name__ == "__main__":
    unittest.main()
