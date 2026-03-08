# Title: Network Information - MacOS hardware ports
# ID: NT_Macos_hardware_ports
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Enumerate macOS hardware ports
# License: GNU GPL
# Version: 1.0
# Mitre: T1016
# Functions Used: print_2title
# Global Variables: $EXTRA_CHECKS, $MACPEAS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ] && [ "$EXTRA_CHECKS" ]; then
  print_2title "Hardware Ports" "T1016"
  networksetup -listallhardwareports
  echo ""

  print_2title "VLANs" "T1016"
  networksetup -listVLANs
  echo ""

  print_2title "Wifi Info" "T1016"
  networksetup -getinfo Wi-Fi
  echo ""

  print_2title "Check Enabled Proxies" "T1016"
  scutil --proxy
  echo ""

  print_2title "Wifi Proxy URL" "T1016"
  networksetup -getautoproxyurl Wi-Fi
  echo ""
  
  print_2title "Wifi Web Proxy" "T1016"
  networksetup -getwebproxy Wi-Fi
  echo ""
fi