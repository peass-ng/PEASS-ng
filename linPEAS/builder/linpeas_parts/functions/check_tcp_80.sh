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
# Generated Global Variables: $local_pid, $TIMEOUT_INTERNET_SECONDS_80
# Fat linpeas: 0
# Small linpeas: 1



check_tcp_80(){
  local TIMEOUT_INTERNET_SECONDS_80=$1
  if ! [ -f "/bin/bash" ]; then
    echo "  /bin/bash not found"
    return
  fi

  # example.com
  (bash -c '(echo >/dev/tcp/104.18.74.230/80 2>/dev/null && echo "Port 80 is accessible" && exit 0) 2>/dev/null || echo "Port 80 is not accessible"') & local_pid=$!

  sleep $TIMEOUT_INTERNET_SECONDS_80 && kill -9 $local_pid 2>/dev/null && echo "Port 80 is not accessible"
}