# Title: Processes & Cron & Services & Timers - Files opened by processes belonging to other users
# ID: PR_Files_open_process_other_user
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: Files opened by processes belonging to other users
# License: GNU GPL
# Version: 1.1
# Functions Used: print_2title, print_info
# Global Variables: $IAMROOT, $nosh_usrs, $SEARCH_IN_FOLDER, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables: $user_uid, $pid, $fd_target, $cmd, $user
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  if ! [ "$IAMROOT" ]; then
    print_2title "Files opened by processes belonging to other users"
    print_info "This is usually empty because of the lack of privileges to read other user processes information"

    # Function to get username by UID
    get_username_by_uid() {
      if [ -r "/etc/passwd" ]; then
        grep "^[^:]*:[^:]*:$1:" "/etc/passwd" 2>/dev/null | cut -d: -f1
      fi
    }

    # Check each process
    for pid in $(find /proc -maxdepth 1 -regex '/proc/[0-9]+' -printf "%f\n" 2>/dev/null); do
      # Skip if process doesn't exist or we can't access it
      [ ! -r "/proc/$pid/status" ] && continue
      [ ! -r "/proc/$pid/fd" ] && continue

      # Get process user
      user_uid=$(grep "^Uid:" "/proc/$pid/status" 2>/dev/null | awk '{print $2}')
      [ -z "$user_uid" ] && continue
      user=$(get_username_by_uid "$user_uid")
      [ -z "$user" ] && continue

      # Skip if process belongs to current user
      [ "$user" = "$USER" ] && continue

      # Get process command
      cmd=$(cat "/proc/$pid/cmdline" 2>/dev/null | tr '\0' ' ' | head -c 100)
      [ -z "$cmd" ] && continue

      # Check file descriptors
      for fd in /proc/$pid/fd/*; do
        [ ! -e "$fd" ] && continue
        fd_target=$(readlink "$fd" 2>/dev/null)
        [ -z "$fd_target" ] && continue

        # Skip if target doesn't exist or is a special file
        [ ! -e "$fd_target" ] && continue
        case "$fd_target" in
          /dev/*|/proc/*|/sys/*) continue ;;
        esac

        echo "Process $pid ($user) - $cmd"
        echo "  └─ Has open file: $fd_target" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed "s,root,${SED_RED},"
      done
    done
    echo ""
  fi
fi