# Title: Software Information - Searching Log4Shell vulnerable libraries
# ID: SI_Log4shell
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching Log4Shell vulnerable libraries
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$PSTORAGE_LOG4SHELL" ] || [ "$DEBUG" ]; then
  print_2title "Searching Log4Shell vulnerable libraries"
  printf "%s\n" "$PSTORAGE_LOG4SHELL" | while read f; do
    echo "$f" | grep -E "log4j\-core\-(1\.[^0]|2\.[0-9][^0-9]|2\.1[0-6])" | sed -${E} "s,log4j\-core\-(1\.[^0]|2\.[0-9][^0-9]|2\.1[0-6]),${SED_RED},";
  done
  echo ""
fi