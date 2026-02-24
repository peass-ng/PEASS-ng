# Title: LinPeasBase - execBin
# ID: check_tcp_80
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if TCP Internet conns are available (via port 80)
# License: GNU GPL
# Version: 1.0
# Functions Used: check_tcp_port_access
# Global Variables:
# Initial Functions:
# Generated Global Variables: $TIMEOUT_INTERNET_SECONDS_80
# Fat linpeas: 0
# Small linpeas: 1



check_tcp_80(){
  local TIMEOUT_INTERNET_SECONDS_80=$1
  check_tcp_port_access 80 "$TIMEOUT_INTERNET_SECONDS_80"
}
