# Title: Network Information - Hostname, hosts and DNS
# ID: NT_Hostname_hosts_dns
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get hostname, hosts and DNS
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, warn_exec
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Hostname, hosts and DNS"
cat /etc/hostname /etc/hosts /etc/resolv.conf 2>/dev/null | grep -v "^#" | grep -Ev "\W+\#|^#" 2>/dev/null
warn_exec dnsdomainname 2>/dev/null
echo ""