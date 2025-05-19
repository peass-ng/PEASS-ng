# Title: System Information - Container/VM Escape
# ID: SY_Container_VM_Escape
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for container/VM escape possibilities that could lead to host system compromise:
#   - Container runtime detection (Docker, Podman, LXC)
#   - Shared resources between container and host
#   - Vulnerable container runtime versions
#   - Container breakout possibilities through capabilities
#   - Exploitation methods:
#     * Shared resources: Abuse mounted volumes, sockets, or devices
#     * Runtime exploits: Use known exploits for vulnerable container runtimes
#     * Capability abuse: Exploit containers with dangerous capabilities
#     * Common escape vectors:
#       - Mount escape (CVE-2021-21284)
#       - Capability escape (CAP_SYS_ADMIN, CAP_DAC_OVERRIDE)
#       - Seccomp bypass
#       - Kernel exploits from container
#       - Shared namespaces abuse
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info, print_list, warn_exec
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

print_2title "Container/VM Escape Information"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/docker-breakout-privilege-escalation"

# Check if running in container
print_list "Running in container? ......... "$NC
if [ -f "/.dockerenv" ]; then
    echo "Yes (Docker)" | sed -${E} "s,.*,${SED_RED},g"
elif [ -f "/run/.containerenv" ]; then
    echo "Yes (Podman)" | sed -${E} "s,.*,${SED_RED},g"
elif [ -f "/proc/1/cgroup" ] && grep -q "docker\|lxc" "/proc/1/cgroup" 2>/dev/null; then
    echo "Yes (Container)" | sed -${E} "s,.*,${SED_RED},g"
else
    echo "No" | sed -${E} "s,.*,${SED_GREEN},g"
fi

# Check for shared resources
print_list "Shared resources with host? ... "$NC
if [ -f "/proc/mounts" ]; then
    grep -E "docker|lxc" /proc/mounts 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
else
    echo_not_found "/proc/mounts"
fi

# Check for container runtime vulnerabilities
print_list "Container runtime version? .... "$NC
if [ "$(command -v docker 2>/dev/null || echo -n '')" ]; then
    docker version 2>/dev/null | grep "Version" | sed -${E} "s,([0-9]+(\.[0-9]+)+),${SED_RED},g"
elif [ "$(command -v podman 2>/dev/null || echo -n '')" ]; then
    podman version 2>/dev/null | grep "Version" | sed -${E} "s,([0-9]+(\.[0-9]+)+),${SED_RED},g"
else
    echo_not_found "container runtime"
fi

# Check for container breakout possibilities
print_list "Container breakout possibilities? "$NC
if [ -f "/proc/self/status" ]; then
    if grep -q "CapEff:\s*0000003fffffffff" "/proc/self/status" 2>/dev/null; then
        echo "Container has all capabilities" | sed -${E} "s,.*,${SED_RED},g"
    fi
    if grep -q "Seccomp:\s*0" "/proc/self/status" 2>/dev/null; then
        echo "Seccomp is disabled" | sed -${E} "s,.*,${SED_RED},g"
    fi
fi

echo "" 