# Title: Container - Kubernetes surfaces
# ID: CT_Kubernetes_surfaces
# Author: Carlos Polop
# Last Update: 05-10-2026
# Description: Enumerate local Kubernetes credentials, node mounts, kubelet volumes, and runtime sockets relevant to pod and node privilege escalation.
# License: GNU GPL
# Version: 1.0
# Mitre: T1613,T1611,T1552.007
# Functions Used: print_2title, print_3title
# Global Variables: $containerType, $EXTRA_CHECKS
# Initial Functions: containerCheck
# Generated Global Variables: $k8s_access_exit, $k8s_access_http, $k8s_access_json, $k8s_access_response, $k8s_access_result, $k8s_aws_bucket, $k8s_cap_eff, $k8s_cap_low, $k8s_cfg, $k8s_cfg_env, $k8s_context_name, $k8s_count, $k8s_current_context, $k8s_dir, $k8s_direct_base, $k8s_direct_ca, $k8s_direct_host, $k8s_direct_port, $k8s_direct_token, $k8s_direct_token_file, $k8s_docker_host, $k8s_file, $k8s_gcs_bucket, $k8s_mount, $k8s_mount_options, $k8s_mount_root, $k8s_namespace, $k8s_node_address, $k8s_node_addresses, $k8s_node_name, $k8s_ns_one, $k8s_ns_self, $k8s_pid, $k8s_probe_host, $k8s_probe_port, $k8s_probe_scheme, $k8s_probe_status, $k8s_rc, $k8s_readable, $k8s_root, $k8s_root_count, $k8s_runc_version, $k8s_secret_response, $k8s_secret_exit, $k8s_secret_http, $k8s_secret_json, $k8s_secret_summary, $k8s_socket, $k8s_writable, $count, $keys, $continued
# Fat linpeas: 0
# Small linpeas: 0

k8s_show_file() {
  [ -e "$1" ] || [ -L "$1" ] || return
  k8s_readable=no
  k8s_writable=no
  [ -r "$1" ] && k8s_readable=yes
  [ -w "$1" ] && k8s_writable=yes
  printf '  %s (readable=%s, writable=%s)\n' "$1" "$k8s_readable" "$k8s_writable"
}

k8s_has_mounted_node_root() {
  [ -r "$1" ] || return 1
  awk '$4 == "/" && $5 != "/" {print $5}' "$1" 2>/dev/null | head -n 100 |
  {
    while IFS= read -r k8s_root; do
      [ -d "$k8s_root/var/lib/kubelet" ] || [ -d "$k8s_root/etc/kubernetes" ] ||
        [ -d "$k8s_root/etc/rancher/rke2" ] || [ -d "$k8s_root/var/lib/k0s" ] || continue
      exit 0
    done
    exit 1
  }
}

k8s_has_kubelet_mount() {
  [ -r "$1" ] || return 1
  awk '$4 ~ "^/var/lib/kubelet(/|$)" {found=1; exit} END {exit !found}' "$1" 2>/dev/null
}

