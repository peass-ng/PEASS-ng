# Title: Network Information - Network Traffic Analysis
# ID: NT_Tcpdump
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check network traffic analysis capabilities and tools
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, print_info, warn_exec
# Global Variables: $EXTRA_CHECKS, $E, $SED_RED, $SED_GREEN
# Initial Functions:
# Generated Global Variables: $tools_found, $tool, $interfaces, $interfaces_found, $iface, $cmd, $pattern, $patterns
# Fat linpeas: 0
# Small linpeas: 1

# Function to check if a command exists and is executable
check_command() {
    local cmd=$1
    if command -v "$cmd" >/dev/null 2>&1; then
        if [ -x "$(command -v "$cmd")" ]; then
            return 0
        fi
    fi
    return 1
}

# Function to check if we can sniff on an interface
check_interface_sniffable() {
    local iface=$1
    if timeout 1 tcpdump -i "$iface" -c 1 >/dev/null 2>&1; then
        return 0
    fi
    return 1
}

# Function to check for promiscuous mode
check_promiscuous_mode() {
    local iface=$1
    if ip link show "$iface" 2>/dev/null | grep -q "PROMISC"; then
        return 0
    fi
    return 1
}

# Main function to check network traffic analysis capabilities
check_network_traffic_analysis() {
    print_2title "Network Traffic Analysis Capabilities"
    
    # Check for sniffing tools
    echo ""
    print_3title "Available Sniffing Tools"
    tools_found=0
    
    if check_command tcpdump; then
        echo "tcpdump is available" | sed -${E} "s,.*,${SED_GREEN},g"
        tools_found=1
        # Check tcpdump version and capabilities
        warn_exec tcpdump --version 2>/dev/null | head -n 1
    fi
    
    if check_command tshark; then
        echo "tshark is available" | sed -${E} "s,.*,${SED_GREEN},g"
        tools_found=1
        # Check tshark version
        warn_exec tshark --version 2>/dev/null | head -n 1
    fi
    
    if check_command wireshark; then
        echo "wireshark is available" | sed -${E} "s,.*,${SED_GREEN},g"
        tools_found=1
    fi
    
    if [ $tools_found -eq 0 ]; then
        echo "No sniffing tools found" | sed -${E} "s,.*,${SED_RED},g"
    fi
    
    # Check network interfaces
    echo ""
    print_3title "Network Interfaces Sniffing Capabilities"
    interfaces_found=0
    
    # Get list of network interfaces
    if command -v ip >/dev/null 2>&1; then
        interfaces=$(ip -o link show | awk -F': ' '{print $2}')
    elif command -v ifconfig >/dev/null 2>&1; then
        interfaces=$(ifconfig -a | grep -o '^[^ ]*:' | tr -d ':')
    else
        interfaces=$(ls /sys/class/net/ 2>/dev/null)
    fi
    
    for iface in $interfaces; do
        if [ "$iface" != "lo" ]; then  # Skip loopback
            echo -n "Interface $iface: "
            if check_interface_sniffable "$iface"; then
                echo "Sniffable" | sed -${E} "s,.*,${SED_GREEN},g"
                interfaces_found=1
                
                # Check promiscuous mode
                if check_promiscuous_mode "$iface"; then
                    echo "  - Promiscuous mode enabled" | sed -${E} "s,.*,${SED_RED},g"
                fi
                
                # Get interface details
                if [ "$EXTRA_CHECKS" ]; then
                    echo "  - Interface details:"
                    warn_exec ip addr show "$iface" 2>/dev/null || ifconfig "$iface" 2>/dev/null
                fi
            else
                echo "Not sniffable" | sed -${E} "s,.*,${SED_RED},g"
            fi
        fi
    done
    
    if [ $interfaces_found -eq 0 ]; then
        echo "No sniffable interfaces found" | sed -${E} "s,.*,${SED_RED},g"
    fi
    
    # Check for sensitive traffic patterns if we have sniffing capabilities
    if [ $tools_found -eq 1 ] && [ $interfaces_found -eq 1 ]; then
        echo ""
        print_3title "Sensitive Traffic Detection"
        print_info "Checking for common sensitive traffic patterns..."
        
        # List of sensitive traffic patterns to check
        patterns="
            - HTTP Basic Auth
            - FTP credentials
            - SMTP credentials
            - MySQL/MariaDB traffic
            - PostgreSQL traffic
            - Redis traffic
            - MongoDB traffic
            - LDAP traffic
            - SMB traffic
            - DNS queries
            - SNMP traffic
            - Many more...
        "
        
        echo "$patterns" | while read -r pattern; do
            if [ -n "$pattern" ]; then
                echo "$pattern"
            fi
        done
        
        print_info "To capture sensitive traffic, you can use:"
        echo "tcpdump -i <interface> -w capture.pcap" | sed -${E} "s,.*,${SED_GREEN},g"
        echo "tshark -i <interface> -w capture.pcap" | sed -${E} "s,.*,${SED_GREEN},g"
    fi
    
    # Additional information
    if [ "$EXTRA_CHECKS" ]; then
        echo ""
        print_3title "Additional Network Analysis Information"
        
        # Check for network monitoring tools
        echo "Checking for network monitoring tools..."
        for tool in nethogs iftop iotop nload bmon; do
            if check_command "$tool"; then
                echo "$tool is available" | sed -${E} "s,.*,${SED_GREEN},g"
            fi
        done
    fi
    
    echo ""
}

# Run the main function
check_network_traffic_analysis
