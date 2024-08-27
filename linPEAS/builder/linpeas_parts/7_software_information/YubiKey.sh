# Title: Software Information - YubiKey athentication
# ID: SI_YubiKey
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: YubiKey athentication
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG, $IAMROOT
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if (grep "auth=" /etc/login.conf 2>/dev/null | grep -v "^#" | grep -q yubikey) || [ "$DEBUG" ]; then
  print_2title "YubiKey authentication"
  printf "System supports$RED YubiKey authentication\n"
  if ! [ "$IAMROOT" ] && [ -w /var/db/yubikey/ ]; then
    echo "${RED}/var/db/yubikey/ is writable by you"
    ls -ld /var/db/yubikey/
  else
    ls -ld /var/db/yubikey/ 2>/dev/null
  fi
  echo ""
fi