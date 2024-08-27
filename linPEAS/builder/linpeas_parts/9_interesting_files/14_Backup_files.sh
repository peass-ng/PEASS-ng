# Title: Interesting Files - Backup files
# ID: IF_Backup_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Backup files
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $notExtensions, $ROOT_FOLDER, $notBackup
# Initial Functions:
# Generated Global Variables: $backs
# Fat linpeas: 0
# Small linpeas: 0


print_2title "Backup files (limited 100)"
backs=$(find $ROOT_FOLDER -type f \( -name "*backup*" -o -name "*\.bak" -o -name "*\.bak\.*" -o -name "*\.bck" -o -name "*\.bck\.*" -o -name "*\.bk" -o -name "*\.bk\.*" -o -name "*\.old" -o -name "*\.old\.*" \) -not -path "/proc/*" 2>/dev/null)
printf "%s\n" "$backs" | head -n 100 | while read b ; do
  if [ -r "$b" ]; then
    ls -l "$b" | grep -Ev "$notBackup" | grep -Ev "$notExtensions" | sed -${E} "s,backup|bck|\.bak|\.old,${SED_RED},g";
  fi;
done
echo ""
