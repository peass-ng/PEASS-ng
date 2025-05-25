# Title: Network Information - Internet access
# ID: NT_Internet_access
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check for internet access
# License: GNU GPL
# Version: 1.0
# Functions Used: check_dns, check_icmp, check_tcp_443, check_tcp_443_bin, check_tcp_80, print_2title, check_external_hostname
# Global Variables:
# Initial Functions:
# Generated Global Variables: $pid4, $pid2, $pid1, $pid3, $$tcp443_bin_status, $NOT_CHECK_EXTERNAL_HOSTNAME, $TIMEOUT_INTERNET_SECONDS
# Fat linpeas: 0
# Small linpeas: 0



print_2title "Internet Access?"

TIMEOUT_INTERNET_SECONDS=5

if [ "$SUPERFAST" ]; then
  TIMEOUT_INTERNET_SECONDS=2.5
fi


# Run all checks in background
check_tcp_80 "$TIMEOUT_INTERNET_SECONDS" 2>/dev/null & pid1=$!
check_tcp_443 "$TIMEOUT_INTERNET_SECONDS" 2>/dev/null & pid2=$!
check_icmp "$TIMEOUT_INTERNET_SECONDS" 2>/dev/null & pid3=$!
check_dns "$TIMEOUT_INTERNET_SECONDS" 2>/dev/null & pid4=$!

# Kill all after 10 seconds
(sleep $(( $TIMEOUT_INTERNET_SECONDS + 1 )) && kill -9 $pid1 $pid2 $pid3 $pid4 2>/dev/null) &

check_tcp_443_bin $TIMEOUT_INTERNET_SECONDS 2>/dev/null
tcp443_bin_status=$?

wait $pid1 $pid2 $pid3 $pid4 2>/dev/null


# Wait for all to finish
wait 2>/dev/null

if [ "$tcp443_bin_status" -eq 0 ] && \
   [ -z "$SUPERFAST" ] && [ -z "$NOT_CHECK_EXTERNAL_HOSTNAME" ]; then
  echo ""
  print_2title "Is hostname malicious or leaked?"
  print_info "This will check the public IP and hostname in known malicious lists and leaks to find any relevant information about the host."
  check_external_hostname 2>/dev/null
fi

echo ""
