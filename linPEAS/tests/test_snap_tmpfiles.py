import os
import shlex
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


class SnapTmpfilesPrerequisiteTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.check = (
            Path(__file__).resolve().parents[1]
            / "builder/linpeas_parts/functions/checkSnapConfineCVE20263888.sh"
        )

    def run_fixture(
        self, release="24.04", version="2.63+24.04", privileged=True,
        cleanup="D /tmp 1777 root root 30d", installed=True, timer_present=True,
        override=None,
    ):
        with tempfile.TemporaryDirectory() as temp:
            base = Path(temp)
            root = base / "root"
            binary = root / "usr/lib/snapd/snap-confine"
            binary.parent.mkdir(parents=True)
            binary.write_text('#!/bin/sh\ntouch "$EXECUTED_MARKER"\n')
            binary.chmod(0o4755 if privileged is True else 0o755)
            (root / "etc").mkdir()
            (root / "etc/os-release").write_text(
                f'ID=ubuntu\nVERSION_ID="{release}"\n'
            )
            if cleanup is not None:
                rule = root / "usr/lib/tmpfiles.d/tmp.conf"
                rule.parent.mkdir(parents=True)
                rule.write_text(cleanup + "\n")
            if override is not None:
                rule = root / "etc/tmpfiles.d/tmp.conf"
                rule.parent.mkdir(parents=True)
                rule.write_text(override + "\n")
            timer = root / "usr/lib/systemd/system/systemd-tmpfiles-clean.timer"
            timer.parent.mkdir(parents=True)
            if timer_present:
                timer.write_text("[Timer]\nOnCalendar=daily\n")
            bindir = base / "bin"
            bindir.mkdir()
            query = bindir / "dpkg-query"
            query.write_text('#!/bin/sh\nprintf "%s %s\\n" "$FAKE_STATUS" "$FAKE_VERSION"\n')
            query.chmod(0o755)
            if privileged == "caps":
                getcap = bindir / "getcap"
                getcap.write_text(
                    '#!/bin/sh\nprintf "%s cap_sys_admin=p\\n" "$1"\n'
                )
                getcap.chmod(0o755)
            marker = base / "executed"
            env = os.environ.copy()
            env.update(
                PATH=f"{bindir}:{env['PATH']}",
                FAKE_STATUS="install ok installed" if installed else "deinstall ok config-files",
                FAKE_VERSION=version,
                EXECUTED_MARKER=str(marker),
            )
            body = "\n".join((
                f"ROOT_FOLDER={shlex.quote(str(root))}",
                "E=E",
                "SED_RED_YELLOW='&'",
                'print_3title() { echo "TITLE: $1"; }',
                'print_info() { :; }',
                f". {shlex.quote(str(self.check))}",
                "checkSnapConfineCVE20263888",
            ))
            result = subprocess.run(
                ["sh", "-c", body], env=env, capture_output=True, text=True
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertFalse(marker.exists(), "snap-confine must not be executed")
            return result.stdout

    @unittest.skipUnless(shutil.which("dpkg"), "requires Debian version comparison")
    def test_below_fix_with_cleanup_is_a_potential_indicator(self):
        output = self.run_fixture()
        self.assertIn("TITLE: snap-confine/tmpfiles race prerequisites", output)
        self.assertIn("below the Ubuntu advisory fix (2.73+ubuntu24.04.2)", output)
        self.assertIn("/tmp cleanup rule: age 30d", output)
        self.assertIn("Potential CVE-2026-3888 prerequisites observed", output)
        self.assertIn("runtime activation unverified", output)

    @unittest.skipUnless(shutil.which("dpkg"), "requires Debian version comparison")
    def test_fixed_versions_are_not_flagged(self):
        for release, version in (
            ("24.04", "2.73+ubuntu24.04.2"),
            ("22.04", "2.73+ubuntu22.04.1"),
            ("20.04", "2.67.1+20.04ubuntu1~esm1"),
            ("26.04", "2.74.1+ubuntu26.04.3"),
        ):
            with self.subTest(release=release):
                output = self.run_fixture(release=release, version=version)
                self.assertIn("at or above the Ubuntu advisory fix", output)
                self.assertNotIn("Potential CVE-2026-3888", output)

    def test_missing_cleanup_and_unknown_package_do_not_raise_indicator(self):
        for kwargs in (
            {"cleanup": None},
            {"cleanup": "D /tmp 1777 root root -"},
            {"version": "2.63+custom"},
            {"installed": False},
            {"timer_present": False},
            {"override": "D /tmp 1777 root root -"},
        ):
            with self.subTest(kwargs=kwargs):
                output = self.run_fixture(**kwargs)
                self.assertNotIn("Potential CVE-2026-3888", output)

    @unittest.skipUnless(shutil.which("dpkg"), "requires Debian version comparison")
    def test_file_capabilities_variant_is_reported(self):
        output = self.run_fixture(
            release="25.10", version="2.72+ubuntu25.10", privileged="caps",
            cleanup="q /tmp 1777 root root 10d",
        )
        self.assertIn("snap-confine: file capabilities", output)
        self.assertIn("below the Ubuntu advisory fix (2.73+ubuntu25.10.1)", output)
        self.assertIn("Potential CVE-2026-3888 prerequisites observed", output)

    def test_unprivileged_binary_is_skipped(self):
        self.assertEqual(self.run_fixture(privileged=False), "")


if __name__ == "__main__":
    unittest.main()
