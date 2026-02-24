# Title: LinPeasBase - check_tcp_443
# ID: check_tcp_443
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if TCP Internet conns are available (via port 443)
# License: GNU GPL
# Version: 1.0
# Functions Used: check_tcp_port_access
# Global Variables:
# Initial Functions:
# Generated Global Variables: $TIMEOUT_INTERNET_SECONDS_443
# Fat linpeas: 0
# Small linpeas: 1



check_tcp_443(){
  local TIMEOUT_INTERNET_SECONDS_443=$1
  check_tcp_port_access 443 "$TIMEOUT_INTERNET_SECONDS_443"
}
