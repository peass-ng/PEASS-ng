import os
import shutil
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SshUserCaCorrelationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.part = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/7_software_information/Ssh.sh"
        )
        cls.functions = cls.part.read_text(encoding="utf-8").split(
            'print_2title "Searching ssl/ssh files"', 1
        )[0]

    def _run(self, root, shell="sh", path_prefix=None):
        with tempfile.TemporaryDirectory() as tmp:
            source = Path(tmp) / "functions.sh"
            source.write_text(self.functions, encoding="utf-8")
            script = (
                'print_3title() { printf "[%s]\\n" "$1"; }; '
                f'. {shlex.quote(str(source))}; ssh_ca_trust_correlation'
            )
            env = os.environ.copy()
            env.update({"ROOT_FOLDER": str(root), "SEARCH_IN_FOLDER": "1"})
            if path_prefix:
                env["PATH"] = f"{path_prefix}:{env['PATH']}"
            return subprocess.run(
                [shell, "-c", script],
                env=env,
                capture_output=True,
                text=True,
                check=False,
                timeout=5,
            )

    def test_included_ca_and_exact_sibling_report_metadata_only(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            ssh = root / "etc/ssh"
            includes = ssh / "sshd_config.d"
            includes.mkdir(parents=True)
            (ssh / "sshd_config").write_text(
                "Include /etc/ssh/sshd_config.d/*.conf\n", encoding="utf-8"
            )
            (includes / "20-ca.conf").write_text(
                "# TrustedUserCAKeys /ignored.pub\n"
                "TrustedUserCAKeys /opt/keys/user-ca.pub # trailing comment\n"
                "AuthorizedPrincipalsFile /etc/ssh/principals/%u\n"
                "PermitRootLogin prohibit-password\n",
                encoding="utf-8",
            )
            keys = root / "opt/keys"
            keys.mkdir(parents=True)
            (keys / "user-ca.pub").write_text("ssh-ed25519 AAAATEST\n", encoding="utf-8")
            private = keys / "user-ca"
            private.write_text(
                "-----BEGIN OPENSSH PRIVATE KEY-----\nsecret-material\n",
                encoding="utf-8",
            )
            private.chmod(0o600)
            (keys / "unrelated").write_text(
                "-----BEGIN OPENSSH PRIVATE KEY-----\n", encoding="utf-8"
            )
            result = self._run(root)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("partial config inspection", result.stdout)
            self.assertIn("TrustedUserCAKeys /opt/keys/user-ca.pub", result.stdout)
            self.assertIn("AuthorizedPrincipalsFile".lower(), result.stdout)
            self.assertIn("PermitRootLogin prohibit-password", result.stdout)
            self.assertIn(f"Readable CA private-key sibling: {private}", result.stdout)
            self.assertIn("owner mode:", result.stdout)
            self.assertNotIn("secret-material", result.stdout)
            self.assertNotIn("unrelated", result.stdout)
            self.assertNotIn("/ignored.pub", result.stdout)
            self.assertNotIn("root certificate login is established", result.stdout)

    def test_match_context_and_command_are_conditional(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            ssh = root / "etc/ssh"
            ssh.mkdir(parents=True)
            (ssh / "sshd_config").write_text(
                "Match User deploy\n"
                "  TrustedUserCAKeys /opt/keys/ca.pub\n"
                "  AuthorizedPrincipalsCommand /usr/local/bin/principals %u\n"
                "  PermitRootLogin no\n",
                encoding="utf-8",
            )
            result = self._run(root)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("Match (conditional)", result.stdout)
            self.assertIn("authorizedprincipalscommand /usr/local/bin/principals", result.stdout)
            self.assertIn("PermitRootLogin no", result.stdout)
            self.assertNotIn("Readable CA private-key sibling", result.stdout)

    def test_bad_header_symlink_and_config_text_do_not_trigger(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            ssh = root / "etc/ssh"
            ssh.mkdir(parents=True)
            marker = root / "executed"
            (ssh / "sshd_config").write_text(
                f"Include $(touch {marker})\n"
                "TrustedUserCAKeys /opt/keys/ca.pub\n",
                encoding="utf-8",
            )
            keys = root / "opt/keys"
            keys.mkdir(parents=True)
            (keys / "ca.pub").write_text("ssh-ed25519 AAAATEST\n", encoding="utf-8")
            private = keys / "ca"
            private.write_text("not a key\n", encoding="utf-8")
            result = self._run(root)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("TrustedUserCAKeys", result.stdout)
            self.assertIn("No AuthorizedPrincipalsFile/Command observed", result.stdout)
            self.assertIn("No PermitRootLogin observed", result.stdout)
            self.assertNotIn("Readable CA private-key sibling", result.stdout)
            self.assertFalse(marker.exists())
            private.write_text("-----BEGIN OPENSSH PRIVATE KEY-----\n", encoding="utf-8")
            link_target = keys / "actual"
            private.rename(link_target)
            private.symlink_to(link_target)
            result = self._run(root)
            self.assertNotIn("Readable CA private-key sibling", result.stdout)

    def test_line_cap_is_conservative(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            ssh = root / "etc/ssh"
            ssh.mkdir(parents=True)
            (ssh / "sshd_config").write_text(
                "# filler\n" * 256 + "TrustedUserCAKeys /opt/keys/ca.pub\n",
                encoding="utf-8",
            )
            result = self._run(root)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stdout, "")

    def test_bsd_stat_fallback(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            ssh = root / "etc/ssh"
            ssh.mkdir(parents=True)
            (ssh / "sshd_config").write_text(
                "TrustedUserCAKeys /opt/keys/ca.pub\n", encoding="utf-8"
            )
            keys = root / "opt/keys"
            keys.mkdir(parents=True)
            (keys / "ca.pub").write_text("ssh-ed25519 AAAATEST\n", encoding="utf-8")
            (keys / "ca").write_text(
                "-----BEGIN OPENSSH PRIVATE KEY-----\n", encoding="utf-8"
            )
            bin_dir = root / "bin"
            bin_dir.mkdir()
            fake_stat = bin_dir / "stat"
            fake_stat.write_text(
                '#!/bin/sh\n'
                'if [ "$1" = "-f" ]; then printf "owner 600\\n"; else exit 1; fi\n',
                encoding="utf-8",
            )
            fake_stat.chmod(0o755)
            result = self._run(root, path_prefix=bin_dir)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("owner mode: owner 600", result.stdout)

    @unittest.skipUnless(shutil.which("dash"), "dash unavailable")
    def test_dash_shell(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            ssh = root / "etc/ssh"
            ssh.mkdir(parents=True)
            (ssh / "sshd_config").write_text(
                "TrustedUserCAKeys /opt/keys/ca.pub\n", encoding="utf-8"
            )
            result = self._run(root, shell="dash")
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("partial config inspection", result.stdout)


if __name__ == "__main__":
    unittest.main()
