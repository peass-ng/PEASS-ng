import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class NeedrestartCVE202448990Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.function_file = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "functions"
            / "checkNeedrestartCVE202448990.sh"
        )

    def _make_root(self, base, main_value=None, snippet_value=None):
        root = base / "root"
        (root / "etc" / "needrestart" / "conf.d").mkdir(parents=True)
        (root / "var" / "lib" / "dpkg").mkdir(parents=True)
        (root / "etc" / "os-release").write_text(
            'ID=ubuntu\nVERSION_CODENAME=noble\nVERSION_ID="24.04"\n',
            encoding="utf-8",
        )
        if main_value is not None:
            (root / "etc" / "needrestart" / "needrestart.conf").write_text(
                f"$nrconf{{interpscan}} = {main_value};\n", encoding="utf-8"
            )
        if snippet_value is not None:
            (root / "etc" / "needrestart" / "conf.d" / "99-security.conf").write_text(
                f"$nrconf{{'interpscan'}} = {snippet_value};\n", encoding="utf-8"
            )
        return root

    def _run_check(self, root, package_version, package_status="install ok installed"):
        bindir = root.parent / "bin"
        bindir.mkdir()
        dpkg_query = bindir / "dpkg-query"
        dpkg_query.write_text(
            '#!/bin/sh\nprintf "%s|%s\\n" "$FAKE_NEEDRESTART_STATUS" '
            '"$FAKE_NEEDRESTART_VERSION"\n',
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

        env = os.environ.copy()
        env.update(
            {
                "FAKE_NEEDRESTART_STATUS": package_status,
                "FAKE_NEEDRESTART_VERSION": package_version,
                "FAKE_DPKG_VERSION_IS_OLDER": (
                    "1" if package_version == "3.6-7ubuntu4.2" else "0"
                ),
                "PATH": f"{bindir}:{env['PATH']}",
            }
        )
        body = "\n".join(
            [
                f"ROOT_FOLDER={shlex.quote(str(root))}",
                "E=E",
                "SED_GREEN='&'",
                "SED_LIGHT_CYAN='&'",
                "SED_RED_YELLOW='&'",
                "SED_YELLOW='&'",
                'print_3title() { echo "TITLE: $1"; }',
                "print_info() { :; }",
                f". {shlex.quote(str(self.function_file))}",
                "checkNeedrestartCVE202448990",
            ]
        )
        return subprocess.run(
            ["sh", "-c", body],
            cwd=str(self.repo_root),
            env=env,
            capture_output=True,
            text=True,
            check=False,
        )

    def test_vulnerable_ubuntu_package_with_default_scanner_is_reported(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir))
            result = self._run_check(root, "3.6-7ubuntu4.2")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("TITLE: Needrestart interpreter-scanner LPE", result.stdout)
        self.assertIn("VULNERABLE to CVE-2024-48990", result.stdout)
        self.assertIn("Effective interpreter scanning: 1", result.stdout)

    def test_vendor_fixed_ubuntu_package_is_not_flagged(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir))
            result = self._run_check(root, "3.6-7ubuntu4.3")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("is not vulnerable to CVE-2024-48990", result.stdout)
        self.assertNotIn("VULNERABLE to CVE-2024-48990", result.stdout)

    def test_removed_package_with_remaining_config_is_ignored(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir))
            result = self._run_check(
                root,
                "3.6-7ubuntu4.2",
                package_status="deinstall ok config-files",
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertNotIn("Needrestart interpreter-scanner LPE", result.stdout)
        self.assertNotIn("VULNERABLE to CVE-2024-48990", result.stdout)

    def test_conf_d_mitigation_overrides_main_configuration(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir), main_value=1, snippet_value=0)
            result = self._run_check(root, "3.6-7ubuntu4.2")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("interpscan=0 mitigation is active", result.stdout)
        self.assertIn("Effective interpreter scanning: 0", result.stdout)
        self.assertNotIn("VULNERABLE to CVE-2024-48990", result.stdout)

    def test_upstream_version_comparison_handles_double_digit_components(self):
        body = "\n".join(
            [
                f". {shlex.quote(str(self.function_file))}",
                "nr48990_version_lt 3.7 3.8 && echo old=yes",
                "nr48990_version_lt 3.10 3.8 || echo new=yes",
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