k8s_context_present() {
  for k8s_cfg in /home/*/.kube/config; do
    [ -f "$k8s_cfg" ] && return 0
  done
  [ -n "$KUBERNETES_SERVICE_HOST" ] ||
  [ -f "$HOME/.kube/config" ] ||
  [ -f /root/.kube/config ] ||
  [ -d /var/run/secrets/kubernetes.io/serviceaccount ] ||
  [ -d /run/secrets/kubernetes.io/serviceaccount ] ||
  [ -d /secrets/kubernetes.io/serviceaccount ] ||
  [ -d /var/lib/kubelet/pods ] ||
  [ -d /etc/kubernetes ] ||
  [ -d /etc/rancher/k3s ] ||
  [ -d /etc/rancher/rke2 ] ||
  [ -d /var/lib/k0s ] ||
  [ -d /var/snap/microk8s/current/credentials ] ||
  k8s_has_mounted_node_root /proc/self/mountinfo ||
  k8s_has_kubelet_mount /proc/self/mountinfo ||
  [ -n "$(env | sed -n 's/^KUBECONFIG=//p' | head -n 1)" ] ||
  printf '%s' "$containerType" | grep -qi kubernetes
}

k8s_scan_kubeconfigs() {
  k8s_cfg_env="$(env | sed -n 's/^KUBECONFIG=//p' | head -n 1)"
  if [ "$k8s_cfg_env" ]; then
    printf '%s\n' "$k8s_cfg_env" | tr ':' '\n' | while IFS= read -r k8s_cfg; do
      k8s_show_file "$k8s_cfg"
    done
  fi
  for k8s_cfg in "$HOME/.kube/config" /root/.kube/config /home/*/.kube/config \
    /etc/kubernetes/admin.conf /etc/kubernetes/kubelet.conf \
    /etc/kubernetes/bootstrap-kubelet.conf \
    /etc/kubernetes/controller-manager.conf /etc/kubernetes/scheduler.conf \
    /etc/kubernetes/kubeconfig /var/lib/kubelet/kubeconfig \
    /var/lib/kubelet/bootstrap-kubeconfig \
    /var/lib/kubelet/config.yaml /etc/rancher/k3s/k3s.yaml \
    /etc/rancher/k3s/config.yaml /etc/rancher/rke2/rke2.yaml \
    /etc/rancher/rke2/config.yaml \
    /var/lib/rancher/rke2/server/cred/admin.kubeconfig \
    /var/lib/rancher/rke2/server/node-token \
    /var/lib/rancher/rke2/agent/etc/crictl.yaml \
    /var/lib/rancher/k3s/server/node-token /var/lib/k0s/pki/admin.conf \
    /var/snap/microk8s/current/credentials/*.config; do
    k8s_show_file "$k8s_cfg"
  done
  for k8s_dir in /etc/kubernetes/pki /var/lib/kubelet/pki \
    /var/lib/rancher/k3s/server/tls /var/lib/k0s/pki \
    /var/lib/rancher/rke2/server/tls /var/snap/microk8s/current/certs; do
    [ -d "$k8s_dir" ] || continue
    printf '  Certificate/key directory: %s\n' "$k8s_dir"
    find "$k8s_dir" -maxdepth 2 -type f \( -name '*.key' -o -name '*.crt' -o -name '*.pem' \) \
      -print 2>/dev/null | head -n 30
  done
  if [ -d /etc/kubernetes/manifests ]; then
    echo '  Static pod manifests:'
    find /etc/kubernetes/manifests -maxdepth 1 -type f -print 2>/dev/null | head -n 20
  fi
  if [ -d /var/lib/rancher/rke2/agent/etc/kubelet.conf.d ]; then
    echo '  RKE2 kubelet configuration files:'
    find /var/lib/rancher/rke2/agent/etc/kubelet.conf.d -maxdepth 1 -type f \
      -print 2>/dev/null | head -n 20
  fi
}

k8s_scan_kubelet_config() {
  [ -r "$1" ] || return
  printf '  Kubelet access settings from %s:\n' "$1"
  grep -E '^[[:space:]]*(readOnlyPort|port|address|authentication|anonymous|enabled|authorization|mode|clientCAFile|rotateCertificates|serverTLSBootstrap):' \
    "$1" 2>/dev/null | head -n 25 | sed 's/^/    /'
}

k8s_scan_pod_volumes() {
  [ -d "$1" ] || return
  k8s_count=0
  for k8s_dir in "$1"/*/volumes/kubernetes.io~secret/* \
    "$1"/*/volumes/kubernetes.io~projected/*; do
    [ -d "$k8s_dir" ] || continue
    printf '  %s\n' "$k8s_dir"
    ls -A "$k8s_dir" 2>/dev/null | head -n 40 | sed 's/^/    /'
    k8s_show_file "$k8s_dir/token"
    k8s_count=$((k8s_count + 1))
    [ "$k8s_count" -lt 60 ] || break
  done
  [ "$k8s_count" -lt 60 ] || echo '  Volume listing stopped at 60 directories'
}

k8s_scan_host_roots() {
  [ -r "$1" ] || return
  k8s_root_count=0
  # A host root may be mounted at an arbitrary path. Restrict candidates to
  # mounts rooted at / that expose kubelet or Kubernetes directories.
  awk '$4 == "/" && $5 != "/" {print $5}' "$1" 2>/dev/null | head -n 100 |
  while IFS= read -r k8s_root; do
    [ -d "$k8s_root/var/lib/kubelet" ] || [ -d "$k8s_root/etc/kubernetes" ] ||
      [ -d "$k8s_root/etc/rancher/rke2" ] || [ -d "$k8s_root/var/lib/k0s" ] || continue
    printf '  Possible mounted node root: %s\n' "$k8s_root"
    for k8s_file in "$k8s_root/etc/kubernetes/admin.conf" \
      "$k8s_root/etc/kubernetes/kubelet.conf" \
      "$k8s_root/var/lib/kubelet/kubeconfig" \
      "$k8s_root/var/lib/kubelet/config.yaml" \
      "$k8s_root/etc/rancher/k3s/k3s.yaml" \
      "$k8s_root/etc/rancher/k3s/config.yaml" \
      "$k8s_root/etc/rancher/rke2/rke2.yaml" \
      "$k8s_root/etc/rancher/rke2/config.yaml" \
      "$k8s_root/var/lib/rancher/rke2/server/cred/admin.kubeconfig" \
      "$k8s_root/var/lib/rancher/rke2/server/node-token" \
      "$k8s_root/var/lib/rancher/k3s/server/node-token" \
      "$k8s_root/var/lib/k0s/pki/admin.conf"; do
      k8s_show_file "$k8s_file"
    done
    k8s_scan_kubelet_config "$k8s_root/var/lib/kubelet/config.yaml"
    if [ -d "$k8s_root/var/lib/rancher/rke2/agent/etc/kubelet.conf.d" ]; then
      find "$k8s_root/var/lib/rancher/rke2/agent/etc/kubelet.conf.d" \
        -maxdepth 1 -type f -print 2>/dev/null | head -n 20
    fi
    k8s_scan_pod_volumes "$k8s_root/var/lib/kubelet/pods"
    k8s_root_count=$((k8s_root_count + 1))
    if [ "$k8s_root_count" -ge 10 ]; then
      echo '  Mounted node-root listing stopped at 10 directories'
      break
    fi
  done
}

k8s_scan_kubelet_mounts() {
  [ -r "$1" ] || return
  awk '$4 ~ "^/var/lib/kubelet(/|$)" {print $4 "|" $5}' "$1" 2>/dev/null |
    head -n 40 |
  while IFS='|' read -r k8s_mount_root k8s_mount; do
    case "$k8s_mount" in /var/lib/kubelet|/var/lib/kubelet/pods) continue ;; esac
    [ -d "$k8s_mount" ] || continue
    printf '  Kubelet source %s mounted at %s\n' "$k8s_mount_root" "$k8s_mount"
    case "$k8s_mount_root" in
      /var/lib/kubelet)
        k8s_show_file "$k8s_mount/kubeconfig"
        k8s_show_file "$k8s_mount/config.yaml"
        k8s_scan_kubelet_config "$k8s_mount/config.yaml"
        k8s_scan_pod_volumes "$k8s_mount/pods"
        ;;
      /var/lib/kubelet/pods)
        k8s_scan_pod_volumes "$k8s_mount"
        ;;
      *'/volumes/kubernetes.io~secret/'*|*'/volumes/kubernetes.io~projected/'*)
        ls -A "$k8s_mount" 2>/dev/null | head -n 40 | sed 's/^/    /'
        k8s_show_file "$k8s_mount/token"
        ;;
    esac
  done
}

k8s_scan_mountinfo() {
  [ -r "$1" ] || return
  awk '$4 ~ "^/var/log(/|$)" || $5 == "/var/log" ||
       $4 ~ "^/var/lib/kubelet(/|$)" || $5 ~ "^/var/lib/kubelet(/|$)" ||
       $4 ~ "^/etc/kubernetes(/|$)" || $5 ~ "^/etc/kubernetes(/|$)" ||
       $5 ~ "^/(host|hostroot|rootfs|node|hostproc|mnt/host|mnt/node)(/|$)" {
         print $4 "|" $5 "|" $6
       }' "$1" 2>/dev/null | head -n 80 |
  while IFS='|' read -r k8s_mount_root k8s_mount k8s_mount_options; do
    k8s_writable=no
    case "$k8s_mount_options" in
      rw|rw,*)
        [ -w "$k8s_mount" ] && [ -x "$k8s_mount" ] && k8s_writable=yes
        ;;
    esac
    printf '  source-root=%s mount=%s options=%s write+search=%s\n' \
      "$k8s_mount_root" "$k8s_mount" "$k8s_mount_options" "$k8s_writable"
    case "$k8s_mount_root:$k8s_mount_options:$k8s_writable" in
      /var/log*:rw,*:yes)
        echo '    Potential writable host-log mount; node origin and nodes/proxy access need confirmation' ;;
    esac
    if [ "$k8s_mount" = /var/log ] && [ "$k8s_writable" = yes ]; then
      echo '    Writable /var/log mount; hostPath origin needs confirmation'
    fi
  done
}

k8s_scan_kernel_mounts() {
  [ -r "$1" ] || return
  awk '$0 ~ / - proc / {print $5}' "$1" 2>/dev/null | head -n 20 |
  while IFS= read -r k8s_mount; do
    k8s_show_file "$k8s_mount/sys/kernel/core_pattern"
  done
  awk '$0 ~ / - cgroup / {print $5}' "$1" 2>/dev/null | head -n 20 |
  while IFS= read -r k8s_mount; do
    k8s_show_file "$k8s_mount/release_agent"
    k8s_show_file "$k8s_mount/notify_on_release"
  done
}

k8s_cap_has() {
  case "$1" in ''|*[!0-9a-fA-F]*) return 1 ;; esac
  k8s_cap_low="$(printf '%s' "$1" | sed 's/.*\(........\)$/\1/')"
  [ "$((0x$k8s_cap_low & (1 << $2)))" -ne 0 ]
}

k8s_scan_escape_prereqs() {
  [ -r /proc/self/status ] || return
  k8s_cap_eff="$(awk '/^CapEff:/ {print $2; exit}' /proc/self/status 2>/dev/null)"
  printf '  Effective UID: %s\n' "$(id -u 2>/dev/null)"
  for k8s_file in 'CAP_SYS_ADMIN:21' 'CAP_SYS_CHROOT:18' 'CAP_SYS_PTRACE:19'; do
    if k8s_cap_has "$k8s_cap_eff" "${k8s_file#*:}"; then
      printf '  %s: present\n' "${k8s_file%%:*}"
    else
      printf '  %s: absent or unknown\n' "${k8s_file%%:*}"
    fi
  done
  for k8s_file in pid user mnt net ipc uts cgroup; do
    k8s_ns_self="$(readlink "/proc/self/ns/$k8s_file" 2>/dev/null)"
    k8s_ns_one="$(readlink "/proc/1/ns/$k8s_file" 2>/dev/null)"
    [ "$k8s_ns_self" ] && [ "$k8s_ns_one" ] || continue
    if [ "$k8s_ns_self" = "$k8s_ns_one" ]; then
      printf '  %s namespace matches visible PID 1\n' "$k8s_file"
    else
      printf '  %s namespace differs from visible PID 1\n' "$k8s_file"
    fi
  done
  # Matching visible PID 1 is not proof that it is the physical node's init.
  if command -v stat >/dev/null 2>&1; then
    k8s_ns_self="$(stat -Lc '%d:%i' /proc/self/root 2>/dev/null)"
    k8s_ns_one="$(stat -Lc '%d:%i' /proc/1/root 2>/dev/null)"
    if [ "$k8s_ns_self" ] && [ "$k8s_ns_one" ] && [ "$k8s_ns_self" != "$k8s_ns_one" ]; then
      echo '  Visible PID 1 has a distinct filesystem root'
      [ -x /proc/1/root/bin/sh ] && echo '  /bin/sh is executable in visible PID 1 root'
    fi
  fi
}

k8s_runc_21626_upstream_range() {
  case "$1" in
    1.0.0-rc*)
      k8s_rc="${1#1.0.0-rc}"
      case "$k8s_rc" in ''|*[!0-9]*) return 1 ;; esac
      [ "$k8s_rc" -ge 93 ]
      return
      ;;
    *-*) return 1 ;;
  esac
  printf '%s\n' "$1" |
    awk -F. '$1 == 1 && ($2 == 0 || ($2 == 1 && $3 ~ /^[0-9]+$/ && $3 < 12)) {found=1}
      END {exit !found}'
}

k8s_scan_runc_21626() {
  command -v runc >/dev/null 2>&1 || return
  k8s_runc_version="$(runc --version 2>/dev/null | sed -n 's/^runc version \([0-9][0-9A-Za-z.+-]*\).*/\1/p' | head -n 1)"
  [ "$k8s_runc_version" ] || return
  printf '  Visible runc version: %s\n' "$k8s_runc_version"
  if k8s_runc_21626_upstream_range "$k8s_runc_version"; then
    echo '  CVE-2024-21626: upstream version is in the affected range; vendor backports need confirmation'
  fi
}

