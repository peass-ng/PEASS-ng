# Title: Network Information - MacOS Network Services
# ID: NT_Macos_network_services
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Enumerate macos network services
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, warn_exec
# Global Variables: $EXTRA_CHECKS, $MACPEAS
# Initial Functions:
# Generated Global Variables: $rmMgmt, $scrShrng, $flShrng, $rLgn, $rAE, $bmM
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ]; then
  print_2title "Any MacOS Sharing Service Enabled?"
  rmMgmt=$(netstat -na | grep LISTEN | grep tcp46 | grep "*.3283" | wc -l);
  scrShrng=$(netstat -na | grep LISTEN | grep -E 'tcp4|tcp6' | grep "*.5900" | wc -l);
  flShrng=$(netstat -na | grep LISTEN | grep -E 'tcp4|tcp6' | grep -E "\*.88|\*.445|\*.548" | wc -l);
  rLgn=$(netstat -na | grep LISTEN | grep -E 'tcp4|tcp6' | grep "*.22" | wc -l);
  rAE=$(netstat -na | grep LISTEN | grep -E 'tcp4|tcp6' | grep "*.3031" | wc -l);
  bmM=$(netstat -na | grep LISTEN | grep -E 'tcp4|tcp6' | grep "*.4488" | wc -l);
  printf "\nThe following services are OFF if '0', or ON otherwise:\nScreen Sharing: %s\nFile Sharing: %s\nRemote Login: %s\nRemote Mgmt: %s\nRemote Apple Events: %s\nBack to My Mac: %s\n\n" "$scrShrng" "$flShrng" "$rLgn" "$rmMgmt" "$rAE" "$bmM";
  echo ""
  print_2title "VPN Creds"
  system_profiler SPNetworkLocationDataType | grep -A 5 -B 7 ": Password"  | sed -${E} "s,Password|Authorization Name.*,${SED_RED},"
  echo ""
  print_2title "Firewall status"
  warn_exec system_profiler SPFirewallDataType
  echo ""

  if [ "$EXTRA_CHECKS" ]; then
    print_2title "Bluetooth Info"
    warn_exec system_profiler SPBluetoothDataType
    echo ""

    print_2title "Ethernet Info"
    warn_exec system_profiler SPEthernetDataType
    echo ""

    print_2title "USB Info"
    warn_exec system_profiler SPUSBDataType
    echo ""
  fi
fi