"""NFS export policy cues are local, bounded, and do not claim effective access."""

import re
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1]
          / "builder/linpeas_parts/1_system_information/7_Mounts.sh")
SOURCE = MODULE.read_text()
FUNCTION = re.search(r"^nfs_export_file_candidates\(\) \{.*?^\}", SOURCE,
                     re.MULTILINE | re.DOTALL).group()
LIVE_BLOCK = SOURCE.split("# Live-host policy must not be mixed into an extracted filesystem review.\n", 1)[1].split('\nif [ -f "/etc/fstab"', 1)[0]


class NfsExportIdentityCandidateTests(unittest.TestCase):
    def run_probe(self, path, shell="sh"):
        result = subprocess.run(
            [shell, "-c", FUNCTION + '\nnfs_export_file_candidates "$1"',
             shell, str(path)], capture_output=True, text=True, timeout=2)
        self.assertEqual(0, result.returncode, result.stderr)
        return result.stdout

    def test_writable_export_only_and_no_policy_overclaim(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "exports"
            path.write_text(
                "# /comment *(rw,root_squash,no_all_squash)\n"
                "/readonly *(ro,sync,root_squash)\n"
                "/valid *(rw,sync,root_squash,no_all_squash)\n"
                "/other client(rw,sync,all_squash)\n"
                "/kerberos client(rw,sec=krb5)\n"
                "/mixed clientA(rw,all_squash) clientB(rw,root_squash,sec=sys)\n"
                "/inline *(ro) # fake(rw)\n"
            )
            output = self.run_probe(path)
            self.assertIn(f"{path}:3", output)
            self.assertIn(f"{path}:6", output)
            self.assertNotIn(f"{path}:1", output)
            self.assertNotIn(f"{path}:2", output)
            self.assertNotIn(f"{path}:4", output)
            self.assertNotIn(f"{path}:5", output)
            self.assertNotIn(f"{path}:7", output)
            self.assertNotIn("/valid", output)
            self.assertIn("Verify active export policy", SOURCE)

    def test_absent_symlink_and_oversized_file_are_bounded(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "exports"
            link = Path(tmp) / "exports.link"
            self.assertEqual("", self.run_probe(path))
            path.write_text("/data *(rw)\n")
            link.symlink_to(path)
            self.assertEqual("", self.run_probe(link))
            path.write_text("/data *(rw)\n" + "x" * 16400)
            self.assertIn("16 KiB file limit", self.run_probe(path))
            self.assertNotIn("candidate", self.run_probe(path))

    def test_line_length_line_count_and_candidate_caps(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "exports"
            path.write_text("/x *(rw)" + "x" * 600 + "\n" + "/data *(rw)\n")
            output = self.run_probe(path)
            self.assertIn("long line", output)
            self.assertIn(f"{path}:2", output)

            path.write_text("/data *(rw)\n" * 5)
            output = self.run_probe(path)
            self.assertEqual(4, output.count("review candidate"))
            self.assertIn("4-candidate file limit", output)

            path.write_text("/readonly *(ro)\n" * 129)
            output = self.run_probe(path)
            self.assertIn("128-line limit", output)
            self.assertNotIn("review candidate", output)

    def test_portable_shell_syntax(self):
        for shell in ("sh", "bash", "dash"):
            binary = shutil.which(shell)
            if binary:
                with self.subTest(shell=shell):
                    result = subprocess.run([binary, "-n", str(MODULE)],
                                            capture_output=True, text=True, timeout=2)
                    self.assertEqual(0, result.returncode, result.stderr)

    def test_offline_firmware_mode_never_reads_host_exports(self):
        script = ('set -e\nSEARCH_IN_FOLDER=/tmp/offline-root\n'
                  'nfs_export_file_candidates() { echo SHOULD_NOT_RUN; }\n'
                  'print_2title() { echo SHOULD_NOT_PRINT; }\n' + LIVE_BLOCK)
        result = subprocess.run(["sh", "-c", script], capture_output=True,
                                text=True, timeout=2)
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertEqual("", result.stdout)


if __name__ == "__main__":
    unittest.main()