k8s_scan_sockets() {
  k8s_docker_host="$(env | sed -n 's/^DOCKER_HOST=//p' | head -n 1)"
  case "$k8s_docker_host" in
    unix://*)
      k8s_socket="${k8s_docker_host#unix://}"
      [ -S "$k8s_socket" ] && k8s_show_file "$k8s_socket"
      ;;
  esac
  for k8s_socket in /run/docker.sock /var/run/docker.sock \
    /run/containerd/containerd.sock /var/run/containerd/containerd.sock \
    /run/k3s/containerd/containerd.sock /run/crio/crio.sock \
    /var/run/crio/crio.sock /run/cri-dockerd.sock \
    /var/run/dockershim.sock /run/podman/podman.sock; do
    [ -S "$k8s_socket" ] && k8s_show_file "$k8s_socket"
  done
  for k8s_dir in /run /var/run; do
    [ -d "$k8s_dir" ] || continue
    find "$k8s_dir" -maxdepth 4 -type s \( -name '*kubelet*.sock' -o \
      -name '*containerd*.sock' -o -name '*crio*.sock' -o \
      -name '*docker*.sock' -o -name '*podman*.sock' \) \
      -print 2>/dev/null | head -n 40
  done
}

k8s_scan_kubelet_processes() {
  for k8s_pid in $(ps -eo pid=,comm= 2>/dev/null | awk '$2 == "kubelet" {print $1}' | head -n 5); do
    [ -r "/proc/$k8s_pid/cmdline" ] || continue
    printf '  kubelet PID %s:\n' "$k8s_pid"
    tr '\000' '\n' < "/proc/$k8s_pid/cmdline" 2>/dev/null |
      awk '
        pending { print "    " pending "=" $0; pending=""; next }
        /^--(kubeconfig|bootstrap-kubeconfig|config|cert-dir|root-dir|read-only-port|port|address|anonymous-auth|authorization-mode|client-ca-file|node-ip)$/ {
          pending=$0; next
        }
        /^--(kubeconfig|bootstrap-kubeconfig|config|cert-dir|root-dir|read-only-port|port|address|anonymous-auth|authorization-mode|client-ca-file|node-ip)=/ {
          print "    " $0
        }
      '
  done
}

