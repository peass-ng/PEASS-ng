# Title: System Information - Path
# ID: SY_Path
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get Information about the Path
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $IAMROOT, $OLDPATH, $PATH, $Wfolders
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "PATH"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#writable-path-abuses"
if ! [ "$IAMROOT" ]; then
    echo "$OLDPATH" 2>/dev/null | sed -${E} "s,$Wfolders|\./|\.:|:\.,${SED_RED_YELLOW},g"
fi

if [ "$DEBUG" ]; then
     echo "New path exported: $PATH"
fi
echo ""