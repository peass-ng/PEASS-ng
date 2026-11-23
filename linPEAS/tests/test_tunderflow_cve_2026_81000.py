"""Deterministic passive-evidence tests for TUNderflow."""

import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


class TUNderflowTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.root_dir = Path(__file__).resolve().parents[2]
        cls.helper = (
            cls.root_dir
            / "linPEAS/builder/linpeas_parts/functions/checkTUNderflowCVE202681000.sh"
        )

    def make_fixture(
        self,
        base,
        kernel="6.19.10-300.fc44.x86_64",
        netkit="y",
        userns="yes",
        cap_eff="0000000000000000",
        device=True,
        ovs=True,
        module_disabled=False,
    ):
        root = base / "root"

        def write(path, value):
            file = root / path
            file.parent.mkdir(parents=True, exist_ok=True)
            file.write_text(value, encoding="utf-8")

        write("proc/sys/kernel/osrelease", kernel + "\n")
        write("proc/sys/kernel/modules_disabled", "1\n" if module_disabled else "0\n")
        write("proc/self/status", "Name:\tlinpeas\nCapEff:\t" + cap_eff + "\n")
        write("proc/sys/user/max_user_namespaces", "15000\n" if userns == "yes" else "0\n")
        write("proc/sys/kernel/unprivileged_userns_clone", "1\n")
        write("proc/sys/kernel/apparmor_restrict_unprivileged_userns", "0\n")
        write(
            f"boot/config-{kernel}",
            "CONFIG_NETKIT=" + netkit + "\n"
            "CONFIG_TUN=m\n"
            + ("CONFIG_OPENVSWITCH=m\n" if ovs else "")
            + "CONFIG_VXLAN=m\n",
        )
        if device:
            write("dev/net/tun", "")
        module_dir = root / "lib/modules" / kernel
        module_dir.mkdir(parents=True)
        modules = ["tun", "vxlan"] + (["openvswitch"] if ovs else [])
        write(
            f"lib/modules/{kernel}/modules.dep",
            "".join(f"kernel/drivers/net/{name}.ko.xz:\n" for name in modules),
        )
        return root

    def run_check(self, root):
        script = "\n".join(
            [
                f"ROOT_FOLDER={shlex.quote(str(root))}",
                "E=E",
                "SED_RED_YELLOW='&'",
                'print_3title() { printf "TITLE: %s\\n" "$1"; }',
                "print_info() { :; }",
                f". {shlex.quote(str(self.helper))}",
                "checkTUNderflowCVE202681000",
            ]
        )
        return subprocess.run(
            ["sh", "-c", script],
            cwd=self.root_dir,
            capture_output=True,
            text=True,
            check=False,
        )

    def test_affected_kernel_and_full_local_chain(self):
        with tempfile.TemporaryDirectory() as temp:
            root = self.make_fixture(Path(temp))
            result = self.run_check(root)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("TITLE: TUNderflow TUN/OVS headroom LPE", result.stdout)
        self.assertIn("TUN=module, Open vSwitch=module, VXLAN=module", result.stdout)
        self.assertIn("unprivileged user namespaces appear enabled", result.stdout)

    def test_upstream_fixed_boundary_suppresses_candidate(self):
        for kernel in ("6.12.109", "6.18.50", "7.2.4", "7.3-rc1"):
            with self.subTest(kernel=kernel), tempfile.TemporaryDirectory() as temp:
                result = self.run_check(self.make_fixture(Path(temp), kernel=kernel))
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(result.stdout, "")

    def test_disabled_namespace_without_capability_suppresses(self):
        with tempfile.TemporaryDirectory() as temp:
            result = self.run_check(self.make_fixture(Path(temp), userns="no"))
        self.assertEqual(result.stdout, "")

    def test_effective_net_admin_is_an_alternate_path(self):
        with tempfile.TemporaryDirectory() as temp:
            result = self.run_check(
                self.make_fixture(Path(temp), userns="no", cap_eff="0000000000001000")
            )
        self.assertIn("CAP_NET_ADMIN is effective", result.stdout)

    def test_missing_path_prerequisites_suppress(self):
        for options in ({"device": False}, {"netkit": "n"}, {"ovs": False}):
            with self.subTest(options=options), tempfile.TemporaryDirectory() as temp:
                result = self.run_check(self.make_fixture(Path(temp), **options))
                self.assertEqual(result.stdout, "")

    def test_configured_but_uninstalled_ovs_module_suppresses(self):
        with tempfile.TemporaryDirectory() as temp:
            root = self.make_fixture(Path(temp))
            release = (root / "proc/sys/kernel/osrelease").read_text().strip()
            deps = root / "lib/modules" / release / "modules.dep"
            deps.write_text(
                "kernel/drivers/net/tun.ko.xz:\n"
                "kernel/drivers/net/vxlan.ko.xz:\n",
                encoding="utf-8",
            )
            result = self.run_check(root)
        self.assertEqual(result.stdout, "")

    def test_modules_disabled_suppresses_unloaded_chain(self):
        with tempfile.TemporaryDirectory() as temp:
            result = self.run_check(self.make_fixture(Path(temp), module_disabled=True))
        self.assertEqual(result.stdout, "")

    def test_hard_disabled_ovs_module_suppresses(self):
        with tempfile.TemporaryDirectory() as temp:
            root = self.make_fixture(Path(temp))
            rule = root / "etc/modprobe.d/disable-ovs.conf"
            rule.parent.mkdir(parents=True)
            rule.write_text("install openvswitch /bin/false\n", encoding="utf-8")
            result = self.run_check(root)
        self.assertEqual(result.stdout, "")

    def test_apparmor_userns_restriction_suppresses_ordinary_user_path(self):
        with tempfile.TemporaryDirectory() as temp:
            root = self.make_fixture(Path(temp))
            (root / "proc/sys/kernel/apparmor_restrict_unprivileged_userns").write_text(
                "1\n", encoding="utf-8"
            )
            result = self.run_check(root)
        self.assertEqual(result.stdout, "")


if __name__ == "__main__":
    unittest.main()
