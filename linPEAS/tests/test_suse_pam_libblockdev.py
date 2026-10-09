import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SusePamLibblockdevTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.parts = Path(__file__).resolve().parents[1] / "builder" / "linpeas_parts"

    def _write(self, root, path, data, executable=False):
        target = root / path
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(data, encoding="utf-8")
        if executable:
            target.chmod(0o755)
        return target

    def _run(self, root, function_name, source_name, package="1.0-1", config_package=None):
        bindir = root.parent / "bin"
        bindir.mkdir(exist_ok=True)
        self._write(bindir, "uname", "#!/bin/sh\necho Linux\n", True)
        self._write(bindir, "timeout", "#!/bin/sh\nshift\nexec \"$@\"\n", True)
        self._write(
            bindir,
            "rpm",
            '#!/bin/sh\n'
            'case " $* " in\n'
            '  *" pam-config "*) printf "%s\\n" "$FAKE_CONFIG_PACKAGE" ;;\n'
            '  *" pam "*) printf "%s\\n" "$FAKE_PAM_PACKAGE" ;;\n'
            '  *) printf "%s\\n" "$FAKE_BLOCKDEV_PACKAGE" ;;\n'
            'esac\n',
            True,
        )
        env = os.environ.copy()
        env.update(
            {
                "PATH": f"{bindir}:{env['PATH']}",
                "FAKE_PAM_PACKAGE": package,
                "FAKE_CONFIG_PACKAGE": config_package if config_package is not None else package,
                "FAKE_BLOCKDEV_PACKAGE": package,
                "EXECUTED_MARKER": str(root.parent / "executed"),
            }
        )
        body = "\n".join(
            (
                f"ROOT_FOLDER={shlex.quote(str(root))}",
                'print_3title() { printf "TITLE: %s\\n" "$1"; }',
                "print_info() { :; }",
                f". {shlex.quote(str(self.parts / 'functions' / 'lp_suse_numeric_version_lt.sh'))}",
                f". {shlex.quote(str(self.parts / 'functions' / source_name))}",
                function_name,
            )
        )
        result = subprocess.run(
            ["sh", "-c", body], env=env, text=True, capture_output=True, check=False
        )
        self.assertFalse((root.parent / "executed").exists(), "udisksd must not run")
        return result

    def _pam_root(self, root, env_option="user_readenv=1", distro="sles"):
        self._write(root, "etc/os-release", f'ID={distro}\nVERSION_ID="15.6"\n')
        self._write(
            root, "etc/pam.d/sshd", "auth include common-auth\nsession include common-session\n"
        )
        self._write(root, "etc/pam.d/common-auth", f"auth required pam_env.so {env_option}\n")
        self._write(root, "etc/pam.d/common-session", "session optional pam_systemd.so\n")

    def test_suse_ssh_pam_path_reports_evidence_and_unknown_patch_status(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / "root"
            self._pam_root(root)
            result = self._run(root, "checkPamAllowActiveCVE20256018", "checkPamAllowActiveCVE20256018.sh")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("CVE-2025-6018", result.stdout)
        self.assertIn("explicit user_readenv=1", result.stdout)
        self.assertIn("pam-config package: 1.0-1", result.stdout)
        self.assertIn("explicit user_readenv=1 in the effective auth stack", result.stdout)
        self.assertIn("effective patch status: unknown", result.stdout)

    def test_disabled_user_environment_or_non_suse_is_suppressed(self):
        for option, distro in (("user_readenv=0", "sles"), ("user_readenv=1", "debian")):
            with self.subTest(option=option, distro=distro), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp) / "root"
                self._pam_root(root, option, distro)
                result = self._run(root, "checkPamAllowActiveCVE20256018", "checkPamAllowActiveCVE20256018.sh")
                self.assertEqual(result.stdout, "")

    def test_pam_path_requires_session_module(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / "root"
            self._pam_root(root)
            self._write(root, "etc/pam.d/common-session", "session optional pam_unix.so\n")
            result = self._run(root, "checkPamAllowActiveCVE20256018", "checkPamAllowActiveCVE20256018.sh")
        self.assertEqual(result.stdout, "")

    def test_pam_patched_or_unknown_implicit_default_is_suppressed(self):
        for package in ("9.0-1", "unknown"):
            with self.subTest(package=package), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp) / "root"
                self._pam_root(root, "")
                result = self._run(root, "checkPamAllowActiveCVE20256018", "checkPamAllowActiveCVE20256018.sh", package)
                self.assertEqual(result.stdout, "")

    def test_explicit_setting_remains_a_candidate_after_pam_update(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / "root"
            self._pam_root(root, "user_readenv=1")
            result = self._run(root, "checkPamAllowActiveCVE20256018", "checkPamAllowActiveCVE20256018.sh", "1.3.0-150000.6.83.1")
        self.assertIn("explicit user_readenv=1", result.stdout)

    def test_stale_auth_stack_with_old_pam_is_reported_after_config_update(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / "root"
            self._pam_root(root, "")
            result = self._run(root, "checkPamAllowActiveCVE20256018", "checkPamAllowActiveCVE20256018.sh", "1.3.0-150000.6.83.0", "1.1-150600.16.8.1")
        self.assertIn("pam below SUSE fixed package version", result.stdout)

    def test_leap_implicit_environment_with_both_old_packages_is_reported(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / "root"
            self._pam_root(root, "", "opensuse-leap")
            result = self._run(root, "checkPamAllowActiveCVE20256018", "checkPamAllowActiveCVE20256018.sh")
        self.assertIn("CVE-2025-6018", result.stdout)
        self.assertIn("implicit user_readenv", result.stdout)

    def _blockdev_root(self, root, active="yes", distro="sles"):
        self._write(root, "etc/os-release", f'ID={distro}\nVERSION_ID="15.6"\n')
        self._write(
            root,
            "usr/libexec/udisks2/udisksd",
            '#!/bin/sh\ntouch "$EXECUTED_MARKER"\n',
            True,
        )
        self._write(root, "usr/sbin/xfs_growfs", "#!/bin/sh\nexit 1\n", True)
        self._write(
            root,
            "usr/share/dbus-1/system-services/org.freedesktop.UDisks2.service",
            "[D-BUS Service]\nName=org.freedesktop.UDisks2\n",
        )
        self._write(
            root,
            "usr/share/polkit-1/actions/org.freedesktop.udisks2.policy",
            '<action id="org.freedesktop.udisks2.modify-device">\n'
            f"<allow_active>{active}</allow_active>\n"
            "</action>\n",
        )

    def test_libblockdev_path_reports_passive_evidence(self):
        for distro in ("sles", "opensuse-leap"):
            with self.subTest(distro=distro), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp) / "root"
                self._blockdev_root(root, distro=distro)
                result = self._run(root, "checkLibblockdevCVE20256019", "checkLibblockdevCVE20256019.sh", "2.26-150400.3.4.1")
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertIn("CVE-2025-6019", result.stdout)
                self.assertIn("libbd_fs2 package: 2.26-150400.3.4.1", result.stdout)
                self.assertIn("Effective patch status: unknown", result.stdout)

    def test_libblockdev_path_requires_active_policy_and_xfs_tool(self):
        for remove_tool, active in ((False, "auth_admin"), (True, "yes")):
            with self.subTest(remove_tool=remove_tool, active=active), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp) / "root"
                self._blockdev_root(root, active)
                if remove_tool:
                    (root / "usr/sbin/xfs_growfs").unlink()
                result = self._run(root, "checkLibblockdevCVE20256019", "checkLibblockdevCVE20256019.sh")
                self.assertEqual(result.stdout, "")

    def test_libblockdev_patched_and_unknown_packages_are_suppressed(self):
        for package in ("2.26-150400.3.5.1", "unknown"):
            with self.subTest(package=package), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp) / "root"
                self._blockdev_root(root)
                result = self._run(root, "checkLibblockdevCVE20256019", "checkLibblockdevCVE20256019.sh", package)
                self.assertEqual(result.stdout, "")

    def test_generic_non_suse_package_does_not_get_a_cve_finding(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / "root"
            self._blockdev_root(root, distro="debian")
            result = self._run(root, "checkLibblockdevCVE20256019", "checkLibblockdevCVE20256019.sh", "2.26-150400.3.4.1")
        self.assertEqual(result.stdout, "")


if __name__ == "__main__":
    unittest.main()
