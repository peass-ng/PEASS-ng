# Title: Interesting Files - Check if Network jobs
# ID: BS_check_network_jobs
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: If Network jobs, just execute that, don't execute the rest of the script
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $DISCOVER_BAN_GOOD, $FOUND_BASH, $HELP, $IP, $PORT_FORWARD, $PORTS, $SCAN_BAN_GOOD, $Wfolder
# Initial Functions:
# Generated Global Variables: $LOCAL_IP, $LOCAL_PORT, $REMOTE_IP, $REMOTE_PORT, $NETMASK, $NEWIP, $MYPORTS, $NC_SCAN, $IP3, $local_ips, $local_ip, $disc_ip
# Fat linpeas: 0
# Small linpeas: 1


basic_net_info(){
  print_title "Basic Network Info"
  (ifconfig || ip a) 2>/dev/null
  echo ""
}


port_forward (){
  LOCAL_IP=$1
  LOCAL_PORT=$2
  REMOTE_IP=$3
  REMOTE_PORT=$4

  echo "In your machine execute:"
  echo "cd /tmp; rm backpipe; mknod backpipe p;"
  echo "nc -lvnp $LOCAL_PORT 0<backpipe | nc -lvnp 9009 1>backpipe"
  echo ""
  read -p "Press any key when you have executed those commands" useless_var

  bash -c "exec 3<>/dev/tcp/$REMOTE_IP/$REMOTE_PORT; exec 4<>/dev/tcp/$LOCAL_IP/9009; cat <&3 >&4 & cat <&4 >&3 &"
  echo "If not error was indicated, your host port $LOCAL_PORT should be forwarded to $REMOTE_IP:$REMOTE_PORT"
}


select_nc (){
  #Select the correct configuration of the netcat found
  NC_SCAN="$FOUND_NC -v -n -z -w 1"
  $($NC_SCAN 127.0.0.1 65321 > /dev/null 2>&1)
  if [ $? -eq 2 ]
  then
    NC_SCAN="timeout 1 $FOUND_NC -v -n"
  fi
}


icmp_recon (){
  #Discover hosts inside a /24 subnetwork using ping (start pingging broadcast addresses)
	IP3=$(echo $1 | cut -d "." -f 1,2,3)

  (timeout 1 ping -b -c 1 "$IP3.255" 2>/dev/null | grep "icmp_seq" | sed -${E} "s,[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+,${SED_RED},") &
  (timeout 1 ping -b -c 1 "255.255.255.255" 2>/dev/null | grep "icmp_seq" | sed -${E} "s,[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+,${SED_RED},") &
	for j in $(seq 0 254)
	do
    (timeout 1 ping -b -c 1 "$IP3.$j" 2>/dev/null | grep "icmp_seq" | sed -${E} "s,[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+,${SED_RED},") &
	done
  wait
}


tcp_recon (){
  #Discover hosts inside a /24 subnetwork using tcp connection to most used ports and selected ones
  IP3=$(echo $1 | cut -d "." -f 1,2,3)
	PORTS=$2
  printf ${YELLOW}"[+]${BLUE} Ports going to be scanned: $PORTS" $NC | tr '\n' " "
  printf "$NC\n"

  for p in $PORTS; do
    for j in $(seq 1 254)
    do
      if [ "$FOUND_BASH" ] && [ "$(command -v timeout 2>/dev/null || echo -n '')" ]; then
        timeout 2.5 $FOUND_BASH -c "(echo </dev/tcp/$IP3.$j/$p) 2>/dev/null && echo -e \"\n[+] Open port at: $IP3.$j:$p\"" &
      elif [ "$NC_SCAN" ]; then
        ($NC_SCAN "$IP3"."$j" "$p" 2>&1 | grep -iv "Connection refused\|No route\|Version\|bytes\| out" | sed -${E} "s,[0-9\.],${SED_RED},g") &
      fi
    done
    wait
  done
}


