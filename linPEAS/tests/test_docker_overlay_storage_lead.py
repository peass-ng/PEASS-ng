"""Host overlay review uses mounted paths and metadata only, with a small cap."""

import os
import shlex
import subprocess
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
HELPER = ROOT / "linPEAS/builder/linpeas_parts/functions/checkDockerOverlayStorage.sh"
DETAILS = ROOT / "linPEAS/builder/linpeas_parts/2_container/3_Container_details.sh"
DOCKER_DETAILS = ROOT / "linPEAS/builder/linpeas_parts/2_container/4_Docker_container_details.sh"
VERSION_HELPER = ROOT / "linPEAS/builder/linpeas_parts/functions/checkDockerVersionExploits.sh"


class DockerOverlayStorageLeadTests(unittest.TestCase):
    def run_probe(self, root, mounts, uid="1000"):
        script = (f". {shlex.quote(str(HELPER))}\n"
                  f"id() {{ printf '%s\\n' {shlex.quote(uid)}; }}\n"
                  f"checkDockerOverlayStorage {shlex.quote(str(root))} {shlex.quote(str(mounts))}\n")
        result = subprocess.run(["sh", "-c", script], capture_output=True,
                                text=True, timeout=3)
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout.splitlines()

    @staticmethod
    def mount_line(path, options="rw,relatime", fstype="overlay"):
        return f"40 30 0:42 / {path} {options} - {fstype} overlay rw\n"

    def test_exact_mounted_paths_and_mount_options(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / "docker"
            overlay = root / "overlay2"
            accepted = overlay / ("a" * 64) / "merged"
            nosuid = overlay / ("b" * 64) / "merged"
            noexec = overlay / ("c" * 64) / "merged"
            wrong_fs = overlay / ("f" * 64) / "merged"
            bad_id = overlay / "not-a-docker-id" / "merged"
            sibling = Path(tmp) / "other" / "overlay2" / ("d" * 64) / "merged"
            for path in (accepted, nosuid, noexec, wrong_fs, bad_id, sibling):
                path.mkdir(parents=True)
            mounts = Path(tmp) / "mountinfo"
            mounts.write_text(self.mount_line(accepted) + self.mount_line(accepted) +
                              self.mount_line(nosuid, "rw,nosuid") +
                              self.mount_line(noexec, "rw,noexec") +
                              self.mount_line(wrong_fs, fstype="tmpfs") +
                              self.mount_line(bad_id) + self.mount_line(sibling))
            self.assertEqual(self.run_probe(root, mounts), [str(accepted)])
            self.assertEqual(self.run_probe(root, mounts, uid="0"), [])

    def test_twelve_path_limit_and_no_path_enumeration(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / "docker"
            paths = [root / "overlay2" / f"{n:064x}" / "merged" for n in range(15)]
            for path in paths:
                path.mkdir(parents=True)
            mounts = Path(tmp) / "mountinfo"
            mounts.write_text("".join(self.mount_line(path) for path in paths))
            self.assertEqual(self.run_probe(root, mounts), list(map(str, paths[:12])))
            self.assertNotIn("find ", HELPER.read_text())

    def test_nontraversable_parent_is_not_reported(self):
        if os.geteuid() == 0:
            self.skipTest("root bypasses directory search permissions")
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / "docker"
            path = root / "overlay2" / ("e" * 64) / "merged"
            path.mkdir(parents=True)
            mounts = Path(tmp) / "mountinfo"
            mounts.write_text(self.mount_line(path))
            root.chmod(0o000)
            try:
                self.assertEqual(self.run_probe(root, mounts), [])
            finally:
                root.chmod(0o700)

    def test_sections_are_wired_and_shell_valid(self):
        self.assertIn("checkDockerOverlayStorage", DETAILS.read_text())
        self.assertIn("may predate CVE-2021-41091 fix", DOCKER_DETAILS.read_text())
        for module in (HELPER, VERSION_HELPER, DETAILS, DOCKER_DETAILS):
            result = subprocess.run(["sh", "-n", str(module)], capture_output=True,
                                    text=True, timeout=3)
            self.assertEqual(result.returncode, 0, f"{module}: {result.stderr}")

    def test_version_boundaries_and_unknown_strings(self):
        for version, expected in (
            ("20.10.8", "Yes"),
            ("20.10.9", "No"),
            ("20.10.5+dfsg1", "Yes"),
            ("24.0.9", "No"),
            ("20.10.9garbage", "Unknown"),
            ("unparseable", "Unknown"),
            ("9" * 65 + ".0.0", "Unknown"),
        ):
            with self.subTest(version=version):
                script = (f". {shlex.quote(str(VERSION_HELPER))}\n"
                          "echo_no() { printf No; }; echo_not_found() { printf Unknown; };\n"
                          f"dockerVersion={shlex.quote(version)}\n"
                          "checkDockerVersionExploits\n"
                          "printf '%s|%s|%s\\n' \"$VULN_CVE_2021_41091\" "
                          "\"$VULN_CVE_2019_13139\" \"$VULN_CVE_2019_5736\"\n")
                result = subprocess.run(["sh", "-c", script], capture_output=True,
                                        text=True, timeout=3)
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual(result.stderr, "")
                current, old_one, old_two = result.stdout.strip().split("|")
                self.assertEqual(current, expected)
                if version in ("20.10.9garbage", "unparseable", "9" * 65 + ".0.0"):
                    self.assertEqual((old_one, old_two), ("Unknown", "Unknown"))


if __name__ == "__main__":
    unittest.main()
