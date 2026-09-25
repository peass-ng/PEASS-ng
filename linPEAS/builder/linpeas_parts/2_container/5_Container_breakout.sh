# Title: Container - Container & breakout enumeration
# ID: CT_Container_breakout
# Author: Carlos Polop, HT Bot
# Last Update: 25-09-2026
# Description: Enumerate container hardening, breakout surfaces, runtime exposure, Kubernetes RBAC, and high-impact escape vectors from inside a container.
# License: GNU GPL
# Version: 1.0
# Mitre: T1611
# Functions Used: checkContainerExploits, checkProcSysBreakouts, containerCheck, enumerateDockerSockets, print_2title, print_3title, print_info, print_list, warn_exec
# Global Variables: $binfmt_misc_breakout, $containercapsB, $containerType, $core_pattern_breakout, $debugfs_present, $debugfs_readable, $dev_mounted, $efi_efivars_writable, $efi_vars_writable, $EXTRA_CHECKS, $GREP_IGNORE_MOUNTS, $inContainer, $kallsyms_readable, $kcore_readable, $kmem_readable, $kmem_writable, $kmsg_readable, $mem_readable, $mem_writable, $modprobe_binary, $modprobe_config_writable, $mountinfo_readable, $panic_on_oom_dos, $panic_sys_fs_dos, $proc_configgz_readable, $proc_keys_readable, $proc_mounted, $proc_timer_list_readable, $release_agent_breakout1, $release_agent_breakout2, $release_agent_breakout3, $run_unshare, $sched_debug_readable, $security_present, $security_writable, $self_mem_readable, $sys_firmware_readable, $sysreq_trigger_dos, $thermal_present, $thermal_readable, $uevent_helper_breakout, $vmcoreinfo_readable, $VULN_CVE_2019_5021
# Initial Functions: containerCheck
# Generated Global Variables: $container_breakout_tools, $containerd_version, $defautl_docker_caps, $gid_map_value, $host_process_count, $host_process_indicators, $k8s_api_base, $k8s_api_host, $k8s_can_create_pods, $k8s_can_exec_pods, $k8s_can_read_secrets, $k8s_can_request_tokens, $k8s_chain_risk, $k8s_host_mounts, $k8s_namespace, $k8s_pod_name, $k8s_pod_json, $k8s_pod_risks, $k8s_sa_ca, $k8s_sa_dir, $k8s_sa_token, $k8s_service_account, $k8s_ssrr_request, $k8s_ssrr_response, $k8s_ssrr_rules, $k8s_token_payload, $k8s_token_payload_b64, $no_new_privs_num, $proc_comm, $root_mount_mode, $runc_version, $seccomp_mode_desc, $seccomp_mode_num, $selinux_context, $selinux_status, $setgroups_value, $tool, $uid_map_value
# Fat linpeas: 0
# Small linpeas: 0

