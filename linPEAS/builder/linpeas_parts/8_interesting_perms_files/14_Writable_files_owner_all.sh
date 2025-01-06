# Title: Interesting Permissions Files - Writable files by ownership or all
# ID: IP_Writable_files_owner_all
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Interesting writable files owned by me or writable by everyone (not in Home)
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $HOME, $IAMROOT, $ITALIC, $notExtensions, $ROOT_FOLDER, $USER, $writeVB, $writeB
# Initial Functions:
# Generated Global Variables: $obmowbe
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$IAMROOT" ]; then
  print_2title "Interesting writable files owned by me or writable by everyone (not in Home) (max 200)"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#writable-files"
  #In the next file, you need to specify type "d" and "f" to avoid fake link files apparently writable by all
  obmowbe=$(find $ROOT_FOLDER '(' -type f -or -type d ')' '(' '(' -user $USER ')' -or '(' -perm -o=w ')' ')' ! -path "/proc/*" ! -path "/sys/*" ! -path "$HOME/*" 2>/dev/null | grep -Ev "$notExtensions" | sort | uniq | awk -F/ '{line_init=$0; if (!cont){ cont=0 }; $NF=""; act=$0; if (act == pre){(cont += 1)} else {cont=0}; if (cont < 5){ print line_init; } if (cont == "5"){print "#)You_can_write_even_more_files_inside_last_directory\n"}; pre=act }' | head -n 200)
  printf "%s\n" "$obmowbe" | while read l; do
    if echo "$l" | grep -q "You_can_write_even_more_files_inside_last_directory"; then printf $ITALIC"$l\n"$NC;
    elif echo "$l" | grep -qE "$writeVB"; then
      echo "$l" | sed -${E} "s,$writeVB,${SED_RED_YELLOW},"
    else
      echo "$l" | sed -${E} "s,$writeB,${SED_RED},"
    fi
  done
  echo ""
fi