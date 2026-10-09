"""Focused root-SUID prerequisites for the existing pkexec candidate."""

from pathlib import Path
import subprocess
import tempfile
import unittest


IMAGE = "python:3.11-bookworm"
MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/6_users_information/10_Pkexec.sh"


class PkexecPrerequisiteTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        for command in (["docker", "info", "--format", "{{.ServerVersion}}"],
                        ["docker", "image", "inspect", IMAGE]):
            try:
                result = subprocess.run(command, capture_output=True, timeout=5)
            except (OSError, subprocess.TimeoutExpired):
                raise unittest.SkipTest("local Docker daemon or fixture image unavailable")
            if result.returncode:
                raise unittest.SkipTest("local Docker daemon or fixture image unavailable")

    def run_case(self, mode, owner=0, version="0.105", symlink=False):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            fixture = root / "pkexec"
            fixture.write_text(
                "#!/bin/sh\n"
                "printf '%s\\n' called >> /tmp/pkexec-queries\n"
                f"printf '%s\\n' 'pkexec version {version}'\n"
            )
            commands = [
                "cp /input/pkexec /tmp/pkexec-real",
                f"chown {owner}:0 /tmp/pkexec-real",
                f"chmod {mode} /tmp/pkexec-real",
            ]
            if symlink:
                commands.append("ln -s /tmp/pkexec-real /tmp/pkexec")
            else:
                commands.append("cp -p /tmp/pkexec-real /tmp/pkexec")
            commands += [
                "print_2title() { :; }; print_3title() { :; }; print_info() { :; }",
                "E='e'; SED_RED='&'; SED_RED_YELLOW='&'; SED_LIGHT_CYAN='&'",
                "PATH=/tmp:/usr/bin:/bin; export PATH",
                ". /module",
                "printf '%s\\n' 'QUERY_LOG:'",
                "cat /tmp/pkexec-queries 2>/dev/null || :",
            ]
            result = subprocess.run(
                ["docker", "run", "--rm", "--network", "none", "--mount",
                 f"type=bind,src={root},dst=/input,readonly", "--mount",
                 f"type=bind,src={MODULE},dst=/module,readonly", IMAGE,
                 "sh", "-c", "\n".join(commands)],
                capture_output=True, text=True, timeout=15,
            )
            self.assertEqual(result.returncode, 0, result.stderr)
            output, log = result.stdout.split("QUERY_LOG:\n", 1)
            return output, log.splitlines()

    def test_root_suid_old_version_is_one_conditional_candidate(self):
        output, log = self.run_case("4755", symlink=True)
        self.assertEqual(["called"], log)
        self.assertEqual(1, output.count("CVE-2021-4034 candidate:"))
        self.assertIn("verify distro package fixes", output)

    def test_non_suid_root_binary_suppresses_candidate_and_version_call(self):
        output, log = self.run_case("0755")
        self.assertEqual([], log)
        self.assertNotIn("CVE-2021-4034 candidate:", output)

    def test_nonroot_suid_and_nonexecutable_binaries_are_not_candidates(self):
        for mode, owner in (("4755", 1000), ("4644", 0)):
            with self.subTest(mode=mode, owner=owner):
                output, log = self.run_case(mode, owner=owner)
                self.assertEqual([], log)
                self.assertNotIn("CVE-2021-4034 candidate:", output)

    def test_newer_version_is_not_prefiltered_as_candidate(self):
        output, log = self.run_case("4755", version="0.120")
        self.assertEqual(["called"], log)
        self.assertNotIn("CVE-2021-4034 candidate:", output)


if __name__ == "__main__":
    unittest.main()