discovery_port_scan (){
  basic_net_info

  #Check if IP and Netmask are correct and the use nc to find hosts. By default check ports: 22 80 443 445 3389
  print_title "Internal Network Discovery - Finding hosts and scanning ports"
  DISCOVERY=$1
  MYPORTS=$2

  IP=$(echo "$DISCOVERY" | cut -d "/" -f 1)
  NETMASK=$(echo "$DISCOVERY" | cut -d "/" -f 2)
  echo "Scanning: $DISCOVERY"

  if [ -z "$IP" ] || [ -z "$NETMASK" ] || [ "$IP" = "$NETMASK" ]; then
    printf $RED"[-] Err: Bad format. Example: 127.0.0.1/24\n"$NC;
    if [ "$IP" = "$NETMASK" ]; then
      printf $RED"[*] This options is used to find active hosts by scanning ports. If you want to perform a port scan of a host use the options: ${YELLOW}-i <IP> [-p <PORT(s)>]\n\n"$NC;
    fi
    printf ${BLUE}"$HELP"$NC;
    exit 0
  fi

  PORTS="22 80 443 445 3389 $(echo $MYPORTS | tr ',' ' ')"
  PORTS=$(echo "$PORTS" | tr " " "\n" | sort -u) #Delete repetitions

  if [ "$NETMASK" -eq "24" ]; then
    printf ${YELLOW}"[+]$GREEN Netmask /24 detected, starting...\n" $NC
		tcp_recon "$IP" "$PORTS"

	elif [ "$NETMASK" -eq "16" ]; then
    printf ${YELLOW}"[+]$GREEN Netmask /16 detected, starting...\n" $NC
		for i in $(seq 0 255)
		do
			NEWIP=$(echo "$IP" | cut -d "." -f 1,2).$i.1
			tcp_recon "$NEWIP" "$PORTS"
		done
  else
      printf $RED"[-] Err: Sorry, only netmask /24 and /16 are supported in port discovery mode. Netmask detected: $NETMASK\n"$NC;
      exit 0
	fi
}


