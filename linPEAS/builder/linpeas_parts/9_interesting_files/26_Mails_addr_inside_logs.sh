# Title: Interesting Files - Emails inside logs
# ID: IF_Mails_addr_inside_logs
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Emails inside logs
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG, $knw_emails, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$DEBUG" ] || ( ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && ! [ "$SEARCH_IN_FOLDER" ] ); then
  print_2title "Searching emails inside logs (limit 70)"
  (find /var/log/ /var/logs/ /private/var/log -type f -exec grep -I -R -E -o "\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,6}\b" "{}" \;) 2>/dev/null | sort | uniq -c | sort -r -n | head -n 70 | sed -${E} "s,$knw_emails,${SED_GREEN},g"
  echo ""
fi