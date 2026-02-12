# Title: Interesting Permissions Files - Users with capabilities
# ID: IP_Users_with_capabilities
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Users with capabilities
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $capsB, $DEBUG, $knw_usrs, $nosh_usrs, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables: $pam_cap_lines
# Fat linpeas: 0
# Small linpeas: 0


if [ -f "/etc/security/capability.conf" ] || [ "$DEBUG" ] || grep -Rqs "pam_cap\.so" /etc/pam.d /etc/pam.conf 2>/dev/null; then
  print_2title "Users with capabilities"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#capabilities"
  if [ -f "/etc/security/capability.conf" ]; then
    grep -v '^#\|none\|^$' /etc/security/capability.conf 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED}," | sed -${E} "s,$capsB,${SED_RED},g"
  else echo_not_found "/etc/security/capability.conf"
  fi
  echo ""
  print_info "Checking if PAM loads pam_cap.so"
  pam_cap_lines=$(grep -RIn "pam_cap\.so" /etc/pam.d /etc/pam.conf 2>/dev/null)
  if [ "$pam_cap_lines" ]; then
    printf "%s\n" "$pam_cap_lines" | sed -${E} "s,pam_cap\\.so,${SED_RED_YELLOW},g"
  else
    echo_not_found "pam_cap.so in /etc/pam.d or /etc/pam.conf"
  fi
  echo ""
fi
