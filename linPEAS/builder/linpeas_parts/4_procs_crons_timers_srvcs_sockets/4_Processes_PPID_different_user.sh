# Title: Processes & Cron & Services & Timers - Process opened by other users
# ID: PR_Processes_PPID_different_user
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: Processes whose PPID belongs to a different user (not root)
# License: GNU GPL
# Version: 1.1
# Functions Used: print_2title, print_info
# Global Variables: $nosh_usrs, $NOUSEPS, $SEARCH_IN_FOLDER, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables: $ppid_user, $pid, $ppid, $user, $ppid_uid, $user_uid
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ] && ! [ "$NOUSEPS" ]; then
  print_2title "Processes whose PPID belongs to a different user (not root)"
  print_info "You will know if a user can somehow spawn processes as a different user"
  
  # Function to get user by PID using /proc
  get_user_by_pid() {
    if [ -r "/proc/$1/status" ]; then
      grep "^Uid:" "/proc/$1/status" 2>/dev/null | awk '{print $2}'
    fi
  }

  # Function to get username by UID
  get_username_by_uid() {
    if [ -r "/etc/passwd" ]; then
      grep "^[^:]*:[^:]*:$1:" "/etc/passwd" 2>/dev/null | cut -d: -f1
    fi
  }

  # Find processes with PPID and user info, then filter those where PPID's user is different from the process's user
  for pid in $(find /proc -maxdepth 1 -regex '/proc/[0-9]+' -printf "%f\n" 2>/dev/null); do
    # Skip if process doesn't exist or we can't access it
    [ ! -r "/proc/$pid/status" ] && continue
    
    # Get process user
    user_uid=$(get_user_by_pid "$pid")
    [ -z "$user_uid" ] && continue
    user=$(get_username_by_uid "$user_uid")
    [ -z "$user" ] && continue

    # Get PPID
    ppid=$(grep "^PPid:" "/proc/$pid/status" 2>/dev/null | awk '{print $2}')
    [ -z "$ppid" ] || [ "$ppid" = "0" ] && continue

    # Get PPID user
    ppid_uid=$(get_user_by_pid "$ppid")
    [ -z "$ppid_uid" ] && continue
    ppid_user=$(get_username_by_uid "$ppid_uid")
    [ -z "$ppid_user" ] && continue

    # Check if users are different and PPID user is not root
    if [ "$user" != "$ppid_user" ] && [ "$ppid_user" != "root" ]; then
      echo "Proc $pid with ppid $ppid is run by user $user but the ppid user is $ppid_user" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed "s,root,${SED_RED},"
    fi
  done
  echo ""
fi
