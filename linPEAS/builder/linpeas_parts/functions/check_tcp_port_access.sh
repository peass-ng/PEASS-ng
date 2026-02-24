# Title: LinPeasBase - check_tcp_port_access
# ID: check_tcp_port_access
# Author: Carlos Polop
# Last Update: 24-02-2026
# Description: Check if a TCP port is accessible
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $local_pid, $PORT_TO_CHECK, $TIMEOUT_INTERNET_SECONDS_PORT
# Fat linpeas: 0
# Small linpeas: 1


check_tcp_port_access(){
  local PORT_TO_CHECK=$1
  local TIMEOUT_INTERNET_SECONDS_PORT=$2
  if ! [ -f "/bin/bash" ]; then
    echo "  /bin/bash not found"
    return
  fi

  # example.com
  (bash -c "(echo >/dev/tcp/104.18.74.230/$PORT_TO_CHECK 2>/dev/null && echo \"Port $PORT_TO_CHECK is accessible\" && exit 0) 2>/dev/null || echo \"Port $PORT_TO_CHECK is not accessible\"") & local_pid=$!

  sleep $TIMEOUT_INTERNET_SECONDS_PORT && kill -9 $local_pid 2>/dev/null && echo "Port $PORT_TO_CHECK is not accessible"
}
