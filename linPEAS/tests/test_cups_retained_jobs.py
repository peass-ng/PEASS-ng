"""Bounded, metadata-only CUPS spool discovery fixtures."""

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest


MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/1_system_information/16_Protections.sh"


class CupsRetainedJobsTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.spool = self.root / "spool"
        self.spool.mkdir()
        self.bin = self.root / "bin"
        self.bin.mkdir()
        for tool in ("awk", "ls"):
            target = shutil.which(tool)
            if target:
                (self.bin / tool).symlink_to(target)

        self.jobs = self.root / "jobs.txt"
        self.jobs.write_text("", encoding="utf-8")
        self.calls = self.root / "calls.txt"
        lpstat = self.bin / "lpstat"
        lpstat.write_text(
            '#!/bin/sh\n'
            'printf "%s\\n" "$*" >> "$CUPS_CALLS"\n'
            '[ "$*" = "-h localhost -W completed -o" ] || exit 1\n'
            '/bin/cat "$CUPS_JOBS"\n',
            encoding="utf-8",
        )
        lpstat.chmod(0o755)
        deadline = self.bin / "deadline"
        deadline.write_text(
            '#!/bin/sh\n'
            '[ "$1" = 2 ] || exit 1\n'
            'shift\n'
            'exec "$@"\n',
            encoding="utf-8",
        )
        deadline.chmod(0o755)

        source = MODULE.read_text(encoding="utf-8")
        start = source.index("# A searchable but unlistable CUPS spool")
        end = source.index("#-- SY) Running in a virtual environment", start)
        self.snippet = source[start:end].replace("/var/spool/cups", str(self.spool))

    def run_probe(self, *, timeout=True, scoped=False, root_folder="/", path=None, shell="/bin/sh"):
        script = (
            'print_2title() { printf "TITLE:%s\\n" "$1"; }\n'
            + self.snippet
        )
        env = {
            "PATH": str(self.bin) if path is None else path,
            "CUPS_JOBS": str(self.jobs),
            "CUPS_CALLS": str(self.calls),
            "TIMEOUT": str(self.bin / "deadline") if timeout else "",
            "SEARCH_IN_FOLDER": str(self.root) if scoped else "",
            "ROOT_FOLDER": root_folder,
        }
        return subprocess.run(
            [shell, "-c", script],
            env=env,
            text=True,
            capture_output=True,
            timeout=3,
            check=True,
        )

    def test_known_ids_reveal_paths_without_document_contents(self):
        self.jobs.write_text(
            "printer-1 alice 7\nprinter-99999 bob 7\nprinter-100000 carol 7\n"
            "garbage text\nprinter-abc wrong 7\n",
            encoding="utf-8",
        )
        for filename in ("d00001-001", "d99999-002", "d100000-003"):
            (self.spool / filename).write_text("ROOT_SECRET_MUST_NOT_LEAK", encoding="utf-8")
        self.spool.chmod(0o111)
        self.addCleanup(self.spool.chmod, 0o755)
        result = self.run_probe()
        self.assertEqual(result.stderr, "")
        self.assertEqual(result.stdout.count("TITLE:"), 1)
        for filename in ("d00001-001", "d99999-002", "d100000-003"):
            self.assertIn(filename, result.stdout)
        self.assertNotIn("ROOT_SECRET_MUST_NOT_LEAK", result.stdout)
        self.assertEqual(self.calls.read_text(encoding="utf-8").strip(), "-h localhost -W completed -o")

    def test_strict_job_and_document_caps(self):
        self.jobs.write_text(
            "".join(f"printer-{i} user 1\n" for i in range(1, 26)), encoding="utf-8"
        )
        (self.spool / "d00020-003").write_text("x", encoding="utf-8")
        (self.spool / "d00021-001").write_text("x", encoding="utf-8")
        (self.spool / "d00001-004").write_text("x", encoding="utf-8")
        result = self.run_probe()
        self.assertIn("d00020-003", result.stdout)
        self.assertNotIn("d00021-001", result.stdout)
        self.assertNotIn("d00001-004", result.stdout)

    def test_skips_symlinks_and_scoped_or_missing_deadline(self):
        self.jobs.write_text("printer-1 alice 7\n", encoding="utf-8")
        (self.spool / "d00001-001").symlink_to(self.jobs)
        self.assertEqual(self.run_probe().stdout, "")
        self.calls.unlink()
        self.assertEqual(self.run_probe(scoped=True).stdout, "")
        self.assertEqual(self.run_probe(root_folder="/offline").stdout, "")
        self.assertEqual(self.run_probe(timeout=False).stdout, "")
        if Path("/bin/ksh").exists():
            self.assertEqual(self.run_probe(timeout=False, shell="/bin/ksh").stdout, "")
        self.assertFalse(self.calls.exists())

    def test_no_cups_program_or_spool(self):
        self.jobs.write_text("printer-1 alice 7\n", encoding="utf-8")
        (self.spool / "d00001-001").write_text("x", encoding="utf-8")
        self.spool.rename(self.root / "hidden-spool")
        self.assertEqual(self.run_probe().stdout, "")
        (self.root / "hidden-spool").rename(self.spool)
        empty_bin = self.root / "empty-bin"
        empty_bin.mkdir()
        self.assertEqual(self.run_probe(path=str(empty_bin)).stdout, "")
        self.assertFalse(self.calls.exists())

    def test_dash_and_ksh_when_available(self):
        self.jobs.write_text("printer-1 alice 7\n", encoding="utf-8")
        (self.spool / "d00001-001").write_text("x", encoding="utf-8")
        for shell in ("/bin/dash", "/bin/ksh"):
            if Path(shell).exists():
                with self.subTest(shell=shell):
                    self.assertIn("d00001-001", self.run_probe(shell=shell).stdout)


if __name__ == "__main__":
    unittest.main()
