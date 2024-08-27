# Title: Interesting Files - Readable files in /tmp, /var/tmp, backups
# ID: IF_Readable_files_tmp_backups
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Readable files in /tmp, /var/tmp, backups
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $backup_folders_row, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $filstmpback
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Readable files inside /tmp, /var/tmp, /private/tmp, /private/var/at/tmp, /private/var/tmp, and backup folders (limit 70)"
  filstmpback=$(find /tmp /var/tmp /private/tmp /private/var/at/tmp /private/var/tmp $backup_folders_row -type f 2>/dev/null | grep -Ev "dpkg\.statoverride\.|dpkg\.status\.|apt\.extended_states\.|dpkg\.diversions\." | head -n 70)
  printf "%s\n" "$filstmpback" | while read f; do if [ -r "$f" ]; then ls -l "$f" 2>/dev/null; fi; done
  echo ""
fi