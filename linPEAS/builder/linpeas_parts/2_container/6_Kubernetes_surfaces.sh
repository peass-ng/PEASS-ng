# Title: Container - Kubernetes surfaces
# ID: CT_Kubernetes_surfaces
# Author: Carlos Polop
# Last Update: 05-10-2026
# Description: Enumerate local Kubernetes credentials, node mounts, kubelet volumes, and runtime sockets relevant to pod and node privilege escalation.
# License: GNU GPL
# Version: 1.0
# Mitre: T1613,T1611,T1552.007
# Functions Used: print_2title, print_3title, print_info
# Global Variables: $containerType
# Initial Functions: containerCheck
# Generated Global Variables: $k8s_cfg, $k8s_cfg_env, $k8s_count, $k8s_dir, $k8s_docker_host, $k8s_file, $k8s_mount, $k8s_mount_options, $k8s_mount_root, $k8s_namespace, $k8s_pid, $k8s_readable, $k8s_socket, $k8s_writable
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

k8s_context_present() {
  [ -n "$KUBERNETES_SERVICE_HOST" ] ||
  [ -f "$HOME/.kube/config" ] ||
  [ -f /root/.kube/config ] ||
  [ -d /var/run/secrets/kubernetes.io/serviceaccount ] ||
  [ -d /run/secrets/kubernetes.io/serviceaccount ] ||
  [ -d /var/lib/kubelet/pods ] ||
  [ -d /etc/kubernetes ] ||
  [ -d /etc/rancher/k3s ] ||
  [ -d /var/snap/microk8s/current/credentials ] ||
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
    /var/snap/microk8s/current/credentials/*.config; do
    k8s_show_file "$k8s_cfg"
  done
  for k8s_dir in /etc/kubernetes/pki /var/lib/kubelet/pki \
    /var/lib/rancher/k3s/server/tls /var/snap/microk8s/current/certs; do
    [ -d "$k8s_dir" ] || continue
    printf '  Certificate/key directory: %s\n' "$k8s_dir"
    find "$k8s_dir" -maxdepth 2 -type f \( -name '*.key' -o -name '*.crt' -o -name '*.pem' \) \
      -print 2>/dev/null | head -n 30
  done
  if [ -d /etc/kubernetes/manifests ]; then
    echo '  Static pod manifests:'
    find /etc/kubernetes/manifests -maxdepth 1 -type f -print 2>/dev/null | head -n 20
  fi
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
        /^--(kubeconfig|bootstrap-kubeconfig|config|cert-dir|root-dir)$/ {
          pending=$0; next
        }
        /^--(kubeconfig|bootstrap-kubeconfig|config|cert-dir|root-dir)=/ {
          print "    " $0
        }
      '
  done
}

if k8s_context_present; then
  print_2title 'Kubernetes credentials and escape surfaces' 'T1613,T1611,T1552.007'
  print_info 'https://github.com/inguardians/peirates/blob/main/docs/commands/README.md'

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
  k8s_scan_kubelet_processes

  print_3title 'Node pod Secret and projected-token volumes' 'T1552.007'
  k8s_scan_pod_volumes /var/lib/kubelet/pods

  print_3title 'Host filesystem and log mounts' 'T1611'
  k8s_scan_mountinfo /proc/self/mountinfo
  k8s_scan_kernel_mounts /proc/self/mountinfo
  [ -d /proc/1/root ] && k8s_show_file /proc/1/root
  [ -r /proc/self/status ] &&
    grep -E '^(Uid|CapEff):' /proc/self/status 2>/dev/null
  for k8s_file in /proc/sys/kernel/core_pattern /sys/fs/cgroup/*/release_agent \
    /sys/fs/cgroup/*/notify_on_release; do
    [ -e "$k8s_file" ] && k8s_show_file "$k8s_file"
  done

  print_3title 'Container runtime and orchestration sockets' 'T1611'
  k8s_scan_sockets

  echo
fi
