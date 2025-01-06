# Title: Processes & Cron & Services & Timers - .socket files
# ID: PR_Socket_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: .socket files
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $IAMROOT, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $socketsbinpaths, $socketslistpaths
# Fat linpeas: 0
# Small linpeas: 0


#TODO: .socket files in MACOS are folders
if ! [ "$IAMROOT" ]; then
  print_2title "Analyzing .socket files"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#sockets"
  printf "%s\n" "$PSTORAGE_SOCKET" | while read s; do
    if ! [ "$IAMROOT" ] && [ -w "$s" ] && [ -f "$s" ] && ! [ "$SEARCH_IN_FOLDER" ]; then
      echo "Writable .socket file: $s" | sed "s,/.*,${SED_RED},g"
    fi
    socketsbinpaths=$(grep -Eo '^(Exec).*?=[!@+-]*/[a-zA-Z0-9_/\-]+' "$s" 2>/dev/null | cut -d '=' -f2 | sed 's,^[@\+!-]*,,')
    printf "%s\n" "$socketsbinpaths" | while read sb; do
      if [ -w "$sb" ]; then
        echo "$s is calling this writable executable: $sb" | sed "s,writable.*,${SED_RED},g"
      fi
    done
    socketslistpaths=$(grep -Eo '^(Listen).*?=[!@+-]*/[a-zA-Z0-9_/\-]+' "$s" 2>/dev/null | cut -d '=' -f2 | sed 's,^[@\+!-]*,,')
    printf "%s\n" "$socketslistpaths" | while read sl; do
      if [ -w "$sl" ]; then
        echo "$s is calling this writable listener: $sl" | sed "s,writable.*,${SED_RED},g";
      fi
    done
  done
  echo ""
fi