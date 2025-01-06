# Title: Interesting Permissions Files - Interesting writable files by group
# ID: IP_Writable_files_group
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Interesting GROUP writable files (not in Home)
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $HOME, $IAMROOT, $notExtensions, $ROOT_FOLDER, $writeVB, $writeB
# Initial Functions:
# Generated Global Variables: $iwfbg
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$IAMROOT" ]; then
  print_2title "Interesting GROUP writable files (not in Home) (max 200)"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#writable-files"
  for g in $(groups); do
    iwfbg=$(find $ROOT_FOLDER '(' -type f -or -type d ')' -group $g -perm -g=w ! -path "/proc/*" ! -path "/sys/*" ! -path "$HOME/*" 2>/dev/null | grep -Ev "$notExtensions" | awk -F/ '{line_init=$0; if (!cont){ cont=0 }; $NF=""; act=$0; if (act == pre){(cont += 1)} else {cont=0}; if (cont < 5){ print line_init; } if (cont == "5"){print "#)You_can_write_even_more_files_inside_last_directory\n"}; pre=act }' | head -n 200)
    if [ "$iwfbg" ] || [ "$DEBUG" ]; then
      printf "  Group $GREEN$g:\n$NC";
      printf "%s\n" "$iwfbg" | while read l; do
        if echo "$l" | grep -q "You_can_write_even_more_files_inside_last_directory"; then printf $ITALIC"$l\n"$NC;
        elif echo "$l" | grep -Eq "$writeVB"; then
          echo "$l" | sed -${E} "s,$writeVB,${SED_RED_YELLOW},"
        else
          echo "$l" | sed -${E} "s,$writeB,${SED_RED},"
        fi
      done
    fi
  done
  echo ""
fi