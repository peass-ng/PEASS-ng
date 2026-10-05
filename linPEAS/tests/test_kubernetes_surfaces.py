import os
import subprocess
import tempfile
import unittest
from pathlib import Path


MODULE = (
    Path(__file__).resolve().parents[1]
    / "builder/linpeas_parts/2_container/6_Kubernetes_surfaces.sh"
)


class KubernetesSurfacesTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        # Load only function definitions; the module's final block scans the
        # real machine and is exercised by the generated-script build.
        cls.functions = MODULE.read_text().split("if k8s_context_present; then\n", 1)[0]

    def run_shell(self, command, *arguments, env=None):
        return subprocess.run(
            ["sh", "-c", self.functions + "\n" + command, "test", *map(str, arguments)],
            check=True,
            capture_output=True,
            text=True,
            env=env,
        ).stdout

    def test_projected_and_legacy_volumes_report_paths_without_values(self):
        with tempfile.TemporaryDirectory() as root:
            pods = Path(root) / "pods"
            legacy = pods / "pod-a/volumes/kubernetes.io~secret/legacy-token"
            projected = pods / "pod-b/volumes/kubernetes.io~projected/kube-api-access-x"
            for path in (legacy, projected):
                path.mkdir(parents=True)
                (path / "token").write_text("SENSITIVE_TOKEN_VALUE")
            result = self.run_shell('k8s_scan_pod_volumes "$1"', pods)
            self.assertIn(str(legacy), result)
            self.assertIn(str(projected), result)
            self.assertIn("readable=yes", result)
            self.assertNotIn("SENSITIVE_TOKEN_VALUE", result)

    def test_writable_hostlog_mount_is_flagged_from_mountinfo(self):
        with tempfile.TemporaryDirectory() as root:
            mountpoint = Path(root) / "logs"
            mountpoint.mkdir()
            mountinfo = Path(root) / "mountinfo"
            mountinfo.write_text(
                f"44 33 0:1 /var/log/pods {mountpoint} rw,relatime - ext4 /dev/sda rw\n"
            )
            result = self.run_shell('k8s_scan_mountinfo "$1"', mountinfo)
            self.assertIn("source-root=/var/log/pods", result)
            self.assertIn("Potential writable host-log mount", result)

            mountinfo.write_text(
                f"44 33 0:1 /var/log/pods {mountpoint} ro,relatime - ext4 /dev/sda ro\n"
            )
            result = self.run_shell('k8s_scan_mountinfo "$1"', mountinfo)
            self.assertIn("write+search=no", result)
            self.assertNotIn("Potential writable host-log mount", result)

    def test_api_environment_detects_kubernetes_without_cgroup_marker(self):
        env = os.environ.copy()
        env["KUBERNETES_SERVICE_HOST"] = "10.0.0.1"
        result = self.run_shell('containerType=No; k8s_context_present && echo found', env=env)
        self.assertEqual("found\n", result)

    def test_alternate_procfs_and_cgroup_mounts_are_found(self):
        with tempfile.TemporaryDirectory() as root:
            proc_mount = Path(root) / "hostproc"
            cgroup_mount = Path(root) / "hostcgroup"
            (proc_mount / "sys/kernel").mkdir(parents=True)
            cgroup_mount.mkdir()
            (proc_mount / "sys/kernel/core_pattern").write_text("core")
            (cgroup_mount / "release_agent").write_text("agent")
            (cgroup_mount / "notify_on_release").write_text("0")
            mountinfo = Path(root) / "mountinfo"
            mountinfo.write_text(
                f"44 33 0:1 / {proc_mount} rw - proc proc rw\n"
                f"45 33 0:2 / {cgroup_mount} rw - cgroup cgroup rw\n"
            )
            result = self.run_shell('k8s_scan_kernel_mounts "$1"', mountinfo)
            self.assertIn(str(proc_mount / "sys/kernel/core_pattern"), result)
            self.assertIn(str(cgroup_mount / "release_agent"), result)
            self.assertIn(str(cgroup_mount / "notify_on_release"), result)


if __name__ == "__main__":
    unittest.main()
