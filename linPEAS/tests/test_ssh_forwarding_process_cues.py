import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


PARTS = Path(__file__).resolve().parents[1] / "builder/linpeas_parts"
SSH_PART = PARTS / "7_software_information/Ssh.sh"
PROCESS_REGEX = PARTS / "variables/processesVB.sh"


class SshForwardingTextReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.functions = SSH_PART.read_text(encoding="utf-8").split(
            'print_2title "Searching ssl/ssh files"', 1
        )[0]

    def _review(self, config):
        with tempfile.TemporaryDirectory() as tmp:
            source = Path(tmp) / "functions.sh"
            source.write_text(self.functions, encoding="utf-8")
            script = (
                'print_3title() { printf "[%s]\\n" "$1"; }; '
                f'. {shlex.quote(str(source))}; '
                f'ssh_forwarding_config_review {shlex.quote(str(config))}'
            )
            return subprocess.run(
                ["sh", "-c", script], capture_output=True, text=True, timeout=5
            )

    def test_global_and_match_scope_with_comments_and_include(self):
        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "sshd_config"
            config.write_text(
                "# AllowTcpForwarding yes\n"
                "Include /etc/ssh/sshd_config.d/*.conf # extra settings\n"
                "AllowTcpForwarding yes\n"
                "Match User restricted\n"
                "  ForceCommand internal-sftp\n"
                "  ChrootDirectory /srv/sftp\n"
                "  PermitOpen 127.0.0.1:9229\n"
                "  DisableForwarding yes # later restriction\n"
                "Match Group administrators\n"
                "  AllowTcpForwarding no\n",
                encoding="utf-8",
            )
            result = self._review(config)
            self.assertEqual(result.returncode, 0, result.stderr)
            output = result.stdout
            self.assertIn("2 [global]: Include /etc/ssh/sshd_config.d/*.conf", output)
            self.assertIn("3 [global]: AllowTcpForwarding yes", output)
            self.assertIn("4: Match User restricted", output)
            self.assertIn("5 [Match User restricted]: ForceCommand internal-sftp", output)
            self.assertIn("6 [Match User restricted]: ChrootDirectory /srv/sftp", output)
            self.assertIn("7 [Match User restricted]: PermitOpen 127.0.0.1:9229", output)
            self.assertIn("8 [Match User restricted]: DisableForwarding yes", output)
            self.assertIn("10 [Match Group administrators]: AllowTcpForwarding no", output)
            self.assertNotIn("# AllowTcpForwarding", output)
            self.assertNotIn("extra settings", output)
            self.assertNotIn("later restriction", output)
            self.assertIn("Include targets", output)
            self.assertIn("authorized-key restrictions", output)
            self.assertNotIn("vulnerable", output.lower())

    def test_force_command_and_forwarding_no_are_review_only(self):
        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "sshd_config"
            config.write_text(
                "Match User restricted\n"
                "  ForceCommand internal-sftp\n"
                "  AllowTcpForwarding no\n",
                encoding="utf-8",
            )
            result = self._review(config)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("[Match User restricted]: AllowTcpForwarding no", result.stdout)
            self.assertIn("review effective SSH policy", result.stdout)
            self.assertNotIn("exploitable", result.stdout.lower())

    def test_authorized_key_helper_directives_keep_scope_and_absence_unknown(self):
        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "sshd_config"
            config.write_text(
                "# AuthorizedKeysCommand /ignored/helper\n"
                "AuthorizedKeysCommand /usr/local/libexec/key-lookup %u %k\n"
                "AuthorizedKeysCommandUser nobody\n"
                "Match User restricted\n"
                "  AuthorizedKeysCommand none\n"
                "  AuthorizedKeysCommandUser root # scoped override\n",
                encoding="utf-8",
            )
            result = self._review(config)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn(
                "2 [global]: AuthorizedKeysCommand /usr/local/libexec/key-lookup %u %k",
                result.stdout,
            )
            self.assertIn("3 [global]: AuthorizedKeysCommandUser nobody", result.stdout)
            self.assertIn(
                "5 [Match User restricted]: AuthorizedKeysCommand none", result.stdout
            )
            self.assertIn(
                "6 [Match User restricted]: AuthorizedKeysCommandUser root", result.stdout
            )
            self.assertNotIn("/ignored/helper", result.stdout)
            self.assertNotIn("scoped override", result.stdout)
            self.assertIn("do not prove execution or vulnerability", result.stdout)

            config.write_text("PermitRootLogin prohibit-password\n", encoding="utf-8")
            absent = self._review(config)
            self.assertEqual(absent.returncode, 0, absent.stderr)
            self.assertNotIn("AuthorizedKeysCommand", absent.stdout)
            self.assertNotIn("key-lookup", absent.stdout)

    def test_unreadable_or_missing_config_remains_unknown(self):
        result = self._review(Path("/does/not/exist/sshd_config"))
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("forwarding policy unknown", result.stdout)

    def test_config_read_is_bounded(self):
        with tempfile.TemporaryDirectory() as tmp:
            config = Path(tmp) / "sshd_config"
            config.write_text(
                "# filler\n" * 512 + "AllowTcpForwarding yes\n",
                encoding="utf-8",
            )
            result = self._review(config)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertNotIn("AllowTcpForwarding yes", result.stdout)
            self.assertIn("first 512 lines", result.stdout)


class ProcessHighlightTests(unittest.TestCase):
    def _highlight(self, line):
        script = (
            f'. {shlex.quote(str(PROCESS_REGEX))}; '
            'printf "%s\\n" "$PROCESS_LINE" | '
            'sed -E "s,$processesVB,[&],g"'
        )
        env = os.environ.copy()
        env["PROCESS_LINE"] = line
        return subprocess.run(
            ["sh", "-c", script], env=env, capture_output=True, text=True, timeout=5
        )

    def test_inspector_and_chromedriver_executable_highlight(self):
        for line, expected in (
            ("dev node --inspect=127.0.0.1:9229", "[ --inspect=]"),
            ("dev node --inspect-brk", "[ --inspect-brk]"),
            ("root /root/chromedriver --port=9515", "[/chromedriver ]"),
            ("root chromedriver", "[ chromedriver]"),
        ):
            with self.subTest(line=line):
                result = self._highlight(line)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertIn(expected, result.stdout)

    def test_similar_names_do_not_highlight(self):
        for line in (
            "dev node --inspector-only",
            "root /root/chromedriver-helper --port=9515",
            "root /root/chromedriver.old --port=9515",
            "root /usr/bin/google-chrome --port=9515",
        ):
            with self.subTest(line=line):
                result = self._highlight(line)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertNotIn("[", result.stdout)


if __name__ == "__main__":
    unittest.main()
