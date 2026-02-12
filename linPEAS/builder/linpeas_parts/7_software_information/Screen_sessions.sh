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
# Generated Global Variables: $screensess, $screensess2, $uscreen
# Fat linpeas: 0
# Small linpeas: 1


if (command -v screen >/dev/null 2>&1 || [ -d "/run/screen" ] || [ "$DEBUG" ]) && ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Searching screen sessions"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#open-shell-sessions"
  screensess=$(screen -ls 2>/dev/null)
  screensess2=$(find /run/screen -type d -path "/run/screen/S-*" 2>/dev/null)
  
  screen -v
  printf "$screensess\n$screensess2" | sed -${E} "s,.*,${SED_RED}," | sed -${E} "s,No Sockets found.*,${C}[32m&${C}[0m,"
  
  find /run/screen -type s -path "/run/screen/S-*" -not -user $USER '(' '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null | while read f; do
    echo "Other user screen socket is writable: $f" | sed "s,$f,${SED_RED_YELLOW},"
  done

  if [ -r "/etc/passwd" ]; then
    print_3title "Checking other users screen sessions"
    cut -d: -f1,7 /etc/passwd 2>/dev/null | grep "sh$" | cut -d: -f1 | grep -v "^$USER$" | while read u; do
      uscreen=$(screen -ls "${u}/" 2>/dev/null | grep -v "No Sockets found" | grep -v "^$")
      if [ "$uscreen" ]; then
        echo "User $u screen sessions:"
        printf "%s\n" "$uscreen" | sed -${E} "s,.*,${SED_RED},"
      fi
    done
  fi
  echo ""
fi
