# Title: Software Information - Useful Software
# ID: SI_Useful_software
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Useful Software
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $SEARCH_IN_FOLDER, $USEFUL_SOFTWARE
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Useful software"
  for t in $USEFUL_SOFTWARE; do command -v "$t" || echo -n ''; done
  echo ""
fi