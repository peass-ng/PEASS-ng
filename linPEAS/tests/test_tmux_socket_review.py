"""Bounded, passive tmux socket-directory fixtures."""

import os
import re
import shutil
import socket
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/7_software_information/Tmux.sh"
)
FUNCTION = re.search(
    r"^tmux_socket_review\(\) \(\n.*?^\)",
    MODULE.read_text(),
    re.MULTILINE | re.DOTALL,
).group()


class TmuxSocketReviewTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.root = Path(self.tmp.name) / "root with spaces"
        self.root.mkdir()
        self.stub_dir = Path(self.tmp.name) / "stub"
        self.stub_dir.mkdir()
        stub_id = self.stub_dir / "id"
        stub_id.write_text(
            "#!/bin/sh\n"
            'case "$1" in\n'
            '  -u) printf "%s\\n" "$FIXTURE_UID" ;;\n'
            '  -G) printf "%s\\n" "$FIXTURE_GROUPS" ;;\n'
            "esac\n"
        )
        stub_id.chmod(0o755)
        self.sockets = []
        self.addCleanup(lambda: [sock.close() for sock in self.sockets])

    def make_socket(self, directory, name="default", mode=0o666):
        directory.mkdir(exist_ok=True)
        path = directory / name
        sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        sock.bind(str(path))
        sock.listen(1)
        os.chmod(path, mode)
        self.sockets.append(sock)
        return path

    def scan(self, root=None, extra="", uid="2000", groups=None, shell="sh"):
        env = os.environ.copy()
        env["PATH"] = str(self.stub_dir) + os.pathsep + env["PATH"]
        env["FIXTURE_UID"] = uid
        env["FIXTURE_GROUPS"] = str(os.getgid() if groups is None else groups)
        result = subprocess.run(
            [shell, "-c", FUNCTION + '\ntmux_socket_review "$1" "$2"',
             shell, str(self.root if root is None else root), str(extra)],
            env=env,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=3,
            check=True,
        )
        self.assertEqual(result.stderr, "")
        return result.stdout

    def test_default_and_custom_socket_names_with_spaces(self):
        directory = self.root / "tmux-1001"
        first = self.make_socket(directory)
        second = self.make_socket(directory, "custom name")
        output = self.scan()
        self.assertIn(f"DIR {directory}\n", output)
        self.assertIn(f"SOCKET {first}\n", output)
        self.assertIn(f"SOCKET {second}\n", output)
        self.assertNotIn("PARTIAL", output)

    def test_other_user_mode_and_group_membership(self):
        directory = self.root / "tmux-1001"
        socket_path = self.make_socket(directory, mode=0o660)
        self.assertIn(f"SOCKET {socket_path}\n", self.scan())
        self.assertNotIn("SOCKET ", self.scan(groups="999999"))
        os.chmod(socket_path, 0o600)
        self.assertNotIn("SOCKET ", self.scan())
        os.chmod(socket_path, 0o666)
        self.assertNotIn("SOCKET ", self.scan(uid=str(os.getuid())))

    def test_symlink_paths_are_not_followed(self):
        directory = self.root / "tmux-1001"
        target = self.make_socket(directory)
        (directory / "linked").symlink_to(target)
        (self.root / "tmux-1002").symlink_to(directory, target_is_directory=True)
        output = self.scan()
        self.assertEqual(output.count("SOCKET "), 1)
        self.assertEqual(output.count("DIR "), 1)

    def test_directory_cap_marks_partial_coverage(self):
        for number in range(33):
            (self.root / f"tmux-{number:04d}").mkdir()
        output = self.scan()
        self.assertEqual(output.count("DIR "), 32)
        self.assertIn("PARTIAL\n", output)

    def test_direct_entry_cap_marks_partial_coverage(self):
        directory = self.root / "tmux-1001"
        directory.mkdir()
        for number in range(65):
            (directory / f"file-{number:04d}").touch()
        output = self.scan()
        self.assertIn(f"DIR {directory}\n", output)
        self.assertIn("PARTIAL\n", output)
        self.assertNotIn("SOCKET ", output)

    def test_custom_root_and_duplicate_root(self):
        extra = Path(self.tmp.name) / "other root"
        extra.mkdir()
        custom = extra / "tmux-1002"
        self.make_socket(custom)
        self.assertIn(f"DIR {custom}\n", self.scan(extra=extra))
        self.assertEqual(self.scan(root=extra, extra=extra).count("DIR "), 1)
        self.assertNotIn(f"DIR {custom}\n", self.scan(extra="relative/path"))

    def test_sh_and_dash_syntax(self):
        for shell in ("sh", "dash"):
            binary = shutil.which(shell)
            if binary is None:
                continue
            with self.subTest(shell=shell):
                result = subprocess.run(
                    [binary, "-n", str(MODULE)], capture_output=True, text=True
                )
                self.assertEqual(result.returncode, 0, result.stderr)
                directory = self.root / "tmux-1001"
                directory.mkdir(exist_ok=True)
                self.assertIn(f"DIR {directory}\n", self.scan(shell=binary))

    def test_existing_session_and_process_output_without_attach(self):
        marker = Path(self.tmp.name) / "tmux-calls"
        fake_tmux = self.stub_dir / "tmux"
        fake_tmux.write_text(
            "#!/bin/sh\n"
            'printf "%s\\n" "$1" >> "$TMUX_CALL_MARKER"\n'
            'case "$1" in\n'
            '  ls) printf "current-session: 1 windows\\n" ;;\n'
            '  -V) printf "tmux fixture version\\n" ;;\n'
            '  *) exit 99 ;;\n'
            "esac\n"
        )
        fake_tmux.chmod(0o755)
        fake_ps = self.stub_dir / "ps"
        fake_ps.write_text('#!/bin/sh\nprintf "other 123 tmux -L custom\\n"\n')
        fake_ps.chmod(0o755)
        env = os.environ.copy()
        env.update(
            PATH=str(self.stub_dir) + os.pathsep + env["PATH"],
            TMUX_CALL_MARKER=str(marker),
            TMUX_TMPDIR=str(self.root),
            FIXTURE_UID="2000",
            FIXTURE_GROUPS=str(os.getgid()),
            SEARCH_IN_FOLDER="",
            SED_RED="&",
            SED_RED_YELLOW="&",
            E="E",
            N="",
            C="",
        )
        setup = (
            'print_2title() { :; }\n'
            'print_info() { :; }\n'
            '. "$1"\n'
        )
        result = subprocess.run(
            ["sh", "-c", setup, "sh", str(MODULE)],
            env=env,
            capture_output=True,
            text=True,
            timeout=4,
            check=True,
        )
        self.assertIn("current-session: 1 windows", result.stdout)
        self.assertIn("other 123 tmux -L custom", result.stdout)
        self.assertEqual(marker.read_text().splitlines(), ["ls", "-V"])


if __name__ == "__main__":
    unittest.main()
