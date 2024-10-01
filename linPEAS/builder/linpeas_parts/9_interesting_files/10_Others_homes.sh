# Title: Interesting Files - Files inside /home
# ID: IF_Others_homes
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Files inside /home
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title
# Global Variables: $HOMESEARCH, $SEARCH_IN_FOLDER, $USER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Files inside others home (limit 20)"
  (find $HOMESEARCH -type f 2>/dev/null | grep -v -i "/"$USER | head -n 20) || echo_not_found
  echo ""
fi