tcp_port_scan (){
  #Scan open ports of a host. Default: nmap top 1000, but the user can select others
  basic_net_info

  print_title "Network Port Scanning"
  IP=$1
	PORTS="$2"

  if [ -z "$PORTS" ]; then
    printf ${YELLOW}"[+]${BLUE} Ports going to be scanned: DEFAULT (nmap top 1000)" $NC | tr '\n' " "
    printf "$NC\n"
    PORTS="1 3 4 6 7 9 13 17 19 20 21 22 23 24 25 26 30 32 33 37 42 43 49 53 70 79 80 81 82 83 84 85 88 89 90 99 100 106 109 110 111 113 119 125 135 139 143 144 146 161 163 179 199 211 212 222 254 255 256 259 264 280 301 306 311 340 366 389 406 407 416 417 425 427 443 444 445 458 464 465 481 497 500 512 513 514 515 524 541 543 544 545 548 554 555 563 587 593 616 617 625 631 636 646 648 666 667 668 683 687 691 700 705 711 714 720 722 726 749 765 777 783 787 800 801 808 843 873 880 888 898 900 901 902 903 911 912 981 987 990 992 993 995 999 1000 1001 1002 1007 1009 1010 1011 1021 1022 1023 1024 1025 1026 1027 1028 1029 1030 1031 1032 1033 1034 1035 1036 1037 1038 1039 1040 1041 1042 1043 1044 1045 1046 1047 1048 1049 1050 1051 1052 1053 1054 1055 1056 1057 1058 1059 1060 1061 1062 1063 1064 1065 1066 1067 1068 1069 1070 1071 1072 1073 1074 1075 1076 1077 1078 1079 1080 1081 1082 1083 1084 1085 1086 1087 1088 1089 1090 1091 1092 1093 1094 1095 1096 1097 1098 1099 1100 1102 1104 1105 1106 1107 1108 1110 1111 1112 1113 1114 1117 1119 1121 1122 1123 1124 1126 1130 1131 1132 1137 1138 1141 1145 1147 1148 1149 1151 1152 1154 1163 1164 1165 1166 1169 1174 1175 1183 1185 1186 1187 1192 1198 1199 1201 1213 1216 1217 1218 1233 1234 1236 1244 1247 1248 1259 1271 1272 1277 1287 1296 1300 1301 1309 1310 1311 1322 1328 1334 1352 1417 1433 1434 1443 1455 1461 1494 1500 1501 1503 1521 1524 1533 1556 1580 1583 1594 1600 1641 1658 1666 1687 1688 1700 1717 1718 1719 1720 1721 1723 1755 1761 1782 1783 1801 1805 1812 1839 1840 1862 1863 1864 1875 1900 1914 1935 1947 1971 1972 1974 1984 1998 1999 2000 2001 2002 2003 2004 2005 2006 2007 2008 2009 2010 2013 2020 2021 2022 2030 2033 2034 2035 2038 2040 2041 2042 2043 2045 2046 2047 2048 2049 2065 2068 2099 2100 2103 2105 2106 2107 2111 2119 2121 2126 2135 2144 2160 2161 2170 2179 2190 2191 2196 2200 2222 2251 2260 2288 2301 2323 2366 2381 2382 2383 2393 2394 2399 2401 2492 2500 2522 2525 2557 2601 2602 2604 2605 2607 2608 2638 2701 2702 2710 2717 2718 2725 2800 2809 2811 2869 2875 2909 2910 2920 2967 2968 2998 3000 3001 3003 3005 3006 3007 3011 3013 3017 3030 3031 3052 3071 3077 3128 3168 3211 3221 3260 3261 3268 3269 3283 3300 3301 3306 3322 3323 3324 3325 3333 3351 3367 3369 3370 3371 3372 3389 3390 3404 3476 3493 3517 3527 3546 3551 3580 3659 3689 3690 3703 3737 3766 3784 3800 3801 3809 3814 3826 3827 3828 3851 3869 3871 3878 3880 3889 3905 3914 3918 3920 3945 3971 3986 3995 3998 4000 4001 4002 4003 4004 4005 4006 4045 4111 4125 4126 4129 4224 4242 4279 4321 4343 4443 4444 4445 4446 4449 4550 4567 4662 4848 4899 4900 4998 5000 5001 5002 5003 5004 5009 5030 5033 5050 5051 5054 5060 5061 5080 5087 5100 5101 5102 5120 5190 5200 5214 5221 5222 5225 5226 5269 5280 5298 5357 5405 5414 5431 5432 5440 5500 5510 5544 5550 5555 5560 5566 5631 5633 5666 5678 5679 5718 5730 5800 5801 5802 5810 5811 5815 5822 5825 5850 5859 5862 5877 5900 5901 5902 5903 5904 5906 5907 5910 5911 5915 5922 5925 5950 5952 5959 5960 5961 5962 5963 5987 5988 5989 5998 5999 6000 6001 6002 6003 6004 6005 6006 6007 6009 6025 6059 6100 6101 6106 6112 6123 6129 6156 6346 6389 6502 6510 6543 6547 6565 6566 6567 6580 6646 6666 6667 6668 6669 6689 6692 6699 6779 6788 6789 6792 6839 6881 6901 6969 7000 7001 7002 7004 7007 7019 7025 7070 7100 7103 7106 7200 7201 7402 7435 7443 7496 7512 7625 7627 7676 7741 7777 7778 7800 7911 7920 7921 7937 7938 7999 8000 8001 8002 8007 8008 8009 8010 8011 8021 8022 8031 8042 8045 8080 8081 8082 8083 8084 8085 8086 8087 8088 8089 8090 8093 8099 8100 8180 8181 8192 8193 8194 8200 8222 8254 8290 8291 8292 8300 8333 8383 8400 8402 8443 8500 8600 8649 8651 8652 8654 8701 8800 8873 8888 8899 8994 9000 9001 9002 9003 9009 9010 9011 9040 9050 9071 9080 9081 9090 9091 9099 9100 9101 9102 9103 9110 9111 9200 9207 9220 9290 9415 9418 9485 9500 9502 9503 9535 9575 9593 9594 9595 9618 9666 9876 9877 9878 9898 9900 9917 9929 9943 9944 9968 9998 9999 10000 10001 10002 10003 10004 10009 10010 10012 10024 10025 10082 10180 10215 10243 10566 10616 10617 10621 10626 10628 10629 10778 11110 11111 11967 12000 12174 12265 12345 13456 13722 13782 13783 14000 14238 14441 14442 15000 15002 15003 15004 15660 15742 16000 16001 16012 16016 16018 16080 16113 16992 16993 17877 17988 18040 18101 18988 19101 19283 19315 19350 19780 19801 19842 20000 20005 20031 20221 20222 20828 21571 22939 23502 24444 24800 25734 25735 26214 27000 27352 27353 27355 27356 27715 28201 30000 30718 30951 31038 31337 32768 32769 32770 32771 32772 32773 32774 32775 32776 32777 32778 32779 32780 32781 32782 32783 32784 32785 33354 33899 34571 34572 34573 35500 38292 40193 40911 41511 42510 44176 44442 44443 44501 45100 48080 49152 49153 49154 49155 49156 49157 49158 49159 49160 49161 49163 49165 49167 49175 49176 49400 49999 50000 50001 50002 50003 50006 50300 50389 50500 50636 50800 51103 51493 52673 52822 52848 52869 54045 54328 55055 55056 55555 55600 56737 56738 57294 57797 58080 60020 60443 61532 61900 62078 63331 64623 64680 65000 65129 65389"
  else
    PORTS="$(echo $PORTS | tr ',' ' ')"
    printf ${YELLOW}"[+]${BLUE} Ports going to be scanned: $PORTS" $NC | tr '\n' " "
    printf "$NC\n"
  fi

  for p in $PORTS; do
    if [ "$FOUND_BASH" ]; then
      $FOUND_BASH -c "(echo </dev/tcp/$IP/$p) 2>/dev/null && echo -n \"[+] Open port at: $IP:$p\"" &
    elif [ "$NC_SCAN" ]; then
      ($NC_SCAN "$IP" "$p" 2>&1 | grep -iv "Connection refused\|No route\|Version\|bytes\| out" | sed -${E} "s,[0-9\.],${SED_RED},g") &
    fi
  done
  wait
}


