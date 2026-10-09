"""Duplicate local UIDs are reported without changing the UID 0 inventory."""

import re
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (Path(__file__).resolve().parents[1]
          / "builder/linpeas_parts/6_users_information/11_Superusers.sh")
SOURCE = MODULE.read_text()
FUNCTION = re.search(r"^summarize_local_passwd\(\) \{.*?^\}", SOURCE,
                     re.MULTILINE | re.DOTALL).group()
DISPLAY = re.search(r"^shared_uid_rows=.*?^fi\nif printf.*?^fi", SOURCE,
                    re.MULTILINE | re.DOTALL).group()


class SharedLocalUidInventoryTests(unittest.TestCase):
    def run_probe(self, entries):
        with tempfile.TemporaryDirectory() as tmp:
            passwd = Path(tmp) / "passwd"
            passwd.write_text("\n".join(entries) + "\n")
            result = subprocess.run(
                ["/bin/sh", "-c", FUNCTION + '\nsummarize_local_passwd "$1"',
                 "sh", str(passwd)], capture_output=True, text=True, timeout=2)
            self.assertEqual(0, result.returncode, result.stderr)
            return result.stdout.splitlines()

    def test_duplicate_nonzero_uid_and_unchanged_root_listing(self):
        lines = self.run_probe([
            "root:x:0:0:root:/root:/bin/sh",
            "alice:x:1000:1000::/home/alice:/bin/sh",
            "alias:secret:01000:1000::/home/alias:/bin/sh",
            "service:x:1:1::/run/service:/bin/false",
        ])
        self.assertIn("R\troot:x:0:0:root:/root:/bin/sh", lines)
        self.assertIn("D\t1000\talice\talias", lines)
        self.assertFalse(any("secret" in line for line in lines if line.startswith("D\t")))
        self.assertEqual(1, sum(line.startswith("D\t") for line in lines))

    def test_distinct_and_invalid_uids_are_not_flagged(self):
        lines = self.run_probe([
            "root:x:0:0::/root:/bin/sh",
            "one:x:1001:1001::/home/one:/bin/sh",
            "bad:x:notanid:1001::/home/bad:/bin/sh",
            "two:x:1002:1002::/home/two:/bin/sh",
        ])
        self.assertFalse(any(line.startswith("D\t") for line in lines))

    def test_caps_duplicate_pairs_and_comparison_rows_but_keeps_root_rows(self):
        entries = ["a:x:1000:1000::/a:/bin/sh"]
        entries += [f"a{i}:x:1000:1000::/a{i}:/bin/sh" for i in range(33)]
        entries += [f"u{i}:x:{2000+i}:1000::/u{i}:/bin/sh"
                    for i in range(4097 - len(entries))]
        entries += ["late:x:1000:1000::/late:/bin/sh",
                    "backuproot:x:0:0::/root:/bin/sh"]
        lines = self.run_probe(entries)
        self.assertEqual(32, sum(line.startswith("D\t") for line in lines))
        self.assertIn("L", lines)
        self.assertIn("P", lines)
        self.assertNotIn("D\t1000\ta\tlate", lines)
        self.assertIn("R\tbackuproot:x:0:0::/root:/bin/sh", lines)

    def test_module_reads_local_passwd_once_for_both_cues(self):
        self.assertEqual(1, SOURCE.count("summarize_local_passwd /etc/passwd"))
        self.assertIn("Shared UIDs can be intentional", SOURCE)
        result = subprocess.run(["/bin/sh", "-n", str(MODULE)],
                                capture_output=True, text=True, timeout=2)
        self.assertEqual(0, result.returncode, result.stderr)

    def test_partial_without_duplicate_has_no_candidate_heading(self):
        entries = [f"u{i}:x:{2000+i}:1000::/u{i}:/bin/sh"
                   for i in range(4097)]
        with tempfile.TemporaryDirectory() as tmp:
            passwd = Path(tmp) / "passwd"
            passwd.write_text("\n".join(entries) + "\n")
            script = (FUNCTION + '\npasswd_identity_rows=$(summarize_local_passwd "$1")\n'
                      + 'print_3title() { printf "TITLE:%s\\n" "$1"; }\n'
                      + DISPLAY)
            result = subprocess.run(["/bin/sh", "-c", script, "sh", str(passwd)],
                                    capture_output=True, text=True, timeout=2)
            self.assertEqual(0, result.returncode, result.stderr)
            self.assertNotIn("TITLE:", result.stdout)
            self.assertIn("Shared-UID comparison partial", result.stdout)


if __name__ == "__main__":
    unittest.main()
