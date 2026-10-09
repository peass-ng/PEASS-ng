import os
import subprocess
import tempfile
import unittest
from pathlib import Path


class SystemdReleaseReportingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        root = Path(__file__).resolve().parents[2]
        script = root / "linPEAS/builder/linpeas_parts/4_procs_crons_timers_srvcs_sockets/11_Systemd.sh"
        source = script.read_text(encoding="utf-8")
        start = source.index("    # The upstream release alone")
        end = source.index("    # Check for systemd services running as root", start)
        cls.release_block = source[start:end]

    def _run(self, first_line):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            fake_systemctl = root / "systemctl"
            fake_systemctl.write_text(
                "#!/bin/sh\n"
                "if [ \"$1\" = --version ]; then\n"
                "  printf '%s\\n' \"$FAKE_VERSION_LINE\" 'FEATURES=stub'\n"
                "fi\n",
                encoding="utf-8",
            )
            fake_systemctl.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{root}:{env.get('PATH', '')}"
            env["FAKE_VERSION_LINE"] = first_line
            shell = "\n".join(
                [
                    "NC=",
                    'print_list() { printf "TITLE: %s\\n" "$1"; }',
                    "check_systemctl() { command -v systemctl >/dev/null 2>&1; }",
                    self.release_block,
                ]
            )
            return subprocess.run(
                ["sh", "-c", shell],
                env=env,
                capture_output=True,
                text=True,
                check=False,
            )

    def test_integer_systemd_release_is_printed_without_vulnerability_verdict(self):
        for line, release in (
            ("systemd 230", "230"),
            ("systemd 240 (240.8-distribution4)", "240"),
            ("systemd 249 (249.11-distribution7)", "249"),
        ):
            with self.subTest(line=line):
                result = self._run(line)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertIn("TITLE: Systemd release (verify package fixes)?", result.stdout)
                self.assertEqual(result.stdout.splitlines()[-1], release)
                self.assertNotIn("Vulnerable", result.stdout)
                self.assertNotIn("CVE-2021-4034", result.stdout)
                self.assertNotIn("CVE-2021-33910", result.stdout)

    def test_malformed_or_unrelated_banner_does_not_claim_release(self):
        for line in ("systemd 249.1", "systemd release unknown", "polkit 249", ""):
            with self.subTest(line=line):
                result = self._run(line)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(
                    result.stdout.strip(),
                    "TITLE: Systemd release (verify package fixes)? ..........",
                )


if __name__ == "__main__":
    unittest.main()
