# Title: Network Information - MacOS hardware ports
# ID: NT_Macos_hardware_ports
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Enumerate macOS hardware ports
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $EXTRA_CHECKS, $MACPEAS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ] && [ "$EXTRA_CHECKS" ]; then
  print_2title "Hardware Ports"
  networksetup -listallhardwareports
  echo ""

  print_2title "VLANs"
  networksetup -listVLANs
  echo ""

  print_2title "Wifi Info"
  networksetup -getinfo Wi-Fi
  echo ""

  print_2title "Check Enabled Proxies"
  scutil --proxy
  echo ""

  print_2title "Wifi Proxy URL"
  networksetup -getautoproxyurl Wi-Fi
  echo ""
  
  print_2title "Wifi Web Proxy"
  networksetup -getwebproxy Wi-Fi
  echo ""
fi