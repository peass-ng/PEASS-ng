"""Exercise the bounded, passive binfmt capability-helper clue."""

import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/8_interesting_perms_files/4_Capabilities.sh"
REGISTER = b"/proc/sys/fs/binfmt_misc/register"


class BinfmtCapabilityHelperTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)

    def binary(self, name, payload=REGISTER, executable=True):
        path = self.root / name
        path.write_bytes(payload)
        path.chmod(0o755 if executable else 0o644)
        return path

    def scan(self, records):
        source = MODULE.read_text()
        start = source.index("  binfmt_probe_count=0\n  getcap -r / 2>/dev/null | head -n 50 | while read cb; do")
        end = source.index("\n  checkSnapConfineCVE20268933", start)
        listing = source[start:end]
        record_text = "\n".join(records)
        script = (
            "E=E; SED_RED='<R>'; SED_RED_YELLOW='<H>'; capsB='cap_dac_override'; "
            "capsVB=''; IAMROOT=''; STRINGS=\"$(command -v strings)\";\n"
            f"getcap() {{ printf '%s\\n' {shlex.quote(record_text)}; }}\n"
            f"{listing}\n"
        )
        result = subprocess.run(
            ["sh", "-c", script],
            env=os.environ,
            capture_output=True,
            text=True,
            timeout=5,
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_effective_capability_and_exact_register_literal(self):
        path = self.binary("helper", b"prefix\0" + REGISTER + b"\0suffix")
        for rights in ("cap_dac_override=ep", "cap_dac_override+eip", "cap_dac_override,cap_net_raw=ep"):
            with self.subTest(rights=rights):
                output = self.scan([f"{path} {rights}"])
                self.assertIn(f"binfmt_misc helper review candidate: {path}", output)
                self.assertNotIn("prefix", output)

    def test_missing_effective_capability_and_unrelated_literal(self):
        path = self.binary("helper")
        for rights in ("cap_dac_override=p", "cap_dac_override=ip", "cap_net_raw=ep"):
            with self.subTest(rights=rights):
                self.assertNotIn("binfmt_misc helper review candidate", self.scan([f"{path} {rights}"]))
        unrelated = self.binary("unrelated", b"/proc/sys/fs/binfmt_misc/register-extra")
        self.assertNotIn("binfmt_misc helper review candidate", self.scan([f"{unrelated} cap_dac_override=ep"]))

    def test_oversized_symlink_and_non_executable_are_skipped(self):
        oversized = self.binary("oversized", REGISTER + b"x" * 1048576)
        target = self.binary("target")
        link = self.root / "link"
        link.symlink_to(target)
        non_exec = self.binary("nonexec", executable=False)
        for path in (oversized, link, non_exec):
            with self.subTest(path=path):
                self.assertNotIn("binfmt_misc helper review candidate", self.scan([f"{path} cap_dac_override=ep"]))

    def test_at_most_eight_qualifying_binaries_are_inspected(self):
        records = []
        for i in range(9):
            path = self.binary(f"helper{i}")
            records.append(f"{path} cap_dac_override=ep")
        output = self.scan(records)
        self.assertEqual(output.count("binfmt_misc helper review candidate:"), 8)
        self.assertNotIn("binfmt_misc helper review candidate: " + str(self.root / "helper8"), output)


if __name__ == "__main__":
    unittest.main()
