# Title: System Information - Disks
# ID: SY_Disks
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get Information about the disks
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
