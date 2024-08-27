# Title: Network Information - Iptables
# ID: NT_Iptables
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Enumerate iptables rules
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title
# Global Variables: $EXTRA_CHECKS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if [ "$EXTRA_CHECKS" ]; then
  print_2title "Iptables rules"
  (timeout 1 iptables -L 2>/dev/null; cat /etc/iptables/* | grep -v "^#" | grep -Ev "\W+\#|^#" 2>/dev/null) 2>/dev/null || echo_not_found "iptables rules"
  echo ""
fi