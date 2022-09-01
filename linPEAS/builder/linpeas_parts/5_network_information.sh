###########################################
#---------) Network Information (---------#
###########################################

if [ "$MACOS" ]; then
  print_2title "Network Capabilities"
  warn_exec system_profiler SPNetworkDataType
  echo ""
fi

#-- NI) Hostname, hosts and DNS
print_2title "Hostname, hosts and DNS"
cat /etc/hostname /etc/hosts /etc/resolv.conf 2>/dev/null | grep -v "^#" | grep -Ev "\W+\#|^#" 2>/dev/null
warn_exec dnsdomainname 2>/dev/null
echo ""

#-- NI) /etc/inetd.conf
if [ "$EXTRA_CHECKS" ]; then
  print_2title "Content of /etc/inetd.conf & /etc/xinetd.conf"
  (cat /etc/inetd.conf /etc/xinetd.conf 2>/dev/null | grep -v "^$" | grep -Ev "\W+\#|^#" 2>/dev/null) || echo_not_found "/etc/inetd.conf"
  echo ""
fi

#-- NI) Interfaces
print_2title "Interfaces"
cat /etc/networks 2>/dev/null
(ifconfig || ip a) 2>/dev/null
echo ""

#-- NI) Neighbours
if [ "$EXTRA_CHECKS" ]; then
  print_2title "Networks and neighbours"
  if [ "$MACOS" ]; then
    netstat -rn 2>/dev/null
  else
    (route || ip n || cat /proc/net/route) 2>/dev/null
  fi
  (arp -e || arp -a || cat /proc/net/arp) 2>/dev/null
  echo ""
fi

if [ "$MACPEAS" ]; then
  print_2title "Firewall status"
  warn_exec system_profiler SPFirewallDataType
fi

#-- NI) Iptables
if [ "$EXTRA_CHECKS" ]; then
  print_2title "Iptables rules"
  (timeout 1 iptables -L 2>/dev/null; cat /etc/iptables/* | grep -v "^#" | grep -Ev "\W+\#|^#" 2>/dev/null) 2>/dev/null || echo_not_found "iptables rules"
  echo ""
fi

#-- NI) Ports
print_2title "Active Ports"
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#open-ports"
( (netstat -punta || ss -nltpu || netstat -anv) | grep -i listen) 2>/dev/null | sed -${E} "s,127.0.[0-9]+.[0-9]+|:::|::1:|0\.0\.0\.0,${SED_RED},"
echo ""

#-- NI) MacOS hardware ports
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

  print_2title "Wifi FTP Proxy"
  networksetup -getftpproxy Wi-Fi
  echo ""
fi

#-- NI) tcpdump
print_2title "Can I sniff with tcpdump?"
timeout 1 tcpdump >/dev/null 2>&1
if [ $? -eq 124 ]; then #If 124, then timed out == It worked
    print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#sniffing"
    echo "You can sniff with tcpdump!" | sed -${E} "s,.*,${SED_RED},"
else echo_no
fi
echo ""

#-- NI) Internet access
if [ "$AUTO_NETWORK_SCAN" ] && [ "$TIMEOUT" ] && [ -f "/bin/bash" ]; then
  print_2title "Internet Access?"
  check_tcp_80 2>/dev/null &
  check_tcp_443 2>/dev/null &
  check_icmp 2>/dev/null &
  check_dns 2>/dev/null &
  wait
  echo ""
fi

if [ "$AUTO_NETWORK_SCAN" ]; then
  if ! [ "$FOUND_NC" ] && ! [ "$FOUND_BASH" ]; then
    printf $RED"[-] $SCAN_BAN_BAD\n$NC"
    echo "The network is not going to be scanned..."
  
  elif ! [ "$(command -v ifconfig)" ] && ! [ "$(command -v ip a)" ]; then
    printf $RED"[-] No ifconfig or ip commands, cannot find local ips\n$NC"
    echo "The network is not going to be scanned..."
  
  else
    print_2title "Scanning local networks (using /24)"

    if ! [ "$PING" ] && ! [ "$FPING" ]; then
      printf $RED"[-] $DISCOVER_BAN_BAD\n$NC"
    fi

    select_nc
    local_ips=$( (ip a 2>/dev/null || ifconfig) | grep -Eo 'inet[^6]\S+[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}' | awk '{print $2}' | grep -E "^10\.|^172\.|^192\.168\.|^169\.254\.")
    printf "%s\n" "$local_ips" | while read local_ip; do
      if ! [ -z "$local_ip" ]; then
        print_3title "Discovering hosts in $local_ip/24"
        
        if [ "$PING" ] || [ "$FPING" ]; then
          discover_network "$local_ip/24" | sed 's/\x1B\[[0-9;]\{1,\}[A-Za-z]//g' | grep -A 256 "Network Discovery" | grep -v "Network Discovery" | grep -Eo '[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}' > $Wfolder/.ips.tmp
        fi
        
        discovery_port_scan "$local_ip/24" 22 | sed 's/\x1B\[[0-9;]\{1,\}[A-Za-z]//g' | grep -A 256 "Ports going to be scanned" | grep -v "Ports going to be scanned" | grep -Eo '[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}' >> $Wfolder/.ips.tmp
        
        sort $Wfolder/.ips.tmp | uniq > $Wfolder/.ips
        rm $Wfolder/.ips.tmp 2>/dev/null
        
        while read disc_ip; do
          me=""
          if [ "$disc_ip" = "$local_ip" ]; then
            me=" (local)"
          fi
          
          echo "Scanning top ports of ${disc_ip}${me}"
          (tcp_port_scan "$disc_ip" "" | grep -A 1000 "Ports going to be scanned" | grep -v "Ports going to be scanned" | sort | uniq) 2>/dev/null
          echo ""
        done < $Wfolder/.ips
        
        rm $Wfolder/.ips 2>/dev/null
        echo ""
      fi
    done
    
    print_3title "Scanning top ports of host.docker.internal"
    (tcp_port_scan "host.docker.internal" "" | grep -A 1000 "Ports going to be scanned" | grep -v "Ports going to be scanned" | sort | uniq) 2>/dev/null
    echo ""
  fi
fi

if [ "$MACOS" ]; then
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
