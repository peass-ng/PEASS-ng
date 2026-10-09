import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SshRootKeyFallbackTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/7_software_information/Ssh.sh"
        )
        cls.functions = cls.module.read_text(encoding="utf-8").split(
            'print_2title "Searching ssl/ssh files"', 1
        )[0]

    def scan(self, root, *, uid="0"):
        with tempfile.TemporaryDirectory() as temp:
            bindir = Path(temp) / "bin"
            bindir.mkdir()
            fake_id = bindir / "id"
            fake_id.write_text(f"#!/bin/sh\nprintf '%s\\n' {shlex.quote(uid)}\n")
            fake_id.chmod(0o755)
            source = Path(temp) / "functions.sh"
            source.write_text(self.functions, encoding="utf-8")
            env = os.environ.copy()
            env["PATH"] = f"{bindir}:{env['PATH']}"
            result = subprocess.run(
                ["sh", "-c", f". {shlex.quote(str(source))}; "
                 f"ssh_root_key_headers {shlex.quote(str(root))}"],
                env=env, capture_output=True, text=True, timeout=5,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            return result.stdout

    def test_direct_arbitrary_key_reports_metadata_without_bytes(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            key = root / "arbitrary-key"
            key.write_text(
                "-----BEGIN OPENSSH PRIVATE KEY-----\nsecret-material\n",
                encoding="utf-8",
            )
            key.chmod(0o600)
            (root / "arbitrary-key.pub").write_text("ssh-ed25519 AAAATEST\n")
            (root / "ordinary").write_text("plain text\n")
            nested = root / "nested"
            nested.mkdir()
            (nested / "another-key").write_text(
                "-----BEGIN OPENSSH PRIVATE KEY-----\nsecret-nested\n"
            )
            output = self.scan(root)
            self.assertIn(str(key), output)
            self.assertIn("owner mode:", output)
            self.assertIn("header only checked", output)
            self.assertNotIn("secret-material", output)
            self.assertNotIn("secret-nested", output)
            self.assertNotIn("another-key", output)
            self.assertNotIn("ordinary", output)
            self.assertNotIn("arbitrary-key.pub", output)
            self.assertEqual(self.scan(root, uid="1000"), "")

    def test_unreadable_symlink_oversized_and_over_cap_are_skipped(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            unreadable = root / "a-unreadable"
            unreadable.write_text("-----BEGIN OPENSSH PRIVATE KEY-----\n")
            unreadable.chmod(0)
            (root / "b-symlink").symlink_to(unreadable)
            (root / "c-oversized").write_text(
                "-----BEGIN OPENSSH PRIVATE KEY-----\n" + "x" * 1048576
            )
            for index in range(64):
                (root / f"d-{index:02d}").write_text("plain text\n")
            late = root / "z-late-key"
            late.write_text("-----BEGIN OPENSSH PRIVATE KEY-----\nsecret-late\n")
            output = self.scan(root)
            self.assertEqual(output, "")

    def test_shell_syntax_metadata_and_fallback_wiring(self):
        from linPEAS.builder.src.linpeasModule import LinpeasModule

        module = LinpeasModule(str(self.module))
        self.assertEqual(module.id, "SI_Ssh")
        self.assertIn("privatekeyfilesroot=$(ssh_root_key_headers /root)",
                      self.module.read_text(encoding="utf-8"))
        result = subprocess.run(
            ["sh", "-n", str(self.module)], capture_output=True, text=True
        )
        self.assertEqual(result.returncode, 0, result.stderr)


if __name__ == "__main__":
    unittest.main()
