# Title: Network Information - Internet access
# ID: NT_Internet_access
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check for internet access
# License: GNU GPL
# Version: 1.0
# Functions Used: check_dns, check_icmp, check_tcp_443, check_tcp_80, print_2title
# Global Variables: $FAST, $TIMEOUT
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$FAST" ] && [ "$TIMEOUT" ] && [ -f "/bin/bash" ]; then
  print_2title "Internet Access?"
  check_tcp_80 2>/dev/null &
  check_tcp_443 2>/dev/null &
  check_icmp 2>/dev/null &
  check_dns 2>/dev/null &
  wait
  echo ""
fi
