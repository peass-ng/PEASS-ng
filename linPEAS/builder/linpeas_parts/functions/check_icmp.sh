# Title: LinPeasBase - check_icmp
# ID: check_icmp
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if ICMP is available
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $TIMEOUT_INTERNET_SECONDS_ICMP, $local_pid
# Fat linpeas: 0
# Small linpeas: 1


check_icmp(){
  local TIMEOUT_INTERNET_SECONDS_ICMP=$1
  if ! [ "$(command -v ping 2>/dev/null || echo -n '')" ]; then
    echo "  ping not found"
    return
  fi

  # example.com
  ((ping -c 1 1.1.1.1 2>/dev/null | grep -Ei "1 received|1 packets received" && echo "ICMP is accessible" || echo "ICMP is not accessible" 2>/dev/null) | grep "accessible" && exit 0 ) 2>/dev/null || echo "ICMP is not accessible" & local_pid=$!

  sleep $TIMEOUT_INTERNET_SECONDS_ICMP && kill -9 $local_pid 2>/dev/null && echo "ICMP is not accessible"
}