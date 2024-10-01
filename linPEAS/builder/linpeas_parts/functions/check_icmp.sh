# Title: LinPeasBase - check_tcp_443
# ID: check_icmp
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if ICMP is available
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


check_icmp(){
  (timeout -s KILL 20 /bin/bash -c '(ping -c 1 1.1.1.1 | grep "1 received" && echo "Ping is available" || echo "Ping is not available") 2>/dev/null | grep "available"') 2>/dev/null || echo "Ping is not available"
}