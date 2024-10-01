# Title: LinPeasBase - execBin
# ID: get_current_user_privot_pid
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Write and exected an embedded binary
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $SEARCH_IN_FOLDER, $NOUSEPS
# Initial Functions:
# Generated Global Variables: $CURRENT_USER_PIVOT_PID, $pid, $ppid, $user, $ppid_user
# Fat linpeas: 0
# Small linpeas: 1


get_current_user_privot_pid(){
    CURRENT_USER_PIVOT_PID=""
    if ! [ "$SEARCH_IN_FOLDER" ] && ! [ "$NOUSEPS" ]; then
        # Function to get user by PID
        get_user_by_pid() {
            ps -p "$1" -o user | grep -v "USER"
        }

        # Find processes with PPID and user info, then filter those where PPID's user is different from the process's user
        ps -eo pid,ppid,user | grep -v "PPID" | while read -r pid ppid user; do
            if [ "$ppid" = "0" ]; then
            continue
            fi
            ppid_user=$(get_user_by_pid "$ppid")
            if echo "$user" | grep -Eqv "$ppid_user|root$"; then
            if [ "$ppid_user" = "$USER" ]; then
                CURRENT_USER_PIVOT_PID="$ppid"
            fi
            fi
        done
        echo ""
    fi
}