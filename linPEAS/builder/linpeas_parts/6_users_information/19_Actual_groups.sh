# Title: Users Information - Actual Group Memberships via newgrp
# ID: UG_Actual_groups
# Author: Muthra
# Last Update: 23-03-2026
# Description: Detects actual group memberships via newgrp (catches /etc/gshadow vs /etc/group desync)
# License: GNU GPL
# Version: 1.0
# Mitre: T1069.001
# Functions Used: print_2title
# Global Variables: $groupsVB, $groupsB
# Initial Functions:
# Generated Global Variables: $ActualGroup, $groupname, $gid, $result, $actual_group_timeout, $actual_group_deadline, $actual_group_gids
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Actual Group Memberships via newgrp" "T1069.001"

# Skip this probe when running as root to avoid root-only newgrp behavior
if [ "${IAMROOT:-0}" != "1" ]; then
    ActualGroup="|"

    # A password prompt can read /dev/tty rather than stdin. Never run newgrp
    # without a watchdog; newer OpenBSD and GNU provide compatible timeout -k.
    actual_group_timeout=""
    if command -v timeout >/dev/null 2>&1; then
        actual_group_timeout=timeout
    elif command -v gtimeout >/dev/null 2>&1; then
        actual_group_timeout=gtimeout
    fi

    if [ -n "$actual_group_timeout" ] && command -v newgrp >/dev/null 2>&1; then
        actual_group_gids=" $(id -G 2>/dev/null) "
        actual_group_deadline=$(($(date +%s) + 15))

        while IFS=: read -r groupname _ gid _; do
            [ "$(date +%s)" -lt "$actual_group_deadline" ] || break
            case "$gid" in ''|*[!0-9]*) continue ;; esac
            case "$actual_group_gids" in *" $gid "*) continue ;; esac

            # Check the effective GID after newgrp: it can start a shell even
            # when the group switch failed. Pass the name as data, not shell code.
            result=$(printf 'id -g\n' | "$actual_group_timeout" -k 1 1 newgrp "$groupname" 2>/dev/null)
            if [ "$result" = "$gid" ]; then
                ActualGroup="${ActualGroup}${groupname}|"
                # groupsVB/groupsB are populated by the builder's variables module.
                # shellcheck disable=SC2154
                echo "Accessible group not shown in id: $groupname (gid=$gid)" | sed -"${E}" "s,$groupsVB,${SED_RED_YELLOW},g" | sed -"${E}" "s,$groupsB,${SED_RED},g"
            fi
        done < /etc/group
    fi

    echo ""
fi
