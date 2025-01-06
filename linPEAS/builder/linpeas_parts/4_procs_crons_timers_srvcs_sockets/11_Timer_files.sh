# Title: Processes & Cron & Services & Timers - .timer files
# ID: PR_Timer_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: .timer files
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $IAMROOT, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $timerbinpaths, $relpath
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Analyzing .timer files"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#timers"
printf "%s\n" "$PSTORAGE_TIMER" | while read t; do
  if ! [ "$IAMROOT" ] && [ -w "$t" ] && ! [ "$SEARCH_IN_FOLDER" ]; then
    echo "$t" | sed -${E} "s,.*,${SED_RED},g"
  fi
  timerbinpaths=$(grep -Po '^Unit=*(.*?$)' $t 2>/dev/null | cut -d '=' -f2)
  printf "%s\n" "$timerbinpaths" | while read tb; do
    if [ -w "$tb" ]; then
      echo "$t timer is calling this writable executable: $tb" | sed "s,writable.*,${SED_RED},g"
    fi
  done
  #relpath="`grep -Po '^Unit=[^/].*' \"$t\" 2>/dev/null`"
  #for rp in "$relpath"; do
  #  echo "$t is calling a relative path: $rp" | sed "s,relative.*,${SED_RED},g"
  #done
done
echo ""