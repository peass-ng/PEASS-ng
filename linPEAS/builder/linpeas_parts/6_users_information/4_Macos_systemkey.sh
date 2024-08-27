# Title: Users Information - Macos systemKey
# ID: UG_Macos_systemkey
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get macOS systemKey
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $MACPEAS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ];then
  print_2title "SystemKey"
  ls -l /var/db/SystemKey
  if [ -r "/var/db/SystemKey" ]; then
    echo "You can read /var/db/SystemKey" | sed -${E} "s,.*,${SED_RED_YELLOW},";
    hexdump -s 8 -n 24 -e '1/1 "%.2x"' /var/db/SystemKey | sed -${E} "s,.*,${SED_RED_YELLOW},";
  fi
  echo ""
fi