if [ "$inContainer" ]; then
    echo ""
    print_2title "Container & breakout enumeration" "T1611"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/containers-namespaces/container-security/index.html"
    
    # Basic container info
    print_list "Container ID ...................$NC $(cat /etc/hostname && echo -n '\n')"
    if [ -f "/proc/1/cpuset" ] && echo "$containerType" | grep -qi "docker"; then
        print_list "Container Full ID ..............$NC $(basename $(cat /proc/1/cpuset))\n"
    fi
    
    # Hardening and isolation controls
    print_3title "Hardening & isolation" "T1611"
    seccomp_mode_num="$(awk '/^Seccomp:/{print $2}' /proc/self/status 2>/dev/null)"
    seccomp_mode_desc="unknown"
    case "$seccomp_mode_num" in
      0) seccomp_mode_desc="disabled" ;;
      1) seccomp_mode_desc="strict" ;;
      2) seccomp_mode_desc="filtering" ;;
    esac

    print_list "Seccomp mode ................... "$NC
    (printf "%s (%s)\n" "$seccomp_mode_desc" "${seccomp_mode_num:-?}") | sed "s,disabled,${SED_RED}," | sed "s,strict,${SED_RED_YELLOW}," | sed "s,filtering,${SED_GREEN},"

    if grep -q "^Seccomp_filters:" /proc/self/status 2>/dev/null; then
      print_list "Seccomp filters ............... "$NC
      awk '/^Seccomp_filters:/{print $2}' /proc/self/status 2>/dev/null | sed -${E} "s,^[0-9]+$,${SED_GREEN}&,"
    fi

    no_new_privs_num="$(awk '/^NoNewPrivs:/{print $2}' /proc/self/status 2>/dev/null)"
    print_list "NoNewPrivs ..................... "$NC
    case "$no_new_privs_num" in
      1) printf "enabled (1)\n" | sed -${E} "s,enabled,${SED_GREEN}," ;;
      0) printf "disabled (0)\n" | sed -${E} "s,disabled,${SED_RED_YELLOW}," ;;
      *) printf "unknown\n" ;;
    esac

    print_list "AppArmor profile ............... "$NC
    (cat /proc/self/attr/current 2>/dev/null || echo "disabled") | sed "s,disabled,${SED_RED}," | sed "s,kernel,${SED_GREEN},"

    selinux_status="disabled"
    if command -v getenforce >/dev/null 2>&1; then
        selinux_status="$(getenforce 2>/dev/null || echo disabled)"
    elif [ -r /sys/fs/selinux/enforce ]; then
        if [ "$(cat /sys/fs/selinux/enforce 2>/dev/null)" = "1" ]; then
            selinux_status="Enforcing"
        else
            selinux_status="Permissive"
        fi
    fi
    print_list "SELinux status ................. "$NC
    printf "%s\n" "$selinux_status" | sed -${E} "s,Enforcing,${SED_GREEN},g" | sed -${E} "s,Permissive,${SED_RED_YELLOW},g" | sed -${E} "s,disabled,${SED_RED},g"

    selinux_context="$(cat /proc/self/attr/current 2>/dev/null | grep -E ':' || true)"
    if [ "$selinux_context" ]; then
        print_list "SELinux context ................ "$NC
        printf "%s\n" "$selinux_context" | sed -${E} "s,container_t|spc_t,${SED_RED_YELLOW}&,g"
    fi

    uid_map_value="$(cat /proc/self/uid_map 2>/dev/null)"
    gid_map_value="$(cat /proc/self/gid_map 2>/dev/null)"
    setgroups_value="$(cat /proc/self/setgroups 2>/dev/null)"
    print_list "User namespace mappings ....... "$NC
    if echo "$uid_map_value" | grep -Eq "^[[:space:]]*0[[:space:]]+0[[:space:]]+4294967295[[:space:]]*$"; then
        echo "initial user namespace" | sed -${E} "s,initial user namespace,${SED_RED_YELLOW},"
    elif [ "$uid_map_value" ]; then
        echo "remapped user namespace" | sed -${E} "s,remapped user namespace,${SED_GREEN},"
    else
        echo "unknown"
    fi
    if [ "$uid_map_value" ]; then
        echo "  UID map (container -> host -> range):"
        echo "$uid_map_value" | awk '{print "  " $1 " -> " $2 " -> " $3}'
    fi
    if [ "$gid_map_value" ]; then
        echo "  GID map (container -> host -> range):"
        echo "$gid_map_value" | awk '{print "  " $1 " -> " $2 " -> " $3}'
    fi
    if [ "$setgroups_value" ]; then
        echo "  setgroups: $setgroups_value"
    fi

    # Known vulnerabilities
    print_3title "Known Vulnerabilities" "T1611"
    checkContainerExploits
    print_list "Vulnerable to CVE-2019-5021 .... $VULN_CVE_2019_5021\n"$NC | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    
    # Check for container escape tools
    container_breakout_tools="$(
      for tool in nsenter unshare chroot capsh setcap getcap docker kubectl ctr runc containerd crio podman lxc rkt nerdctl; do
        command -v "$tool" 2>/dev/null
      done
    )"
    print_list "Container escape tools present . "$NC
    if [ "$container_breakout_tools" ]; then
        printf "%s\n" "$container_breakout_tools" | sed -${E} "s,.*,${SED_RED}&,"
    else
        echo "No"
    fi
    
    # Runtime vulnerabilities
    print_3title "Runtime Vulnerabilities" "T1611"
    # Check for known runtime vulnerabilities
    if [ "$(command -v runc || echo -n '')" ]; then
        print_list "Runc version ................. "$NC
        warn_exec runc --version
        # Check for specific runc vulnerabilities
        runc_version=$(runc --version 2>/dev/null | grep -i "version" | grep -Eo "[0-9]+\.[0-9]+\.[0-9]+")
        if [ "$runc_version" ]; then
            print_list "Runc CVE-2019-5736 ........... "$NC
            if [ "$(echo $runc_version | awk -F. '{ if ($1 < 1 || ($1 == 1 && $2 < 0) || ($1 == 1 && $2 == 0 && $3 < 7)) print "Yes"; else print "No"; }')" = "Yes" ]; then
                echo "Yes - Vulnerable" | sed -${E} "s,Yes,${SED_RED},"
            else
                echo "No"
            fi
        fi
    fi
    
    if [ "$(command -v containerd || echo -n '')" ]; then
        print_list "Containerd version ........... "$NC
        warn_exec containerd --version
        # Check for specific containerd vulnerabilities
        containerd_version=$(containerd --version 2>/dev/null | grep -Eo "[0-9]+\.[0-9]+\.[0-9]+")
        if [ "$containerd_version" ]; then
            print_list "Containerd CVE-2020-15257 ..... "$NC
            if [ "$(echo $containerd_version | awk -F. '{ if ($1 < 1 || ($1 == 1 && $2 < 4) || ($1 == 1 && $2 == 4 && $3 < 3)) print "Yes"; else print "No"; }')" = "Yes" ]; then
                echo "Yes - Vulnerable" | sed -${E} "s,Yes,${SED_RED},"
            else
                echo "No"
            fi
        fi
    fi
    
    # Mount, procfs and sysfs escape surfaces
    print_3title "Mount, procfs & sysfs surfaces" "T1611"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/containers-namespaces/container-security/sensitive-host-mounts.html"
    
    checkProcSysBreakouts
    root_mount_mode="$(awk '$5=="/"{print $6; exit}' /proc/self/mountinfo 2>/dev/null | cut -d',' -f1)"
    print_list "/proc heavily populated ........ $proc_mounted\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "/dev heavily populated ......... $dev_mounted\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "Root filesystem mode ........... ${root_mount_mode:-unknown}\n" | sed -${E} "s,rw,${SED_RED_YELLOW}," | sed -${E} "s,ro,${SED_GREEN},"
    print_list "Run unshare .................... $run_unshare\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "release_agent surface 1 ........ $release_agent_breakout1\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "release_agent surface 2 ........ $release_agent_breakout2\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "release_agent surface 3 ........ $release_agent_breakout3\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "Writable core_pattern .......... $core_pattern_breakout\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "Writable binfmt_misc/register .. $binfmt_misc_breakout\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "Writable uevent_helper ......... $uevent_helper_breakout\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    
    # Additional mount checks
    print_list "Mounted runtime sockets ........ "$NC
    (mount | grep -E "docker.sock|containerd.sock|crio.sock|podman.sock|buildkitd.sock|kubelet.sock|firecracker-containerd.sock" || echo "No") | sed -${E} "s,docker.sock|containerd.sock|crio.sock|podman.sock|buildkitd.sock|kubelet.sock|firecracker-containerd.sock,${SED_RED},g"
    
    print_list "Common host filesystem mounted?  "$NC
    (mount | grep -E "host|/host|/mnt/host|/rootfs" || echo "No") | sed -${E} "s,host|/host|/mnt/host|/rootfs,${SED_RED},g"
    
    print_list "Interesting mounts ............. "$NC
    mount | grep -E "docker|container|overlay|kubelet|buildkit|crio|podman|/host|/rootfs" | grep -v "proc" | sed -${E} "s,docker.sock|containerd.sock|crio.sock|podman.sock|kubelet.sock|buildkitd.sock|host|rootfs|privileged,${SED_RED},g"
    
    # Check for writable mount points
    print_list "Writable mount points ......... "$NC
    mount | grep -E "rw," | grep -v "ro," | sed -${E} "s,docker.sock|host|privileged,${SED_RED},g"
    
    # Check for shared mount points
    print_list "Shared mount points ........... "$NC
    mount | grep -E "shared|slave" | sed -${E} "s,docker.sock|host|privileged,${SED_RED},g"
    
    # Capability checks
    print_3title "Capability Checks" "T1611"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/containers-namespaces/container-security/protections/capabilities.html"
    
    print_list "Dangerous capabilities ......... "$NC
    if [ "$(command -v capsh || echo -n '')" ]; then 
        capsh --print 2>/dev/null | sed -${E} "s,$containercapsB,${SED_RED},g"
    else
        defautl_docker_caps="00000000a80425fb=cap_chown,cap_dac_override,cap_fowner,cap_fsetid,cap_kill,cap_setgid,cap_setuid,cap_setpcap,cap_net_bind_service,cap_net_raw,cap_sys_chroot,cap_mknod,cap_audit_write,cap_setfcap"
        cat /proc/self/status | tr '\t' ' ' | grep Cap | sed -${E} "s, .*,${SED_RED},g" | sed -${E} "s/00000000a80425fb/$defautl_docker_caps/g" | sed -${E} "s,0000000000000000|00000000a80425fb,${SED_GREEN},g"
        echo $ITALIC"Run capsh --decode=<hex> to decode the capabilities"$NC
    fi

    print_list "Ambient capabilities ........... "$NC
    (grep "CapAmb:" /proc/self/status 2>/dev/null | grep -v "0000000000000000" | sed "s,CapAmb:.,," || echo "No") | sed -${E} "s,No,${SED_GREEN}," | sed -${E} "s,[0-9a-fA-F]\+,${SED_RED}&,"
    
    # Additional capability checks
    print_list "ptrace_scope (host) ........... "$NC
    if [ -f "/proc/sys/kernel/yama/ptrace_scope" ]; then
        (cat /proc/sys/kernel/yama/ptrace_scope 2>/dev/null || echo "Not found") | sed -${E} "s,0,${SED_RED},"
    else
        echo "Not found"
    fi
    
    # Namespace checks. From inside a container we often cannot prove host namespace sharing directly,
    # so prefer raw namespace handles and practical indicators over misleading "host namespace = yes/no" guesses.
    print_3title "Namespaces & sharing indicators" "T1611"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/containers-namespaces/container-security/protections/namespaces/index.html"
    
    print_list "Current namespaces ............. "$NC
    ls -l /proc/self/ns/
    
    if ps -e -o pid= >/dev/null 2>&1; then
        host_process_count="$(ps -e -o pid= 2>/dev/null | wc -l | tr -d ' ')"
        host_process_indicators="$(ps -eo comm= 2>/dev/null | grep -E '^(systemd|init|kthreadd|dockerd|containerd|kubelet|sshd|udevd|NetworkManager|dbus-daemon)$' | sort -u)"
    else
        host_process_count="$(ls -d /proc/[0-9]* 2>/dev/null | wc -l | tr -d ' ')"
        host_process_indicators="$(for proc_comm in /proc/[0-9]*/comm; do cat "$proc_comm" 2>/dev/null; done | grep -E '^(systemd|init|kthreadd|dockerd|containerd|kubelet|sshd|udevd|NetworkManager|dbus-daemon)$' | sort -u)"
    fi
    print_list "Processes visible .............. $host_process_count\n" | sed -${E} "s,^[^0-9]*([5-9][0-9]|[1-9][0-9]{2,}).*,${SED_RED_YELLOW}&,"
    print_list "Host-like processes visible .... "$NC
    if [ "$host_process_indicators" ]; then
        printf "%s\n" "$host_process_indicators" | sed -${E} "s,.*,${SED_RED_YELLOW}&,"
    else
        echo "No obvious host daemons"
    fi

    print_list "Network interfaces ............. "$NC
    if command -v ip >/dev/null 2>&1; then
        ip -o link show 2>/dev/null | awk -F': ' '{print $2}'
    else
        ls /sys/class/net 2>/dev/null
    fi

    print_list "Namespace inode summary ........ "$NC
    for ns in cgroup ipc mnt net pid time user uts; do
        if [ -L "/proc/self/ns/$ns" ]; then
            printf "%s -> %s\n" "$ns" "$(readlink "/proc/self/ns/$ns" 2>/dev/null)"
        fi
    done

    print_list "Looking and enumerating runtime sockets:\n"$NC
    enumerateDockerSockets
    
    # Additional breakout vectors
    print_3title "Writable kernel helper paths" "T1611"
    print_list "modprobe helper binary ......... $modprobe_binary\n" | sed -${E} "s,/.*,${SED_RED},"
    print_list "modprobe path writable ......... $modprobe_config_writable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "panic_on_oom writable .......... $panic_on_oom_dos\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "suid_dumpable writable ......... $panic_sys_fs_dos\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "DoS via sysreq_trigger_dos ..... $sysreq_trigger_dos\n" | sed -${E} "s,Yes,${SED_RED},"

    print_3title "Sensitive procfs/sysfs exposure" "T1611"
    print_list "/proc/config.gz readable ....... $proc_configgz_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/sched_debug readable ..... $sched_debug_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/*/mountinfo readable ..... $mountinfo_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/keys readable ............ $proc_keys_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/timer_list readable ...... $proc_timer_list_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/kmsg readable ............ $kmsg_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/kallsyms readable ........ $kallsyms_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/self/mem readable ........ $self_mem_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/kcore readable ........... $kcore_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/kmem readable ............ $kmem_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/kmem writable ............ $kmem_writable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/mem readable ............. $mem_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/mem writable ............. $mem_writable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/firmware readable ......... $sys_firmware_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/kernel/debug present ...... $debugfs_present\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/kernel/debug readable ..... $debugfs_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/class/thermal present ..... $thermal_present\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "/sys/class/thermal readable .... $thermal_readable\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "/sys/kernel/security present ... $security_present\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/kernel/security writable .. $security_writable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/kernel/vmcoreinfo readable  $vmcoreinfo_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/firmware/efi/vars writable  $efi_vars_writable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/firmware/efi/efivars writable $efi_efivars_writable\n" | sed -${E} "s,Yes,${SED_RED},"
    
    # Additional kernel checks
    print_list "Kernel version .............. "$NC
    uname -a | sed -${E} "s,$(uname -r),${SED_RED},"
    
    print_list "Kernel modules ............. "$NC
    if command -v lsmod >/dev/null 2>&1; then
        lsmod | grep -E "overlay|aufs|btrfs|device_mapper|floppy|loop|squashfs|udf|veth|vbox|vmware|kvm|xen|docker|containerd|runc|crio" | sed -${E} "s,overlay|aufs|btrfs|device_mapper|floppy|loop|squashfs|udf|veth|vbox|vmware|kvm|xen|docker|containerd|runc|crio,${SED_RED},g"
    elif [ -r /proc/modules ]; then
        cat /proc/modules | grep -E "overlay|aufs|btrfs|device_mapper|floppy|loop|squashfs|udf|veth|vbox|vmware|kvm|xen|docker|containerd|runc|crio" | sed -${E} "s,overlay|aufs|btrfs|device_mapper|floppy|loop|squashfs|udf|veth|vbox|vmware|kvm|xen|docker|containerd|runc|crio,${SED_RED},g"
    else
        echo_not_found "lsmod and /proc/modules"
    fi
    
    # Additional container runtime checks
    print_list "Container runtime sockets .. "$NC
    (find /var/run /run -name "*.sock" 2>/dev/null | grep -E "docker|containerd|crio|podman|lxc|rkt|kubelet|buildkit|firecracker" || echo "No") | sed -${E} "s,docker|containerd|crio|podman|lxc|rkt|kubelet|buildkit|firecracker,${SED_RED},g"
    
    print_list "Container runtime configs .. "$NC
    (find /etc -name "*.conf" -o -name "*.json" 2>/dev/null | grep -E "docker|containerd|crio|podman|lxc|rkt|kubelet|buildkit|firecracker" || echo "No") | sed -${E} "s,docker|containerd|crio|podman|lxc|rkt|kubelet|buildkit|firecracker,${SED_RED},g"
    
    # Kubernetes-specific checks. API requests are opt-in (-a/-e), read-only,
    # short-lived, and never print the projected bearer token.
    if echo "$containerType" | grep -qi "kubernetes"; then
        print_3title "Kubernetes container escape checks" "T1611"
        print_info "https://cloud.hacktricks.wiki/en/pentesting-cloud/kubernetes-security/attacking-kubernetes-from-inside-a-pod.html"

        k8s_sa_dir=""
        for k8s_sa_dir in /var/run/secrets/kubernetes.io/serviceaccount /run/secrets/kubernetes.io/serviceaccount /secrets/kubernetes.io/serviceaccount; do
            if [ -r "$k8s_sa_dir/token" ]; then break; fi
        done
        k8s_sa_token="$k8s_sa_dir/token"
        k8s_sa_ca="$k8s_sa_dir/ca.crt"
        k8s_namespace="$(cat "$k8s_sa_dir/namespace" 2>/dev/null)"
        k8s_pod_name=""
        k8s_service_account=""

        # Bound service-account tokens contain pod and service-account names in
        # their local JWT payload. Decode only the payload and never display it.
        if [ -r "$k8s_sa_token" ] && command -v base64 >/dev/null 2>&1; then
            k8s_token_payload_b64="$(cut -d. -f2 < "$k8s_sa_token" 2>/dev/null | tr '_-' '/+')"
            case $((${#k8s_token_payload_b64} % 4)) in
                2) k8s_token_payload_b64="${k8s_token_payload_b64}==" ;;
                3) k8s_token_payload_b64="${k8s_token_payload_b64}=" ;;
            esac
            k8s_token_payload="$(printf "%s" "$k8s_token_payload_b64" | base64 -d 2>/dev/null)"
            k8s_pod_name="$(printf "%s" "$k8s_token_payload" | grep -o '"pod"[[:space:]]*:[[:space:]]*{[^}]*}' | sed -n 's/.*"name"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -n 1)"
            k8s_service_account="$(printf "%s" "$k8s_token_payload" | grep -o '"serviceaccount"[[:space:]]*:[[:space:]]*{[^}]*}' | sed -n 's/.*"name"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -n 1)"
            if [ -z "$k8s_service_account" ]; then
                k8s_service_account="$(printf "%s" "$k8s_token_payload" | sed -n 's/.*"sub"[[:space:]]*:[[:space:]]*"system:serviceaccount:[^:"]*:\([^"]*\)".*/\1/p')"
            fi
            k8s_token_payload=""
            k8s_token_payload_b64=""
        fi
        if [ -z "$k8s_pod_name" ]; then k8s_pod_name="$(hostname 2>/dev/null)"; fi

        print_list "Namespace ...................... ${k8s_namespace:-unknown}\n"
        print_list "Pod name ....................... ${k8s_pod_name:-unknown}\n"
        if [ -r "$k8s_sa_token" ]; then
            print_list "Projected API token ............ present and readable (contents suppressed)\n" | sed -${E} "s,present and readable,${SED_RED_YELLOW},"
        else
            print_list "Projected API token ............ not readable\n" | sed -${E} "s,not readable,${SED_GREEN},"
        fi
        if [ -r "$k8s_sa_ca" ]; then
            print_list "Projected cluster CA ........... present and readable\n"
        else
            print_list "Projected cluster CA ........... not readable\n"
        fi

        k8s_api_host="${KUBERNETES_SERVICE_HOST:-}"
        case "$k8s_api_host" in *:*) k8s_api_host="[$k8s_api_host]" ;; esac
        if [ "$k8s_api_host" ]; then
            k8s_api_base="https://${k8s_api_host}:${KUBERNETES_SERVICE_PORT_HTTPS:-443}"
            print_list "Cluster API endpoint ........... $k8s_api_base\n"
        else
            k8s_api_base=""
            print_list "Cluster API endpoint ........... unknown\n"
        fi

        k8s_host_mounts="$(awk '$4=="/" && ($5=="/host" || $5=="/rootfs" || $5=="/mnt/host") {print $5 " (" $6 ")"}' /proc/self/mountinfo 2>/dev/null)"
        print_list "Possible host-root mounts ...... "$NC
        if [ "$k8s_host_mounts" ]; then
            printf "%s\n" "$k8s_host_mounts" | sed -${E} "s,.*,${SED_RED}&,"
        else
            echo "None at common mount points"
        fi

        k8s_can_create_pods="Unknown"
        k8s_can_exec_pods="Unknown"
        k8s_can_read_secrets="Unknown"
        k8s_can_request_tokens="Unknown"
        k8s_pod_risks=""
        k8s_chain_risk=""

        if [ "$dev_mounted" = "Yes" ]; then k8s_pod_risks="${k8s_pod_risks}hostDevices "; fi
        if command -v capsh >/dev/null 2>&1 && capsh --print 2>/dev/null | grep '^Current:' | grep -qw 'cap_sys_admin'; then
            k8s_pod_risks="${k8s_pod_risks}CAP_SYS_ADMIN "
        fi

        if [ "$EXTRA_CHECKS" ] && command -v curl >/dev/null 2>&1 && [ -r "$k8s_sa_token" ] && [ -r "$k8s_sa_ca" ] && [ "$k8s_api_base" ] && [ "$k8s_namespace" ]; then
            k8s_api_curl() {
                printf 'header = "Authorization: Bearer %s"\n' "$(tr -d '\r\n' < "$k8s_sa_token")" |
                    curl -sS --connect-timeout 1 --max-time 3 --cacert "$k8s_sa_ca" --config - "$@" 2>/dev/null
            }
            k8s_rules_allow() {
                printf "%s\n" "$k8s_ssrr_rules" |
                    grep -E "\"resources\"[[:space:]]*:[[:space:]]*\[[^]]*\"($1|\\*)\"([[:space:]]*,|[[:space:]]*\])" |
                    grep -E '"apiGroups"[[:space:]]*:[[:space:]]*\[[^]]*"(\*)?"' |
                    grep -E "\"verbs\"[[:space:]]*:[[:space:]]*\[[^]]*\"($2)\"" >/dev/null 2>&1
            }

            # SelfSubjectRulesReview evaluates permissions without creating,
            # modifying, or requesting any workload or credential.
            k8s_ssrr_request="{\"apiVersion\":\"authorization.k8s.io/v1\",\"kind\":\"SelfSubjectRulesReview\",\"spec\":{\"namespace\":\"$k8s_namespace\"}}"
            k8s_ssrr_response="$(k8s_api_curl -X POST -H 'Content-Type: application/json' --data "$k8s_ssrr_request" "$k8s_api_base/apis/authorization.k8s.io/v1/selfsubjectrulesreviews")"
            if printf "%s" "$k8s_ssrr_response" | grep -q '"resourceRules"[[:space:]]*:'; then
                # ResourceRule JSON objects begin with "verbs" in Kubernetes'
                # response. Split them before matching so verbs from one rule
                # cannot be combined with resources from another rule.
                k8s_ssrr_rules="$(printf "%s" "$k8s_ssrr_response" | tr '\n' ' ' | sed -${E} 's/}[[:space:]]*,[[:space:]]*\{[[:space:]]*"verbs"/}|{"verbs"/g' | tr '|' '\n')"
                if k8s_rules_allow "pods" 'create|\*'; then k8s_can_create_pods="Yes"; else k8s_can_create_pods="No"; fi
                if k8s_rules_allow "pods/(exec|\\*)" 'create|\*'; then k8s_can_exec_pods="Yes"; else k8s_can_exec_pods="No"; fi
                if k8s_rules_allow "secrets" 'get|list|watch|\*'; then k8s_can_read_secrets="Yes"; else k8s_can_read_secrets="No"; fi
                if k8s_rules_allow "serviceaccounts/token" 'create|\*'; then k8s_can_request_tokens="Yes"; else k8s_can_request_tokens="No"; fi
            fi

            # Fetch only this pod to inspect the submitted security context.
            if [ "$k8s_pod_name" ]; then
                k8s_pod_json="$(k8s_api_curl "$k8s_api_base/api/v1/namespaces/$k8s_namespace/pods/$k8s_pod_name")"
                if printf "%s" "$k8s_pod_json" | grep -q '"kind"[[:space:]]*:[[:space:]]*"Pod"'; then
                    if [ -z "$k8s_service_account" ]; then
                        k8s_service_account="$(printf "%s" "$k8s_pod_json" | sed -n 's/.*"serviceAccountName"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p')"
                    fi
                    if printf "%s" "$k8s_pod_json" | grep -Eq '"hostPID"[[:space:]]*:[[:space:]]*true'; then k8s_pod_risks="${k8s_pod_risks}hostPID "; fi
                    if printf "%s" "$k8s_pod_json" | grep -Eq '"hostIPC"[[:space:]]*:[[:space:]]*true'; then k8s_pod_risks="${k8s_pod_risks}hostIPC "; fi
                    if printf "%s" "$k8s_pod_json" | grep -Eq '"hostNetwork"[[:space:]]*:[[:space:]]*true'; then k8s_pod_risks="${k8s_pod_risks}hostNetwork "; fi
                    if printf "%s" "$k8s_pod_json" | grep -Eq '"privileged"[[:space:]]*:[[:space:]]*true'; then k8s_pod_risks="${k8s_pod_risks}privileged "; fi
                    if printf "%s" "$k8s_pod_json" | grep -Eq '"allowPrivilegeEscalation"[[:space:]]*:[[:space:]]*true'; then k8s_pod_risks="${k8s_pod_risks}allowPrivilegeEscalation "; fi
                    if printf "%s" "$k8s_pod_json" | grep -Eq '"add"[[:space:]]*:[[:space:]]*\[[^]]*"SYS_ADMIN"' && ! printf "%s" "$k8s_pod_risks" | grep -q 'CAP_SYS_ADMIN'; then k8s_pod_risks="${k8s_pod_risks}CAP_SYS_ADMIN "; fi
                    if printf "%s" "$k8s_pod_json" | grep -Eq '"hostPath"[[:space:]]*:[[:space:]]*\{[^}]*"path"[[:space:]]*:[[:space:]]*"/"'; then
                        k8s_pod_risks="${k8s_pod_risks}hostPath:/ "
                    elif printf "%s" "$k8s_pod_json" | grep -Eq '"hostPath"[[:space:]]*:[[:space:]]*\{[^}]*"path"[[:space:]]*:[[:space:]]*"/(etc|proc|sys|dev|var/run|var/lib/kubelet)(/|\")'; then
                        k8s_pod_risks="${k8s_pod_risks}sensitiveHostPath "
                    elif printf "%s" "$k8s_pod_json" | grep -Eq '"hostPath"[[:space:]]*:[[:space:]]*\{'; then
                        k8s_pod_risks="${k8s_pod_risks}hostPath "
                    fi
                fi
            fi
        else
            print_list "API permission/pod checks ...... skipped (use -a or -e; requires curl, token and CA)\n"
        fi

        print_list "Service account ................ ${k8s_service_account:-unknown}\n"
        print_list "RBAC create pods ............... $k8s_can_create_pods\n" | sed -${E} "s,Yes,${SED_RED_YELLOW}," | sed -${E} "s,No,${SED_GREEN},"
        print_list "RBAC create pods/exec .......... $k8s_can_exec_pods\n" | sed -${E} "s,Yes,${SED_RED_YELLOW}," | sed -${E} "s,No,${SED_GREEN},"
        print_list "RBAC read secrets .............. $k8s_can_read_secrets\n" | sed -${E} "s,Yes,${SED_RED_YELLOW}," | sed -${E} "s,No,${SED_GREEN},"
        print_list "RBAC create serviceaccounts/token $k8s_can_request_tokens\n" | sed -${E} "s,Yes,${SED_RED_YELLOW}," | sed -${E} "s,No,${SED_GREEN},"
        print_list "Current pod escape settings .... "$NC
        if [ "$k8s_pod_risks" ]; then
            printf "%s\n" "$k8s_pod_risks" | sed -${E} "s,.*,${SED_RED}&,"
        else
            echo "None found or pod object is not readable"
        fi

        if { [ "$k8s_pod_risks" ] || [ "$k8s_host_mounts" ]; } && { [ "$k8s_can_create_pods" = "Yes" ] || [ "$k8s_can_exec_pods" = "Yes" ]; }; then
            k8s_chain_risk="HIGH - dangerous pod/host exposure combined with workload-control RBAC"
        elif [ "$k8s_can_create_pods" = "Yes" ] && { [ "$k8s_can_exec_pods" = "Yes" ] || [ "$k8s_can_read_secrets" = "Yes" ] || [ "$k8s_can_request_tokens" = "Yes" ]; }; then
            k8s_chain_risk="Dangerous RBAC chain; admission controls still determine privileged-pod creation"
        fi
        if [ "$k8s_chain_risk" ]; then
            print_list "Kubernetes escape chain ........ $k8s_chain_risk\n" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        fi
    fi
    
    # Interesting files and mounts
    print_3title "Interesting Files & Mounts" "T1611"
    print_list "Interesting files mounted ........ "$NC
    (mount -l || cat /proc/self/mountinfo || cat /proc/1/mountinfo || cat /proc/mounts || cat /proc/self/mounts || cat /proc/1/mounts )2>/dev/null | grep -Ev "$GREP_IGNORE_MOUNTS" | sed -${E} "s,.sock,${SED_RED}," | sed -${E} "s,docker.sock,${SED_RED_YELLOW}," | sed -${E} "s,/dev/,${SED_RED},g"
    
    print_list "Possible entrypoints ........... "$NC
    ls -lah /*.sh /*entrypoint* /**/entrypoint* /**/*.sh /deploy* 2>/dev/null | sort | uniq
    
    echo ""
fi