k8s_direct_api_ready() {
  command -v curl >/dev/null 2>&1 && command -v jq >/dev/null 2>&1 || return 1
  k8s_direct_token_file=''
  k8s_direct_ca=''
  [ "$#" -gt 0 ] || set -- /var/run/secrets/kubernetes.io/serviceaccount \
    /run/secrets/kubernetes.io/serviceaccount /secrets/kubernetes.io/serviceaccount
  for k8s_dir do
    if [ -r "$k8s_dir/token" ] && [ -r "$k8s_dir/ca.crt" ]; then
      k8s_direct_token_file="$k8s_dir/token"
      k8s_direct_ca="$k8s_dir/ca.crt"
      break
    fi
  done
  [ "$k8s_direct_token_file" ] || return 1
  if [ -r "$k8s_dir/namespace" ] && [ ! "$k8s_namespace" ]; then
    k8s_namespace="$(head -n 1 "$k8s_dir/namespace" 2>/dev/null)"
  fi
  [ "$k8s_namespace" ] || k8s_namespace=default
  case "$k8s_namespace" in ''|*[!a-z0-9-]*) return 1 ;; esac
  k8s_direct_host="${KUBERNETES_SERVICE_HOST:-kubernetes.default.svc}"
  case "$k8s_direct_host" in ''|*[!A-Za-z0-9.:-]*) return 1 ;; esac
  case "$k8s_direct_host" in *:*) k8s_direct_host="[$k8s_direct_host]" ;; esac
  k8s_direct_port="${KUBERNETES_SERVICE_PORT_HTTPS:-443}"
  case "$k8s_direct_port" in ''|*[!0-9]*) return 1 ;; esac
  k8s_direct_base="https://$k8s_direct_host:$k8s_direct_port"
  k8s_direct_token="$(tr -d '\r\n' < "$k8s_direct_token_file" 2>/dev/null)"
  case "$k8s_direct_token" in ''|*[!A-Za-z0-9._~-]*) k8s_direct_token=''; return 1 ;; esac
}

