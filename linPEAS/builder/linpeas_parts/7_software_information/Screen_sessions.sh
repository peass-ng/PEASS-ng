# Title: Software Information - Screen sessions
# ID: SI_Screen_sessions
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Screen sessions
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables:$DEBUG, $SEARCH_IN_FOLDER, $USER, $wgroups
# Initial Functions:
# Generated Global Variables: $screensess, $screensess2
# Fat linpeas: 0
# Small linpeas: 1


if ([ "$screensess" ] || [ "$screensess2" ] || [ "$DEBUG" ]) && ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Searching screen sessions"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#open-shell-sessions"
  screensess=$(screen -ls 2>/dev/null)
  screensess2=$(find /run/screen -type d -path "/run/screen/S-*" 2>/dev/null)
  
  screen -v
  printf "$screensess\n$screensess2" | sed -${E} "s,.*,${SED_RED}," | sed -${E} "s,No Sockets found.*,${C}[32m&${C}[0m,"
  
  find /run/screen -type s -path "/run/screen/S-*" -not -user $USER '(' '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null | while read f; do
    echo "Other user screen socket is writable: $f" | sed "s,$f,${SED_RED_YELLOW},"
  done
  echo ""
fi