discover_network (){
  #Check if IP and Netmask are correct and the use fping or ping to find hosts
  basic_net_info

  print_title "Network Discovery"

  DISCOVERY=$1
  IP=$(echo "$DISCOVERY" | cut -d "/" -f 1)
  NETMASK=$(echo "$DISCOVERY" | cut -d "/" -f 2)

  if [ -z "$IP" ] || [ -z "$NETMASK" ]; then
    printf $RED"[-] Err: Bad format. Example: 127.0.0.1/24"$NC;
    printf ${BLUE}"$HELP"$NC;
    exit 0
  fi

  #Using fping if possible
  if [ "$FPING" ]; then
    $FPING -a -q -g "$DISCOVERY" | sed -${E} "s,.*,${SED_RED},"

  #Loop using ping
  else
    if [ "$NETMASK" -eq "24" ]; then
      printf ${YELLOW}"[+]$GREEN Netmask /24 detected, starting...\n$NC"
      icmp_recon $IP

    elif [ "$NETMASK" -eq "16" ]; then
      printf ${YELLOW}"[+]$GREEN Netmask /16 detected, starting...\n$NC"
      for i in $(seq 1 254)
      do
        NEWIP=$(echo "$IP" | cut -d "." -f 1,2).$i.1
        icmp_recon "$NEWIP"
      done
    else
      printf $RED"[-] Err: Sorry, only Netmask /24 and /16 supported in ping mode. Netmask detected: $NETMASK"$NC;
      exit 0
    fi
  fi
}


