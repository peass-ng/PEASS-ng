import os
import shlex
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path


@unittest.skipUnless(shutil.which("dpkg"), "dpkg version comparison required")
class AppArmorCrackArmorTests(unittest.TestCase):
    module = Path(__file__).resolve().parents[1] / (
        "builder/linpeas_parts/8_interesting_perms_files/9_App_armour_profiles.sh"
    )

    def review(self, version="2.37.2-4ubuntu3.4", enabled="Y",
               policy_mode=0o666, su_mode=0o4755, release="22.04"):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            control = root / "sys/module/apparmor/parameters"
            control.mkdir(parents=True)
            (control / "enabled").write_text(enabled)
            policy = root / "sys/kernel/security/apparmor"
            policy.mkdir(parents=True)
            for name in (".load", ".replace", ".remove"):
                path = policy / name
                path.write_text("")
                path.chmod(policy_mode)
            su = root / "usr/bin/su"
            su.parent.mkdir(parents=True)
            su.write_text("#!/bin/sh\nexit 99\n")
            su.chmod(su_mode)

            # Mock ownership only; the check never executes this fake su.
            tools = root / "tools"
            tools.mkdir()
            stat = tools / "stat"
            stat.write_text("#!/bin/sh\nprintf '0\\n'\n")
            stat.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{tools}:{env['PATH']}"
            script = "\n".join((
                "SEARCH_IN_FOLDER=1", "E=E", "SED_RED_YELLOW='&'",
                "print_3title() { printf '%s\\n' \"$1\"; }",
                "print_info() { :; }",
                f". {shlex.quote(str(self.module))}",
                "apparmor_crackarmor_review " + " ".join(map(shlex.quote, (
                    str(root), release, version
                ))),
            ))
            result = subprocess.run(["sh", "-c", script], env=env,
                                    capture_output=True, text=True, timeout=10)
            self.assertEqual(result.returncode, 0, result.stderr)
            return result.stdout

    def test_reports_only_unmitigated_prerequisites(self):
        self.assertIn("CVE-2026-23268", self.review())
        self.assertEqual("", self.review(version="2.37.2-4ubuntu3.5"))
        self.assertEqual("", self.review(enabled="N"))
        self.assertEqual("", self.review(policy_mode=0o664))
        self.assertEqual("", self.review(su_mode=0o755))
        self.assertEqual("", self.review(release="26.04"))


if __name__ == "__main__":
    unittest.main()
