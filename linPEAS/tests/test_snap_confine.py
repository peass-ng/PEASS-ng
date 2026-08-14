import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class SnapConfineCVE20268933Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.function_file = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "functions"
            / "checkSnapConfineCVE20268933.sh"
        )

    def _make_root(self, base, release="24.04", snap_version=None):
        root = base / "root"
        (root / "etc").mkdir(parents=True)
        (root / "etc" / "os-release").write_text(
            f'ID=ubuntu\nVERSION_ID="{release}"\n', encoding="utf-8"
        )
        if snap_version is None:
            binary = root / "usr" / "lib" / "snapd" / "snap-confine"
        else:
            binary = (
                root
                / "snap"
                / "snapd"
                / "current"
                / "usr"
                / "lib"
                / "snapd"
                / "snap-confine"
            )
            meta = root / "snap" / "snapd" / "current" / "meta"
            meta.mkdir(parents=True)
            (meta / "snap.yaml").write_text(
                f"name: snapd\nversion: {snap_version}\n", encoding="utf-8"
            )
        binary.parent.mkdir(parents=True, exist_ok=True)
        binary.write_text("#!/bin/sh\ntouch \"$EXECUTED_MARKER\"\n", encoding="utf-8")
        binary.chmod(0o755)
        return root, binary

    def _run_check(self, root, package_version="2.75+ubuntu24.04"):
        bindir = root.parent / "bin"
        bindir.mkdir()
        getcap = bindir / "getcap"
        getcap.write_text(
            '#!/bin/sh\nprintf "%s cap_chown,cap_sys_admin=p\\n" "$1"\n',
            encoding="utf-8",
        )
        getcap.chmod(0o755)
        dpkg_query = bindir / "dpkg-query"
        dpkg_query.write_text(
            '#!/bin/sh\nprintf "%s\\n" "$FAKE_SNAPD_VERSION"\n', encoding="utf-8"
        )
        dpkg_query.chmod(0o755)

        marker = root.parent / "executed"
        env = os.environ.copy()
        env.update(
            {
                "EXECUTED_MARKER": str(marker),
                "FAKE_SNAPD_VERSION": package_version,
                "PATH": f"{bindir}:{env['PATH']}",
            }
        )
        body = "\n".join(
            [
                f"ROOT_FOLDER={shlex.quote(str(root))}",
                "E=E",
                "SED_RED_YELLOW='&'",
                "SED_LIGHT_CYAN='&'",
                "SED_GREEN='&'",
                'print_3title() { echo "TITLE: $1"; }',
                "print_info() { :; }",
                f". {shlex.quote(str(self.function_file))}",
                "checkSnapConfineCVE20268933",
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

    def test_vulnerable_ubuntu_package_is_reported_without_executing_binary(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root, binary = self._make_root(Path(tmpdir))
            result, marker = self._run_check(root)
            self.assertFalse(marker.exists(), "the privileged binary must never be executed")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("TITLE: Set-capabilities snap-confine", result.stdout)
        self.assertIn("Vulnerable to CVE-2026-8933", result.stdout)

    def test_release_specific_fixed_versions(self):
        cases = (
            ("2.76+ubuntu22.04", "22.04", True),
            ("2.76+ubuntu22.04.1", "22.04", False),
            ("2.76+ubuntu24.04", "24.04", True),
            ("2.76+ubuntu24.04.1", "24.04", False),
            ("2.76+ubuntu26.04.2", "26.04", True),
            ("2.76+ubuntu26.04.3", "26.04", False),
            ("2.75.1", "snap", True),
            ("2.76.1", "snap", False),
        )
        checks = []
        for version, release, vulnerable in cases:
            kind = "snap" if release == "snap" else "deb"
            checks.append(
                "sc8933_version_is_vulnerable "
                f"{shlex.quote(version)} {kind} {release} && "
                f"echo {version}=yes || echo {version}=no"
            )
        body = "\n".join(
            [f". {shlex.quote(str(self.function_file))}"] + checks
        )
        result = subprocess.run(
            ["sh", "-c", body],
            cwd=str(self.repo_root),
            capture_output=True,
            text=True,
            check=False,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        for version, _, vulnerable in cases:
            expected = "yes" if vulnerable else "no"
            self.assertIn(f"{version}={expected}", result.stdout)

    def test_fixed_ubuntu_package_is_not_flagged(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root, binary = self._make_root(Path(tmpdir))
            result, _ = self._run_check(root, package_version="2.76+ubuntu24.04.1")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("not in the known CVE-2026-8933 vulnerable range", result.stdout)
        self.assertNotIn("Vulnerable to CVE-2026-8933", result.stdout)

    def test_vulnerable_snap_revision_is_reported(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root, binary = self._make_root(Path(tmpdir), snap_version="2.75.1")
            result, _ = self._run_check(root)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Vulnerable to CVE-2026-8933", result.stdout)
        self.assertIn("version 2.75.1", result.stdout)

    def test_setuid_snap_confine_is_excluded(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root, binary = self._make_root(Path(tmpdir))
            binary.chmod(0o4755)
            result, _ = self._run_check(root)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)


if __name__ == "__main__":
    unittest.main()
