# Title: Network Information - Open ports
# ID: NT_Open_ports
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Enumerate open ports
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Active Ports"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#open-ports"
( (netstat -punta || ss -nltpu || netstat -anv) | grep -i listen) 2>/dev/null | sed -${E} "s,127.0.[0-9]+.[0-9]+|:::|::1:|0\.0\.0\.0,${SED_RED},g"
echo ""
