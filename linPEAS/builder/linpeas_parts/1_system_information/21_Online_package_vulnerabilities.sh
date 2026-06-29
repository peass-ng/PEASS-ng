# Title: System Information - Package Vulnerabilities
# ID: SY_Online_package_vulnerabilities
# Author: Carlos Polop
# Last Update: 29-06-2026
# Description: Print package vulnerabilities returned by the optional HackTricks online package lookup.
# License: GNU GPL
# Version: 1.0
# Mitre: T1082
# Functions Used: print_2title, print_info, linpeas_print_package_vulnerabilities
# Global Variables: $ONLINE_VULN_CHECKS, $NOT_CHECK_EXTERNAL_HOSTNAME
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

if [ "$ONLINE_VULN_CHECKS" ] && [ -z "$NOT_CHECK_EXTERNAL_HOSTNAME" ]; then
  print_2title "Package Vulnerabilities" "T1082"
  print_info "This uses the optional HackTricks online lookup enabled with -V or -a. Output is capped at 50 vulnerable packages."
  linpeas_print_package_vulnerabilities
  echo ""
fi
