# Title: Interesting Files - Backup folders
# ID: IF_Backup_folders
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Backup folders
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.001
# Functions Used: print_2title
# Global Variables: $DEBUG, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $entry
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ "$PSTORAGE_BACKUPS" ] || [ "$DEBUG" ]; then
    print_2title "Backup folders" "T1552.001"
    printf "%s\n" "$PSTORAGE_BACKUPS" | while IFS= read -r b; do
      [ -d "$b" ] && [ -r "$b" ] && [ -x "$b" ] || continue
      ls -ld "$b" 2>/dev/null | sed -${E} "s,backups|backup,${SED_RED},g"
      # Prune each child: list immediate entries only and cap metadata calls.
      find "$b" ! -path "$b" -prune -print 2>/dev/null | head -n 30 |
        while IFS= read -r entry; do ls -ld "$entry" 2>/dev/null; done
      echo ""
    done
    echo ""
  fi
fi
