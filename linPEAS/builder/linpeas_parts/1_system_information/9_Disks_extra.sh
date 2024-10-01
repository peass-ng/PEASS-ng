# Title: System Information - Disks
# ID: SY_Disks_extra
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get Information about the disks
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