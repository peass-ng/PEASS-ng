# Title: Container - Container & breakout enumeration
# ID: CT_Container_breakout
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Container breakout enumeration to identify potential escape vectors:
#   - Container runtime vulnerabilities
#   - Mount point misconfigurations
#   - Capability abuse
#   - Namespace escape
#   - Common vulnerable scenarios:
#     * Privileged containers
#     * Misconfigured mounts
#     * Excessive capabilities
#     * Namespace isolation bypass
#     * Runtime vulnerabilities
#     * Container escape tools
#     * Shared kernel exploits
#     * Container escape CVEs
#   - Exploitation methods:
#     * Mount escape: Abuse mount misconfigurations
#     * Capability abuse: Exploit excessive capabilities
#     * Namespace escape: Break out of container namespaces
#     * Runtime escape: Exploit container runtime vulnerabilities
#     * Common attack vectors:
#       - Mount point manipulation
#       - Capability exploitation
#       - Namespace breakout
#       - Runtime vulnerability abuse
#       - Kernel exploit abuse
#       - Container escape tool usage
#     * Exploit techniques:
#       - Mount point abuse
#       - Capability escalation
#       - Namespace escape
#       - Runtime exploitation
#       - Kernel exploitation
#       - Container escape tool execution
# License: GNU GPL
# Version: 1.0
# Functions Used: checkContainerExploits, checkProcSysBreakouts, containerCheck, print_2title, print_3title, print_info, print_list, warn_exec
# Global Variables: $binfmt_misc_breakout, $containercapsB, $containerType, $core_pattern_breakout, $dev_mounted, $efi_efivars_writable, $efi_vars_writable, $GREP_IGNORE_MOUNTS, $inContainer, $kallsyms_readable, $kcore_readable, $kmem_readable, $kmem_writable, $kmsg_readable, $mem_readable, $mem_writable, $modprobe_present, $mountinfo_readable, $panic_on_oom_dos, $panic_sys_fs_dos, $proc_configgz_readable, $proc_mounted, $run_unshare, $release_agent_breakout1, $release_agent_breakout2, $release_agent_breakout3, $sched_debug_readable, $security_present, $security_writable, $sysreq_trigger_dos, $uevent_helper_breakout, $vmcoreinfo_readable, $VULN_CVE_2019_5021, $self_mem_readable
# Initial Functions: containerCheck
# Generated Global Variables: $defautl_docker_caps, $containerd_version, $runc_version, $containerd_version
# Fat linpeas: 0
# Small linpeas: 0