k8s_sa_api_get() {
  # Ignore curlrc: inherited tracing or credentials must not affect discovery.
  printf 'header = "Authorization: Bearer %s"\n' "$k8s_direct_token" |
    curl -q --config - -fsS --connect-timeout 2 --max-time 5 \
      --cacert "$k8s_direct_ca" -H 'Accept: application/json' \
      "$k8s_direct_base$1" 2>/dev/null
}

k8s_sa_kube_system_secret_access() {
  # SelfSubjectAccessReview evaluates the mounted token without reading Secrets.
  k8s_access_response="$(printf 'header = "Authorization: Bearer %s"\n' "$k8s_direct_token" |
    curl -q --config - -fsS --connect-timeout 2 --max-time 5 --max-filesize 4096 \
      --cacert "$k8s_direct_ca" -H 'Accept: application/json' \
      -H 'Content-Type: application/json' \
      --data-binary '{"apiVersion":"authorization.k8s.io/v1","kind":"SelfSubjectAccessReview","spec":{"resourceAttributes":{"namespace":"kube-system","verb":"list","resource":"secrets"}}}' \
      -w '\n%{http_code}' \
      "$k8s_direct_base/apis/authorization.k8s.io/v1/selfsubjectaccessreviews" 2>/dev/null)"
  k8s_access_exit=$?
  if [ "${#k8s_access_response}" -gt 4100 ]; then
    echo unknown
    return
  fi
  k8s_access_http="$(printf '%s' "$k8s_access_response" | tail -c 3)"
  case "$k8s_access_exit:$k8s_access_http" in 0:200|0:201) ;; *) echo unknown; return ;; esac
  k8s_access_json="${k8s_access_response%????}"
  k8s_access_result="$(printf '%s' "$k8s_access_json" | jq -r '
    if (.status | type) != "object" or
       (.status.allowed | type) != "boolean" or
       ((.status.evaluationError // "") != "") or
       (.status.allowed == true and .status.denied == true)
    then "unknown"
    elif .status.allowed then "allowed"
    else "denied"
    end
  ' 2>/dev/null | head -n 1)"
  case "$k8s_access_result" in allowed|denied) echo "$k8s_access_result" ;; *) echo unknown ;; esac
}

