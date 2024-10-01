# Title: Processes & Cron & Services & Timers - Services
# ID: PR_Services
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Services outdated versions
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $EXTRA_CHECKS, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ "$EXTRA_CHECKS" ]; then
    print_2title "Services"
    print_info "Search for outdated versions"
    (service --status-all || service -e || chkconfig --list || rc-status || launchctl list) 2>/dev/null || echo_not_found "service|chkconfig|rc-status|launchctl"
    echo ""
  fi
fi