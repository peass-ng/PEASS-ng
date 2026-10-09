import os
import re
import subprocess
import tempfile
import unittest
from pathlib import Path


class UsbCreatorPolicyReviewTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = (
            Path(__file__).resolve().parents[1]
            / "builder"
            / "linpeas_parts"
            / "1_system_information"
            / "3_USBCreator.sh"
        )

    def _run(self, policy=None, groups="sudo", service=True, symlink=False):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            service_path = root / "service"
            policy_path = root / "policy"
            if service:
                service_path.write_text("[D-BUS Service]\nName=com.ubuntu.USBCreator\n")
            if policy is not None:
                policy_path.write_text(policy)
            if symlink:
                link = root / "policy-link"
                link.symlink_to(policy_path)
                policy_path = link
            bindir = root / "bin"
            bindir.mkdir()
            fake_id = bindir / "id"
            fake_id.write_text(
                "#!/bin/sh\n"
                "case \"$1\" in -u) echo 1000 ;; -Gn) printf '%s\\n' \"$FAKE_GROUPS\" ;; esac\n"
            )
            fake_id.chmod(0o755)
            source = self.module.read_text()
            source, count = re.subn(
                r"for usbcreator_policy in /etc/dbus-1/system-services/com[.]ubuntu[.]USBCreator[.]service \\\n(?:.*\\\n)*?    /usr/share/dbus-1/system-services/com[.]ubuntu[.]USBCreator[.]service; do",
                f'for usbcreator_policy in "{service_path}"; do',
                source,
            )
            self.assertEqual(count, 1)
            source = source.replace(
                "/var/lib/polkit-1/localauthority/10-vendor.d/com.ubuntu.desktop.pkla",
                str(policy_path),
            )
            env = os.environ.copy()
            env["PATH"] = f"{bindir}:{env['PATH']}"
            env["FAKE_GROUPS"] = groups
            env["DEBUG"] = ""
            prefix = "print_2title() { printf 'TITLE %s\\n' \"$1\"; }; print_info() { :; }\n"
            return subprocess.run(
                ["/bin/sh", "-c", prefix + source],
                env=env,
                capture_output=True,
                text=True,
                check=False,
            )

    def test_same_stanza_passwordless_group_policy_is_conditional(self):
        result = self._run(
            "[USB imaging]\nIdentity=unix-group:admin;unix-group:sudo\n"
            "Action=com.ubuntu.usbcreator.mount;com.ubuntu.usbcreator.image\n"
            "ResultActive=yes\n"
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("system-bus service descriptor", result.stdout)
        self.assertIn("legacy group policy candidate", result.stdout)
        self.assertIn("effective rules and helper behavior unverified", result.stdout)
        self.assertNotIn("Vulnerable!!", result.stdout)

    def test_absent_service_or_unrelated_group_has_no_policy_candidate(self):
        policy = (
            "[USB imaging]\nIdentity=unix-group:sudo\n"
            "Action=com.ubuntu.usbcreator.image\nResultActive=yes\n"
        )
        for kwargs in ({"service": False}, {"groups": "users"}):
            with self.subTest(kwargs=kwargs):
                result = self._run(policy, **kwargs)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertNotIn("legacy group policy candidate", result.stdout)

    def test_policy_requires_exact_same_stanza_and_passwordless_result(self):
        cases = (
            "[one]\nIdentity=unix-group:sudo\nResultActive=yes\n[two]\nAction=com.ubuntu.usbcreator.image\n",
            "[one]\nIdentity=unix-group:sudo\nAction=com.ubuntu.usbcreator.mount\nResultActive=yes\n",
            "[one]\nIdentity=unix-group:sudo\nAction=com.ubuntu.usbcreator.image\nResultActive=auth_admin\n",
            "[one]\nIdentity=unix-group:sudoers\nAction=com.ubuntu.usbcreator.image\nResultActive=yes\n",
        )
        for policy in cases:
            with self.subTest(policy=policy):
                result = self._run(policy)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertNotIn("legacy group policy candidate", result.stdout)

    def test_symlink_large_or_long_policy_is_skipped(self):
        policy = (
            "[USB imaging]\nIdentity=unix-group:sudo\n"
            "Action=com.ubuntu.usbcreator.image\nResultActive=yes\n"
        )
        for kwargs in (
            {"policy": policy, "symlink": True},
            {"policy": policy + "#" * 17000},
            {"policy": "#\n" * 201 + policy},
        ):
            with self.subTest(kwargs=kwargs):
                result = self._run(**kwargs)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertNotIn("legacy group policy candidate", result.stdout)


if __name__ == "__main__":
    unittest.main()