k8s_scan_sa_secrets() {
  # The API response contains values; keep it in memory and emit only selected metadata.
  # curl's size limit bounds the response even when the server ignores ?limit=100.
  k8s_secret_response="$(printf 'header = "Authorization: Bearer %s"\n' "$k8s_direct_token" |
    curl -q --config - -fsS --connect-timeout 2 --max-time 5 --max-filesize 1048576 \
      --cacert "$k8s_direct_ca" -H 'Accept: application/json' -w '\n%{http_code}' \
      "$k8s_direct_base/api/v1/namespaces/$k8s_namespace/secrets?limit=40" 2>/dev/null)"
  k8s_secret_exit=$?
  if [ "${#k8s_secret_response}" -gt 1048580 ]; then
    echo '    secrets: response exceeded 1 MiB; inventory skipped'
    return
  fi
  k8s_secret_http="$(printf '%s' "$k8s_secret_response" | tail -c 3)"
  case "$k8s_secret_exit:$k8s_secret_http" in
    0:200) ;;
    22:403) echo '    secrets: access denied (RBAC)' ; return ;;
    22:401) echo '    secrets: authentication denied' ; return ;;
    22:404) echo '    secrets: namespace unavailable' ; return ;;
    28:*) echo '    secrets: request timed out' ; return ;;
    63:*) echo '    secrets: response exceeded 1 MiB; inventory skipped' ; return ;;
    *) echo '    secrets: API request failed or returned an unexpected status' ; return ;;
  esac
  k8s_secret_json="${k8s_secret_response%????}"
  k8s_secret_summary="$(printf '%s' "$k8s_secret_json" | jq -r '
    if type != "object" or (.items | type) != "array" or
       any(.items[]; type != "object" or (.metadata.name | type) != "string" or
           ((.type // "") | type) != "string" or
           ((.data // {}) | type) != "object" or
           ((.stringData // {}) | type) != "object")
    then error("invalid Secret list")
    else
      (.items | length) as $count |
      ((.metadata["continue"] // "") != "") as $continued |
      if $count == 0 then "    secrets: none (accessible list)"
      else
        .items[:40][] |
        ((.data // {} | keys) + (.stringData // {} | keys) | unique) as $keys |
        "    secrets: name=\(.metadata.name | @json) type=\((.type // "unknown") | @json) keys=\($keys[:20] | @json)\(if ($keys | length) > 20 then " (additional keys omitted)" else "" end)"
      end,
      if $count > 40 or $continued then "    secrets: additional results omitted (40 shown; no pagination)" else empty end
    end
  ' 2>/dev/null)" || {
    echo '    secrets: malformed API response; inventory unknown'
    return
  }
  printf '%s\n' "$k8s_secret_summary"
}

k8s_scan_sa_api() {
  if ! command -v curl >/dev/null 2>&1 || ! command -v jq >/dev/null 2>&1; then
    echo '  Kubernetes API discovery skipped (curl or jq unavailable)'
    return
  fi
  k8s_direct_api_ready "$@" || return
  echo '  Kubernetes API discovery with mounted service account (names and settings only):'
  k8s_sa_api_get '/api/v1/namespaces?limit=100' |
    jq -r '.items[]?.metadata.name // empty' 2>/dev/null | head -n 40 | sed 's/^/    namespace: /'
  k8s_sa_api_get "/api/v1/namespaces/$k8s_namespace/pods?limit=100" |
    jq -r '.items[]? | (
      "\(.metadata.name) sa=\(.spec.serviceAccountName // "default") hostPID=\(.spec.hostPID // false) hostIPC=\(.spec.hostIPC // false) hostNetwork=\(.spec.hostNetwork // false)",
      (.spec.containers[]? | "  container \(.name) privileged=\(.securityContext.privileged // false) allowPE=\((.securityContext.allowPrivilegeEscalation | if . == null then "default" else . end)) caps=\((.securityContext.capabilities.add // []) | join(","))"),
      (.spec.volumes[]? | select(.hostPath.path != null) | "  hostPath \(.hostPath.path)")
    )' 2>/dev/null | head -n 100 | sed 's/^/    pod: /'
  for k8s_file in services serviceaccounts; do
    k8s_sa_api_get "/api/v1/namespaces/$k8s_namespace/$k8s_file?limit=100" |
      jq -r '.items[]? | .metadata.name // empty' 2>/dev/null | head -n 40 |
      sed "s/^/    $k8s_file: /"
  done
  k8s_scan_sa_secrets
  printf '  kube-system secrets list authorization (mounted identity): %s\n' \
    "$(k8s_sa_kube_system_secret_access)"
  k8s_sa_api_get '/api/v1/nodes?limit=100' |
    jq -r '.items[]?.metadata.name // empty' 2>/dev/null | head -n 40 | sed 's/^/    node: /'
  k8s_direct_token=''
}

k8s_kubectl() {
  # Request timeouts do not bound credential plugins or all paginated requests.
  # Skip kubectl when a process-wide deadline cannot be enforced.
  command -v timeout >/dev/null 2>&1 || return 1
  timeout -s KILL 5 kubectl "$@"
}

k8s_scan_kubectl() {
  if ! command -v kubectl >/dev/null 2>&1; then
    [ "$EXTRA_CHECKS" ] && k8s_scan_sa_api
    return 0
  fi
  echo '  Local kubeconfig contexts:'
  k8s_kubectl config get-contexts -o name 2>/dev/null | head -n 30 | sed 's/^/    /'
  [ "$EXTRA_CHECKS" ] || return 0

  k8s_current_context="$(k8s_kubectl config current-context 2>/dev/null | head -n 1)"
  if [ ! "$k8s_current_context" ] && k8s_direct_api_ready; then
    k8s_direct_token=''
    k8s_scan_sa_api
    return 0
  fi
  k8s_context_name="$(k8s_kubectl config view --minify -o 'jsonpath={..namespace}' 2>/dev/null | head -c 200)"
  [ "$k8s_context_name" ] && k8s_namespace="$k8s_context_name"
  [ "$k8s_namespace" ] || k8s_namespace=default

  echo '  Current context authorization rules:'
  k8s_kubectl --request-timeout=5s auth can-i --list -n "$k8s_namespace" \
    2>/dev/null | head -n 80
  k8s_access_result="$(k8s_kubectl --request-timeout=5s auth can-i list secrets \
    -n kube-system 2>/dev/null | head -n 1)"
  case "$k8s_access_result" in yes) k8s_access_result=allowed ;; no) k8s_access_result=denied ;; *) k8s_access_result=unknown ;; esac
  printf '  kube-system secrets list authorization (current context): %s\n' "$k8s_access_result"
  echo '  Other configured context authorization rules (up to five):'
  k8s_kubectl config get-contexts -o name 2>/dev/null | head -n 5 |
  while IFS= read -r k8s_context_name; do
    [ "$k8s_context_name" = "$k8s_current_context" ] && continue
    printf '    Context: %s\n' "$k8s_context_name"
    k8s_kubectl --context="$k8s_context_name" --request-timeout=5s auth can-i --list \
      2>/dev/null | head -n 40 | sed 's/^/      /'
  done
  echo '  Accessible namespace names:'
  k8s_kubectl --request-timeout=5s get namespaces -o name 2>/dev/null | head -n 40
  printf '  Accessible object names in namespace %s:\n' "$k8s_namespace"
  k8s_kubectl --request-timeout=5s get pods,services,serviceaccounts,secrets \
    -n "$k8s_namespace" -o name 2>/dev/null | head -n 80
  echo '  Accessible node names:'
  k8s_kubectl --request-timeout=5s get nodes -o name 2>/dev/null | head -n 40
  echo '  Node-specific get nodes/proxy authorization (up to eight nodes):'
  k8s_kubectl --request-timeout=5s get nodes -o name 2>/dev/null | head -n 8 |
  while IFS= read -r k8s_node_name; do
    case "$k8s_node_name" in node/*|nodes/*) k8s_node_name="${k8s_node_name#*/}" ;; *) continue ;; esac
    k8s_probe_status="$(k8s_kubectl --request-timeout=5s auth can-i get \
      "nodes/$k8s_node_name" --subresource=proxy 2>/dev/null)"
    case "$k8s_probe_status" in
      yes|no) printf '    %s: %s\n' "$k8s_node_name" "$k8s_probe_status" ;;
    esac
  done
  echo '  Pod service accounts, host namespaces and container security settings:'
  k8s_kubectl --request-timeout=5s get pods -n "$k8s_namespace" \
    -o 'jsonpath={range .items[*]}{.metadata.name}{" sa="}{.spec.serviceAccountName}{" hostPID="}{.spec.hostPID}{" hostIPC="}{.spec.hostIPC}{" hostNetwork="}{.spec.hostNetwork}{" containers="}{range .spec.containers[*]}{.name}{"(privileged="}{.securityContext.privileged}{",allowPE="}{.securityContext.allowPrivilegeEscalation}{",caps="}{.securityContext.capabilities.add}{") "}{end}{"\n"}{end}' \
    2>/dev/null | head -n 60
  echo '  Pod hostPath sources:'
  k8s_kubectl --request-timeout=5s get pods -n "$k8s_namespace" \
    -o 'jsonpath={range .items[*]}{.metadata.name}{": "}{range .spec.volumes[*]}{.hostPath.path}{" "}{end}{"\n"}{end}' \
    2>/dev/null | grep -E ': /' | head -n 60
}

k8s_probe_kubelet() {
  command -v curl >/dev/null 2>&1 || return
  case "$1" in ''|*[!0-9a-fA-F:.]*) return ;; esac
  k8s_probe_host="$1"
  case "$k8s_probe_host" in *:*) k8s_probe_host="[$k8s_probe_host]" ;; esac
  for k8s_probe_port in 10250 10255; do
    k8s_probe_scheme=https
    [ "$k8s_probe_port" = 10255 ] && k8s_probe_scheme=http
    # No credentials are sent. An HTTP status only establishes reachability;
    # it does not prove kubelet authentication or authorization.
    k8s_probe_status="$(curl -q -ksS --noproxy '*' --connect-timeout 1 --max-time 2 \
      -o /dev/null -w '%{http_code}' \
      "$k8s_probe_scheme://$k8s_probe_host:$k8s_probe_port/healthz" 2>/dev/null)"
    case "$k8s_probe_status" in
      [1-5][0-9][0-9])
        printf '  Kubelet %s://%s:%s responded HTTP %s\n' \
          "$k8s_probe_scheme" "$k8s_probe_host" "$k8s_probe_port" "$k8s_probe_status"
        ;;
    esac
  done
}

