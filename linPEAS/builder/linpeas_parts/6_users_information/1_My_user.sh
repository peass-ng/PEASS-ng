# Title: Users Information - My User
# ID: UG_My_user
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: My User
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $groupsB, $groupsVB, $idB, $knw_grps , $knw_usrs, $nosh_usrs,$sh_usrs, $USER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "My user"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#users"
(id || (whoami && groups)) 2>/dev/null | sed -${E} "s,$groupsB,${SED_RED},g" | sed -${E} "s,$groupsVB,${SED_RED_YELLOW},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,root,${SED_RED}," | sed -${E} "s,$knw_grps,${SED_GREEN},g" | sed -${E} "s,$idB,${SED_RED},g"
echo ""