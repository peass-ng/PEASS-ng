# Title: Processes & Cron & Services & Timers - Systemd PATH
# ID: PR_Systemd_path
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Systemd PATH
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $SEARCH_IN_FOLDER, $Wfolders
# Initial Functions:
# Generated Global Variables: $WRITABLESYSTEMDPATH
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Systemd PATH"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#systemd-path---relative-paths"
  systemctl show-environment 2>/dev/null | grep "PATH" | sed -${E} "s,$Wfolders\|\./\|\.:\|:\.,${SED_RED_YELLOW},g"
  WRITABLESYSTEMDPATH=$(systemctl show-environment 2>/dev/null | grep "PATH" | grep -E "$Wfolders")
  echo ""
fi