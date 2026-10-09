import re
import subprocess
import tempfile
import unittest
from pathlib import Path

import yaml


class HostapdEapUserTests(unittest.TestCase):
    def setUp(self):
        self.linpeas_dir = Path(__file__).resolve().parents[1]
        self.repo_root = self.linpeas_dir.parent

    def test_generated_hostapd_check_is_narrow_and_filters_eap_rows(self):
        with tempfile.TemporaryDirectory() as tmpdir:
            output = Path(tmpdir) / "linpeas.sh"
            build = subprocess.run(
                [
                    "python3", "-m", "builder.linpeas_builder", "--include",
                    "Passwords_php_files,Extra_software", "--output", str(output),
                ],
                cwd=self.linpeas_dir,
                capture_output=True,
                text=True,
            )
            self.assertEqual(build.returncode, 0, build.stdout + build.stderr)
            script = output.read_text()

            etc_find = re.search(r"(?m)^\s*FIND_ETC=(.*)$", script)
            var_find = re.search(r"(?m)^\s*FIND_VAR=(.*)$", script)
            storage = re.search(r"(?m)^\s*PSTORAGE_HOSTAPD=(.*)$", script)
            self.assertIsNotNone(etc_find)
            self.assertIsNotNone(var_find)
            self.assertIsNotNone(storage)
            self.assertIn('*.eap_user', etc_find.group(1))
            self.assertNotIn('*.eap_user', var_find.group(1))
            self.assertIn(r'^/etc/hostapd/[^/]*\.eap_user$', storage.group(1))
            self.assertIn(r'hostapd\.conf$', storage.group(1))

            section = re.search(
                r'(?ms)^if \[ "\$PSTORAGE_HOSTAPD" \] \|\| \[ "\$DEBUG" \]; then\n.*?^fi$',
                script,
            )
            self.assertIsNotNone(section)
            fixture = self.linpeas_dir / "tests" / "fixtures" / "hostapd" / "office.eap_user"
            run = subprocess.run(
                ["bash", "-c", 'print_2title() { :; }; E=E; SED_RED=""; ' + section.group(0)],
                env={"PATH": "/usr/bin:/bin", "PSTORAGE_HOSTAPD": str(fixture)},
                capture_output=True,
                text=True,
            )
            self.assertEqual(run.returncode, 0, run.stderr)
            self.assertIn('"wireless-admin" MSCHAPV2 "wireless-secret" [2]', run.stdout)
            self.assertIn('"tunnel-user" TTLS-PAP "tunnel-secret" [2]', run.stdout)
            for excluded in (
                "commented", "certificate-user", "hashed-user", "phase-one",
                "malformed", "not-a-secret",
            ):
                self.assertNotIn(excluded, run.stdout)

    def test_hostapd_path_filter_keeps_existing_config_paths(self):
        data = yaml.safe_load((self.repo_root / "build_lists" / "sensitive_files.yaml").read_text())
        entry = next(item for item in data["search"] if item["name"] == "Hostapd")
        files = {item["name"]: item["value"] for item in entry["value"]["files"]}
        combined = "|".join(item["check_extra_path"] for item in files.values())
        candidates = (
            "/etc/hostapd/main.eap_user",
            "/etc/hostapd/hostapd_wpe.eap_user",
            "/opt/service/hostapd.conf",
        )
        rejected = (
            "/etc/hostapd/subdir/main.eap_user",
            "/var/www/main.eap_user",
            "/etc/hostapd/backup.eap_user.bak",
        )
        for path in candidates:
            self.assertRegex(path, combined)
        for path in rejected:
            self.assertNotRegex(path, combined)


if __name__ == "__main__":
    unittest.main()
