# Title: Interesting Perms Files - check_critial_root_path
# ID: check_critial_root_path
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: Check if you have write privileges over critical root paths
# License: GNU GPL
# Version: 1.1
# Functions Used:
# Global Variables: $USER, $wgroups
# Initial Functions:
# Generated Global Variables: folder_path, folder_writable_files, folder_nonroot_files
# Fat linpeas: 0
# Small linpeas: 1


check_critial_root_path(){
  folder_path="$1"
  if [ -w "$folder_path" ]; then echo "You have write privileges over $folder_path" | sed -${E} "s,.*,${SED_RED_YELLOW},"; fi
  folder_writable_files=$(find "$folder_path" -type f '(' '(' -user "$USER" ')' -or '(' -perm -o=w ')' -or '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null) || :
  if [ "$folder_writable_files" ]; then echo "You have write privileges over $folder_writable_files" | sed -${E} "s,.*,${SED_RED_YELLOW},"; fi
  folder_nonroot_files=$(find "$folder_path" -type f -not -user root 2>/dev/null) || :
  if [ "$folder_nonroot_files" ]; then echo "The following files aren't owned by root: $folder_nonroot_files"; fi
}
