# Title: Container - Container & breakout enumeration
# ID: CT_Container_breakout
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Container breakout enumeration to see if in case we are inside a container we could escape
# License: GNU GPL
# Version: 1.0
# Functions Used: checkContainerExploits, checkProcSysBreakouts, containerCheck, echo_no, echo_not_found, print_2title, print_3title, print_info, print_list
# Global Variables: $binfmt_misc_breakout, $containercapsB, $containerType, $core_pattern_breakout, $dev_mounted, $efi_efivars_writable, $efi_vars_writable, $GREP_IGNORE_MOUNTS, $inContainer, $kallsyms_readable, $kcore_readable, $kmem_readable, $kmem_writable, $kmsg_readable, $mem_readable, $mem_writable, $modprobe_present, $mountinfo_readable, $panic_on_oom_dos, $panic_sys_fs_dos, $proc_configgz_readable, $proc_mounted, $run_unshare, $release_agent_breakout1, $release_agent_breakout2, $release_agent_breakout3, $sched_debug_readable, $security_present, $security_writable, $sysreq_trigger_dos,  $uevent_helper_breakout, $vmcoreinfo_readable, $VULN_CVE_2019_5021, $self_mem_readable
# Initial Functions: containerCheck
# Generated Global Variables: $defautl_docker_caps
# Fat linpeas: 0
# Small linpeas: 0


