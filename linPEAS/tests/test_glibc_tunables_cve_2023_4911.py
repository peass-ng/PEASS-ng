import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class GlibcTunablesPackageReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.root = Path(__file__).resolve().parents[2]
        cls.check = (
            cls.root
            / "linPEAS/builder/linpeas_parts/functions/checkGlibcTunablesCVE20234911.sh"
        )

    def run_fixture(self, distro, codename, version, fixed):
        with tempfile.TemporaryDirectory() as dirname:
            base = Path(dirname)
            root = base / "root"
            (root / "etc").mkdir(parents=True)
            (root / "var/lib/dpkg").mkdir(parents=True)
            (root / "etc/os-release").write_text(
                f'ID="{distro}"\nVERSION_CODENAME="{codename}"\n', encoding="utf-8"
            )
            bindir = base / "bin"
            bindir.mkdir()
            query = bindir / "dpkg-query"
            query.write_text(
                '#!/bin/sh\nprintf "install ok installed|%s\\n" "$FIXTURE_VERSION"\n',
                encoding="utf-8",
            )
            query.chmod(0o755)
            dpkg = bindir / "dpkg"
            dpkg.write_text(
                '#!/bin/sh\n'
                '[ "$1" = "--compare-versions" ] || exit 91\n'
                '[ "$2" = "$FIXTURE_VERSION" ] || exit 92\n'
                '[ "$3" = "lt" ] || exit 93\n'
                '[ "$4" = "$FIXTURE_FIXED" ] || exit 94\n'
                '[ "$FIXTURE_VERSION" = "$FIXTURE_FIXED" ] && exit 1\n'
                'exit 0\n',
                encoding="utf-8",
            )
            dpkg.chmod(0o755)
            env = os.environ.copy()
            env.update(
                PATH=f"{bindir}:{env['PATH']}",
                FIXTURE_VERSION=version,
                FIXTURE_FIXED=fixed,
            )
            shell = "\n".join(
                (
                    f"ROOT_FOLDER={shlex.quote(str(root))}",
                    'print_3title() { printf "TITLE: %s\\n" "$1"; }',
                    "print_info() { :; }",
                    f". {shlex.quote(str(self.check))}",
                    "checkGlibcTunablesCVE20234911",
                )
            )
            return subprocess.run(
                ["sh", "-c", shell],
                env=env,
                cwd=self.root,
                text=True,
                capture_output=True,
                check=False,
            )

    def test_ubuntu_jammy_boundary(self):
        fixed = "2.35-0ubuntu3.4"
        older = self.run_fixture("ubuntu", "jammy", "2.35-0ubuntu3.3", fixed)
        patched = self.run_fixture("ubuntu", "jammy", fixed, fixed)
        self.assertEqual(older.returncode, patched.returncode, 0)
        self.assertIn("Package below vendor fix: review candidate only", older.stdout)
        self.assertIn("meets this vendor's fixed threshold", patched.stdout)

    def test_debian_bullseye_backport_boundary(self):
        fixed = "2.31-13+deb11u7"
        older = self.run_fixture("debian", "bullseye", "2.31-13+deb11u6", fixed)
        patched = self.run_fixture("debian", "bullseye", fixed, fixed)
        self.assertEqual(older.returncode, patched.returncode, 0)
        self.assertIn("Package below vendor fix", older.stdout)
        self.assertIn("meets this vendor's fixed threshold", patched.stdout)

    def test_ubuntu_lunar_and_debian_bookworm_thresholds(self):
        for distro, codename, old, fixed in (
            ("ubuntu", "lunar", "2.37-0ubuntu2", "2.37-0ubuntu2.1"),
            ("debian", "bookworm", "2.36-9+deb12u2", "2.36-9+deb12u3"),
            ("ubuntu", "mantic", "2.38-1ubuntu5", "2.38-1ubuntu6"),
        ):
            with self.subTest(distro=distro, codename=codename):
                result = self.run_fixture(distro, codename, old, fixed)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertIn(f"fixed threshold for {distro} {codename}: {fixed}", result.stdout)
                self.assertIn("Package below vendor fix", result.stdout)

    def test_unsupported_release_is_silent(self):
        for distro, codename in (("ubuntu", "focal"), ("debian", "trixie"), ("alpine", "v3.20")):
            with self.subTest(distro=distro):
                result = self.run_fixture(distro, codename, "0", "unused")
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(result.stdout, "")

    def test_missing_package_version_is_silent(self):
        result = self.run_fixture("ubuntu", "jammy", "", "2.35-0ubuntu3.4")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout, "")


if __name__ == "__main__":
    unittest.main()
