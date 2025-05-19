# Title: System Information - Systemd
# ID: SY_Systemd
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for systemd vulnerabilities and misconfigurations that could lead to privilege escalation:
#   - Systemd version vulnerabilities (CVE-2021-4034, CVE-2021-33910, etc.)
#   - Services running as root that could be exploited
#   - Services with dangerous capabilities that could be abused
#   - Services with writable paths that could be used to inject malicious code
#   - Exploitation methods:
#     * Version exploits: Use known exploits for vulnerable systemd versions
#     * Root services: Abuse services running as root to execute commands
#     * Capabilities: Abuse services with dangerous capabilities (CAP_SYS_ADMIN, etc.)
#     * Writable paths: Replace executables in writable paths to get code execution
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info, print_list, warn_exec
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

print_2title "Systemd Information"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/systemd-privilege-escalation"

# Check systemd version
print_list "Systemd version? .............. "$NC
if [ "$(command -v systemctl 2>/dev/null || echo -n '')" ]; then
    systemctl --version | head -n 1 | sed -${E} "s,([0-9]+(\.[0-9]+)+),${SED_RED},g"
else
    echo_not_found "systemctl"
fi

# Check for systemd services running as root
print_list "Services running as root? ..... "$NC
if [ "$(command -v systemctl 2>/dev/null || echo -n '')" ]; then
    systemctl list-units --type=service --state=running 2>/dev/null | grep -E "root|0:0" | sed -${E} "s,root|0:0,${SED_RED},g"
else
    echo_not_found "systemctl"
fi

# Check for systemd services with capabilities
print_list "Running services with capabilities? ... "$NC
if [ "$(command -v systemctl 2>/dev/null || echo -n '')" ]; then
    for service in $(systemctl list-units --type=service --state=running 2>/dev/null | grep -E "\.service" | awk '{print $1}'); do
        if [ -f "/etc/systemd/system/$service" ] || [ -f "/lib/systemd/system/$service" ]; then
            if grep -q "CapabilityBoundingSet" "/etc/systemd/system/$service" "/lib/systemd/system/$service" 2>/dev/null; then
                echo "$service" | sed -${E} "s,.*,${SED_RED},g"
            fi
        fi
    done
else
    echo_not_found "systemctl"
fi

# Check for systemd services with writable paths
print_list "Services with writable paths? . "$NC
if [ "$(command -v systemctl 2>/dev/null || echo -n '')" ]; then
    for service in $(systemctl list-units --type=service --state=running 2>/dev/null | grep -E "\.service" | awk '{print $1}'); do
        if [ -f "/etc/systemd/system/$service" ] || [ -f "/lib/systemd/system/$service" ]; then
            if grep -q "ExecStart\|ExecStartPre\|ExecStartPost" "/etc/systemd/system/$service" "/lib/systemd/system/$service" 2>/dev/null; then
                for path in $(grep -E "ExecStart|ExecStartPre|ExecStartPost" "/etc/systemd/system/$service" "/lib/systemd/system/$service" 2>/dev/null | awk '{print $2}' | tr -d '"'); do
                    if [ -w "$path" ]; then
                        echo "$service: $path" | sed -${E} "s,.*,${SED_RED},g"
                    fi
                done
            fi
        fi
    done
else
    echo_not_found "systemctl"
fi

echo "" 