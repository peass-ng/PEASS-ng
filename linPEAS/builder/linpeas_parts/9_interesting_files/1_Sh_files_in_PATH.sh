# Title: Interesting Files - .sh files in path
# ID: IF_Sh_files_in_PATH
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: finds .sh files in path
# License: GNU GPL
# Version: 1.0 
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $IAMROOT, $SEARCH_IN_FOLDER, $shscripsG, $Wfolders, $PATH
# Initial Functions:
# Generated Global Variables: $broken_links
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title ".sh files in path"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#scriptbinaries-in-path"
  echo $PATH | tr ":" "\n" | while read d; do
    for f in $(find "$d" -name "*.sh" -o -name "*.sh.*" 2>/dev/null); do
      if ! [ "$IAMROOT" ] && [ -O "$f" ]; then
        echo "You own the script: $f" | sed -${E} "s,.*,${SED_RED},"
      elif ! [ "$IAMROOT" ] && [ -w "$f" ]; then #If write permision, win found (no check exploits)
        echo "You can write script: $f" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      else
        echo $f | sed -${E} "s,$shscripsG,${SED_GREEN}," | sed -${E} "s,$Wfolders,${SED_RED},";
      fi
    done
  done
  echo ""

  broken_links=$(find "$d" -type l 2>/dev/null | xargs file 2>/dev/null | grep broken)
  if [ "$broken_links" ] || [ "$DEBUG" ]; then 
    print_2title "Broken links in path"
    echo $PATH | tr ":" "\n" | while read d; do
      find "$d" -type l 2>/dev/null | xargs file 2>/dev/null | grep broken | sed -${E} "s,broken,${SED_RED},";
    done
    echo ""
  fi
fi