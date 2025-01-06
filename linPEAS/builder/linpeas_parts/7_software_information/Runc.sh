# Title: Software Information - Runc
# ID: SI_Runc
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Runc
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $runc
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  runc=$(command -v runc || echo -n '')
  if [ "$runc" ] || [ "$DEBUG" ]; then
    print_2title "Checking if runc is available"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#runc--privilege-escalation"
    if [ "$runc" ]; then
      echo "runc was found in $runc, you may be able to escalate privileges with it" | sed -${E} "s,.*,${SED_RED},"
    fi
    echo ""
  fi
fi