# Title: System Information - Protections
# ID: SY_Protections
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for system security protections and their bypass possibilities:
#   - AppArmor/SELinux status and profiles
#   - ASLR status
#   - Seccomp filters
#   - Capabilities
#   - Common vulnerable scenarios:
#     * Disabled security modules
#     * Weak security profiles
#     * Missing security features
#     * Misconfigured protections
#   - Exploitation methods:
#     * Protection bypass: Circumvent security measures
#     * Common attack vectors:
#       - AppArmor/SELinux bypass
#       - ASLR bypass
#       - Seccomp filter bypass
#       - Capability abuse
#     * Exploit techniques:
#       - Profile bypass
#       - Memory randomization bypass
#       - Filter bypass
#       - Capability exploitation
#       - Protection circumvention
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_list, warn_exec
# Global Variables:
# Initial Functions:
# Generated Global Variables: $ASLR, $hypervisorflag, $detectedvirt, $unpriv_userns_clone, $perf_event_paranoid, $mmap_min_addr, $ptrace_scope, $dmesg_restrict, $kptr_restrict, $unpriv_bpf_disabled, $protected_symlinks, $protected_hardlinks, $label, $sysctl_path, $sysctl_var, $zero_color, $nonzero_color, $sysctl_value
# Fat linpeas: 0
# Small linpeas: 0


print_sysctl_eq_zero() {
    local label="$1"
    local sysctl_path="$2"
    local sysctl_var="$3"
    local zero_color="$4"
    local nonzero_color="$5"
    local sysctl_value

    print_list "$label" "$NC"
    sysctl_value=$(cat "$sysctl_path" 2>/dev/null)
    eval "$sysctl_var=\$sysctl_value"
    if [ -z "$sysctl_value" ]; then
        echo_not_found "$sysctl_path"
    else
        if [ "$sysctl_value" -eq 0 ]; then
            echo "0" | sed -${E} "s,0,${zero_color},"
        else
            echo "$sysctl_value" | sed -${E} "s,.*,${nonzero_color},g"
        fi
    fi
}

#-- SY) AppArmor
print_2title "Protections"
print_list "AppArmor enabled? .............. "$NC
if [ "$(command -v aa-status 2>/dev/null || echo -n '')" ]; then
    aa-status 2>&1 | sed "s,disabled,${SED_RED},"
elif [ "$(command -v apparmor_status 2>/dev/null || echo -n '')" ]; then
    apparmor_status 2>&1 | sed "s,disabled,${SED_RED},"
elif [ "$(ls -d /etc/apparmor* 2>/dev/null)" ]; then
    ls -d /etc/apparmor*
else
    echo_not_found "AppArmor"
fi

#-- SY) AppArmor2
print_list "AppArmor profile? .............. "$NC
(cat /proc/self/attr/current 2>/dev/null || echo "unconfined") | sed "s,unconfined,${SED_RED}," | sed "s,kernel,${SED_GREEN},"

#-- SY) LinuxONE
print_list "is linuxONE? ................... "$NC
( (uname -a | grep "s390x" >/dev/null 2>&1) && echo "Yes" || echo_not_found "s390x")

#-- SY) grsecurity
print_list "grsecurity present? ............ "$NC
( (uname -r | grep "\-grsec" >/dev/null 2>&1 || grep "grsecurity" /etc/sysctl.conf >/dev/null 2>&1) && echo "Yes" || echo_not_found "grsecurity")

#-- SY) PaX
print_list "PaX bins present? .............. "$NC
(command -v paxctl-ng paxctl >/dev/null 2>&1 && echo "Yes" || echo_not_found "PaX")

#-- SY) Execshield
print_list "Execshield enabled? ............ "$NC
(grep "exec-shield" /etc/sysctl.conf 2>/dev/null || echo_not_found "Execshield") | sed "s,=0,${SED_RED},"

#-- SY) SElinux
print_list "SELinux enabled? ............... "$NC
(sestatus 2>/dev/null || echo_not_found "sestatus") | sed "s,disabled,${SED_RED},"

#-- SY) Seccomp
print_list "Seccomp enabled? ............... "$NC
([ "$(grep Seccomp /proc/self/status 2>/dev/null | grep -v 0)" ] && echo "enabled" || echo "disabled") | sed "s,disabled,${SED_RED}," | sed "s,enabled,${SED_GREEN},"

#-- SY) AppArmor
print_list "User namespace? ................ "$NC
if [ "$(cat /proc/self/uid_map 2>/dev/null)" ]; then echo "enabled" | sed "s,enabled,${SED_GREEN},"; else echo "disabled" | sed "s,disabled,${SED_RED},"; fi

#-- SY) Unprivileged user namespaces
print_sysctl_eq_zero "unpriv_userns_clone? ........... " "/proc/sys/kernel/unprivileged_userns_clone" "unpriv_userns_clone" "$SED_GREEN" "$SED_RED"

#-- SY) Unprivileged eBPF
print_sysctl_eq_zero "unpriv_bpf_disabled? ........... " "/proc/sys/kernel/unprivileged_bpf_disabled" "unpriv_bpf_disabled" "$SED_RED" "$SED_GREEN"

#-- SY) cgroup2
print_list "Cgroup2 enabled? ............... "$NC
([ "$(grep cgroup2 /proc/filesystems 2>/dev/null)" ] && echo "enabled" || echo "disabled") | sed "s,disabled,${SED_RED}," | sed "s,enabled,${SED_GREEN},"

