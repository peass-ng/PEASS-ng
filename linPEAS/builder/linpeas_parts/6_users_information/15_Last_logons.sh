# Title: Users Information - Last logons
# ID: UG_Last_logons
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Last logons
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $knw_usrs, $nosh_usrs, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


print_2title "Last logons"
(last -Faiw || last) 2>/dev/null | tail | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_RED}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
echo ""
