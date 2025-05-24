# Title: Network Information - MacOS Network Services
# ID: NT_Macos_network_services
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Enumerate macos network services
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, warn_exec
# Global Variables: $EXTRA_CHECKS, $MACPEAS, $E, $SED_RED
# Initial Functions:
# Generated Global Variables: $sharing_service, $profile, $port3, $service_count, $port1, $port, $services, $total, $port_list, $count, $ports, $active_ports, $port2
# Fat linpeas: 0
# Small linpeas: 0

# Function to check if a port is listening
check_listening_port() {
    local port=$1
    local service=$2
    local count=0
    
    # Check both IPv4 and IPv6
    count=$(netstat -na 2>/dev/null | grep LISTEN | grep -E 'tcp4|tcp6' | grep "*.${port}" | wc -l)
    echo "$count"
}

# Function to get sharing services status
get_sharing_services_status() {
    print_2title "MacOS Sharing Services Status"
    
    # Define services and their ports using parallel arrays
    services="Screen Sharing File Sharing Remote Login Remote Management Remote Apple Events Back to My Mac AirPlay Receiver AirDrop Bonjour Printer Sharing Internet Sharing"
    ports="5900 88,445,548 22 3283 3031 4488 7000 5353 5353 515,631 67,68"

    # Check each service
    echo "Service Status (0=OFF, >0=ON):"
    echo "--------------------------------"
    
    # Get number of services
    service_count=$(echo "$services" | wc -w)
    
    # Loop through services using index
    i=1
    while [ $i -le $service_count ]; do
        sharing_service=$(echo "$services" | cut -d' ' -f$i)
        port_list=$(echo "$ports" | cut -d' ' -f$i)
        total=0
        active_ports=""
        
        # Check each port for the service
        port1=$(echo "$port_list" | cut -d',' -f1)
        port2=$(echo "$port_list" | cut -d',' -f2)
        port3=$(echo "$port_list" | cut -d',' -f3)
        for port in $port1 $port2 $port3; do
            if [ -n "$port" ]; then
                count=$(check_listening_port "$port" "$sharing_service")
                if [ "$count" -gt 0 ]; then
                    total=$((total + count))
                    if [ -n "$active_ports" ]; then
                        active_ports="${active_ports},"
                    fi
                    active_ports="${active_ports}${port}"
                fi
            fi
        done
        
        # Print service status
        if [ "$total" -gt 0 ]; then
            printf "%-20s: ON  (Ports: %s)\n" "$sharing_service" "$active_ports" | sed -${E} "s,ON.*,${SED_RED},g"
        else
            printf "%-20s: OFF\n" "$sharing_service"
        fi
        
        i=$((i + 1))
    done
    echo ""
}

# Function to get VPN information
get_vpn_info() {
    print_3title "VPN Information"
    
    # Get VPN configurations
    warn_exec system_profiler SPNetworkLocationDataType | grep -A 5 -B 7 ": Password" | sed -${E} "s,Password|Authorization Name.*,${SED_RED},g"
    
    # Check for VPN profiles
    if [ -d "/Library/Preferences/SystemConfiguration" ]; then
        echo -e "\nVPN Profiles:"
        find /Library/Preferences/SystemConfiguration -name "*.plist" -exec grep -l "VPN" {} \; 2>/dev/null | while read -r profile; do
            echo "Profile: $profile"
            warn_exec plutil -p "$profile" | grep -A 5 "VPN"
        done
    fi
    echo ""
}

# Function to get firewall information
get_firewall_info() {
    print_3title "Firewall Information"
    
    # Get firewall status
    warn_exec system_profiler SPFirewallDataType
    
    # Get application firewall rules
    if command -v /usr/libexec/ApplicationFirewall/socketfilterfw >/dev/null 2>&1; then
        echo -e "\nApplication Firewall Rules:"
        warn_exec /usr/libexec/ApplicationFirewall/socketfilterfw --listapps
    fi
    
    # Get pf firewall rules if available
    if command -v pfctl >/dev/null 2>&1; then
        echo -e "\nPF Firewall Rules:"
        warn_exec pfctl -s rules 2>/dev/null
    fi
    echo ""
}

# Function to get additional network information
get_additional_network_info() {
    if [ "$EXTRA_CHECKS" ]; then
        print_3title "Additional Network Information"
        
        # Bluetooth information
        echo "Bluetooth Status:"
        warn_exec system_profiler SPBluetoothDataType
        
        # Ethernet information
        echo -e "\nEthernet Status:"
        warn_exec system_profiler SPEthernetDataType
        
        # USB network adapters
        echo -e "\nUSB Network Adapters:"
        warn_exec system_profiler SPUSBDataType
        
        # Network kernel extensions
        echo -e "\nNetwork Kernel Extensions:"
        warn_exec kextstat | grep -i "network\|ethernet\|wifi\|bluetooth"
        
        # Network daemons
        echo -e "\nNetwork Daemons:"
        warn_exec launchctl list | grep -i "network\|vpn\|firewall\|sharing"
    fi
    echo ""
}

# Main function to get all network services information
get_macos_network_services() {
    if [ "$MACPEAS" ]; then
        # Get sharing services status
        get_sharing_services_status
        
        # Get VPN information
        get_vpn_info
        
        # Get firewall information
        get_firewall_info
        
        # Get additional network information if EXTRA_CHECKS is enabled
        get_additional_network_info
    fi
}

if [ "$MACPEAS" ]; then
    get_macos_network_services
fi