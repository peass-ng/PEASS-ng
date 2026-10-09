import os
import shlex
import shutil
import socket
import subprocess
import tempfile
import unittest
from pathlib import Path


class SshControlSocketTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.part = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/7_software_information/Ssh.sh"
        )
        cls.functions = cls.part.read_text(encoding="utf-8").split(
            'print_2title "Searching ssl/ssh files"', 1
        )[0]

    def run_scan(self, home, *, offline=False, shell="sh"):
        with tempfile.TemporaryDirectory(prefix="ssh-test-") as temp:
            functions = Path(temp) / "functions.sh"
            functions.write_text(self.functions, encoding="utf-8")
            marker = Path(temp) / "ssh-called"
            fake_ssh = Path(temp) / "ssh"
            fake_ssh.write_text(
                "#!/bin/sh\n: > " + shlex.quote(str(marker)) + "\n", encoding="utf-8"
            )
            fake_ssh.chmod(0o755)
            env = os.environ.copy()
            env.update({
                "PATH": f"{temp}:{env['PATH']}",
                "SEARCH_IN_FOLDER": "1" if offline else "",
            })
            command = (
                'print_3title() { printf "[%s]\\n" "$1"; }; '
                f'. {shlex.quote(str(functions))}; '
                f'ssh_control_socket_candidates {shlex.quote(str(home))}'
            )
            result = subprocess.run(
                [shell, "-c", command], env=env, capture_output=True,
                text=True, timeout=5,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertFalse(marker.exists(), "the selector must not call SSH")
            return result.stdout

    def test_root_style_home_socket_and_metadata_without_attach(self):
        with tempfile.TemporaryDirectory(prefix="ssh-mux-") as temp:
            home = Path(temp) / "root"
            control_dir = home / ".ssh" / "controlmaster"
            control_dir.mkdir(parents=True)
            config = home / ".ssh" / "config"
            config.write_text("ControlMaster auto\nControlPath ~/.ssh/controlmaster/%r@%h:%p\n")
            path = control_dir / "remote:22"
            sock = socket.socket(socket.AF_UNIX)
            try:
                sock.bind(str(path))
                path.chmod(0o600)
                output = self.run_scan(home)
                for shell in ("dash", "ksh"):
                    if shutil.which(shell):
                        self.assertIn(str(path), self.run_scan(home, shell=shell))
            finally:
                sock.close()
            self.assertIn(str(path), output)
            self.assertIn("ControlMaster candidates", output)
            self.assertIn("Path, owner and mode only", output)
            self.assertIn("srw", output)
            self.assertNotIn("ControlPath ~/.ssh", output)

    def test_depth_type_and_offline_guard(self):
        with tempfile.TemporaryDirectory(prefix="ssh-mux-") as temp:
            home = Path(temp) / "user home"
            first = home / ".ssh"
            first.mkdir(parents=True)
            direct = first / "direct.sock"
            deep_dir = first / "a" / "b"
            deep_dir.mkdir(parents=True)
            deep = deep_dir / "deep.sock"
            regular = first / "regular.sock"
            regular.write_text("ordinary file")
            sock1 = socket.socket(socket.AF_UNIX)
            sock2 = socket.socket(socket.AF_UNIX)
            try:
                sock1.bind(str(direct))
                sock2.bind(str(deep))
                output = self.run_scan(home)
                self.assertIn(str(direct), output)
                self.assertNotIn(str(deep), output)
                self.assertNotIn(str(regular), output)
                self.assertEqual(self.run_scan(home, offline=True), "")
            finally:
                sock1.close()
                sock2.close()

    def test_output_cap(self):
        with tempfile.TemporaryDirectory(prefix="ssh-mux-") as temp:
            home = Path(temp)
            ssh_dir = home / ".ssh"
            ssh_dir.mkdir()
            sockets = []
            try:
                for index in range(25):
                    path = ssh_dir / f"s{index:02d}"
                    sock = socket.socket(socket.AF_UNIX)
                    sock.bind(str(path))
                    sockets.append(sock)
                output = self.run_scan(home)
                self.assertEqual(sum("/.ssh/s" in line for line in output.splitlines()), 20)
                self.assertIn("Output capped at 20 sockets", output)
            finally:
                for sock in sockets:
                    sock.close()

    def test_shell_syntax(self):
        result = subprocess.run(
            ["sh", "-n", str(self.part)], capture_output=True, text=True
        )
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
