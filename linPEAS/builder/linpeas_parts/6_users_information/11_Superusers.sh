# Title: Users Information - Superusers
# ID: UG_Superusers
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check for superusers and users with UID 0
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $knw_usrs, $nosh_usrs, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables: $group
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Superusers and UID 0 Users"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/interesting-groups-linux-pe/index.html"

# Check /etc/passwd for UID 0 users
echo ""
print_3title "Users with UID 0 in /etc/passwd"
awk -F: '($3 == "0") {print}' /etc/passwd 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_RED_YELLOW},g" | sed "s,root,${SED_RED},g"

if [ command -v getent >/dev/null 2>&1 ]; then
    for group in sudo wheel adm docker lxd lxc root shadow disk video; do
        if getent group "$group" >/dev/null 2>&1; then
            echo "- Users in group '$group':"
            getent group "$group" 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_RED},g" | sed "s,root,${SED_RED},g"
        fi
    done
fi

# Check for users with sudo privileges in sudoers
echo ""
print_3title "Users with sudo privileges in sudoers"
grep -v "^#" /etc/sudoers 2>/dev/null | grep -v "^$" | grep -v "^Defaults" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_RED_YELLOW},g" | sed "s,root,${SED_RED},g"
echo ""
