# Title: Processes & Cron & Services & Timers - Process binaries permissions
# ID: PR_Process_binaries_perms
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: Check the permissions of the binaries of the running processes
# License: GNU GPL
# Version: 1.2
# Functions Used: print_2title, print_info
# Global Variables: $knw_usrs, $nosh_usrs, $NOUSEPS, $SEARCH_IN_FOLDER, $sh_usrs, $USER, $Wfolders
# Initial Functions:
# Generated Global Variables: $binW, $bpath, $pid
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ "$NOUSEPS" ]; then
    print_2title "Binary processes permissions (non 'root root' and not belonging to current user)"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#processes"
    
    # Get list of writable binaries
    binW=""
    for pid in $(find /proc -maxdepth 1 -regex '/proc/[0-9]+' -printf "%f\n" 2>/dev/null); do
      # Skip if process doesn't exist or we can't access it
      [ ! -r "/proc/$pid/exe" ] && continue
      
      # Get binary path
      bpath=$(readlink "/proc/$pid/exe" 2>/dev/null)
      [ -z "$bpath" ] && continue
      
      # Check if binary is writable
      if [ -w "$bpath" ]; then
        if [ -z "$binW" ]; then
          binW="$bpath"
        else
          binW="$binW|$bpath"
        fi
      fi
    done

    # Get and display binary permissions
    for pid in $(find /proc -maxdepth 1 -regex '/proc/[0-9]+' -printf "%f\n" 2>/dev/null); do
      # Skip if process doesn't exist or we can't access it
      [ ! -r "/proc/$pid/exe" ] && continue
      
      # Get binary path
      bpath=$(readlink "/proc/$pid/exe" 2>/dev/null)
      [ -z "$bpath" ] && continue
      
      # Display binary permissions if file exists
      if [ -e "$bpath" ]; then
        ls -la "$bpath" 2>/dev/null
      fi
    done | grep -Ev "\sroot\s+root" | grep -v " $USER " | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g" | sed -${E} "s,$binW,${SED_RED_YELLOW},g" | sed -${E} "s,$sh_usrs,${SED_RED}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED}," | sed "s,root,${SED_GREEN},"
    echo ""
  fi
fi