#-- SY) Kernel hardening sysctls
print_sysctl_eq_zero "kptr_restrict? ................. " "/proc/sys/kernel/kptr_restrict" "kptr_restrict" "$SED_RED" "$SED_GREEN"

print_sysctl_eq_zero "dmesg_restrict? ................ " "/proc/sys/kernel/dmesg_restrict" "dmesg_restrict" "$SED_RED" "$SED_GREEN"

print_sysctl_eq_zero "ptrace_scope? .................. " "/proc/sys/kernel/yama/ptrace_scope" "ptrace_scope" "$SED_RED" "$SED_GREEN"

print_sysctl_eq_zero "protected_symlinks? ............ " "/proc/sys/fs/protected_symlinks" "protected_symlinks" "$SED_RED" "$SED_GREEN"

print_sysctl_eq_zero "protected_hardlinks? ........... " "/proc/sys/fs/protected_hardlinks" "protected_hardlinks" "$SED_RED" "$SED_GREEN"

print_list "perf_event_paranoid? ........... "$NC
perf_event_paranoid=$(cat /proc/sys/kernel/perf_event_paranoid 2>/dev/null)
if [ -z "$perf_event_paranoid" ]; then
    echo_not_found "/proc/sys/kernel/perf_event_paranoid"
else
    if [ "$perf_event_paranoid" -le 1 ]; then echo "$perf_event_paranoid" | sed -${E} "s,.*,${SED_RED},g"; else echo "$perf_event_paranoid" | sed -${E} "s,.*,${SED_GREEN},g"; fi
fi

print_sysctl_eq_zero "mmap_min_addr? ................. " "/proc/sys/vm/mmap_min_addr" "mmap_min_addr" "$SED_RED" "$SED_GREEN"

print_list "lockdown mode? ................. "$NC
if [ -f "/sys/kernel/security/lockdown" ]; then
    cat /sys/kernel/security/lockdown 2>/dev/null | sed -${E} "s,none,${SED_RED},g; s,integrity|confidentiality,${SED_GREEN},g"
else
    echo_not_found "/sys/kernel/security/lockdown"
fi

#-- SY) Kernel hardening config flags
print_list "Kernel hardening flags? ........ "$NC
if [ -f "/boot/config-$(uname -r)" ]; then
    grep -E 'CONFIG_RANDOMIZE_BASE|CONFIG_STACKPROTECTOR|CONFIG_SLAB_FREELIST_|CONFIG_KASAN' /boot/config-$(uname -r) 2>/dev/null
elif [ -f "/proc/config.gz" ]; then
    zcat /proc/config.gz 2>/dev/null | grep -E 'CONFIG_RANDOMIZE_BASE|CONFIG_STACKPROTECTOR|CONFIG_SLAB_FREELIST_|CONFIG_KASAN'
else
    echo_not_found "kernel config"
fi

#-- SY) Gatekeeper
if [ "$MACPEAS" ]; then
    print_list "Gatekeeper enabled? .......... "$NC
    (spctl --status 2>/dev/null || echo_not_found "sestatus") | sed "s,disabled,${SED_RED},"

    print_list "sleepimage encrypted? ........ "$NC
    (sysctl vm.swapusage | grep "encrypted" | sed "s,encrypted,${SED_GREEN},") || echo_no

    print_list "XProtect? .................... "$NC
    (system_profiler SPInstallHistoryDataType 2>/dev/null | grep -A 4 "XProtectPlistConfigData" | tail -n 5 | grep -Iv "^$") || echo_no

    print_list "SIP enabled? ................. "$NC
    csrutil status | sed "s,enabled,${SED_GREEN}," | sed "s,enabled,${SED_GREEN}," | sed "s,disabled,${SED_RED}," || echo_no

    print_list "Sealed Snapshot? ............. "$NC
    diskutil apfs list | grep "Snapshot Sealed" | awk -F: '{print $2}' | tr -d '[:space:]' | sed "s,Yes,${SED_GREEN}," | sed "s,No,${SED_RED}," || echo_not_found

    print_list "Sealed Snapshot (2nd)? ....... "$NC
    csrutil authenticated-root status | sed "s,enabled,${SED_GREEN}," | sed "s,disabled,${SED_RED}," || echo_no


    print_list "Connected to JAMF? ........... "$NC
    warn_exec jamf checkJSSConnection

    print_list "Connected to AD? ............. "$NC
    dsconfigad -show && echo "" || echo_no
fi

#-- SY) ASLR
print_list "Is ASLR enabled? ............... "$NC
ASLR=$(cat /proc/sys/kernel/randomize_va_space 2>/dev/null)
if [ -z "$ASLR" ]; then
    echo_not_found "/proc/sys/kernel/randomize_va_space";
else
    if [ "$ASLR" -eq "0" ]; then printf $RED"No"$NC; else printf $GREEN"Yes"$NC; fi
    echo ""
fi

#-- SY) Printer
print_list "Printer? ....................... "$NC
(lpstat -a || system_profiler SPPrintersDataType || echo_no) 2>/dev/null

#-- SY) Running in a virtual environment
print_list "Is this a virtual machine? ..... "$NC
hypervisorflag=$(grep flags /proc/cpuinfo 2>/dev/null | grep hypervisor)
if [ "$(command -v systemd-detect-virt 2>/dev/null || echo -n '')" ]; then
    detectedvirt=$(systemd-detect-virt)
    if [ "$hypervisorflag" ]; then printf $RED"Yes ($detectedvirt)"$NC; else printf $GREEN"No"$NC; fi
else
    if [ "$hypervisorflag" ]; then printf $RED"Yes"$NC; else printf $GREEN"No"$NC; fi
fi

echo ""
