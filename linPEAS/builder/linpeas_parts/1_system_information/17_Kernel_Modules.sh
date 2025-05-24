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
# Functions Used: print_2title, print_3title
# Global Variables: 
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

echo ""
print_2title "Kernel Modules Information"

# List loaded kernel modules
if [ "$EXTRA_CHECKS" ] || [ "$DEBUG" ]; then
    print_3title "Loaded kernel modules"
    if [ -f "/proc/modules" ]; then
        lsmod
    else
        echo_not_found "/proc/modules"
    fi
fi

# Check for kernel modules with weak permissions
print_3title "Kernel modules with weak perms?"
if [ -d "/lib/modules" ]; then
    find /lib/modules -type f -name "*.ko" -ls 2>/dev/null | grep -Ev "root\s+root" | sed -${E} "s,.*,${SED_RED},g"
    if [ $? -eq 1 ]; then
        echo "No kernel modules with weak permissions found"
    fi
else
    echo_not_found "/lib/modules"
fi
echo ""

# Check for kernel modules that can be loaded by unprivileged users
print_3title "Kernel modules loadable? "
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