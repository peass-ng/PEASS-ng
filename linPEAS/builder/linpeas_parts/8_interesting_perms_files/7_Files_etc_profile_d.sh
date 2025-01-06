# Title: Interesting Permissions Files - /etc/profile.d/
# ID: IP_Files_etc_profile_d
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Files (scripts) in /etc/profile.d/
# License: GNU GPL
# Version: 1.0
# Functions Used: check_critial_root_path, echo_not_found, print_2title, print_info
# Global Variables: $IAMROOT, $MACPEAS, $profiledG, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Files (scripts) in /etc/profile.d/"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#profiles-files"
  if [ ! "$MACPEAS" ] && ! [ "$IAMROOT" ]; then #Those folders don´t exist on a MacOS
    (ls -la /etc/profile.d/ 2>/dev/null | sed -${E} "s,$profiledG,${SED_GREEN},") || echo_not_found "/etc/profile.d/"
    check_critial_root_path "/etc/profile"
    check_critial_root_path "/etc/profile.d/"
  fi
  echo ""
fi