if [ "$PORTS" ]; then
  if [ "$SCAN_BAN_GOOD" ]; then
    if [ "$(echo -n $PORTS | sed 's,[0-9, ],,g')" ]; then
      printf $RED"[-] Err: Symbols detected in the port, for discovering purposes select only 1 port\n"$NC;
      printf ${BLUE}"$HELP"$NC;
      exit 0
    else
      #Select the correct configuration of the netcat found
      select_nc
    fi
  else
    printf $RED"  Err: Port scan not possible, any netcat in PATH\n"$NC;
    printf ${BLUE}"$HELP"$NC;
    exit 0
  fi
fi

if [ "$DISCOVERY" ]; then
  if [ "$PORTS" ]; then
    discovery_port_scan $DISCOVERY $PORTS
  else
    if [ "$DISCOVER_BAN_GOOD" ]; then
      discover_network $DISCOVERY
    else
      printf $RED"  Err: Discovery not possible, no fping or ping in PATH\n"$NC;
    fi
  fi
  exit 0

elif [ "$IP" ]; then
  select_nc
  tcp_port_scan $IP "$PORTS"
  exit 0
fi

if [ "$PORT_FORWARD" ]; then
  if ! [ "$FOUND_BASH" ]; then
    printf $RED"[-] Err: Port forwarding not possible, no bash in PATH\n"$NC;
    exit 0
  fi

  LOCAL_IP="$(echo -n $PORT_FORWARD | cut -d ':' -f 1)"
  LOCAL_PORT="$(echo -n $PORT_FORWARD | cut -d ':' -f 2)"
  REMOTE_IP="$(echo -n $PORT_FORWARD | cut -d ':' -f 3)"
  REMOTE_PORT="$(echo -n $PORT_FORWARD | cut -d ':' -f 4)"

  if ! [ "$LOCAL_IP" ] || ! [ "$LOCAL_PORT" ] || ! [ "$REMOTE_IP" ] || ! [ "$REMOTE_PORT" ]; then
    printf $RED"[-] Err: Invalid port forwarding configuration: $PORT_FORWARD. The format is: LOCAL_IP:LOCAL_PORT:REMOTE_IP:REMOTE_PORT\nFor example: 10.10.14.8:7777:127.0.0.1:8000"$NC;
    exit 0
  fi

  #Check if LOCAL_PORT is a number
  if ! [ "$(echo $LOCAL_PORT | grep -E '^[0-9]+$')" ]; then
    printf $RED"[-] Err: Invalid port forwarding configuration: $PORT_FORWARD. The format is: LOCAL_IP:LOCAL_PORT:REMOTE_IP:REMOTE_PORT\nFor example: 10.10.14.8:7777:127.0.0.1:8000"$NC;
  fi

  #Check if REMOTE_PORT is a number
  if ! [ "$(echo $REMOTE_PORT | grep -E '^[0-9]+$')" ]; then
    printf $RED"[-] Err: Invalid port forwarding configuration: $PORT_FORWARD. The format is: LOCAL_IP:LOCAL_PORT:REMOTE_IP:REMOTE_PORT\nFor example: 10.10.14.8:7777:127.0.0.1:8000"$NC;
  fi

  port_forward "$LOCAL_IP" "$LOCAL_PORT" "$REMOTE_IP" "$REMOTE_PORT"
  exit 0
fi

if [ "$AUTO_NETWORK_SCAN" ]; then
  basic_net_info
  if ! [ "$FOUND_NC" ] && ! [ "$FOUND_BASH" ]; then
    printf $RED"[-] $SCAN_BAN_BAD\n$NC"
    echo "The network is not going to be scanned..."
  
  elif ! [ "$(command -v ifconfig)" ] && ! [ "$(command -v ip  || echo -n '')" ]; then
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
  exit 0
fi
