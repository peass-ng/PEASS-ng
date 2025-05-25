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
# Generated Global Variables: $pid, $pids
# Fat linpeas: 0
# Small linpeas: 1


check_tcp_80(){
  if ! [ -f "/bin/bash" ]; then
    echo "  /bin/bash not found"
    return
  fi

  /bin/bash -c '
      for ip in 1.1.1.1; do
        (echo >/dev/tcp/$ip/80 && echo "Port 80 is accessible" && exit 0) &
        pids+=($!)
      done
      for pid in ${pids[@]}; do
        wait $pid && exit 0
      done
      echo "Port 80 is not accessible"
    ' 2>/dev/null | grep "accessible"
}