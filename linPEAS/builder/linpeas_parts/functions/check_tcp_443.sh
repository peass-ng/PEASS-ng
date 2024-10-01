# Title: LinPeasBase - check_tcp_443
# ID: check_tcp_443
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if TCP Internet conns are available (via port 443)
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


check_tcp_443(){
  (timeout -s KILL 20 /bin/bash -c '(echo >/dev/tcp/1.1.1.1/443 && echo "Port 443 is accessible" || echo "Port 443 is not accessible") 2>/dev/null | grep "accessible"') 2>/dev/null || echo "Port 443 is not accessible"
}