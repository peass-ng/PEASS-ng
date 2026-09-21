import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class DiagSpillCVE202674469Tests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.repo_root = Path(__file__).resolve().parents[2]
        cls.function_file = (
            cls.repo_root
            / "linPEAS"
            / "builder"
            / "linpeas_parts"
            / "functions"
            / "checkDiagSpillCVE202674469.sh"
        )

    def _make_root(
        self,
        base,
        kernel="6.12.102",
        loaded=("sctp", "sctp_diag"),
        config="",
        module_deps="",
        modules_disabled="0",
        modprobe_rule="",
    ):
        root = base / "root"
        (root / "proc" / "sys" / "kernel").mkdir(parents=True)
        (root / "proc" / "sys" / "kernel" / "osrelease").write_text(
            f"{kernel}\n", encoding="utf-8"
        )
        (root / "proc" / "sys" / "kernel" / "modules_disabled").write_text(
            f"{modules_disabled}\n", encoding="utf-8"
        )
        for module in loaded:
            (root / "sys" / "module" / module).mkdir(parents=True)

        if config:
            (root / "boot").mkdir(parents=True)
            (root / "boot" / f"config-{kernel}").write_text(
                config, encoding="utf-8"
            )
        if module_deps:
            module_dir = root / "lib" / "modules" / kernel
            module_dir.mkdir(parents=True)
            (module_dir / "modules.dep").write_text(module_deps, encoding="utf-8")
        if modprobe_rule:
            (root / "etc" / "modprobe.d").mkdir(parents=True)
            (root / "etc" / "modprobe.d" / "disable-sctp.conf").write_text(
                modprobe_rule, encoding="utf-8"
            )
        return root

    def _run_check(self, root):
        body = "\n".join(
            [
                f"ROOT_FOLDER={shlex.quote(str(root))}",
                "E=E",
                "SED_RED_YELLOW='&'",
                "SED_LIGHT_CYAN='&'",
                'print_3title() { echo "TITLE: $1"; }',
                "print_info() { :; }",
                f". {shlex.quote(str(self.function_file))}",
                "checkDiagSpillCVE202674469",
            ]
        )
        return subprocess.run(
            ["sh", "-c", body],
            cwd=str(self.repo_root),
            capture_output=True,
            text=True,
            check=False,
        )

    def test_vulnerable_kernel_with_loaded_chain_is_reported(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir))
            result = self._run_check(root)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn(
            "TITLE: DiagSpill SCTP kernel exposure (CVE-2026-74469)",
            result.stdout,
        )
        self.assertIn("sctp: loaded; sctp_diag: loaded", result.stdout)
        self.assertIn("needs no capability or unprivileged user namespace", result.stdout)

    def test_fixed_kernel_suppresses_finding(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir), kernel="6.12.103")
            result = self._run_check(root)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)

    def test_missing_diagnostic_module_suppresses_finding(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir), loaded=("sctp",))
            result = self._run_check(root)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)

    def test_installed_modular_chain_is_reported(self):
        deps = (
            "kernel/net/sctp/sctp.ko.xz:\n"
            "kernel/net/sctp/sctp_diag.ko.xz: kernel/net/sctp/sctp.ko.xz\n"
        )
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir), loaded=(), module_deps=deps)
            result = self._run_check(root)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("sctp: module; sctp_diag: module", result.stdout)

    def test_hard_disabled_module_suppresses_modular_finding(self):
        deps = (
            "kernel/net/sctp/sctp.ko:\n"
            "kernel/net/sctp/sctp_diag.ko: kernel/net/sctp/sctp.ko\n"
        )
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(
                Path(tmpdir),
                loaded=(),
                module_deps=deps,
                modprobe_rule="install sctp_diag /bin/false\n",
            )
            result = self._run_check(root)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)

    def test_disabled_module_loading_suppresses_modular_finding(self):
        deps = (
            "kernel/net/sctp/sctp.ko.zst:\n"
            "kernel/net/sctp/sctp_diag.ko.zst: kernel/net/sctp/sctp.ko.zst\n"
        )
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(
                Path(tmpdir),
                loaded=(),
                module_deps=deps,
                modules_disabled="1",
            )
            result = self._run_check(root)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual("", result.stdout)

    def test_built_in_chain_is_reported(self):
        config = "CONFIG_IP_SCTP=y\nCONFIG_INET_SCTP_DIAG=y\n"
        with tempfile.TemporaryDirectory() as tmpdir:
            root = self._make_root(Path(tmpdir), loaded=(), config=config)
            result = self._run_check(root)

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("sctp: built-in; sctp_diag: built-in", result.stdout)

    def test_upstream_fixed_version_boundaries(self):
        cases = {
            "4.6.9": False,
            "4.7": True,
            "4.7.0": True,
            "5.10.264": True,
            "5.10.265": False,
            "5.15.215": True,
            "5.15.216": False,
            "6.1.182": True,
            "6.1.183": False,
            "6.6.150": True,
            "6.6.151": False,
            "6.12.102": True,
            "6.12.103": False,
            "6.18.43": True,
            "6.18.44": False,
            "7.1.7": True,
            "7.1.8": False,
            "7.2.0-rc5": True,
            "7.2.0-rc6": False,
            "7.2.0": False,
        }
        checks = [
            f"ds74469_kernel_is_affected {shlex.quote(version)} && "
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
        for version, affected in cases.items():
            expected = "yes" if affected else "no"
            self.assertIn(f"{version}={expected}", result.stdout)


if __name__ == "__main__":
    unittest.main()
