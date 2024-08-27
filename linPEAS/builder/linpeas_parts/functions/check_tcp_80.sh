# Title: LinPeasBase - execBin
# ID: check_tcp_80
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if TCP Internet conns are available (via port 80)
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


check_tcp_80(){
  (timeout -s KILL 20 /bin/bash -c '( echo >/dev/tcp/1.1.1.1/80 && echo "Port 80 is accessible" || echo "Port 80 is not accessible") 2>/dev/null | grep "accessible"') 2>/dev/null || echo "Port 80 is not accessible"
}