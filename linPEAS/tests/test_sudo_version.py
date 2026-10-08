"""Bounded sudo version enumeration in a disposable Linux container."""

import subprocess
import tempfile
import unittest
from pathlib import Path


IMAGE = "python:3.11-bookworm"
MODULE = Path(__file__).resolve().parents[1] / "builder/linpeas_parts/1_system_information/2_Sudo_version.sh"


class SudoVersionTests(unittest.TestCase):
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

    def run_case(self, versions, setuid=(), aliases=()):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            for name, version in versions.items():
                directory = root / name
                directory.mkdir()
                version_command = (
                    "sleep 10\n" if version == "stall" else
                    f"printf '%s\\n' 'Sudo version {version}'\n"
                )
                (directory / "sudo").write_text(
                    "#!/bin/sh\n"
                    f"printf '%s\\n' '{name}' >> /tmp/sudo-queries\n"
                    + version_command
                )
            for name, target in aliases:
                directory = root / name
                directory.mkdir()
                (directory / "sudo").symlink_to(f"../{target}/sudo")

            path_names = [*versions, *(name for name, _ in aliases)]
            commands = [
                "cp -R /input/. /tmp/sudo-fixture",
                "chown -R root:root /tmp/sudo-fixture",
            ]
            for name in versions:
                mode = "4755" if name in setuid else "0755"
                commands.append(f"chmod {mode} /tmp/sudo-fixture/{name}/sudo")
            path = ":".join(f"/tmp/sudo-fixture/{name}" for name in path_names)
            commands += [
                "print_2title() { :; }; print_info() { :; }; echo_not_found() { echo missing-sudo; }",
                f"PATH='{path}:/usr/bin:/bin'; export PATH",
                ". /module",
                "printf '%s\\n' 'QUERY_LOG:'",
                "cat /tmp/sudo-queries 2>/dev/null || :",
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

    def test_alternate_path_versions_and_symlink_deduplication(self):
        output, log = self.run_case(
            {"first": "1.9.13p3", "second": "1.9.17"},
            setuid=("first", "second"), aliases=(("alias", "second"),),
        )
        self.assertEqual(log, ["first", "second"])
        self.assertEqual(output.count("CVE-2025-32462 upstream candidate"), 2)
        self.assertEqual(output.count("CVE-2025-32463 upstream candidate"), 1)
        self.assertEqual(output.count("Version alone cannot confirm exposure"), 2)

    def test_fixed_patch_releases_are_not_candidates(self):
        output, log = self.run_case(
            {"first": "1.9.17p1", "second": "1.9.17p2"},
            setuid=("first", "second"),
        )
        self.assertEqual(log, ["first", "second"])
        self.assertEqual(output.count("No CVE-2025-32462/32463 upstream candidate"), 2)
        self.assertNotIn("upstream candidate (", output)

    def test_upstream_range_boundaries(self):
        output, log = self.run_case(
            {"old": "1.8.7", "host": "1.8.8", "chroot": "1.9.14"},
            setuid=("old", "host", "chroot"),
        )
        self.assertEqual(log, ["old", "host", "chroot"])
        self.assertEqual(output.count("CVE-2025-32462 upstream candidate"), 2)
        self.assertEqual(output.count("CVE-2025-32463 upstream candidate"), 1)
        self.assertIn("Sudo version 1.8.7\n    No CVE-2025", output)

    def test_non_setuid_is_not_executed_and_backport_is_caveated(self):
        output, log = self.run_case(
            {"first": "1.9.17", "second": "1.9.13p3"},
            setuid=("second",),
        )
        self.assertEqual(log, ["second"])
        self.assertIn("not setuid; version query skipped", output)
        self.assertIn("CVE-2025-32462 upstream candidate", output)
        self.assertNotIn("CVE-2025-32463 upstream candidate", output)
        self.assertIn("vendors backport fixes", output)

    def test_version_query_has_a_per_binary_timeout(self):
        output, log = self.run_case({"first": "stall"}, setuid=("first",))
        self.assertEqual(log, ["first"])
        self.assertIn("version unavailable or query timed out", output)
        self.assertNotIn("upstream candidate (", output)

    def test_path_entries_are_bounded(self):
        versions = {f"slot{index:02}": "1.9.17p1" for index in range(25)}
        output, log = self.run_case(versions, setuid=tuple(versions))
        self.assertEqual(log, list(versions)[:24])
        self.assertEqual(output.count("No CVE-2025-32462/32463 upstream candidate"), 24)


if __name__ == "__main__":
    unittest.main()
