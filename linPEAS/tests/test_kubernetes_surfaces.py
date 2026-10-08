import json
import os
import shutil
import subprocess
import tempfile
import time
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
            timeout=15,
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

    def test_mounted_node_root_exposes_kubelet_files_and_volumes(self):
        with tempfile.TemporaryDirectory() as root:
            hostroot = Path(root) / "mounted-node"
            kubelet = hostroot / "var/lib/kubelet"
            kubelet.mkdir(parents=True)
            (kubelet / "config.yaml").write_text(
                "readOnlyPort: 10255\nauthentication:\n  anonymous:\n    enabled: true\n"
                "privateKey: SENSITIVE_VALUE\n"
            )
            secret = kubelet / "pods/pod-a/volumes/kubernetes.io~secret/app"
            secret.mkdir(parents=True)
            (secret / "token").write_text("SENSITIVE_TOKEN_VALUE")
            mountinfo = Path(root) / "mountinfo"
            mountinfo.write_text(f"44 33 0:1 / {hostroot} rw - ext4 /dev/sda rw\n")

            result = self.run_shell(
                'k8s_has_mounted_node_root "$1" && k8s_scan_host_roots "$1"', mountinfo
            )
            self.assertIn(f"Possible mounted node root: {hostroot}", result)
            self.assertIn(str(secret), result)
            self.assertIn("readOnlyPort: 10255", result)
            self.assertNotIn("SENSITIVE_VALUE", result)
            self.assertNotIn("SENSITIVE_TOKEN_VALUE", result)

            mountinfo.write_text("44 33 0:1 / /no-kubelet-here rw - ext4 /dev/sda rw\n")
            self.assertEqual(
                "missing\n",
                self.run_shell('k8s_has_mounted_node_root "$1" || echo missing', mountinfo),
            )

    def test_kubelet_bind_mounts_at_arbitrary_destinations_are_scanned(self):
        with tempfile.TemporaryDirectory() as root:
            kubelet = Path(root) / "mounted-kubelet"
            projected = kubelet / "pods/pod-a/volumes/kubernetes.io~projected/sa"
            projected.mkdir(parents=True)
            (projected / "token").write_text("SENSITIVE_TOKEN_VALUE")
            (kubelet / "config.yaml").write_text("readOnlyPort: 10255\n")
            secret = Path(root) / "mounted-secret"
            secret.mkdir()
            (secret / "token").write_text("SENSITIVE_SECRET_VALUE")
            mountinfo = Path(root) / "mountinfo"
            mountinfo.write_text(
                f"44 33 0:1 /var/lib/kubelet {kubelet} rw - ext4 /dev/sda rw\n"
                f"45 33 0:1 /var/lib/kubelet/pods/pod-b/volumes/kubernetes.io~secret/app "
                f"{secret} rw - ext4 /dev/sda rw\n"
            )
            result = self.run_shell(
                'k8s_has_kubelet_mount "$1" && k8s_scan_kubelet_mounts "$1"', mountinfo
            )
            self.assertIn(str(projected), result)
            self.assertIn(str(secret / "token"), result)
            self.assertIn("readOnlyPort: 10255", result)
            self.assertNotIn("SENSITIVE_TOKEN_VALUE", result)
            self.assertNotIn("SENSITIVE_SECRET_VALUE", result)

    def test_rke2_and_k0s_credentials_are_found_under_mounted_root(self):
        with tempfile.TemporaryDirectory() as root:
            hostroot = Path(root) / "node"
            rke2 = hostroot / "etc/rancher/rke2/rke2.yaml"
            k0s = hostroot / "var/lib/k0s/pki/admin.conf"
            for path in (rke2, k0s):
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("SENSITIVE_KUBECONFIG")
            mountinfo = Path(root) / "mountinfo"
            mountinfo.write_text(f"44 33 0:1 / {hostroot} rw - ext4 /dev/sda rw\n")
            result = self.run_shell(
                'k8s_has_mounted_node_root "$1" && k8s_scan_host_roots "$1"', mountinfo
            )
            self.assertIn(str(rke2), result)
            self.assertIn(str(k0s), result)
            self.assertNotIn("SENSITIVE_KUBECONFIG", result)

    def test_kubectl_discovery_is_opt_in_and_uses_bounded_name_queries(self):
        with tempfile.TemporaryDirectory() as root:
            kubectl = Path(root) / "kubectl"
            log = Path(root) / "calls"
            kubectl.write_text(
                "#!/bin/sh\n"
                'printf "%s\\n" "$*" >> "$KUBECTL_CALLS"\n'
                'case "$*" in\n'
                '  *"config get-contexts"*) echo cluster-a ;;\n'
                '  *"config current-context"*) echo current ;;\n'
                '  *"config view"*) echo testing ;;\n'
                '  *"get namespaces"*) echo namespace/testing ;;\n'
                '  *"get pods,services,serviceaccounts,secrets"*) echo pod/example ;;\n'
                '  *"auth can-i get nodes/"*) echo yes ;;\n'
                '  *"get nodes"*) echo node/example ;;\n'
                '  *"hostPath.path"*) echo "example: /host" ;;\n'
                '  *"get pods"*) echo "example sa=default hostPID=true" ;;\n'
                '  *"auth can-i"*) echo "pods list" ;;\n'
                'esac\n'
            )
            kubectl.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{root}:{env['PATH']}"
            env["KUBECTL_CALLS"] = str(log)

            output = self.run_shell("k8s_namespace=''; EXTRA_CHECKS=''; k8s_scan_kubectl", env=env)
            self.assertIn("cluster-a", output)
            self.assertNotIn("pod/example", output)
            self.assertEqual(1, len(log.read_text().splitlines()))

            output = self.run_shell("k8s_namespace=''; EXTRA_CHECKS=1; k8s_scan_kubectl", env=env)
            self.assertIn("namespace/testing", output)
            self.assertIn("pod/example", output)
            self.assertIn("example: /host", output)
            self.assertIn("Context: cluster-a", output)
            self.assertIn("example: yes", output)
            for call in log.read_text().splitlines()[1:]:
                if "config " not in call:
                    self.assertIn("--request-timeout=5s", call)

    def test_service_account_api_fallback_reports_names_without_secret_values(self):
        with tempfile.TemporaryDirectory() as root:
            root = Path(root)
            sa = root / "serviceaccount"
            sa.mkdir()
            (sa / "token").write_text("SENSITIVE_TOKEN_VALUE")
            (sa / "ca.crt").write_text("test CA")
            (sa / "namespace").write_text("workloads\n")
            calls = root / "curl-calls"
            curl = root / "curl"
            curl.write_text(
                "#!/bin/sh\n"
                'printf "%s\\n" "$*" >> "$CURL_CALLS"\n'
                'read -r header\n'
                'case "$*" in\n'
                '  *"/api/v1/namespaces/workloads/pods"*)\n'
                "    echo '{\"items\":[{\"metadata\":{\"name\":\"pod-a\"},\"spec\":{\"serviceAccountName\":\"runner\",\"hostPID\":true,\"containers\":[{\"name\":\"app\",\"securityContext\":{\"privileged\":true}}],\"volumes\":[{\"hostPath\":{\"path\":\"/var/log\"}}]}}]}' ;;\n"
                '  *"/api/v1/namespaces/workloads/secrets"*) echo \'{"items":[{"metadata":{"name":"my-secret"},"type":"Opaque","data":{"password":"SENSITIVE_SECRET_VALUE"}}]}\' ;;\n'
                '  *"/api/v1/namespaces/workloads/services"*) echo \'{"items":[{"metadata":{"name":"svc-a"}}]}\' ;;\n'
                '  *"/api/v1/namespaces/workloads/serviceaccounts"*) echo \'{"items":[{"metadata":{"name":"runner"}}]}\' ;;\n'
                '  *"/api/v1/namespaces?"*) echo \'{"items":[{"metadata":{"name":"workloads"}}]}\' ;;\n'
                '  *"/api/v1/nodes?"*) echo \'{"items":[{"metadata":{"name":"node-a"}}]}\' ;;\n'
                'esac\n'
                "printf '\\n200'\n"
            )
            curl.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{root}:{env['PATH']}"
            env["CURL_CALLS"] = str(calls)
            env["KUBERNETES_SERVICE_HOST"] = "10.0.0.1"
            output = self.run_shell('k8s_namespace=""; k8s_scan_sa_api "$1"', sa, env=env)
            self.assertIn("namespace: workloads", output)
            self.assertIn("pod: pod-a sa=runner hostPID=true", output)
            self.assertIn("container app privileged=true", output)
            self.assertIn("hostPath /var/log", output)
            self.assertIn('secrets: name="my-secret" type="Opaque" keys=["password"]', output)
            self.assertIn("node: node-a", output)
            self.assertNotIn("SENSITIVE_TOKEN_VALUE", output)
            self.assertNotIn("SENSITIVE_SECRET_VALUE", output)
            self.assertNotIn("SENSITIVE_TOKEN_VALUE", calls.read_text())
            self.assertTrue(all("--max-time 5" in call for call in calls.read_text().splitlines()))
            self.assertTrue(all(call.startswith("-q ") for call in calls.read_text().splitlines()))
            self.assertEqual(1, sum("/secrets?limit=40" in call for call in calls.read_text().splitlines()))

    def run_secret_fixture(self, root, payload, status="200", exit_code=0):
        root = Path(root)
        fixture = root / "secrets.json"
        fixture.write_text(payload)
        calls = root / "curl-calls"
        curl = root / "curl"
        curl.write_text(
            "#!/bin/sh\n"
            'printf "%s\\n" "$*" >> "$CURL_CALLS"\n'
            'cat "$SECRET_FIXTURE"\n'
            'printf "\\n%s" "$SECRET_STATUS"\n'
            'exit "$SECRET_EXIT"\n'
        )
        curl.chmod(0o755)
        env = os.environ.copy()
        env.update({
            "PATH": f"{root}:{env['PATH']}",
            "CURL_CALLS": str(calls),
            "SECRET_FIXTURE": str(fixture),
            "SECRET_STATUS": status,
            "SECRET_EXIT": str(exit_code),
        })
        output = self.run_shell(
            'k8s_direct_token=SENSITIVE_TOKEN_VALUE; k8s_direct_ca=/dev/null; '
            'k8s_direct_base=https://127.0.0.1:443; k8s_namespace=workloads; '
            'k8s_scan_sa_secrets', env=env
        )
        return output, calls.read_text().splitlines()

    def test_secret_inventory_only_prints_bounded_metadata(self):
        with tempfile.TemporaryDirectory() as root:
            items = [
                {"metadata": {"name": "credentials"}, "type": "Opaque", "data": {
                    "password": "SENSITIVE_PASSWORD_VALUE", "username": "SENSITIVE_USER_VALUE"}},
                {"metadata": {"name": "release"}, "type": "helm.sh/release.v1", "data": {
                    "release": "SENSITIVE_HELM_VALUE"}},
            ]
            items += [{"metadata": {"name": f"item-{n}"}, "type": "Opaque",
                       "data": {f"key-{k:02}": "SENSITIVE_EXTRA_VALUE" for k in range(25)}}
                      for n in range(43)]
            output, calls = self.run_secret_fixture(
                root, json.dumps({"items": items, "metadata": {"continue": "opaque-cursor"}})
            )
            self.assertIn('name="credentials" type="Opaque" keys=["password","username"]', output)
            self.assertIn('name="release" type="helm.sh/release.v1" keys=["release"]', output)
            self.assertIn('"key-19"] (additional keys omitted)', output)
            self.assertNotIn("key-20", output)
            self.assertNotIn("item-38", output)
            self.assertIn("additional results omitted", output)
            self.assertEqual(40, output.count("secrets: name="))
            self.assertNotIn("SENSITIVE_", output)
            self.assertEqual(1, len(calls))
            self.assertIn("/api/v1/namespaces/workloads/secrets?limit=40", calls[0])
            self.assertIn("--max-filesize 1048576", calls[0])
            self.assertNotIn("SENSITIVE_TOKEN_VALUE", calls[0])

    def test_secret_inventory_error_and_empty_cases(self):
        cases = [
            ("", "403", 22, "access denied (RBAC)"),
            ("", "404", 22, "namespace unavailable"),
            ("", "000", 28, "request timed out"),
            ("", "200", 63, "response exceeded 1 MiB"),
            ("{invalid", "200", 0, "malformed API response"),
            ('{"items":{}}', "200", 0, "malformed API response"),
            ('{"items":[]}', "200", 0, "none (accessible list)"),
        ]
        for payload, status, exit_code, expected in cases:
            with self.subTest(expected=expected), tempfile.TemporaryDirectory() as root:
                output, calls = self.run_secret_fixture(root, payload, status, exit_code)
                self.assertIn(expected, output)
                self.assertEqual(1, len(calls))
                self.assertNotIn("SENSITIVE_TOKEN_VALUE", output)

    def test_direct_api_inventory_requires_opt_in_and_dependencies(self):
        output = self.run_shell(
            'command() { [ "$2" != kubectl ]; }; '
            'k8s_scan_sa_api() { echo DIRECT_API_CALLED; }; '
            'EXTRA_CHECKS=""; k8s_scan_kubectl; '
            'EXTRA_CHECKS=1; k8s_scan_kubectl'
        )
        self.assertEqual("DIRECT_API_CALLED\n", output)
        output = self.run_shell(
            'command() { [ "$2" != jq ]; }; '
            'k8s_scan_sa_api'
        )
        self.assertIn("discovery skipped (curl or jq unavailable)", output)

    def test_service_account_api_preserves_explicit_false_security_setting(self):
        with tempfile.TemporaryDirectory() as root:
            fixture = Path(root) / "pods.json"
            fixture.write_text(json.dumps({"items": [{
                "metadata": {"name": "example"},
                "spec": {"containers": [
                    {"name": "restricted", "securityContext": {"allowPrivilegeEscalation": False}},
                    {"name": "allowed", "securityContext": {"allowPrivilegeEscalation": True}},
                    {"name": "unspecified"},
                ]},
            }]}))
            output = self.run_shell(
                'fixture="$1"; '
                'k8s_direct_api_ready() { return 0; }; '
                'k8s_sa_api_get() { case "$1" in '
                '*/pods\\?*) cat "$fixture" ;; '
                '*) echo \'{"items":[]}\' ;; esac; }; '
                'k8s_scan_sa_api',
                fixture,
            )
            self.assertIn("container restricted privileged=false allowPE=false", output)
            self.assertIn("container allowed privileged=false allowPE=true", output)
            self.assertIn("container unspecified privileged=false allowPE=default", output)

    @unittest.skipUnless(shutil.which("timeout"), "timeout is required")
    def test_kubectl_has_process_deadline_not_only_request_timeout(self):
        with tempfile.TemporaryDirectory() as root:
            kubectl = Path(root) / "kubectl"
            kubectl.write_text("#!/bin/sh\nsleep 30\n")
            kubectl.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{root}:{env['PATH']}"
            started = time.monotonic()
            output = self.run_shell(
                "k8s_kubectl config current-context || echo bounded", env=env
            )
            self.assertEqual("bounded\n", output)
            self.assertLess(time.monotonic() - started, 10)

    def test_kubectl_is_skipped_without_deadline_tool(self):
        output = self.run_shell(
            'command() { return 1; }; '
            'kubectl() { echo SHOULD_NOT_RUN; }; '
            'k8s_kubectl config current-context || echo skipped'
        )
        self.assertEqual("skipped\n", output)

    def test_escape_capability_bits_are_decoded(self):
        result = self.run_shell(
            'for bit in 18 19 21; do '
            'k8s_cap_has 00000000002c0000 "$bit" && echo "$bit present"; '
            'done; k8s_cap_has 0000000000000000 21 || echo "21 absent"'
        )
        self.assertEqual(
            "18 present\n19 present\n21 present\n21 absent\n", result
        )

    def test_runc_release_range_is_reported_as_upstream_evidence(self):
        result = self.run_shell(
            'for version in 1.0.0-rc92 1.0.0-rc93 1.0.3 1.1.11 1.1.12 1.2.0; do '
            'k8s_runc_21626_upstream_range "$version" && echo "$version flagged"; '
            'done; true'
        )
        self.assertEqual("1.0.0-rc93 flagged\n1.0.3 flagged\n1.1.11 flagged\n", result)
        with tempfile.TemporaryDirectory() as root:
            runc = Path(root) / "runc"
            runc.write_text("#!/bin/sh\necho 'runc version 1.1.11'\n")
            runc.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{root}:{env['PATH']}"
            output = self.run_shell("k8s_scan_runc_21626", env=env)
            self.assertIn("CVE-2024-21626", output)
            self.assertIn("vendor backports need confirmation", output)

    def test_kubelet_probes_are_bounded_and_send_no_credentials(self):
        with tempfile.TemporaryDirectory() as root:
            log = Path(root) / "curl-calls"
            kubectl = Path(root) / "kubectl"
            kubectl.write_text(
                "#!/bin/sh\n"
                'printf "10.1.2.3\\n10.1.2.3\\ninvalid.example.com\\n"\n'
            )
            kubectl.chmod(0o755)
            curl = Path(root) / "curl"
            curl.write_text(
                "#!/bin/sh\n"
                'printf "%s\\n" "$*" >> "$CURL_CALLS"\n'
                'printf "401"\n'
            )
            curl.chmod(0o755)
            dig = Path(root) / "dig"
            dig.write_text("#!/bin/sh\nprintf '0 100 8080 service.svc.cluster.local.\\n'\n")
            dig.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{root}:{env['PATH']}"
            env["CURL_CALLS"] = str(log)

            output = self.run_shell("k8s_scan_kubelet_network; k8s_scan_service_dns", env=env)
            self.assertIn("Kubelet https://10.1.2.3:10250 responded HTTP 401", output)
            self.assertIn("Kubelet http://10.1.2.3:10255 responded HTTP 401", output)
            self.assertIn("service.svc.cluster.local", output)
            calls = log.read_text().splitlines()
            self.assertEqual(2, len(calls))
            self.assertTrue(all("--max-time 2" in call for call in calls))
            self.assertTrue(all("Authorization" not in call for call in calls))
            self.assertTrue(all(call.startswith("-q ") for call in calls))

    def test_kops_storage_lists_candidate_names_without_reading_objects(self):
        with tempfile.TemporaryDirectory() as root:
            calls = Path(root) / "cloud-calls"
            timeout = Path(root) / "timeout"
            timeout.write_text(
                '#!/bin/sh\n'
                'printf "%s\\n" "$*" >> "$TIMEOUT_CALLS"\n'
                '[ "$1" = -s ] && shift 2\nshift\n"$@"\n'
            )
            timeout.chmod(0o755)
            aws = Path(root) / "aws"
            aws.write_text(
                '#!/bin/sh\n'
                'printf "aws %s\\n" "$*" >> "$CLOUD_CALLS"\n'
                'case "$*" in\n'
                '  *list-buckets*) printf "kops-state\\tother-bucket\\n" ;;\n'
                '  *list-objects-v2*) printf "cluster/secrets/sa-token\\tconfig.yaml\\n" ;;\n'
                'esac\n'
            )
            aws.chmod(0o755)
            gcloud = Path(root) / "gcloud"
            gcloud.write_text(
                '#!/bin/sh\n'
                'printf "gcloud %s\\n" "$*" >> "$CLOUD_CALLS"\n'
                'case "$*" in\n'
                '  *"buckets list"*) echo state-bucket ;;\n'
                '  *"objects list"*) echo cluster/secrets/sa-token ;;\n'
                'esac\n'
            )
            gcloud.chmod(0o755)
            env = os.environ.copy()
            env["PATH"] = f"{root}:{env['PATH']}"
            env["CLOUD_CALLS"] = str(calls)
            deadlines = Path(root) / "timeout-calls"
            env["TIMEOUT_CALLS"] = str(deadlines)

            output = self.run_shell("k8s_scan_kops_storage", env=env)
            self.assertIn("s3://kops-state", output)
            self.assertIn("gs://state-bucket", output)
            self.assertIn("candidate key: cluster/secrets/sa-token", output)
            self.assertIn("candidate object: cluster/secrets/sa-token", output)
            self.assertNotIn("config.yaml", output)
            self.assertTrue(all("list" in call for call in calls.read_text().splitlines()))
            self.assertTrue(all(call.startswith("-s KILL 12 ") for call in deadlines.read_text().splitlines()))


if __name__ == "__main__":
    unittest.main()
