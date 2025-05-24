# Title: LinPeasBase - check_icmp
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
  (ping -c 1 1.1.1.1 | grep -E "1 received|1 packets received" && echo "Ping is available" || echo "Ping is not available" 2>/dev/null) | grep -i "available"
}