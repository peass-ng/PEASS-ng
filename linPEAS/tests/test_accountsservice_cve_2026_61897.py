import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


@unittest.skipUnless(
    Path("/usr/bin/dpkg-query").is_file() and Path("/usr/bin/dpkg").is_file(),
    "dpkg tools are needed for package-boundary fixtures",
)
class AccountsServiceCVE202661897Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.check = (
            Path(__file__).resolve().parents[2]
            / "linPEAS/builder/linpeas_parts/functions/checkAccountsServiceCVE202661897.sh"
        )

    def run_fixture(
        self,
        version="23.13.9-2ubuntu6",
        release="24.04",
        distro="ubuntu",
        installed=True,
        service=True,
        helper=True,
        daemon=True,
    ):
        with tempfile.TemporaryDirectory() as dirname:
            root = Path(dirname)
            for relative in (
                "etc",
                "var/lib/dpkg",
                "usr/share/dbus-1/system-services",
                "usr/share/language-tools",
                "usr/libexec",
            ):
                (root / relative).mkdir(parents=True, exist_ok=True)
            (root / "etc/os-release").write_text(
                f'ID="{distro}"\nVERSION_ID="{release}"\n', encoding="utf-8"
            )
            (root / "var/lib/dpkg/status").write_text(
                "Package: accountsservice\n"
                f"Status: {'install ok installed' if installed else 'deinstall ok config-files'}\n"
                "Priority: optional\nSection: admin\nArchitecture: amd64\n"
                f"Version: {version}\n"
                "Maintainer: Fixture <fixture@example.invalid>\n"
                "Description: test package\n\n",
                encoding="utf-8",
            )
            if service:
                (root / "usr/share/dbus-1/system-services/org.freedesktop.Accounts.service").write_text(
                    "[D-BUS Service]\nName=org.freedesktop.Accounts\n"
                    "SystemdService=accounts-daemon.service\n",
                    encoding="utf-8",
                )
            if helper:
                (root / "usr/share/language-tools/set-language-helper").write_text(
                    "fixture helper - not executed\n", encoding="utf-8"
                )
            if daemon:
                target = root / "usr/libexec/accounts-daemon"
                target.write_text("fixture daemon - not executed\n", encoding="utf-8")
                target.chmod(0o755)
            script = "\n".join(
                (
                    f"ROOT_FOLDER={shlex.quote(str(root))}",
                    'print_3title() { printf "TITLE: %s\\n" "$1"; }',
                    "print_info() { :; }",
                    f". {shlex.quote(str(self.check))}",
                    "checkAccountsServiceCVE202661897",
                )
            )
            return subprocess.run(
                ["sh", "-c", script], text=True, capture_output=True, check=False
            )

    def test_noble_unpatched_package_with_activation_evidence(self):
        result = self.run_fixture()
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("CVE-2026-61897", result.stdout)
        self.assertIn("23.13.9-2ubuntu6.1", result.stdout)
        self.assertNotIn("fixture helper", result.stdout)

    def test_esm_and_current_release_boundaries(self):
        for release, old, fixed in (
            ("20.04", "0.6.55-0ubuntu12~20.04.7", "0.6.55-0ubuntu12~20.04.7+esm1"),
            ("22.04", "22.07.5-2ubuntu1.5", "22.07.5-2ubuntu1.6"),
            ("26.04", "23.13.9-8ubuntu5.1", "23.13.9-8ubuntu5.2"),
        ):
            with self.subTest(release=release):
                vulnerable = self.run_fixture(version=old, release=release)
                patched = self.run_fixture(version=fixed, release=release)
                self.assertEqual(vulnerable.returncode, 0, vulnerable.stderr)
                self.assertIn("CVE-2026-61897", vulnerable.stdout)
                self.assertEqual(patched.returncode, 0, patched.stderr)
                self.assertEqual(patched.stdout, "")

    def test_non_candidates_are_quiet(self):
        for case in (
            {"version": "23.13.9-2ubuntu6.1"},
            {"distro": "debian"},
            {"release": "25.10"},
            {"installed": False},
            {"service": False},
            {"helper": False},
            {"daemon": False},
        ):
            with self.subTest(case=case):
                result = self.run_fixture(**case)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(result.stdout, "")


if __name__ == "__main__":
    unittest.main()
