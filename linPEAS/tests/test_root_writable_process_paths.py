import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class RootWritableProcessPathsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.helper = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/functions/checkRootWritableProcessPaths.sh"
        )

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name)
        self.proc = self.root / "proc"
        self.proc.mkdir()
        self.bin = self.root / "bin"
        self.bin.mkdir()
        self.target_dir = self.root / "protected"
        self.target_dir.mkdir(mode=0o700)
        self.target = self.target_dir / "helper"
        self.target.write_text("pass\n", encoding="utf-8")
        self.target.chmod(0o400)
        self.target_dir.chmod(0o500)
        self._stub("uname", "#!/bin/sh\necho Linux\n")
        # GNU stat flags are used by the Linux check; emulate only those on macOS.
        self._stub(
            "stat",
            "#!/usr/bin/env python3\n"
            "import os, sys\n"
            "follow = sys.argv[1] == '-Lc'\n"
            "path = sys.argv[-1]\n"
            "override = os.getenv('FAKE_STICKY_PARENT_UID' if follow else 'FAKE_STICKY_ENTRY_UID')\n"
            "print(override if override else (os.stat(path) if follow else os.lstat(path)).st_uid)\n",
        )

    def _stub(self, name, body):
        path = self.bin / name
        path.write_text(body, encoding="utf-8")
        path.chmod(0o755)

    def process(self, pid, argv, uid=0, readable=True):
        entry = self.proc / str(pid)
        entry.mkdir()
        (entry / "status").write_text(
            f"Name:\ttest\nUid:\t1000\t{uid}\t1000\t1000\n",
            encoding="ascii",
        )
        (entry / "cmdline").write_bytes(
            b"\0".join(arg.encode("ascii") for arg in argv) + b"\0"
        )
        if not readable:
            (entry / "status").chmod(0)

    def run_check(self, **overrides):
        env = os.environ.copy()
        env["PATH"] = f"{self.bin}:{env['PATH']}"
        env.update(overrides)
        body = "\n".join(
            [
                'print_3title() { echo "TITLE: $1"; }',
                f". {shlex.quote(str(self.helper))}",
                f"checkRootWritableProcessPaths {shlex.quote(str(self.proc))}",
            ]
        )
        result = subprocess.run(
            ["sh", "-c", body], env=env, capture_output=True, text=True, timeout=15
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout

    def test_writable_script_and_replaceable_parent_are_distinct(self):
        self.target.chmod(0o600)
        self.process(1, ["/bin/sh", str(self.target), "get-config", "health.api.key"])
        output = self.run_check()
        self.assertIn(f"script: {self.target} (file writable)", output)
        self.assertNotIn("parent allows replacement", output)

        self.target.chmod(0o400)
        self.target_dir.chmod(0o700)
        output = self.run_check()
        self.assertIn(f"script: {self.target} (parent allows replacement)", output)
        self.assertNotIn("file writable", output)

    def test_absolute_executable_and_no_unsafe_argument_inference(self):
        self.target.chmod(0o600)
        self.process(1, [str(self.target), "x"])
        self.process(2, ["/bin/sh", "-c", str(self.target)])
        self.process(3, ["/bin/sh", "/absent", str(self.target)])
        self.process(4, ["/bin/sh", "health.api.key", str(self.target)])
        output = self.run_check()
        self.assertEqual(output.count("PID "), 1)
        self.assertIn(f"executable: {self.target}", output)

    def test_uid_protected_and_unreadable_entries_are_skipped(self):
        self.target.chmod(0o600)
        self.process(1, ["/bin/sh", str(self.target)], uid=1000)
        self.process(2, ["/bin/sh", str(self.target)], readable=False)
        self.assertNotIn("PID ", self.run_check())
        self.target.chmod(0o400)
        self.assertNotIn("PID ", self.run_check())

    def test_symlink_content_and_entry_replacement(self):
        link_dir = self.root / "links"
        link_dir.mkdir(mode=0o700)
        link = link_dir / "helper"
        link.symlink_to(self.target)
        self.process(1, ["/bin/sh", str(link)])
        self.assertIn("parent allows replacement", self.run_check())
        link_dir.chmod(0o500)
        self.assertNotIn("PID ", self.run_check())
        self.target.chmod(0o600)
        self.assertIn("file writable", self.run_check())

    def test_sticky_parent_requires_entry_or_directory_ownership(self):
        sticky = self.root / "sticky"
        sticky.mkdir(mode=0o1777)
        sticky.chmod(0o1777)
        entry = sticky / "helper"
        entry.write_text("pass\n", encoding="utf-8")
        entry.chmod(0o400)
        self.process(1, ["/bin/sh", str(entry)])
        self.assertIn("parent allows replacement", self.run_check())
        other = str(os.getuid() + 1)
        self.assertNotIn(
            "PID ",
            self.run_check(
                FAKE_STICKY_PARENT_UID=other, FAKE_STICKY_ENTRY_UID=other
            ),
        )

        # A symlink's entry owner, rather than its protected target owner,
        # controls replacement in a sticky directory.
        entry.unlink()
        entry.symlink_to(self.target)
        self.assertNotIn(
            "PID ",
            self.run_check(
                FAKE_STICKY_PARENT_UID=other, FAKE_STICKY_ENTRY_UID=other
            ),
        )
        self.target.chmod(0o600)
        self.assertIn(
            "file writable",
            self.run_check(
                FAKE_STICKY_PARENT_UID=other, FAKE_STICKY_ENTRY_UID=other
            ),
        )

    def test_non_linux_and_root_caller_skip(self):
        self.target.chmod(0o600)
        self.process(1, ["/bin/sh", str(self.target)])
        self._stub("uname", "#!/bin/sh\necho FreeBSD\n")
        self.assertNotIn("PID ", self.run_check())
        self._stub("uname", "#!/bin/sh\necho Linux\n")
        self._stub("id", "#!/bin/sh\necho 0\n")
        self.assertNotIn("PID ", self.run_check())

    def test_byte_pid_and_finding_caps(self):
        self.target.chmod(0o600)
        for pid in range(1, 216):
            args = ["/bin/sh", str(self.target)]
            if pid == 1:
                args.append("x" * 1100)
            self.process(pid, args)
        output = self.run_check()
        self.assertNotIn("PID 1:", output)
        self.assertEqual(output.count("PID "), 12)
        self.process(9999, ["/bin/sh", str(self.target)])
        for entry in self.proc.iterdir():
            if entry.name != "9999":
                (entry / "status").write_text(
                    "Uid:\t1000\t1000\t1000\t1000\n", encoding="ascii"
                )
        self.assertNotIn("PID 9999:", self.run_check())


if __name__ == "__main__":
    unittest.main()
