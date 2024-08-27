# Title: Software Information - S/Key athentication
# ID: SI_SKey
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: S/Key athentication
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG, $IAMROOT
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if (grep auth= /etc/login.conf 2>/dev/null | grep -v "^#" | grep -q skey) || [ "$DEBUG" ] ; then
  print_2title "S/Key authentication"
  printf "System supports$RED S/Key$NC authentication\n"
  if ! [ -d /etc/skey/ ]; then
    echo "${GREEN}S/Key authentication enabled, but has not been initialized"
  elif ! [ "$IAMROOT" ] && [ -w /etc/skey/ ]; then
    echo "${RED}/etc/skey/ is writable by you"
    ls -ld /etc/skey/
  else
    ls -ld /etc/skey/ 2>/dev/null
  fi
  echo ""
fi
