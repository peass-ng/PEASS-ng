# Title: Users Information - Users & groups
# ID: UG_Users_groups
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get all users & groups
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $groupsB, $groupsVB, $knw_grps, $knw_usrs, $MACPEAS, $nosh_usrs, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "All users & groups"
if [ "$MACPEAS" ]; then
  dscl . list /Users | while read i; do id $i;done 2>/dev/null | sort | sed -${E} "s,$groupsB,${SED_RED},g" | sed -${E} "s,$groupsVB,${SED_RED},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,root,${SED_RED}," | sed -${E} "s,$knw_grps,${SED_GREEN},g"
else
  cut -d":" -f1 /etc/passwd 2>/dev/null| while read i; do id $i;done 2>/dev/null | sort | sed -${E} "s,$groupsB,${SED_RED},g" | sed -${E} "s,$groupsVB,${SED_RED},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,root,${SED_RED}," | sed -${E} "s,$knw_grps,${SED_GREEN},g"
fi
echo ""