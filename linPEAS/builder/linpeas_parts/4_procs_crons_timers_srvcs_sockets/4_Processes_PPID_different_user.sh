# Title: Processes & Cron & Services & Timers - Process opened by other users
# ID: PR_Processes_PPID_different_user
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Processes whose PPID belongs to a different user (not root)
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $nosh_usrs, $NOUSEPS, $SEARCH_IN_FOLDER, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables: $ppid_user, $pid, $ppid, $user
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ] && ! [ "$NOUSEPS" ]; then
  print_2title "Processes whose PPID belongs to a different user (not root)"
  print_info "You will know if a user can somehow spawn processes as a different user"
  
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
    if echo "$ppid_user" | grep -Eqv "$user|root$"; then
      echo "Proc $pid with ppid $ppid is run by user $user but the ppid user is $ppid_user" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed "s,root,${SED_RED},"
    fi
  done
  echo ""
fi
