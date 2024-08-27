# Title: Interesting Files - Macos Unsigned Applications
# ID: IF_Macos_unsigned_apps
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get the macOS unsigned applications
# License: GNU GPL
# Version: 1.0
# Functions Used: macosNotSigned, print_2title
# Global Variables: $MACPEAS 
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ]; then
  print_2title "Unsigned Applications"
  macosNotSigned /System/Applications
fi