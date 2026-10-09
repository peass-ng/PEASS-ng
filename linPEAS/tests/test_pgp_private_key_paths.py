import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class PgpPrivateKeyPathsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.root = Path(__file__).resolve().parents[2]
        cls.module = cls.root / "linPEAS/builder/linpeas_parts/6_users_information/5_Pgp_keys.sh"
        cls.helpers = cls.module.read_text(encoding="utf-8").split("# Check for GPG", 1)[0]

    def scan(self, homes, *, current_home=None):
        with tempfile.TemporaryDirectory() as temp:
            helper_file = Path(temp) / "helpers.sh"
            helper_file.write_text(self.helpers, encoding="utf-8")
            env = os.environ.copy()
            env["HOME"] = str(current_home or Path(temp) / "current-home")
            result = subprocess.run(
                ["sh", "-c", f". {shlex.quote(str(helper_file))}; pgp_private_key_candidates"],
                input="".join(f"{home}\n" for home in homes),
                capture_output=True,
                text=True,
                env=env,
                timeout=5,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            return result.stdout

    @staticmethod
    def make_home(root, name, *, key_name="sample.key"):
        home = root / name
        keydir = home / ".gnupg/private-keys-v1.d"
        keydir.mkdir(parents=True)
        key = keydir / key_name
        key.write_text("PRIVATE-KEY-CONTENT-DO-NOT-PRINT\n", encoding="utf-8")
        return home, key

    def test_cross_user_key_and_nearby_ciphertext_are_metadata_only(self):
        with tempfile.TemporaryDirectory() as temp:
            home, key = self.make_home(Path(temp).resolve(), "other user")
            backup = home / "backup"
            backup.mkdir()
            ciphertext = backup / "keyvault.gpg"
            ciphertext.write_text("CIPHERTEXT-DO-NOT-PRINT\n")
            output = self.scan([home])
            self.assertIn(str(key), output)
            self.assertIn("Readable GnuPG private key (review candidate)", output)
            self.assertIn(f"yes: {ciphertext}", output)
            self.assertRegex(output, r"(?m)^[-dl][rwx-]{9}[@+]?\s+\d+\s+")
            self.assertNotIn("PRIVATE-KEY-CONTENT", output)
            self.assertNotIn("CIPHERTEXT-DO-NOT-PRINT", output)

    def test_no_ciphertext_and_public_key_only(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp).resolve()
            home, key = self.make_home(root, "has-secret")
            public = root / "public-only/.gnupg"
            public.mkdir(parents=True)
            (public / "pubring.kbx").write_text("public")
            output = self.scan([home, public.parent])
            self.assertIn(str(key), output)
            self.assertIn("Readable nearby .gpg ciphertext: no", output)
            self.assertNotIn("pubring.kbx", output)

    def test_current_home_and_symlinked_backup_are_not_reported(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp).resolve()
            home, key = self.make_home(root, "other")
            outside = root / "outside"
            outside.mkdir()
            (outside / "vault.gpg").write_text("ciphertext")
            (home / "backup").symlink_to(outside, target_is_directory=True)
            output = self.scan([home])
            self.assertIn(str(key), output)
            self.assertIn("Readable nearby .gpg ciphertext: no", output)
            self.assertEqual(self.scan([home], current_home=home), "")

    def test_unreadable_missing_traversal_and_symlinks_are_ignored(self):
        if os.geteuid() == 0:
            self.skipTest("permission fixture requires a non-root user")
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp).resolve()
            unreadable_home, unreadable_key = self.make_home(root, "unreadable")
            unreadable_key.chmod(0)
            blocked_home, _ = self.make_home(root, "blocked")
            blocked_dir = blocked_home / ".gnupg"
            blocked_dir.chmod(0o600)
            valid_home, valid_key = self.make_home(root, "valid")
            (valid_key.parent / "linked.key").symlink_to(valid_key)
            linked_home = root / "linked-home"
            linked_home.symlink_to(valid_home, target_is_directory=True)
            try:
                output = self.scan([unreadable_home, blocked_home, valid_home, linked_home])
            finally:
                blocked_dir.chmod(0o700)
            self.assertEqual(output.count("Readable GnuPG private key"), 1)
            self.assertIn(str(valid_key), output)
            self.assertNotIn(str(unreadable_key), output)
            self.assertNotIn("linked.key", output)
            self.assertNotIn(str(linked_home), output)

    def test_candidate_and_home_caps(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp).resolve()
            early, _ = self.make_home(root, "early")
            for index in range(30):
                (early / ".gnupg/private-keys-v1.d" / f"k{index:02d}.key").write_text("secret")
            late, late_key = self.make_home(root, "late")
            output = self.scan([early, late])
            self.assertEqual(output.count("Readable GnuPG private key"), 24)
            self.assertNotIn(str(late_key), output)
            output = self.scan([root / f"absent-{i}" for i in range(64)] + [late])
            self.assertNotIn(str(late_key), output)

    def test_shell_syntax_and_yaml_selector(self):
        result = subprocess.run(["sh", "-n", str(self.module)], capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)
        yaml = (self.root / "build_lists/sensitive_files.yaml").read_text(encoding="utf-8")
        self.assertNotIn('name: "private-keys-v1.d/*.key"', yaml)


if __name__ == "__main__":
    unittest.main()
