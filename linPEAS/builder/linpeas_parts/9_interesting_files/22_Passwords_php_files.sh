# Title: Interesting Files - passwords in PHP files
# ID: IF_Passwords_php_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching passwords in PHP config and webroot login/auth files
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.001
# Functions Used: print_2title
# Global Variables: $DEBUG, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $PHP_AUTH_FILES
# Fat linpeas: 0
# Small linpeas: 1


PHP_AUTH_FILES=""
if ! [ "$SEARCH_IN_FOLDER" ]; then
  PHP_AUTH_FILES=$(find /var/www -maxdepth 5 -type f -size -1048576c \( -name login.php -o -name auth.php \) -print 2>/dev/null | head -n 40)
fi
if [ "$PSTORAGE_PHP_FILES" ] || [ "$PHP_AUTH_FILES" ] || [ "$DEBUG" ]; then
  print_2title "Searching passwords in PHP files" "T1552.001"
  printf "%s\n%s\n" "$PSTORAGE_PHP_FILES" "$PHP_AUTH_FILES" | sed '/^$/d' | sort -u | while IFS= read -r c; do grep -EiIH "(pwd|passwd|password|PASSWD|PASSWORD|dbuser|dbpass).*[=:].+|define ?\('(\w*passw|\w*user|\w*datab)" "$c" 2>/dev/null | grep -Ev "function|password.*= ?\"\"|password.*= ?''" | sed '/^.\{150\}./d' | sort | uniq | sed -${E} "s,[pP][aA][sS][sS][wW]|[dD][bB]_[pP][aA][sS][sS],${SED_RED},g"; done
  echo ""
fi