if [ "$inContainer" ]; then
    echo ""
    print_2title "Container & breakout enumeration"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/docker-security/docker-breakout-privilege-escalation/index.html"
    print_list "Container ID ...................$NC $(cat /etc/hostname && echo -n '\n')"
    if [ -f "/proc/1/cpuset" ] && echo "$containerType" | grep -qi "docker"; then
        print_list "Container Full ID ..............$NC $(basename $(cat /proc/1/cpuset))\n"
    fi
    print_list "Seccomp enabled? ............... "$NC
    ([ "$(grep Seccomp /proc/self/status | grep -v 0)" ] && echo "enabled" || echo "disabled") | sed "s,disabled,${SED_RED}," | sed "s,enabled,${SED_GREEN},"

    print_list "AppArmor profile? .............. "$NC
    (cat /proc/self/attr/current 2>/dev/null || echo "disabled") | sed "s,disabled,${SED_RED}," | sed "s,kernel,${SED_GREEN},"

    print_list "User proc namespace? ........... "$NC
    if [ "$(cat /proc/self/uid_map 2>/dev/null)" ]; then (printf "enabled"; cat /proc/self/uid_map) | sed "s,enabled,${SED_GREEN},"; else echo "disabled" | sed "s,disabled,${SED_RED},"; fi

    checkContainerExploits
    print_list "Vulnerable to CVE-2019-5021 .... $VULN_CVE_2019_5021\n"$NC | sed -${E} "s,Yes,${SED_RED_YELLOW},"

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
    print_list "is modprobe present ............ $modprobe_present\n" | sed -${E} "s,/.*,${SED_RED},"
    print_list "DoS via panic_on_oom ........... $panic_on_oom_dos\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "DoS via panic_sys_fs ........... $panic_sys_fs_dos\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "DoS via sysreq_trigger_dos ..... $sysreq_trigger_dos\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/config.gz readable ....... $proc_configgz_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/sched_debug readable ..... $sched_debug_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/proc/*/mountinfo readable ..... $mountinfo_readable\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/kernel/security present ... $security_present\n" | sed -${E} "s,Yes,${SED_RED},"
    print_list "/sys/kernel/security writable .. $security_writable\n" | sed -${E} "s,Yes,${SED_RED},"
    if [ "$EXTRA_CHECKS" ]; then
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
    fi
    
    echo ""
    print_3title "Namespaces"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/docker-security/namespaces/index.html"
    ls -l /proc/self/ns/

    if echo "$containerType" | grep -qi "kubernetes"; then
        print_list "Kubernetes namespace ...........$NC $(cat /run/secrets/kubernetes.io/serviceaccount/namespace /var/run/secrets/kubernetes.io/serviceaccount/namespace /secrets/kubernetes.io/serviceaccount/namespace 2>/dev/null)\n"
        print_list "Kubernetes token ...............$NC $(cat /run/secrets/kubernetes.io/serviceaccount/token /var/run/secrets/kubernetes.io/serviceaccount/token /secrets/kubernetes.io/serviceaccount/token 2>/dev/null)\n"
        echo ""
        
        print_2title "Kubernetes Information"
        print_info "https://cloud.hacktricks.wiki/en/pentesting-cloud/kubernetes-security/attacking-kubernetes-from-inside-a-pod.html"
        
        
        print_3title "Kubernetes service account folder"
        ls -lR /run/secrets/kubernetes.io/ /var/run/secrets/kubernetes.io/ /secrets/kubernetes.io/ 2>/dev/null
        echo ""
        
        print_3title "Kubernetes env vars"
        (env | set) | grep -Ei "kubernetes|kube" | grep -Ev "^WF=|^Wfolders=|^mounted=|^USEFUL_SOFTWARE='|^INT_HIDDEN_FILES=|^containerType="
        echo ""

        print_3title "Current sa user k8s permissions"
        print_info "https://cloud.hacktricks.wiki/en/pentesting-cloud/kubernetes-security/kubernetes-role-based-access-control-rbac.html"
        kubectl auth can-i --list 2>/dev/null || curl -s -k -d "$(echo \"eyJraW5kIjoiU2VsZlN1YmplY3RSdWxlc1JldmlldyIsImFwaVZlcnNpb24iOiJhdXRob3JpemF0aW9uLms4cy5pby92MSIsIm1ldGFkYXRhIjp7ImNyZWF0aW9uVGltZXN0YW1wIjpudWxsfSwic3BlYyI6eyJuYW1lc3BhY2UiOiJlZXZlZSJ9LCJzdGF0dXMiOnsicmVzb3VyY2VSdWxlcyI6bnVsbCwibm9uUmVzb3VyY2VSdWxlcyI6bnVsbCwiaW5jb21wbGV0ZSI6ZmFsc2V9fQo=\"|base64 -d)" \
          "https://${KUBERNETES_SERVICE_HOST}:${KUBERNETES_SERVICE_PORT_HTTPS}/apis/authorization.k8s.io/v1/selfsubjectrulesreviews" \
            -X 'POST' -H 'Content-Type: application/json' \
            --header "Authorization: Bearer $(cat /var/run/secrets/kubernetes.io/serviceaccount/token)" | sed "s,secrets|exec|create|patch|impersonate|\"*\",${SED_RED},"

    fi
    echo ""

    print_2title "Container Capabilities"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/docker-security/docker-breakout-privilege-escalation/index.html#capabilities-abuse-escape"
    if [ "$(command -v capsh || echo -n '')" ]; then 
      capsh --print 2>/dev/null | sed -${E} "s,$containercapsB,${SED_RED},g"
    else
      defautl_docker_caps="00000000a80425fb=cap_chown,cap_dac_override,cap_fowner,cap_fsetid,cap_kill,cap_setgid,cap_setuid,cap_setpcap,cap_net_bind_service,cap_net_raw,cap_sys_chroot,cap_mknod,cap_audit_write,cap_setfcap"
      cat /proc/self/status | tr '\t' ' ' | grep Cap | sed -${E} "s, .*,${SED_RED},g" | sed -${E} "s/00000000a80425fb/$defautl_docker_caps/g" | sed -${E} "s,0000000000000000|00000000a80425fb,${SED_GREEN},g"
      echo $ITALIC"Run capsh --decode=<hex> to decode the capabilities"$NC
    fi
    echo ""

    print_2title "Privilege Mode"
    if [ -x "$(command -v fdisk || echo -n '')" ]; then
        if [ "$(fdisk -l 2>/dev/null | wc -l)" -gt 0 ]; then
            echo "Privilege Mode is enabled"| sed -${E} "s,enabled,${SED_RED_YELLOW},"
        else
            echo "Privilege Mode is disabled"| sed -${E} "s,disabled,${SED_GREEN},"
        fi
    else
        echo_not_found
    fi
    echo ""

    print_2title "Interesting Files Mounted"
    (mount -l || cat /proc/self/mountinfo || cat /proc/1/mountinfo || cat /proc/mounts || cat /proc/self/mounts || cat /proc/1/mounts )2>/dev/null | grep -Ev "$GREP_IGNORE_MOUNTS" | sed -${E} "s,.sock,${SED_RED}," | sed -${E} "s,docker.sock,${SED_RED_YELLOW}," | sed -${E} "s,/dev/,${SED_RED},g"
    echo ""

    print_2title "Possible Entrypoints"
    ls -lah /*.sh /*entrypoint* /**/entrypoint* /**/*.sh /deploy* 2>/dev/null | sort | uniq
    echo ""
fi
