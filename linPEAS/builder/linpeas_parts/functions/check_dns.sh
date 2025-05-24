# Title: LinPeasBase - check_dns
# ID: check_dns
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the DNS is available
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $pid, $pids
# Fat linpeas: 0
# Small linpeas: 1


check_dns(){
  if ! [ -f "/bin/bash" ]; then
    echo "  /bin/bash not found"
    return
  fi

  /bin/bash -c '
      for ip in 1.1.1.1 8.8.8.8 ; do
        (( echo cfc9 0100 0001 0000 0000 0000 0a64 7563 6b64 7563 6b67 6f03 636f 6d00 0001 0001 | xxd -p -r >&3; dd bs=9000 count=1 <&3 2>/dev/null | xxd ) 3>/dev/udp/$ip/53 && echo "DNS available" && exit 0) &
        pids+=($!)
      done
      for pid in ${pids[@]}; do
        wait $pid && exit 0
      done
      echo "DNS not available"
    ' 2>/dev/null | grep "available" || echo "DNS not available"
}