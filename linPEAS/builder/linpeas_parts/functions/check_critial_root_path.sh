# Title: Interesting Perms Files - check_critial_root_path
# ID: check_critial_root_path
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if you have write privileges over critical root paths
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $USER, $wgroups
# Initial Functions:
# Generated Global Variables: folder_path
# Fat linpeas: 0
# Small linpeas: 1


check_critial_root_path(){
  folder_path="$1"
  if [ -w "$folder_path" ]; then echo "You have write privileges over $folder_path" | sed -${E} "s,.*,${SED_RED_YELLOW},"; fi
  if [ "$(find $folder_path -type f '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null)" ]; then echo "You have write privileges over $(find $folder_path -type f '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')')" | sed -${E} "s,.*,${SED_RED_YELLOW},"; fi
  if [ "$(find $folder_path -type f -not -user root 2>/dev/null)" ]; then echo "The following files aren't owned by root: $(find $folder_path -type f -not -user root 2>/dev/null)"; fi
}