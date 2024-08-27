# Title: Interesting Files - Mail applications
# ID: IF_Mail_apps
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Mail applications
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $mail_apps, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Searching installed mail applications"
  ls /bin /sbin /usr/bin /usr/sbin /usr/local/bin /usr/local/sbin /etc 2>/dev/null | grep -Ewi "$mail_apps" | sort | uniq
  echo ""
fi