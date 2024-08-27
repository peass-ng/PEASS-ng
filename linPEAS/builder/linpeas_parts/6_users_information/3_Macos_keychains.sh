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
  print_info "https://book.hacktricks.xyz/macos/macos-security-and-privilege-escalation#chainbreaker"
  security list-keychains
  echo ""
fi