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
# Generated Global Variables: $TIMEOUT_INTERNET_SECONDS_DNS, $local_pid
# Fat linpeas: 0
# Small linpeas: 1

check_dns(){
  local TIMEOUT_INTERNET_SECONDS_DNS=$1
  if ! [ -f "/bin/bash" ]; then
    echo "  /bin/bash not found"
    return
  fi

  # example.com
  (bash -c '((( echo cfc9 0100 0001 0000 0000 0000 0a64 7563 6b64 7563 6b67 6f03 636f 6d00 0001 0001 | xxd -p -r >&3; dd bs=9000 count=1 <&3 2>/dev/null | xxd ) 3>/dev/udp/1.1.1.1/53 && echo "DNS accessible") | grep "accessible" && exit 0 ) 2>/dev/null || echo "DNS is not accessible"') & local_pid=$!

  sleep $TIMEOUT_INTERNET_SECONDS_DNS && kill -9 $local_pid 2>/dev/null && echo "DNS is not accessible"
}