import os
import shlex
import stat
import subprocess
import tempfile
import unittest
from pathlib import Path


class BelowLogDirectoryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/7_software_information/Below_Log_Directory.sh"
        )

    def _fixture(self, base, directory_mode=0o777, installed=True):
        old_umask = os.umask(0)
        try:
            directory = base / "below-logs"
            directory.mkdir(mode=directory_mode)
            binary = base / "below"
            if installed:
                fd = os.open(binary, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o755)
                os.close(fd)
        finally:
            os.umask(old_umask)
        self.assertEqual(stat.S_IMODE(directory.stat().st_mode), directory_mode)
        return binary, directory, directory / "error_root.log"

    def _run(self, binary, directory, log, sudo_rule=True, deny_access=False, symlink_log=False, ls_mode=None):
        sudo_text = f" (root) NOPASSWD: {binary} *" if sudo_rule is True else sudo_rule or ""
        script = "\n".join(
            [
                "SEARCH_IN_FOLDER=1",
                "MACPEAS=",
                f"sudo_l_output={shlex.quote(sudo_text)}",
                "sudo_l_cached_output=",
                "sudo_l_password_output=",
                "ps() { return 1; }",
                'print_2title() { printf "TITLE: %s\\n" "$1"; }',
                f". {shlex.quote(str(self.module))}",
                (
                    f"ls() {{ printf '%s\\n' {shlex.quote(ls_mode)}; }}"
                    if ls_mode
                    else ":"
                ),
                "lp_below_effective_access() { return 1; }" if deny_access else ":",
                (
                    f"lp_below_is_symlink() {{ [ \"$1\" = {shlex.quote(str(log))} ]; }}"
                    if symlink_log
                    else ":"
                ),
                f"lp_below_check_log_directory {shlex.quote(str(binary))} "
                f"{shlex.quote(str(directory))} {shlex.quote(str(log))}",
            ]
        )
        return subprocess.run(["sh", "-c", script], capture_output=True, text=True, check=False)

    def test_writable_nonsticky_directory_and_root_sudo_rule(self):
        with tempfile.TemporaryDirectory() as temp:
            binary, directory, log = self._fixture(Path(temp))
            result = self._run(binary, directory, log)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("potential privileged log-file exposure", result.stdout)
        self.assertIn("effective write/search access; nonsticky", result.stdout)
        self.assertIn("Patch status: unknown", result.stdout)
        self.assertNotIn("Vulnerable to CVE", result.stdout)

    def test_sticky_directory_is_excluded(self):
        with tempfile.TemporaryDirectory() as temp:
            binary, directory, log = self._fixture(Path(temp))
            result = self._run(binary, directory, log, ls_mode="drwxrwxrwt")
        self.assertEqual(result.stdout, "")

    def test_no_effective_access_is_excluded_even_if_mode_is_writable(self):
        with tempfile.TemporaryDirectory() as temp:
            binary, directory, log = self._fixture(Path(temp))
            result = self._run(binary, directory, log, deny_access=True, ls_mode="drwxrwxrwx+")
        self.assertEqual(result.stdout, "")

    def test_owner_writable_0755_uses_effective_access_not_world_bits(self):
        with tempfile.TemporaryDirectory() as temp:
            binary, directory, log = self._fixture(Path(temp), 0o755)
            result = self._run(binary, directory, log)
        self.assertIn("effective write/search access", result.stdout)

    def test_installed_binary_and_privileged_path_are_both_required(self):
        with tempfile.TemporaryDirectory() as temp:
            binary, directory, log = self._fixture(Path(temp), installed=False)
            self.assertEqual(self._run(binary, directory, log).stdout, "")
            old_umask = os.umask(0)
            try:
                fd = os.open(binary, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o755)
                os.close(fd)
            finally:
                os.umask(old_umask)
            self.assertEqual(self._run(binary, directory, log, sudo_rule=False).stdout, "")

    def test_negated_sudo_rule_is_not_privileged_evidence(self):
        with tempfile.TemporaryDirectory() as temp:
            binary, directory, log = self._fixture(Path(temp))
            result = self._run(binary, directory, log, sudo_rule=f" (root) NOPASSWD: !{binary}")
        self.assertEqual(result.stdout, "")

    def test_existing_symlink_is_labeled_without_reading_target(self):
        with tempfile.TemporaryDirectory() as temp:
            binary, directory, log = self._fixture(Path(temp))
            result = self._run(binary, directory, log, symlink_log=True)
        self.assertIn("is a symlink (target not read)", result.stdout)


if __name__ == "__main__":
    unittest.main()
