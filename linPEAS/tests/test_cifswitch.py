import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class CIFSwitchCVE202646243Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.function_file = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "functions"
            / "checkCIFSwitchCVE202646243.sh"
        )

    def _make_root(self, base, kernel="6.12.91", patch_marker=False, active_rule=True):
        root = base / "root"
        (root / "sys" / "module" / "cifs").mkdir(parents=True)
        (root / "proc" / "sys" / "kernel").mkdir(parents=True)
        (root / "proc" / "sys" / "kernel" / "osrelease").write_text(
            f"{kernel}\n", encoding="utf-8"
        )
        kallsyms = "0000000000000000 t unrelated_symbol\n"
        if patch_marker:
            kallsyms += "0000000000000000 t cifs_spnego_key_vet_description\n"
        (root / "proc" / "kallsyms").write_text(kallsyms, encoding="utf-8")

        (root / "etc" / "request-key.d").mkdir(parents=True)
        prefix = "" if active_rule else "# "
        (root / "etc" / "request-key.d" / "cifs.conf").write_text(
            f"{prefix}create cifs.spnego * * /usr/sbin/cifs.upcall %k\n",
            encoding="utf-8",
        )

        helper = root / "usr" / "sbin" / "cifs.upcall"
        helper.parent.mkdir(parents=True)
        helper.write_text('#!/bin/sh\ntouch "$EXECUTED_MARKER"\n', encoding="utf-8")
        helper.chmod(0o755)
        request_key = root / "sbin" / "request-key"
        request_key.parent.mkdir(parents=True)
        request_key.write_text('#!/bin/sh\ntouch "$EXECUTED_MARKER"\n', encoding="utf-8")
        request_key.chmod(0o755)
        return root

    def _run_check(self, root):
        marker = root.parent / "executed"
        env = os.environ.copy()
        env["EXECUTED_MARKER"] = str(marker)
        body = "\n".join(
            [
                f"ROOT_FOLDER={shlex.quote(str(root))}",
                "E=E",
                "SED_RED_YELLOW='&'",
                "SED_LIGHT_CYAN='&'",
                'print_3title() { echo "TITLE: $1"; }',
                "print_info() { :; }",
                f". {shlex.quote(str(self.function_file))}",
                "checkCIFSwitchCVE202646243",
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

    def test_complete_vulnerable_chain_is_reported_without_executing_helper(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir))
            result, marker = self._run_check(root)
            self.assertFalse(marker.exists(), "cifs.upcall must never be executed")

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("TITLE: CIFSwitch attack chain (CVE-2026-46243)", result.stdout)
        self.assertIn("Loaded CIFS module + active cifs.spnego rule", result.stdout)

    def test_backported_patch_marker_suppresses_finding(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir), patch_marker=True)
            result, _ = self._run_check(root)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)

    def test_fixed_upstream_stable_version_suppresses_finding(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir), kernel="6.12.92")
            result, _ = self._run_check(root)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)

    def test_upstream_fixed_version_boundaries(self):
        cases = {
            "2.6.23": True,
            "2.6.24": False,
            "5.10.257": False,
            "5.10.258": True,
            "5.15.208": False,
            "5.15.209": True,
            "6.1.174": False,
            "6.1.175": True,
            "6.6.141": False,
            "6.6.142": True,
            "6.12.91": False,
            "6.12.92": True,
            "6.18.33": False,
            "6.18.34": True,
            "7.0.10": False,
            "7.0.11": True,
            "7.1.0": True,
        }
        checks = [
            f"cs46243_kernel_is_fixed {shlex.quote(version)} && "
            f"echo {version}=yes || echo {version}=no"
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
        for version, fixed in cases.items():
            expected = "yes" if fixed else "no"
            self.assertIn(f"{version}={expected}", result.stdout)

    def test_inactive_upcall_rule_suppresses_finding(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir), active_rule=False)
            result, _ = self._run_check(root)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)

    def test_unloaded_cifs_module_suppresses_finding(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir))
            (root / "sys" / "module" / "cifs").rmdir()
            result, _ = self._run_check(root)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)


if __name__ == "__main__":
    unittest.main()
