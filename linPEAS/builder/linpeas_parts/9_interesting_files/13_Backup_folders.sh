# Title: Interesting Files - Backup folders
# ID: IF_Backup_folders
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Backup folders
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ "$PSTORAGE_BACKUPS" ] || [ "$DEBUG" ]; then
    print_2title "Backup folders"
    printf "%s\n" "$PSTORAGE_BACKUPS" | while read b ; do
      ls -ld "$b" 2> /dev/null | sed -${E} "s,backups|backup,${SED_RED},g";
      ls -l "$b" 2>/dev/null && echo ""
    done
    echo ""
  fi
fi