"""A privileged runtime helper is a passive SUID review candidate."""

import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SUID_MODULE = ROOT / "linPEAS/builder/linpeas_parts/8_interesting_perms_files/1_SUID.sh"


class PinnsSuidCandidateTests(unittest.TestCase):
    def run_fixture(self, mode=0o4755, uid="0", nnp="0", mount="rw,relatime",
                    iamroot="", bsd_stat=False, basename="pinns", kernel="Linux"):
        with tempfile.TemporaryDirectory() as tmp:
            helper = Path(tmp) / basename
            marker = Path(tmp) / "executed"
            helper.write_text(f"#!/bin/sh\ntouch '{marker}'\n")
            helper.chmod(mode)
            stat_guard = '[ "$1" = -f ] || return 1;' if bsd_stat else ''
            shell = (
                "print_2title() { :; }; print_info() { :; }; echo_not_found() { :; }; "
                "check_privileged_file_location() { :; }; "
                f"find() {{ printf '%s\\n' '{helper}'; }}; "
                f"stat() {{ {stat_guard} printf '%s\\n' '{uid}'; }}; "
                f"findmnt() {{ printf '%s\\n' '{mount}'; }}; "
                f"uname() {{ printf '%s\\n' '{kernel}'; }}; "
                "awk() { if [ \"$2\" = /proc/self/status ]; then "
                f"printf '%s\\n' '{nnp}'; else command awk \"$@\"; fi; }}; "
                f"IAMROOT='{iamroot}'; ROOT_FOLDER=/fixture; FAST=1; E=E; SED_RED='&'; "
                + SUID_MODULE.read_text()
            )
            result = subprocess.run(["bash", "-c", shell], capture_output=True,
                                    text=True, timeout=3)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertFalse(marker.exists(), "privileged helper was executed")
            return result.stdout

    def test_candidate_and_non_candidate_boundaries(self):
        candidate = self.run_fixture()
        self.assertIn("pinns review candidate (CVE-2022-0811)", candidate)
        self.assertIn("NoNewPrivs=0", candidate)
        self.assertIn("vendor fixes unknown", candidate)
        self.assertIn("You own the SUID file:", candidate)
        self.assertEqual(1, candidate.count("/pinns"), "existing SUID classification should print the path once")
        self.assertNotIn("Trying to execute", candidate)
        self.assertIn("pinns review candidate", self.run_fixture(bsd_stat=True))

        for kwargs in ({"uid": "1000"}, {"mode": 0o0755}, {"mode": 0o4000},
                       {"iamroot": "1"}, {"basename": "pinns-backup"},
                       {"kernel": "OpenBSD"}):
            with self.subTest(kwargs=kwargs):
                self.assertNotIn("pinns review candidate", self.run_fixture(**kwargs))
        self.assertNotIn("pinns review candidate", self.run_fixture(nnp="1"))
        self.assertIn("blocked by NoNewPrivs", self.run_fixture(nnp="1"))
        self.assertNotIn("pinns review candidate", self.run_fixture(mount="rw,nosuid,relatime"))
        self.assertIn("blocked by nosuid", self.run_fixture(mount="rw,nosuid,relatime"))

        uncertain = self.run_fixture(nnp="", mount="")
        self.assertIn("pinns review candidate", uncertain)
        self.assertIn("NoNewPrivs unknown", uncertain)
        self.assertIn("mount SUID policy unknown", uncertain)


if __name__ == "__main__":
    unittest.main()
