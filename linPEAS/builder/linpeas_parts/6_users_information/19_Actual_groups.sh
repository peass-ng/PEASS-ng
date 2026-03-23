# Title: Users Information - Actual Group Memberships via newgrp
# ID: UG_Actual_groups
# Author: Muthra
# Last Update: 23-03-2026
# Description: Detects actual group memberships via newgrp (catches /etc/gshadow vs /etc/group desync)
# License: GNU GPL
# Version: 1.0
# Mitre: T1069.001
# Functions Used: print_2title
# Global Variables: $groupsVB, $groupsB, $Groups
# Initial Functions:
# Generated Global Variables:  $ActualGroup, $groupname, $gid, $result
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Actual Group Memberships via newgrp" "T1069.001"

ActualGroup="|"

while IFS=: read -r groupname _ gid _; do
    result=$(timeout 1 sh -c 'echo id | newgrp "$1"' sh "$groupname" 2>/dev/null)
    if echo "$result" | grep -q "uid="; then
        if ! echo "${Groups}|" | grep -Fq "|${groupname}|"; then
            ActualGroup="${ActualGroup}${groupname}|"
            echo "Accessible group not shown in id: $groupname (gid=$gid)" | sed -${E} "s,$groupsVB,${SED_RED_YELLOW},g" | sed -${E} "s,$groupsB,${SED_RED},g"
        fi
    fi
done < /etc/group

echo ""
