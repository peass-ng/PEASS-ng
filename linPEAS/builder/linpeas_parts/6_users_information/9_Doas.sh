# Title: Users Information - Doas
# ID: UG_Doas
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check doas configuration and permissions for privilege escalation
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $DEBUG, $nosh_usrs, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables: $doas_dir_name, $doas_bin, $conf_file
# Fat linpeas: 0
# Small linpeas: 1


if [ -f "/etc/doas.conf" ] || [ -f "/usr/local/etc/doas.conf" ] || [ "$DEBUG" ]; then
  print_2title "Doas Configuration"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#doas"

  # Find doas binary and its config locations
  doas_bin=$(command -v doas 2>/dev/null)
  if [ -n "$doas_bin" ]; then
    doas_dir_name=$(dirname "$doas_bin")
    echo "Doas binary found at: $doas_bin" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    
    # Check doas binary permissions
    if [ -u "$doas_bin" ]; then
      echo "Doas binary has SUID bit set!" | sed -${E} "s,.*,${SED_RED},g"
    fi
    ls -l "$doas_bin" 2>/dev/null | sed -${E} "s,.*,${SED_RED_YELLOW},g"
  fi

  # Check all possible doas.conf locations
  echo -e "\nChecking doas.conf files:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
  for conf_file in "/etc/doas.conf" "$doas_dir_name/doas.conf" "$doas_dir_name/../etc/doas.conf" "$doas_dir_name/etc/doas.conf" "/usr/local/etc/doas.conf"; do
    if [ -f "$conf_file" ]; then
      echo "Found: $conf_file" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
      if [ -w "$conf_file" ]; then
        echo "WARNING: $conf_file is writable!" | sed -${E} "s,.*,${SED_RED},g"
      fi
      cat "$conf_file" 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_RED},g" | sed "s,root,${SED_RED},g" | sed "s,nopass,${SED_RED},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed "s,$USER,${SED_RED_YELLOW},g"
    fi
  done

  # Check if doas is working
  if [ -n "$doas_bin" ]; then
    echo -e "\nTesting doas:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    if $doas_bin -l 2>/dev/null; then
      echo "doas -l command works!" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
    fi
  fi
else
  echo_not_found "doas.conf"
fi
echo ""