k8s_scan_kubelet_network() {
  echo '  Reachability of node kubelet ports (up to eight addresses; status only):'
  k8s_node_addresses=''
  if command -v kubectl >/dev/null 2>&1; then
    k8s_node_addresses="$(k8s_kubectl --request-timeout=5s get nodes \
      -o 'jsonpath={range .items[*]}{range .status.addresses[*]}{.address}{"\n"}{end}{end}' \
      2>/dev/null)"
  fi
  if [ ! "$k8s_node_addresses" ] && k8s_direct_api_ready; then
    k8s_node_addresses="$(k8s_sa_api_get '/api/v1/nodes?limit=100' |
      jq -r '.items[]?.status.addresses[]? | select(.type == "InternalIP" or .type == "ExternalIP") | .address' \
        2>/dev/null)"
    k8s_direct_token=''
  fi
  printf '%s\n' "$k8s_node_addresses" | awk 'NF && !seen[$0]++' | head -n 8 |
  while IFS= read -r k8s_node_address; do
    k8s_probe_kubelet "$k8s_node_address"
  done
}

k8s_scan_service_dns() {
  command -v dig >/dev/null 2>&1 || return
  echo '  Kubernetes service SRV records:'
  dig +time=2 +tries=1 +short SRV any.any.svc.cluster.local 2>/dev/null | head -n 40
}

