# Title: Processes & Cron & Services & Timers - Process binaries permissions
# ID: PR_Process_binaries_perms
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check the permissions of the binaries of the running processes
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $knw_usrs, $nosh_usrs, $NOUSEPS, $SEARCH_IN_FOLDER, $sh_usrs, $USER, $Wfolders
# Initial Functions:
# Generated Global Variables: $binW, $bpath
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ "$NOUSEPS" ]; then
    print_2title "Binary processes permissions (non 'root root' and not belonging to current user)"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#processes"
    binW="IniTialiZZinnggg"
    ps auxwww 2>/dev/null | awk '{print $11}' | while read bpath; do
      if [ -w "$bpath" ]; then
        binW="$binW|$bpath"
      fi
    done
    ps auxwww 2>/dev/null | awk '{print $11}' | xargs ls -la 2>/dev/null |awk '!x[$0]++' 2>/dev/null | grep -v " root root " | grep -v " $USER " | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g" | sed -${E} "s,$binW,${SED_RED_YELLOW},g" | sed -${E} "s,$sh_usrs,${SED_RED}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED}," | sed "s,root,${SED_GREEN},"
    echo ""
  fi
fi