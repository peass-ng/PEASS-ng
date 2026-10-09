"""The exact Java scripting SUID cue uses metadata and never launches the binary."""

import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/8_interesting_perms_files/1_SUID.sh"


@unittest.skipUnless(os.geteuid() == 0, "root is needed to create a root-owned fixture")
class JjsSuidReviewTests(unittest.TestCase):
    def run_fixture(self, *, basename="jjs", mode=0o4755, owner=0,
                    nnp="0", mount="rw,relatime", iamroot=""):
        with tempfile.TemporaryDirectory() as tmp:
            base = Path(tmp)
            base.chmod(0o755)
            helper = base / basename
            marker = base / "executed"
            helper.write_text(f"#!/bin/sh\ntouch {shlex.quote(str(marker))}\n")
            os.chown(helper, owner, owner)
            helper.chmod(mode)
            shell = "\n".join((
                "print_2title() { :; }; print_info() { :; }; echo_not_found() { :; }",
                "check_privileged_file_location() { :; }",
                f"find() {{ printf '%s\\n' {shlex.quote(str(helper))}; }}",
                f"findmnt() {{ printf '%s\\n' {shlex.quote(mount)}; }}",
                "awk() { if [ \"$2\" = /proc/self/status ]; then "
                f"printf '%s\\n' {shlex.quote(nnp)}; else command awk \"$@\"; fi; }}",
                f"IAMROOT={shlex.quote(iamroot)}; ROOT_FOLDER=/fixture; FAST=1; E=E;",
                "SED_RED='&'; SED_RED_YELLOW='&'; SED_GREEN='&'; "
                "sidG1=__none__; sidG2=__none__; sidG3=__none__; sidG4=__none__; "
                "sidVB=__none__; sidVB2=__none__",
                f". {shlex.quote(str(MODULE))}",
            ))

            def drop_privileges():
                os.setgroups([])
                os.setgid(65534)
                os.setuid(65534)

            result = subprocess.run(
                ["sh", "-c", shell], capture_output=True, text=True,
                timeout=4, preexec_fn=drop_privileges,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertFalse(marker.exists(), "SUID fixture was executed")
            return result.stdout

    def test_exact_access_and_owner_boundaries(self):
        candidate = self.run_fixture()
        self.assertIn("jjs SUID file-access review candidate", candidate)
        self.assertEqual(candidate.count("/jjs"), 1)
        for kwargs in (
            {"basename": "jjs-helper"}, {"mode": 0o0755},
            {"mode": 0o4700}, {"owner": 1000}, {"iamroot": "1"},
        ):
            with self.subTest(kwargs=kwargs):
                output = self.run_fixture(**kwargs)
                self.assertNotIn("jjs SUID file-access review candidate", output)
                self.assertIn("Unknown SUID binary!", output)

    def test_mount_and_nnp_boundaries(self):
        nnp = self.run_fixture(nnp="1")
        self.assertIn("blocked by NoNewPrivs", nnp)
        self.assertNotIn("review candidate", nnp)
        nosuid = self.run_fixture(mount="rw,nosuid,relatime")
        self.assertIn("blocked by nosuid", nosuid)
        self.assertNotIn("review candidate", nosuid)
        unknown = self.run_fixture(nnp="", mount="")
        self.assertIn("review candidate", unknown)
        self.assertIn("verify effective UID", unknown)


if __name__ == "__main__":
    unittest.main()
