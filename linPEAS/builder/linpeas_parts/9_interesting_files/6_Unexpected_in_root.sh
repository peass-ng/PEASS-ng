# Title: Interesting Files - Unexpected folders in /
# ID: IF_Unexpected_in_root
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Unexpected folders in /
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title
# Global Variables: $commonrootdirsG, $commonrootdirsMacG, $MACPEAS, $ROOT_FOLDER, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Unexpected in root"
  if [ "$MACPEAS" ]; then
    (find $ROOT_FOLDER -maxdepth 1 | grep -Ev "$commonrootdirsMacG" | sed -${E} "s,.*,${SED_RED},") || echo_not_found
  else
    (find $ROOT_FOLDER -maxdepth 1 | grep -Ev "$commonrootdirsG" | sed -${E} "s,.*,${SED_RED},") || echo_not_found
  fi
  echo ""
fi