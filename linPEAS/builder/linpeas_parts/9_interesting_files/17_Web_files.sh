# Title: Interesting Files - Web files
# ID: IF_Web_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Web files
# License: GNU GPL
# Version: 1.0
# Functions Used:  print_2title
# Global Variables: $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Web files?(output limit)"
  ls -alhR /var/www/ 2>/dev/null | head
  ls -alhR /srv/www/htdocs/ 2>/dev/null | head
  ls -alhR /usr/local/www/apache22/data/ 2>/dev/null | head
  ls -alhR /opt/lampp/htdocs/ 2>/dev/null | head
  echo ""
fi
