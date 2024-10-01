# Title: Network Information - Network interfaces
# ID: NT_Network_interfaces
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check network interfaces
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Interfaces"
cat /etc/networks 2>/dev/null
(ifconfig || ip a || (cat /proc/net/dev; cat /proc/net/fib_trie; cat /proc/net/fib_trie6)) 2>/dev/null
echo ""