"""Fragnesia review requires ESP-in-TCP, reachable ESP, and an affected kernel."""

import os
from pathlib import Path
import subprocess
import tempfile
import unittest


HELPER = (Path(__file__).resolve().parents[1] / "builder/linpeas_parts/functions/checkDirtyFrag.sh")
SHELL = """
E=E
SED_RED_YELLOW='&'
print_3title() { printf '%s\\n' "$1"; }
print_info() { :; }
. "$HELPER"
fg26_check_evidence "$FIXTURE" "$RELEASE"
"""


class FragnesiaEvidenceTests(unittest.TestCase):
    def run_check(self, root, release):
        env = dict(os.environ, HELPER=str(HELPER), FIXTURE=str(root), RELEASE=release)
        return subprocess.run(["/bin/sh"], input=SHELL, text=True,
                              capture_output=True, timeout=5, env=env, check=True).stdout

    def fixture(self, root, release="6.8.0"):
        config = root / "boot" / f"config-{release}"
        config.parent.mkdir(parents=True, exist_ok=True)
        config.write_text("CONFIG_INET_ESP=m\nCONFIG_INET_ESPINTCP=y\n")
        deps = root / "lib" / "modules" / release / "modules.dep"
        deps.parent.mkdir(parents=True, exist_ok=True)
        deps.write_text("kernel/net/ipv4/esp4.ko.xz:\n")
        return config

    def test_upstream_boundary_and_explicit_configuration(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.fixture(root)
            self.assertIn("Fragnesia ESP-in-TCP review", self.run_check(root, "6.8.0"))
            config = root / "boot/config-6.8.0"
            config.write_text("CONFIG_INET_ESP=m\n# CONFIG_INET_ESPINTCP is not set\n")
            self.assertEqual("", self.run_check(root, "6.8.0"))

            for affected, fixed in (("5.10.256", "5.10.257"),
                                    ("5.15.207", "5.15.208"),
                                    ("6.1.173", "6.1.174"),
                                    ("6.6.140", "6.6.141"),
                                    ("6.12.90", "6.12.91"),
                                    ("6.18.32", "6.18.33"),
                                    ("7.0.9", "7.0.10")):
                self.fixture(root, affected)
                self.fixture(root, fixed)
                with self.subTest(affected=affected):
                    self.assertIn("Fragnesia ESP-in-TCP review", self.run_check(root, affected))
                    self.assertEqual("", self.run_check(root, fixed))
            self.fixture(root, "7.1-rc4")
            self.fixture(root, "7.1-rc5")
            self.assertIn("Fragnesia ESP-in-TCP review", self.run_check(root, "7.1-rc4"))
            self.assertEqual("", self.run_check(root, "7.1-rc5"))

    def test_ipv6_builtin_transport_is_visible(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            config = self.fixture(root)
            config.write_text("CONFIG_INET6_ESP=y\nCONFIG_INET6_ESPINTCP=y\n")
            self.assertIn("esp6(active or built-in)", self.run_check(root, "6.8.0"))

    def test_install_mitigation_and_loaded_module(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.fixture(root)
            rule = root / "etc/modprobe.d/esp.conf"
            rule.parent.mkdir(parents=True)
            rule.write_text("install esp4 /bin/false\n")
            self.assertEqual("", self.run_check(root, "6.8.0"))
            modules = root / "proc/modules"
            modules.parent.mkdir(parents=True)
            modules.write_text("esp4 16384 0 - Live 0x0\n")
            self.assertIn("esp4(active or built-in)", self.run_check(root, "6.8.0"))

    def test_user_namespace_condition_is_not_a_vulnerability_verdict(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.fixture(root)
            flag = root / "proc/sys/kernel/unprivileged_userns_clone"
            flag.parent.mkdir(parents=True)
            flag.write_text("0\n")
            result = self.run_check(root, "6.8.0")
            self.assertIn("user namespaces: disabled", result)
            self.assertIn("vendor backports", result)
            self.assertNotIn("LIKELY VULNERABLE", result)


if __name__ == "__main__":
    unittest.main()
