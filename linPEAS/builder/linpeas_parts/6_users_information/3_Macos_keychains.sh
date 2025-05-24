# Title: Users Information - MacOS Keychains
# ID: UG_Macos_keychains
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get macOS keychains information
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $MACPEAS
# Initial Functions:
# Generated Global Variables: $user_home
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ];then
  print_2title "Keychains"
  print_info "https://book.hacktricks.wiki/en/macos-hardening/macos-security-and-privilege-escalation/macos-files-folders-and-binaries/macos-sensitive-locations.html#chainbreaker"
  echo "System Keychains:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
  security list-keychains 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
  echo -e "\nUser Keychains:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
  for user_home in /Users/*/; do
    if [ -d "${user_home}Library/Keychains" ]; then
      echo "- User: $(basename "$user_home")" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
      ls -la "${user_home}Library/Keychains/" 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
    fi
  done
  echo ""
fi