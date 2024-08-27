# Title: Users Information - Doas
# ID: UG_Doas
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Checking doas.conf
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title
# Global Variables: $DEBUG, $nosh_usrs,  $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables: $doas_dir_name
# Fat linpeas: 0
# Small linpeas: 1


if [ -f "/etc/doas.conf" ] || [ "$DEBUG" ]; then
  print_2title "Checking doas.conf"
  doas_dir_name=$(dirname "$(command -v doas || echo -n '')" 2>/dev/null)
  if [ "$(cat /etc/doas.conf $doas_dir_name/doas.conf $doas_dir_name/../etc/doas.conf $doas_dir_name/etc/doas.conf 2>/dev/null)" ]; then
    cat /etc/doas.conf "$doas_dir_name/doas.conf" "$doas_dir_name/../etc/doas.conf" "$doas_dir_name/etc/doas.conf" 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_RED}," | sed "s,root,${SED_RED}," | sed "s,nopass,${SED_RED}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed "s,$USER,${SED_RED_YELLOW},"
  else echo_not_found "doas.conf"
  fi
  echo ""
fi