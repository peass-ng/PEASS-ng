"""A qpdf sudo hint requires an unrestricted root-capable grant."""

import re
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1]
          / "builder/linpeas_parts/6_users_information/7_Sudo_l.sh")
FUNCTION = re.search(r"^sudo_qpdf_attachment_review\(\) \{\n.*?^\}",
                     MODULE.read_text(), re.MULTILINE | re.DOTALL).group()
MARKER = "Sudo qpdf attachment file-read review candidate:"


class SudoQpdfAttachmentTests(unittest.TestCase):
    def scan(self, policy):
        with tempfile.TemporaryDirectory() as tmp:
            binary = Path(tmp) / "qpdf"
            binary.write_text("#!/bin/sh\necho MUST_NOT_RUN\n")
            binary.chmod(0o755)
            rule = policy.replace("{qpdf}", str(binary))
            result = subprocess.run(
                ["sh", "-c", FUNCTION + '\nsudo_qpdf_attachment_review "$1"', "sh", rule],
                text=True, capture_output=True, timeout=3,
            )
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertEqual("", result.stderr)
            self.assertNotIn("MUST_NOT_RUN", result.stdout)
            return result.stdout

    def test_root_unrestricted_grants(self):
        for runas in ("root", "ALL", "ALL : ALL", "#0"):
            with self.subTest(runas=runas):
                out = self.scan(f"    ({runas}) NOPASSWD: {{qpdf}}\n")
                self.assertEqual(1, out.count(MARKER))
                self.assertIn("confirm qpdf >=10.2", out)
        self.assertIn(MARKER, self.scan("    (root) {qpdf} *\n"))

    def test_rejects_restricted_or_nonroot_grants(self):
        for rule in (
            "    (builder) {qpdf}\n",
            "    (ALL, !root) {qpdf}\n",
            "    (root, !root) {qpdf}\n",
            "    (root) {qpdf} --check /tmp/fixed.pdf\n",
            '    (root) {qpdf} ""\n',
            "    (root) /usr/bin/qpdf-helper\n",
            "    (root) ! {qpdf}\n",
        ):
            with self.subTest(rule=rule):
                self.assertEqual("", self.scan(rule))

    def test_denials_and_overlong_policy_fail_closed(self):
        allowed = "    (root) {qpdf}\n"
        for policy in (
            allowed + "    (root) ! {qpdf}\n",
            allowed + "    (root) !ALL\n",
            allowed + "    (root) ! {qpdf} --add-attachment *\n",
            allowed + "        --check /tmp/fixed.pdf\n",
            "x" * 2049 + "\n" + allowed,
            "x\n" * 3001 + allowed,
        ):
            with self.subTest(policy=policy[:80]):
                self.assertEqual("", self.scan(policy))

    def test_deduplicates_and_module_parses(self):
        self.assertEqual(1, self.scan("    (root) {qpdf}\n" * 3).count(MARKER))
        result = subprocess.run(["sh", "-n", str(MODULE)], text=True,
                                capture_output=True, timeout=3)
        self.assertEqual(0, result.returncode, result.stderr)


if __name__ == "__main__":
    unittest.main()