if [ "$inContainer" ]; then
    echo ""
    print_2title "Container & breakout enumeration"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/docker-security/docker-breakout-privilege-escalation/index.html"
    
    # Basic container info
    print_list "Container ID ...................$NC $(cat /etc/hostname && echo -n '\n')"
    if [ -f "/proc/1/cpuset" ] && echo "$containerType" | grep -qi "docker"; then
        print_list "Container Full ID ..............$NC $(basename $(cat /proc/1/cpuset))\n"
    fi
    
    # Security mechanisms
    print_3title "Security Mechanisms"
    print_list "Seccomp enabled? ............... "$NC
    ([ "$(grep Seccomp /proc/self/status | grep -v 0)" ] && echo "enabled" || echo "disabled") | sed "s,disabled,${SED_RED}," | sed "s,enabled,${SED_GREEN},"

    print_list "AppArmor profile? .............. "$NC
    (cat /proc/self/attr/current 2>/dev/null || echo "disabled") | sed "s,disabled,${SED_RED}," | sed "s,kernel,${SED_GREEN},"

    print_list "User proc namespace? ........... "$NC
    if [ "$(cat /proc/self/uid_map 2>/dev/null)" ]; then (printf "enabled"; cat /proc/self/uid_map) | sed "s,enabled,${SED_GREEN},"; else echo "disabled" | sed "s,disabled,${SED_RED},"; fi

    # Known vulnerabilities
    print_3title "Known Vulnerabilities"
    
    checkContainerExploits
    print_list "Vulnerable to CVE-2019-5021 .... $VULN_CVE_2019_5021\n"$NC | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    
    # Check for container escape tools
    print_list "Container escape tools present .. "$NC
    (command -v nsenter || command -v unshare || command -v chroot || command -v capsh || command -v setcap || command -v getcap || command -v docker || command -v kubectl || command -v ctr || command -v runc || command -v containerd || command -v crio || command -v podman || command -v lxc || command -v rkt || command -v nerdctl || echo "No") | sed -${E} "s,nsenter|unshare|chroot|capsh|setcap|getcap|docker|kubectl|ctr|runc|containerd|crio|podman|lxc|rkt|nerdctl,${SED_RED},g"
    
    # Runtime vulnerabilities
    print_3title "Runtime Vulnerabilities"
    
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
    
    # Mount escape vectors
    print_3title "Breakout via mounts"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/docker-security/docker-breakout-privilege-escalation/sensitive-mounts.html"
    
    checkProcSysBreakouts
    print_list "/proc mounted? ................. $proc_mounted\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "/dev mounted? .................. $dev_mounted\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "Run unshare .................... $run_unshare\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "release_agent breakout 1........ $release_agent_breakout1\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "release_agent breakout 2........ $release_agent_breakout2\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "release_agent breakout 3........ $release_agent_breakout3\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "core_pattern breakout .......... $core_pattern_breakout\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "binfmt_misc breakout ........... $binfmt_misc_breakout\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "uevent_helper breakout ......... $uevent_helper_breakout\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    
    # Additional mount checks
    print_list "Docker socket mounted? ......... "$NC
    (mount | grep -E "docker.sock|/var/run/docker.sock" || echo "No") | sed -${E} "s,Yes|docker.sock,${SED_RED},"
    
    print_list "Common host filesystem mounted?  "$NC
    (mount | grep -E "host|/host|/mnt/host" || echo "No") | sed -${E} "s,Yes|host,${SED_RED},"
    
    print_list "Interesting mounts ............. "$NC
    mount | grep -E "docker|container|overlay|kubelet" | grep -v "proc" | sed -${E} "s,docker.sock|host|privileged,${SED_RED},g"
    
    # Check for writable mount points
    print_list "Writable mount points ......... "$NC
    mount | grep -E "rw," | grep -v "ro," | sed -${E} "s,docker.sock|host|privileged,${SED_RED},g"
    
    # Check for shared mount points
    print_list "Shared mount points ........... "$NC
    mount | grep -E "shared|slave" | sed -${E} "s,docker.sock|host|privileged,${SED_RED},g"
    
    # Capability checks
    print_3title "Capability Checks"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/docker-security/docker-breakout-privilege-escalation/capabilities-abuse-escape.html"
    
    print_list "Dangerous capabilities ......... "$NC
    if [ "$(command -v capsh || echo -n '')" ]; then 
        capsh --print 2>/dev/null | sed -${E} "s,$containercapsB,${SED_RED},g"
    else
        defautl_docker_caps="00000000a80425fb=cap_chown,cap_dac_override,cap_fowner,cap_fsetid,cap_kill,cap_setgid,cap_setuid,cap_setpcap,cap_net_bind_service,cap_net_raw,cap_sys_chroot,cap_mknod,cap_audit_write,cap_setfcap"
        cat /proc/self/status | tr '\t' ' ' | grep Cap | sed -${E} "s, .*,${SED_RED},g" | sed -${E} "s/00000000a80425fb/$defautl_docker_caps/g" | sed -${E} "s,0000000000000000|00000000a80425fb,${SED_GREEN},g"
        echo $ITALIC"Run capsh --decode=<hex> to decode the capabilities"$NC
    fi
    
    # Additional capability checks
    print_list "Dangerous syscalls allowed ... "$NC
    if [ -f "/proc/sys/kernel/yama/ptrace_scope" ]; then
        (cat /proc/sys/kernel/yama/ptrace_scope 2>/dev/null || echo "Not found") | sed -${E} "s,0,${SED_RED},"
    else
        echo "Not found"
    fi
    
    # Namespace checks
    print_3title "Namespace Checks"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/docker-security/namespaces/index.html"
    
    print_list "Current namespaces ............. "$NC
    ls -l /proc/self/ns/
    
    print_list "Host network namespace? ........ "$NC
    if [ "$(ip netns list 2>/dev/null)" ]; then
        echo "Yes - Host network namespace accessible" | sed -${E} "s,Yes,${SED_RED},"
    else
        echo "No"
    fi
    
    # Additional namespace checks
    print_list "Host IPC namespace? ........... "$NC
    if [ "$(ls -l /proc/self/ns/ipc 2>/dev/null)" = "$(ls -l /proc/1/ns/ipc 2>/dev/null)" ]; then
        echo "Yes - Host IPC namespace shared" | sed -${E} "s,Yes,${SED_RED},"
    else
        echo "No"
    fi
    
    print_list "Host PID namespace? ........... "$NC
    if [ "$(ls -l /proc/self/ns/pid 2>/dev/null)" = "$(ls -l /proc/1/ns/pid 2>/dev/null)" ]; then
        echo "Yes - Host PID namespace shared" | sed -${E} "s,Yes,${SED_RED},"
    else
        echo "No"
    fi
    
    print_list "Host UTS namespace? ........... "$NC
    if [ "$(ls -l /proc/self/ns/uts 2>/dev/null)" = "$(ls -l /proc/1/ns/uts 2>/dev/null)" ]; then
        echo "Yes - Host UTS namespace shared" | sed -${E} "s,Yes,${SED_RED},"
    else
        echo "No"
    fi
    
    # Additional breakout vectors
    print_3title "Additional Breakout Vectors"
    
    print_list "is modprobe present ............ $modprobe_present\n" | sed -${E} "s,/.*,${SED_RED},"
    print_list "DoS via panic_on_oom ........... $panic_on_oom_dos\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "DoS via panic_sys_fs ........... $panic_sys_fs_dos\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "DoS via sysreq_trigger_dos ..... $sysreq_trigger_dos\n" | sed -${E} "s,Yes,${SED_RED},"
    
    # Check for container escape tools in PATH
    print_list "Container escape tools in PATH . "$NC
    (which nsenter 2>/dev/null || which unshare 2>/dev/null || which chroot 2>/dev/null || which capsh 2>/dev/null || which setcap 2>/dev/null || which getcap 2>/dev/null || echo "No") | sed -${E} "s,nsenter|unshare|chroot|capsh|setcap|getcap,${SED_RED},g"

    print_3title "Extra Breakout Vectors"
    print_list "/proc/config.gz readable ....... $proc_configgz_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/sched_debug readable ..... $sched_debug_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/*/mountinfo readable ..... $mountinfo_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/kernel/security present ... $security_present\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/kernel/security writable .. $security_writable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/kmsg readable ............ $kmsg_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/kallsyms readable ........ $kallsyms_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/self/mem readable ........ $self_mem_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/kcore readable ........... $kcore_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/kmem readable ............ $kmem_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/kmem writable ............ $kmem_writable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/mem readable ............. $mem_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/mem writable ............. $mem_writable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/kernel/vmcoreinfo readable  $vmcoreinfo_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/firmware/efi/vars writable  $efi_vars_writable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/firmware/efi/efivars writable $efi_efivars_writable\n" | sed -${E} "s,Yes,${SED_RED},"
    
    # Additional kernel checks
    print_list "Kernel version .............. "$NC
    uname -a | sed -${E} "s,$(uname -r),${SED_RED},"
    
    print_list "Kernel modules ............. "$NC
    lsmod | grep -E "overlay|aufs|btrfs|device_mapper|floppy|loop|squashfs|udf|veth|vbox|vmware|kvm|xen|docker|containerd|runc|crio" | sed -${E} "s,overlay|aufs|btrfs|device_mapper|floppy|loop|squashfs|udf|veth|vbox|vmware|kvm|xen|docker|containerd|runc|crio,${SED_RED},g"
    
    # Additional container runtime checks
    print_list "Container runtime sockets .. "$NC
    (find /var/run -name "*.sock" 2>/dev/null | grep -E "docker|containerd|crio|podman|lxc|rkt" || echo "No") | sed -${E} "s,docker|containerd|crio|podman|lxc|rkt,${SED_RED},g"
    
    print_list "Container runtime configs .. "$NC
    (find /etc -name "*.conf" -o -name "*.json" 2>/dev/null | grep -E "docker|containerd|crio|podman|lxc|rkt" || echo "No") | sed -${E} "s,docker|containerd|crio|podman|lxc|rkt,${SED_RED},g"
    
    # Kubernetes specific checks
    if echo "$containerType" | grep -qi "kubernetes"; then
        print_3title "Kubernetes Specific Checks"
        print_info "https://cloud.hacktricks.wiki/en/pentesting-cloud/kubernetes-security/attacking-kubernetes-from-inside-a-pod.html"
        
        print_list "Kubernetes namespace ...........$NC $(cat /run/secrets/kubernetes.io/serviceaccount/namespace /var/run/secrets/kubernetes.io/serviceaccount/namespace /secrets/kubernetes.io/serviceaccount/namespace 2>/dev/null)\n"
        print_list "Kubernetes token ...............$NC $(cat /run/secrets/kubernetes.io/serviceaccount/token /var/run/secrets/kubernetes.io/serviceaccount/token /secrets/kubernetes.io/serviceaccount/token 2>/dev/null)\n"
        
        print_list "Kubernetes service account folder" | sed -${E} "s,.*,${SED_RED},"
        ls -lR /run/secrets/kubernetes.io/ /var/run/secrets/kubernetes.io/ /secrets/kubernetes.io/ 2>/dev/null
        
        print_list "Kubernetes env vars" | sed -${E} "s,.*,${SED_RED},"
        (env | set) | grep -Ei "kubernetes|kube" | grep -Ev "^WF=|^Wfolders=|^mounted=|^USEFUL_SOFTWARE='|^INT_HIDDEN_FILES=|^containerType="
        
        print_list "Current sa user k8s permissions" | sed -${E} "s,.*,${SED_RED},"
        kubectl auth can-i --list 2>/dev/null || curl -s -k -d "$(echo \"eyJraW5kIjoiU2VsZlN1YmplY3RSdWxlc1JldmlldyIsImFwaVZlcnNpb24iOiJhdXRob3JpemF0aW9uLms4cy5pby92MSIsIm1ldGFkYXRhIjp7ImNyZWF0aW9uVGltZXN0YW1wIjpudWxsfSwic3BlYyI6eyJuYW1lc3BhY2UiOiJlZXZlZSJ9LCJzdGF0dXMiOnsicmVzb3VyY2VSdWxlcyI6bnVsbCwibm9uUmVzb3VyY2VSdWxlcyI6bnVsbCwiaW5jb21wbGV0ZSI6ZmFsc2V9fQo=\"|base64 -d)" \
          "https://${KUBERNETES_SERVICE_HOST}:${KUBERNETES_SERVICE_PORT_HTTPS}/apis/authorization.k8s.io/v1/selfsubjectrulesreviews" \
            -X 'POST' -H 'Content-Type: application/json' \
            --header "Authorization: Bearer $(cat /var/run/secrets/kubernetes.io/serviceaccount/token)" | sed "s,secrets|exec|create|patch|impersonate|\"*\",${SED_RED},"
        
        # Additional Kubernetes checks
        print_list "Kubernetes API server ...... "$NC
        (curl -s -k https://${KUBERNETES_SERVICE_HOST}:${KUBERNETES_SERVICE_PORT_HTTPS}/version 2>/dev/null || echo "Not accessible") | sed -${E} "s,Not accessible,${SED_GREEN},"
        
        print_list "Kubernetes secrets ......... "$NC
        (kubectl get secrets 2>/dev/null || echo "Not accessible") | sed -${E} "s,Not accessible,${SED_GREEN},"
        
        print_list "Kubernetes pods ............ "$NC
        (kubectl get pods 2>/dev/null || echo "Not accessible") | sed -${E} "s,Not accessible,${SED_GREEN},"
        
        print_list "Kubernetes services ........ "$NC
        (kubectl get services 2>/dev/null || echo "Not accessible") | sed -${E} "s,Not accessible,${SED_GREEN},"
        
        print_list "Kubernetes nodes ........... "$NC
        (kubectl get nodes 2>/dev/null || echo "Not accessible") | sed -${E} "s,Not accessible,${SED_GREEN},"
    fi
    
    # Interesting files and mounts
    print_3title "Interesting Files & Mounts"
    print_list "Interesting files mounted ........ "$NC
    (mount -l || cat /proc/self/mountinfo || cat /proc/1/mountinfo || cat /proc/mounts || cat /proc/self/mounts || cat /proc/1/mounts )2>/dev/null | grep -Ev "$GREP_IGNORE_MOUNTS" | sed -${E} "s,.sock,${SED_RED}," | sed -${E} "s,docker.sock,${SED_RED_YELLOW}," | sed -${E} "s,/dev/,${SED_RED},g"
    
    print_list "Possible entrypoints ........... "$NC
    ls -lah /*.sh /*entrypoint* /**/entrypoint* /**/*.sh /deploy* 2>/dev/null | sort | uniq
    
    echo ""
fi
