# Title: Network Information - Network neighbours
# ID: NT_Network_neighbours
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Networks and neighbours
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title
# Global Variables: $EXTRA_CHECKS, $MACPEAS
# Initial Functions:
# Generated Global Variables: $hwtype, $flags, $line, $iface, $dest, $ref, $use, $mask, $metric, $device, $hwaddr
# Fat linpeas: 0
# Small linpeas: 0

# Function to parse routing information from /proc/net/route
parse_proc_route() {
    print_3title "Routing Table (from /proc/net/route)"
    echo "Destination         Gateway         Genmask         Flags Metric Ref    Use Iface"
    echo "--------------------------------------------------------------------------------"
    # Skip header line and process each route
    tail -n +2 /proc/net/route 2>/dev/null | while read -r line; do
        if [ -n "$line" ]; then
            # Extract fields
            iface=$(echo "$line" | awk '{print $1}')
            dest=$(printf "%d.%d.%d.%d" $(echo "$line" | awk '{printf "0x%s 0x%s 0x%s 0x%s", substr($2,7,2), substr($2,5,2), substr($2,3,2), substr($2,1,2)}'))
            gw=$(printf "%d.%d.%d.%d" $(echo "$line" | awk '{printf "0x%s 0x%s 0x%s 0x%s", substr($3,7,2), substr($3,5,2), substr($3,3,2), substr($3,1,2)}'))
            mask=$(printf "%d.%d.%d.%d" $(echo "$line" | awk '{printf "0x%s 0x%s 0x%s 0x%s", substr($4,7,2), substr($4,5,2), substr($4,3,2), substr($4,1,2)}'))
            flags=$(echo "$line" | awk '{print $5}')
            metric=$(echo "$line" | awk '{print $6}')
            ref=$(echo "$line" | awk '{print $7}')
            use=$(echo "$line" | awk '{print $8}')
            
            # Print formatted output
            printf "%-18s %-15s %-15s %-6s %-6s %-6s %-6s %s\n" "$dest" "$gw" "$mask" "$flags" "$metric" "$ref" "$use" "$iface"
        fi
    done
    echo ""
}

# Function to parse ARP information from /proc/net/arp
parse_proc_arp() {
    print_3title "ARP Table (from /proc/net/arp)"
    echo "IP address       HW type     Flags     HW address           Mask     Device"
    echo "------------------------------------------------------------------------"
    # Skip header line and process each ARP entry
    tail -n +2 /proc/net/arp 2>/dev/null | while read -r line; do
        if [ -n "$line" ]; then
            ip=$(echo "$line" | awk '{print $1}')
            hwtype=$(echo "$line" | awk '{print $2}')
            flags=$(echo "$line" | awk '{print $3}')
            hwaddr=$(echo "$line" | awk '{print $4}')
            mask=$(echo "$line" | awk '{print $5}')
            device=$(echo "$line" | awk '{print $6}')
            
            # Print formatted output
            printf "%-15s %-11s %-9s %-18s %-8s %s\n" "$ip" "$hwtype" "$flags" "$hwaddr" "$mask" "$device"
        fi
    done
    echo ""
}

# Function to get network neighbors information
get_network_neighbors() {
    print_2title "Networks and neighbours"

    # Get routing information
    print_3title "Routing Information"
    if [ "$MACPEAS" ]; then
        # macOS specific
        if command -v netstat >/dev/null 2>&1; then
            netstat -rn 2>/dev/null
        else
            echo "No routing information available"
        fi
    else
        # Linux systems
        if command -v ip >/dev/null 2>&1; then
            ip route 2>/dev/null
            echo -e "\nNeighbor table:"
            ip neigh 2>/dev/null
        elif command -v route >/dev/null 2>&1; then
            route -n 2>/dev/null
        elif [ -f "/proc/net/route" ]; then
            parse_proc_route
        else
            echo "No routing information available"
        fi
    fi

    # Get ARP information
    print_3title "ARP Information"
    if command -v arp >/dev/null 2>&1; then
        if [ "$MACPEAS" ]; then
            arp -a 2>/dev/null
        else
            arp -e 2>/dev/null || arp -a 2>/dev/null
        fi
    elif [ -f "/proc/net/arp" ]; then
        parse_proc_arp
    else
        echo "No ARP information available"
    fi

    # Additional neighbor discovery methods
    print_3title "Additional Neighbor Information"
    
    # Check for IPv6 neighbors if available
    if [ -f "/proc/net/ipv6_neigh" ]; then
        echo "IPv6 Neighbors:"
        cat /proc/net/ipv6_neigh 2>/dev/null | grep -v "^IP" | while read -r line; do
            if [ -n "$line" ]; then
                echo "  $line"
            fi
        done
    fi

    # Try to get LLDP neighbors if available
    if command -v lldpctl >/dev/null 2>&1; then
        echo -e "\nLLDP Neighbors:"
        lldpctl 2>/dev/null | grep -A2 "Interface:" | while read -r line; do
            echo "  $line"
        done
    fi

    # Try to get CDP neighbors if available
    if command -v cdp >/dev/null 2>&1; then
        echo -e "\nCDP Neighbors:"
        cdp 2>/dev/null | grep -v "^$" | while read -r line; do
            echo "  $line"
        done
    fi

    echo ""
}

if [ "$EXTRA_CHECKS" ]; then
    get_network_neighbors
fi