k8s_scan_kops_storage() {
  command -v timeout >/dev/null 2>&1 || return
  if command -v aws >/dev/null 2>&1; then
    echo '  Accessible S3 buckets (up to 10) and candidate kOps Secret keys (first 100 objects each):'
    AWS_MAX_ATTEMPTS=1 timeout -s KILL 12 aws s3api list-buckets \
      --max-items 10 --query 'Buckets[].Name' --output text \
      --cli-connect-timeout 2 --cli-read-timeout 5 \
      2>/dev/null | tr '\t' '\n' | head -n 10 |
    while IFS= read -r k8s_aws_bucket; do
      case "$k8s_aws_bucket" in ''|*[!a-z0-9.-]*) continue ;; esac
      printf '    s3://%s\n' "$k8s_aws_bucket"
      AWS_MAX_ATTEMPTS=1 timeout -s KILL 12 aws s3api list-objects-v2 \
        --bucket "$k8s_aws_bucket" --max-items 100 \
        --query 'Contents[].Key' --output text \
        --cli-connect-timeout 2 --cli-read-timeout 5 \
        2>/dev/null | tr '\t' '\n' | grep -E '(^|/)secrets/' | head -n 20 |
        sed 's/^/      candidate key: /'
    done
  fi
  if command -v gcloud >/dev/null 2>&1; then
    echo '  Accessible GCS buckets (up to 10) and candidate kOps Secret objects (first 100 each):'
    timeout -s KILL 12 gcloud --quiet storage buckets list --limit=10 \
      --format='value(name)' 2>/dev/null | head -n 10 |
    while IFS= read -r k8s_gcs_bucket; do
      k8s_gcs_bucket="${k8s_gcs_bucket#gs://}"
      case "$k8s_gcs_bucket" in ''|*[!a-z0-9._-]*) continue ;; esac
      printf '    gs://%s\n' "$k8s_gcs_bucket"
      timeout -s KILL 12 gcloud --quiet storage objects list \
        "gs://$k8s_gcs_bucket/**" --limit=100 --format='value(name)' \
        2>/dev/null | grep -E '(^|/)secrets/' | head -n 20 |
        sed 's/^/      candidate object: /'
    done
  fi
}

if k8s_context_present; then
  print_2title 'Kubernetes credentials and escape surfaces' 'T1613,T1611,T1552.007'

  print_3title 'Pod identity and projected credentials' 'T1552.007'
  k8s_namespace=''
  for k8s_dir in /var/run/secrets/kubernetes.io/serviceaccount \
    /run/secrets/kubernetes.io/serviceaccount /secrets/kubernetes.io/serviceaccount; do
    [ -d "$k8s_dir" ] || continue
    k8s_show_file "$k8s_dir/token"
    k8s_show_file "$k8s_dir/ca.crt"
    k8s_show_file "$k8s_dir/namespace"
    if [ -r "$k8s_dir/namespace" ]; then
      k8s_namespace="$(head -n 1 "$k8s_dir/namespace" 2>/dev/null)"
    fi
  done
  for k8s_file in $(env | sed -n -E 's/^(AWS_WEB_IDENTITY_TOKEN_FILE|AZURE_FEDERATED_TOKEN_FILE|GOOGLE_APPLICATION_CREDENTIALS)=//p'); do
    k8s_show_file "$k8s_file"
  done
  [ "$k8s_namespace" ] && printf '  Namespace: %s\n' "$k8s_namespace"
  [ -n "$KUBERNETES_SERVICE_HOST" ] &&
    printf '  API server: %s:%s\n' "$KUBERNETES_SERVICE_HOST" "${KUBERNETES_SERVICE_PORT_HTTPS:-443}"

  print_3title 'Kubeconfigs, client keys and kubelet settings' 'T1552.007'
  k8s_scan_kubeconfigs
  k8s_scan_kubelet_config /var/lib/kubelet/config.yaml
  k8s_scan_kubelet_processes

  print_3title 'Node pod Secret and projected-token volumes' 'T1552.007'
  k8s_scan_pod_volumes /var/lib/kubelet/pods
  k8s_scan_host_roots /proc/self/mountinfo
  k8s_scan_kubelet_mounts /proc/self/mountinfo

  print_3title 'Host filesystem and log mounts' 'T1611'
  k8s_scan_mountinfo /proc/self/mountinfo
  k8s_scan_kernel_mounts /proc/self/mountinfo
  k8s_scan_escape_prereqs
  k8s_scan_runc_21626
  [ -d /proc/1/root ] && k8s_show_file /proc/1/root
  for k8s_file in /proc/sys/kernel/core_pattern /sys/fs/cgroup/*/release_agent \
    /sys/fs/cgroup/*/notify_on_release; do
    [ -e "$k8s_file" ] && k8s_show_file "$k8s_file"
  done

  print_3title 'Container runtime and orchestration sockets' 'T1611'
  k8s_scan_sockets

  print_3title 'Kubernetes contexts and API discovery' 'T1613'
  k8s_scan_kubectl
  if [ "$EXTRA_CHECKS" ]; then
    print_3title 'Kubelet reachability and service DNS' 'T1613'
    k8s_scan_kubelet_network
    k8s_scan_service_dns
    print_3title 'Cloud storage kOps state candidates' 'T1552.007'
    k8s_scan_kops_storage
  fi

  echo
fi
