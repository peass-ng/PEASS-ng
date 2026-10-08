import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class XvfbFbdirTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/18_Xvfb_fbdir.sh"
        )

    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.proc_root = self.root / "proc"
        self.proc_root.mkdir()
        self.fbdir = self.root / "frames"
        self.fbdir.mkdir()
        self.screen = self.fbdir / "Xvfb_screen0"
        self.screen.write_bytes(b"private framebuffer bytes must never be printed")
        self.screen.chmod(0o644)

        # The check uses Linux stat syntax; this shim gives fixtures the same
        # metadata shape on macOS without changing the production shell code.
        bin_dir = self.root / "bin"
        bin_dir.mkdir()
        stat = bin_dir / "stat"
        stat.write_text(
            "#!/bin/sh\n"
            "exec python3 -c 'import os, sys; s = os.stat(sys.argv[-1]); "
            "print(f\"{s.st_uid} {s.st_mode & 0o7777:o}\")' \"$@\"\n",
            encoding="utf-8",
        )
        stat.chmod(0o755)
        self.path = f"{bin_dir}:{os.environ['PATH']}"

    def make_process(self, pid="123", comm="Xvfb", argv=None, uid=None):
        process = self.proc_root / pid
        process.mkdir()
        (process / "comm").write_text(comm + "\n", encoding="utf-8")
        if argv is None:
            argv = ["/usr/bin/Xvfb", ":1", "-fbdir", str(self.fbdir)]
        (process / "cmdline").write_bytes(b"\0".join(a.encode() for a in argv) + b"\0")
        effective_uid = os.getuid() if uid is None else uid
        (process / "status").write_text(
            f"Name:\t{comm}\nUid:\t{effective_uid}\t{effective_uid}\t{effective_uid}\t{effective_uid}\n",
            encoding="utf-8",
        )
        (process / "cwd").symlink_to(self.root)
        return process

    def run_check(self, current_uid=None):
        if current_uid is None:
            current_uid = os.getuid() + 1
        script = "\n".join(
            [
                "SEARCH_IN_FOLDER=1",
                'print_2title() { printf "TITLE: %s\\n" "$1"; }',
                f". {shlex.quote(str(self.module))}",
                f"lp_check_xvfb_fbdir {shlex.quote(str(self.proc_root))} {current_uid}",
            ]
        )
        return subprocess.run(
            ["sh", "-c", script],
            env={**os.environ, "PATH": self.path},
            capture_output=True,
            text=True,
            check=False,
        )

    def test_running_other_user_xvfb_readable_regular_screen(self):
        self.make_process()
        result = self.run_check()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("TITLE: Readable cross-user Xvfb framebuffer files", result.stdout)
        self.assertIn(f"Review: running Xvfb PID 123 exposes readable framebuffer: {self.screen}", result.stdout)
        self.assertIn(f"owner UID {os.getuid()}, mode 0644", result.stdout)
        self.assertNotIn("private framebuffer bytes", result.stdout)

    def test_relative_fbdir_resolves_against_process_cwd(self):
        self.make_process(argv=["Xvfb", ":1", "-fbdir", "frames"])
        self.assertIn(str(self.screen), self.run_check().stdout)

    def test_non_xvfb_or_missing_fbdir_is_silent(self):
        self.make_process(comm="bash")
        self.assertEqual("", self.run_check().stdout)
        (self.proc_root / "123" / "comm").write_text("Xvfb\n", encoding="utf-8")
        (self.proc_root / "123" / "cmdline").write_bytes(b"/usr/bin/Xvfb\0:1\0")
        self.assertEqual("", self.run_check().stdout)

    def test_same_uid_and_different_file_owner_are_silent(self):
        self.make_process()
        self.assertEqual("", self.run_check(current_uid=os.getuid()).stdout)
        (self.proc_root / "123" / "status").write_text(
            f"Uid:\t{os.getuid() + 2}\t{os.getuid() + 2}\n", encoding="utf-8"
        )
        self.assertEqual("", self.run_check().stdout)

    def test_symlink_and_non_regular_screen_are_silent(self):
        self.make_process()
        self.screen.unlink()
        self.screen.symlink_to(self.root / "target")
        (self.root / "target").write_bytes(b"private data")
        (self.fbdir / "Xvfb_screen1").mkdir()
        self.assertEqual("", self.run_check().stdout)

    def test_process_discovery_beyond_256_entries(self):
        for pid in range(1, 301):
            self.make_process(pid=str(pid), comm="sleep")
        self.make_process(pid="999")
        self.assertIn("running Xvfb PID 999", self.run_check().stdout)

    def test_process_and_file_caps(self):
        for pid in range(1, 10):
            self.make_process(pid=str(pid))
        self.assertEqual(self.run_check().stdout.count("Review:"), 8)
        for pid in range(2, 10):
            (self.proc_root / str(pid) / "comm").write_text("sleep\n", encoding="utf-8")
        for number in range(1, 12):
            (self.fbdir / f"Xvfb_screen{number}").write_bytes(b"private data")
        result = self.run_check()
        self.assertEqual(result.stdout.count("Review:"), 8)


if __name__ == "__main__":
    unittest.main()
