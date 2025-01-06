# Title: Processes & Cron & Services & Timers - System Timers
# ID: PR_System_timers
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: System Timers
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $SEARCH_IN_FOLDER, $timersG
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "System timers"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#timers"
  (systemctl list-timers --all 2>/dev/null | grep -Ev "(^$|timers listed)" | sed -${E} "s,$timersG,${SED_GREEN},") || echo_not_found
  echo ""
fi