# Title: Interesting Permissions Files - Writable root-owned executables
# ID: IP_Writable_root_execs
# Author: HT Bot
# Last Update: 29-11-2025
# Description: Locate root-owned executables outside home folders that the current user can modify
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info, echo_not_found
# Global Variables: $DEBUG, $IAMROOT, $ROOT_FOLDER, $HOME, $writeVB
# Initial Functions:
# Generated Global Variables: $writable_root_execs
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$IAMROOT" ]; then
  print_2title "Writable root-owned executables I can modify (max 200)"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#writable-files"

  writable_root_execs=$(
    find "$ROOT_FOLDER" -type f -user root -perm -u=x \
      \( -perm -g=w -o -perm -o=w \) \
      ! -path "/proc/*" ! -path "/sys/*" ! -path "/run/*" ! -path "/dev/*" ! -path "/snap/*" ! -path "$HOME/*" 2>/dev/null \
      | while IFS= read -r f; do
          if [ -w "$f" ]; then
            ls -l "$f" 2>/dev/null
          fi
        done | head -n 200
  )

  if [ "$writable_root_execs" ] || [ "$DEBUG" ]; then
    printf "%s\n" "$writable_root_execs" | sed -${E} "s,$writeVB,${SED_RED_YELLOW},"
  else
    echo_not_found "Writable root-owned executables"
  fi
  echo ""
fi
