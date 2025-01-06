# Title: Processes & Cron & Services & Timers - Different processes 1 min
# ID: PR_Different_procs_1min
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Different processes executed during 1 min
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $nosh_usrs, $sh_usrs, $Wfolders
# Initial Functions:
# Generated Global Variables: $temp_file
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  if ! [ "$FAST" ] && ! [ "$SUPERFAST" ]; then
    print_2title "Different processes executed during 1 min (interesting is low number of repetitions)"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#frequent-cron-jobs"
    temp_file=$(mktemp)
    if [ "$(ps -e -o user,command 2>/dev/null)" ]; then 
      for i in $(seq 1 1210); do 
        ps -e -o user,command >> "$temp_file" 2>/dev/null; sleep 0.05; 
      done;
      sort "$temp_file" 2>/dev/null | uniq -c | grep -v "\[" | sed '/^.\{200\}./d' | sort -r -n | grep -E -v "\s*[1-9][0-9][0-9][0-9]" | sed -${E} "s,$Wfolders,${SED_RED},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed "s,root,${SED_RED},"; 
      rm "$temp_file";
    fi
    echo ""
  fi
fi