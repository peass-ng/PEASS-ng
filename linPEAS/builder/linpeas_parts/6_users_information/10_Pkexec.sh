# Title: Users Information - Pkexec
# ID: UG_Pkexec
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check Pkexec policy
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $Groups, $groupsB, $groupsVB,$nosh_usrs, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Checking Pkexec policy"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/interesting-groups-linux-pe/index.html#pe---method-2"
(cat /etc/polkit-1/localauthority.conf.d/* 2>/dev/null | grep -v "^#" | grep -Ev "\W+\#|^#" 2>/dev/null | sed -${E} "s,$groupsB,${SED_RED}," | sed -${E} "s,$groupsVB,${SED_RED}," | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed "s,$USER,${SED_RED_YELLOW}," | sed -${E} "s,$Groups,${SED_RED_YELLOW},") || echo_not_found "/etc/polkit-1/localauthority.conf.d"
echo ""
