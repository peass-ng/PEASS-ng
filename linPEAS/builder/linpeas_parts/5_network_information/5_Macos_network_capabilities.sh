# Title: Network Information - MacOS network capabilities
# ID: NT_Macos_network_capabilities
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: MacOS network Capabilities
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, warn_exec
# Global Variables: $MACPEAS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ]; then
  print_2title "Network Capabilities"
  warn_exec system_profiler SPNetworkDataType
  echo ""
fi