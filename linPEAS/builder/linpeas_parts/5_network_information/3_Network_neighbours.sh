# Title: Network Information - Network neighbours
# ID: NT_Network_neighbours
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Networks and neighbours
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $EXTRA_CHECKS, $MACPEAS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$EXTRA_CHECKS" ]; then
  print_2title "Networks and neighbours"
  if [ "$MACPEAS" ]; then
    netstat -rn 2>/dev/null
  else
    (route || ip n || cat /proc/net/route) 2>/dev/null
  fi
  (arp -e || arp -a || cat /proc/net/arp) 2>/dev/null
  echo ""
fi
