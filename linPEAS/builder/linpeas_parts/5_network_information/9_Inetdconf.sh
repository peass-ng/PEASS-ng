# Title: Network Information - Inetconf
# ID: NT_Inetdconf
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check content of /etc/inetd.conf & /etc/xinetd.conf
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title 
# Global Variables: $EXTRA_CHECKS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$EXTRA_CHECKS" ]; then
  print_2title "Content of /etc/inetd.conf & /etc/xinetd.conf"
  (cat /etc/inetd.conf /etc/xinetd.conf 2>/dev/null | grep -v "^$" | grep -Ev "\W+\#|^#" 2>/dev/null) || echo_not_found "/etc/inetd.conf"
  echo ""
fi