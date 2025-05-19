# Title: System Information - Disks
# ID: SY_Disks
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for disk information and misconfigurations that could lead to privilege escalation:
#   - Available disks
#   - Disk permissions
#   - SMB shares
#   - Common vulnerable scenarios:
#     * Writable disks
#     * Insecure SMB shares
#     * Exposed disk devices
#     * Shared storage
#   - Exploitation methods:
#     * Disk access abuse: Exploit disk misconfigurations
#     * Common attack vectors:
#       - Disk device modification
#       - SMB share abuse
#       - Storage device access
#       - Shared disk exploitation
#     * Exploit techniques:
#       - Disk device manipulation
#       - SMB share exploitation
#       - Storage device abuse
#       - Shared disk access
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, warn_exec
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if [ -d "/dev" ] || [ "$DEBUG" ] ; then
    print_2title "Any sd*/disk* disk in /dev? (limit 20)"
    ls /dev 2>/dev/null | grep -Ei "^sd|^disk" | sed "s,crypt,${SED_RED}," | head -n 20
    echo ""
fi


if [ "$(command -v smbutil 2>/dev/null || echo -n '')" ] || [ "$DEBUG" ]; then
    print_2title "Mounted SMB Shares"
    warn_exec smbutil statshares -a
    echo ""
fi
