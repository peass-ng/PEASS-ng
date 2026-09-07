import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class XFSTangoCVE202680530Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.function_file = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "functions"
            / "checkXFSTangoCVE202680530.sh"
        )

    def _run_check(
        self,
        kernel,
        filesystem="xfs",
        reflink="1",
        with_xfs_info=True,
        distro_id="ubuntu",
        distro_version="26.04",
    ):
        with tempfile.TemporaryDirectory() as tmpdir:
            base = Path(tmpdir)
            mounts = base / "mounts"
            mounts.write_text(
                f"/dev/test /srv/test {filesystem} rw,relatime 0 0\n",
                encoding="utf-8",
            )
            os_release = base / "os-release"
            os_release.write_text(
                f"ID={distro_id}\nVERSION_ID=\"{distro_version}\"\n",
                encoding="utf-8",
            )
            bindir = base / "bin"
            bindir.mkdir()
            timeout = bindir / "timeout"
            timeout.write_text('#!/bin/sh\nshift\nexec "$@"\n', encoding="utf-8")
            timeout.chmod(0o755)
            marker = base / "xfs_info_args"
            if with_xfs_info:
                xfs_info = bindir / "xfs_info"
                xfs_info.write_text(
                    "#!/bin/sh\n"
                    'printf "%s\\n" "$*" > "$XFS_INFO_MARKER"\n'
                    'printf "data = bsize=4096 reflink=%s bigtime=1\\n" "$FAKE_REFLINK"\n',
                    encoding="utf-8",
                )
                xfs_info.chmod(0o755)

            env = os.environ.copy()
            env.update(
                {
                    "FAKE_REFLINK": reflink,
                    "PATH": f"{bindir}:{env['PATH']}",
                    "XFS_INFO_MARKER": str(marker),
                }
            )
            body = "\n".join(
                [
                    "E=E",
                    f"TIMEOUT={shlex.quote(str(timeout))}",
                    "SED_GREEN='&'",
                    "SED_LIGHT_CYAN='&'",
                    "SED_RED_YELLOW='&'",
                    "SED_YELLOW='&'",
                    'print_3title() { echo "TITLE: $1"; }',
                    "print_info() { :; }",
                    f". {shlex.quote(str(self.function_file))}",
                    "checkXFSTangoCVE202680530 "
                    f"{shlex.quote(kernel)} {shlex.quote(str(mounts))} "
                    f"{shlex.quote(str(os_release))}",
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
            marker_value = marker.read_text(encoding="utf-8").strip() if marker.exists() else ""
        return result, marker_value

    def test_affected_kernel_and_reflink_filesystem_are_reported(self):
        result, marker = self._run_check("6.12.104-custom")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("TITLE: XFSTango XFS reflink LPE", result.stdout)
        self.assertIn("POTENTIALLY VULNERABLE to CVE-2026-80530", result.stdout)
        self.assertIn("/srv/test - reflink enabled", result.stdout)
        self.assertEqual("/srv/test", marker)

    def test_fixed_stable_kernel_is_not_reported_as_vulnerable(self):
        result, _ = self._run_check("6.12.105")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("NOT VULNERABLE by upstream version", result.stdout)
        self.assertNotIn("POTENTIALLY VULNERABLE", result.stdout)

    def test_reflink_disabled_is_reported_as_mitigated(self):
        result, _ = self._run_check("7.1.9", reflink="0")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("Mitigated: the mounted XFS filesystem(s) report reflink disabled", result.stdout)
        self.assertNotIn("POTENTIALLY VULNERABLE", result.stdout)

    def test_unknown_reflink_state_preserves_a_cautious_warning(self):
        result, marker = self._run_check("6.18.45", with_xfs_info=False)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("confirm whether reflink is enabled", result.stdout)
        self.assertEqual("", marker)

    def test_rhel9_backport_is_not_missed_by_upstream_version_gate(self):
        result, _ = self._run_check(
            "5.14.0-570.el9_6.x86_64", distro_id="rhel", distro_version="9.6"
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("POTENTIALLY VULNERABLE to CVE-2026-80530", result.stdout)
        self.assertIn("Red Hat lists RHEL 9 affected despite its 5.14 base", result.stdout)

    def test_rhel10_vendor_status_overrides_upstream_range(self):
        result, _ = self._run_check(
            "6.12.0-55.el10.x86_64", distro_id="rhel", distro_version="10.0"
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("NOT VULNERABLE according to the Red Hat advisory", result.stdout)
        self.assertNotIn("POTENTIALLY VULNERABLE", result.stdout)

    def test_non_xfs_mount_is_suppressed(self):
        result, marker = self._run_check("6.12.104", filesystem="ext4")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)
        self.assertEqual("", marker)

    def test_upstream_version_boundaries(self):
        cases = {
            "6.9.12": "predates",
            "6.10.0": "affected",
            "6.12.104": "affected",
            "6.12.105": "fixed",
            "6.18.45": "affected",
            "6.18.46": "fixed",
            "7.0.9": "affected",
            "7.1.9": "affected",
            "7.1.10": "fixed",
            "7.2.0-rc6": "affected",
            "7.2.0-rc7": "fixed",
            "7.2.0": "fixed",
        }
        checks = [
            f"echo {version}=$(xft80530_kernel_status {shlex.quote(version)})"
            for version in cases
        ]
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
        for version, status in cases.items():
            self.assertIn(f"{version}={status}", result.stdout)


if __name__ == "__main__":
    unittest.main()
