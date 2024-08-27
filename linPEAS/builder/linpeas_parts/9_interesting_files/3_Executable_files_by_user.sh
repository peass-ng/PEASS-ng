# Title: Interesting Files - Executable files potentially added by user
# ID: IF_Executable_files_by_user
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Executable files potentially added by user
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $SEARCH_IN_FOLDER 
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


print_2title "Executable files potentially added by user (limit 70)"
if ! [ "$SEARCH_IN_FOLDER" ]; then
  find / -type f -executable -printf "%T+ %p\n" 2>/dev/null | grep -Ev "000|/site-packages|/python|/node_modules|\.sample|/gems|/cgroup/" | sort -r | head -n 70
else
  find "$SEARCH_IN_FOLDER" -type f -executable -printf "%T+ %p\n" 2>/dev/null | grep -Ev "/site-packages|/python|/node_modules|\.sample|/gems|/cgroup/" | sort -r | head -n 70
fi
echo ""

