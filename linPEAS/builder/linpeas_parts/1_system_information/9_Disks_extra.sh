# Title: System Information - Disks Extra
# ID: SY_Disks_extra
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for additional disk information and system resources relevant to privilege escalation:
#   - Disk utilization
#   - System resources
#   - Storage statistics
#   - Common vulnerable scenarios:
#     * Low disk space (potential for race conditions)
#     * Resource exhaustion
#     * Storage device misconfigurations
#     * System resource abuse
#   - Exploitation methods:
#     * Resource-based attacks: Abuse system resources
#     * Common attack vectors:
#       - Disk space exhaustion
#       - Resource starvation
#       - Storage device abuse
#       - System resource manipulation
#     * Exploit techniques:
#       - Resource exhaustion
#       - Storage device exploitation
#       - System resource abuse
#       - Resource-based attacks
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, warn_exec
# Global Variables: $DEBUG, $EXTRA_CHECKS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if ([ "$(command -v diskutil 2>/dev/null || echo -n '')" ] || [ "$DEBUG" ]) && [ "$EXTRA_CHECKS" ]; then
    print_2title "Mounted disks information"
    warn_exec diskutil list
    echo ""
fi

if [ "$EXTRA_CHECKS" ] || [ "$DEBUG" ]; then
    print_2title "System stats"
    (df -h || lsblk) 2>/dev/null || echo_not_found "df and lsblk"
    warn_exec free 2>/dev/null
    echo ""
fi