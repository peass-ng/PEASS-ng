# Title: Interesting Files - Files inside HOME
# ID: IF_My_home
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Files inside HOME
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title
# Global Variables: $HOME, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Files inside $HOME (limit 20)"
  (ls -la $HOME 2>/dev/null | head -n 23) || echo_not_found
  echo ""
fi