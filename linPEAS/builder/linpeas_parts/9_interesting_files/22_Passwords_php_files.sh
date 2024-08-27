# Title: Interesting Files - passwords in config PHP files
# ID: IF_Passwords_php_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching passwords in config PHP files
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if [ "$PSTORAGE_PHP_FILES" ] || [ "$DEBUG" ]; then
  print_2title "Searching passwords in config PHP files"
  printf "%s\n" "$PSTORAGE_PHP_FILES" | while read c; do grep -EiIH "(pwd|passwd|password|PASSWD|PASSWORD|dbuser|dbpass).*[=:].+|define ?\('(\w*passw|\w*user|\w*datab)" "$c" 2>/dev/null | grep -Ev "function|password.*= ?\"\"|password.*= ?''" | sed '/^.\{150\}./d' | sort | uniq | sed -${E} "s,[pP][aA][sS][sS][wW]|[dD][bB]_[pP][aA][sS][sS],${SED_RED},g"; done
  echo ""
fi