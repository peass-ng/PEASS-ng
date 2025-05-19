# Title: System Information - Mounts
# ID: SY_Mounts
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for mount point misconfigurations that could lead to privilege escalation:
#   - Unmounted filesystems
#   - Mount point permissions
#   - Mount options
#   - Common vulnerable scenarios:
#     * Writable mount points
#     * Insecure mount options
#     * Unmounted sensitive filesystems
#     * Shared mount points
#   - Exploitation methods:
#     * Mount point abuse: Exploit mount misconfigurations
#     * Common attack vectors:
#       - Mount point modification
#       - Filesystem remounting
#       - Mount option abuse
#       - Shared mount exploitation
#     * Exploit techniques:
#       - Mount point manipulation
#       - Filesystem remounting
#       - Mount option exploitation
#       - Shared mount abuse
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $mountG, $mountpermsB, $mountpermsG, $notmounted, $Wfolders, $mounted
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if [ -f "/etc/fstab" ] || [ "$DEBUG" ]; then
    print_2title "Unmounted file-system?"
    print_info "Check if you can mount umounted devices"
    grep -v "^#" /etc/fstab 2>/dev/null | grep -Ev "\W+\#|^#" | sed -${E} "s,$mountG,${SED_GREEN},g" | sed -${E} "s,$notmounted,${SED_RED},g" | sed -${E} "s%$mounted%${SED_BLUE}%g" | sed -${E} "s,$Wfolders,${SED_RED}," | sed -${E} "s,$mountpermsB,${SED_RED},g" | sed -${E} "s,$mountpermsG,${SED_GREEN},g"
    echo ""
fi
