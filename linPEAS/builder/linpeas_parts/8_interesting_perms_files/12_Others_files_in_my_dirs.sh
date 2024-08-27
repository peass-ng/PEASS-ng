# Title: Interesting Permissions Files - Others files in my dirs
# ID: IP_Others_files_in_my_dirs
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching folders owned by me containing others files on it
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $IAMROOT, $knw_usrs, $nosh_usrs, $ROOT_FOLDER, $sh_usrs ,$USER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$IAMROOT" ]; then
  print_2title "Searching folders owned by me containing others files on it (limit 100)"
  (find $ROOT_FOLDER -type d -user "$USER" ! -path "/proc/*" ! -path "/sys/*" 2>/dev/null | head -n 100 | while read d; do find "$d" -maxdepth 1 ! -user "$USER" \( -type f -or -type d \) -exec ls -l {} \; 2>/dev/null; done) | sort | uniq | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed "s,root,${C}[1;13m&${C}[0m,g"
  echo ""
fi