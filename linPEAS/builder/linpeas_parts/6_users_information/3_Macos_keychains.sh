# Title: Users Information - Macos systemKey
# ID: UG_Macos_keychains
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
  print_2title "Keychains"
  print_info "https://book.hacktricks.wiki/en/macos-hardening/macos-security-and-privilege-escalation/macos-files-folders-and-binaries/macos-sensitive-locations.html#chainbreaker"
  security list-keychains
  echo ""
fi