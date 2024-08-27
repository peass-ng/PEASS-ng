# Title: Users Information - Superusers
# ID: UG_Superusers
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Superusers
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables:$knw_usrs ,$nosh_usrs,$sh_usrs, $USER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Superusers"
awk -F: '($3 == "0") {print}' /etc/passwd 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED_YELLOW}," | sed "s,root,${SED_RED},"
echo ""
