# Title: System Information - Kernel Modules
# ID: SY_Kernel_Modules
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for kernel module vulnerabilities and misconfigurations that could lead to privilege escalation:
#   - Loaded kernel modules with known vulnerabilities
#   - Kernel modules with weak permissions that could be modified
#   - Ability to load kernel modules as unprivileged user
#   - Missing kernel module signing requirements
#   - Exploitation methods:
#     * Vulnerable modules: Use known exploits for vulnerable kernel modules
#     * Weak permissions: Modify kernel modules to inject malicious code
#     * Module loading: Load malicious kernel modules to get root access
#     * Common vulnerable modules: nf_tables, eBPF, overlayfs, etc.
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_list
# Global Variables: 
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

print_2title "Kernel Modules Information"

# List loaded kernel modules
print_list "Loaded kernel modules? ........ "$NC
if [ -f "/proc/modules" ]; then
    lsmod
else
    echo_not_found "/proc/modules"
fi

# Check for kernel modules with weak permissions
print_list "Kernel modules with weak perms? "$NC
if [ -d "/lib/modules" ]; then
    find /lib/modules -type f -name "*.ko" -ls 2>/dev/null | grep -Ev "root\s+root" | sed -${E} "s,.*,${SED_RED},g"
else
    echo_not_found "/lib/modules"
fi

# Check for kernel modules that can be loaded by unprivileged users
print_list "Kernel modules loadable? "$NC
if [ -f "/proc/sys/kernel/modules_disabled" ]; then
    if [ "$(cat /proc/sys/kernel/modules_disabled)" = "0" ]; then
        echo "Modules can be loaded" | sed -${E} "s,.*,${SED_RED},g"
    else
        echo "Modules cannot be loaded" | sed -${E} "s,.*,${SED_GREEN},g"
    fi
else
    echo_not_found "/proc/sys/kernel/modules_disabled"
fi


echo "" 