import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class UDisksCVE20267867Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.function_file = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "functions"
            / "checkUDisksCVE20267867.sh"
        )

    def _make_root(self, base, options="defaults,user"):
        root = base / "root"
        (root / "etc").mkdir(parents=True)
        (root / "etc" / "fstab").write_text(
            f"UUID=test /media/test ext4 {options} 0 2\n", encoding="utf-8"
        )
        (root / "etc" / "os-release").write_text(
            'ID=ubuntu\nVERSION_CODENAME=noble\nVERSION_ID="24.04"\n',
            encoding="utf-8",
        )
        (root / "var" / "lib" / "dpkg").mkdir(parents=True)
        daemon = root / "usr" / "libexec" / "udisks2" / "udisksd"
        daemon.parent.mkdir(parents=True)
        daemon.write_text('#!/bin/sh\ntouch "$EXECUTED_MARKER"\n', encoding="utf-8")
        daemon.chmod(0o755)
        service = (
            root
            / "usr"
            / "share"
            / "dbus-1"
            / "system-services"
            / "org.freedesktop.UDisks2.service"
        )
        service.parent.mkdir(parents=True)
        service.write_text(
            "[D-BUS Service]\nName=org.freedesktop.UDisks2\nExec=/usr/libexec/udisks2/udisksd\n",
            encoding="utf-8",
        )
        return root

    def _run_check(self, root, package_version, older_than_vendor_fix):
        bindir = root.parent / "bin"
        bindir.mkdir()
        dpkg_query = bindir / "dpkg-query"
        dpkg_query.write_text(
            '#!/bin/sh\nprintf "%s|%s\\n" "$FAKE_UDISKS_STATUS" "$FAKE_UDISKS_VERSION"\n',
            encoding="utf-8",
        )
        dpkg_query.chmod(0o755)
        dpkg = bindir / "dpkg"
        dpkg.write_text(
            '#!/bin/sh\n'
            'if [ "$1" = "--compare-versions" ]; then\n'
            '  [ "$FAKE_DPKG_VERSION_IS_OLDER" = "1" ]\n'
            '  exit\n'
            'fi\n'
            'exit 1\n',
            encoding="utf-8",
        )
        dpkg.chmod(0o755)
        rpm = bindir / "rpm"
        rpm.write_text("#!/bin/sh\nexit 1\n", encoding="utf-8")
        rpm.chmod(0o755)

        marker = root.parent / "executed"
        env = os.environ.copy()
        env.update(
            {
                "EXECUTED_MARKER": str(marker),
                "FAKE_UDISKS_STATUS": "install ok installed",
                "FAKE_UDISKS_VERSION": package_version,
                "FAKE_DPKG_VERSION_IS_OLDER": "1" if older_than_vendor_fix else "0",
                "PATH": f"{bindir}:{env['PATH']}",
            }
        )
        body = "\n".join(
            [
                f"ROOT_FOLDER={shlex.quote(str(root))}",
                "E=E",
                "SED_LIGHT_CYAN='&'",
                "SED_RED_YELLOW='&'",
                'print_3title() { echo "TITLE: $1"; }',
                "print_info() { :; }",
                f". {shlex.quote(str(self.function_file))}",
                "checkUDisksCVE20267867",
            ]
        )
        result = subprocess.run(
            ["sh", "-c", body],
            cwd=str(self.repo_root),
            env=env,
            capture_output=True,
            text=True,
            check=False,
        )
        return result, marker

    def test_vulnerable_ubuntu_package_and_fstab_entry_are_reported_passively(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir))
            result, marker = self._run_check(root, "2.10.1-6ubuntu1.4", True)
            self.assertFalse(marker.exists(), "udisksd must never be executed")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("TITLE: UDisks as-user mount authorization bypass", result.stdout)
        self.assertIn("VULNERABLE to CVE-2026-7867", result.stdout)
        self.assertIn("UUID=test /media/test ext4 defaults,user 0 2", result.stdout)

    def test_vendor_fixed_ubuntu_package_is_suppressed(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir))
            result, _ = self._run_check(root, "2.10.1-6ubuntu1.5", False)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)

    def test_unaffected_pre_210_upstream_version_is_suppressed(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir))
            (root / "etc" / "os-release").write_text(
                "ID=debian\nVERSION_CODENAME=bookworm\n", encoding="utf-8"
            )
            result, _ = self._run_check(root, "2.9.4-4+deb12u2", False)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)

    def test_nonqualifying_nouser_option_is_suppressed(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir), options="defaults,nouser")
            result, _ = self._run_check(root, "2.10.1-6ubuntu1.4", True)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)

    def test_missing_dbus_activation_service_is_suppressed(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir))
            (
                root
                / "usr"
                / "share"
                / "dbus-1"
                / "system-services"
                / "org.freedesktop.UDisks2.service"
            ).unlink()
            result, _ = self._run_check(root, "2.10.1-6ubuntu1.4", True)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)

    def test_version_comparison_handles_double_digit_components(self):
        body = "\n".join(
            [
                f". {shlex.quote(str(self.function_file))}",
                "ud7867_version_lt 2.10.91 2.11.2 && echo old=yes",
                "ud7867_version_lt 2.11.10 2.11.2 || echo new=yes",
            ]
        )
        result = subprocess.run(
            ["sh", "-c", body],
            cwd=str(self.repo_root),
            capture_output=True,
            text=True,
            check=False,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("old=yes", result.stdout)
        self.assertIn("new=yes", result.stdout)


if __name__ == "__main__":
    unittest.main()
