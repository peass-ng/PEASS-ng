# Title: Interesting Files - All hidden files
# ID: IF_Hidden_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get all relevant hidden files
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables:$INT_HIDDEN_FILES, $ROOT_FOLDER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


print_2title "All relevant hidden files (not in /sys/ or the ones listed in the previous check) (limit 70)"
find $ROOT_FOLDER -type f -iname ".*" ! -path "/sys/*" ! -path "/System/*" ! -path "/private/var/*" -exec ls -l {} \; 2>/dev/null | grep -Ev "$INT_HIDDEN_FILES" | grep -Ev "_history$|\.gitignore|.npmignore|\.listing|\.ignore|\.uuid|\.depend|\.placeholder|\.gitkeep|\.keep|\.keepme|\.travis.yml" | head -n 70
echo ""