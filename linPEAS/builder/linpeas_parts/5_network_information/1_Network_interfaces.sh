# Title: Network Information - Network interfaces
# ID: NT_Network_interfaces
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check network interfaces
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables:
# Initial Functions:
# Generated Global Variables: $iface, $state, $mac, $ip_file, $line
# Fat linpeas: 0
# Small linpeas: 1

# Function to parse network interfaces from /proc/net/dev and other sources
parse_network_interfaces() {
    # Try to get interfaces from /proc/net/dev
    if [ -f "/proc/net/dev" ]; then
        echo "Network Interfaces from /proc/net/dev:"
        echo "----------------------------------------"
        # Skip header lines and format output
        grep -v "^Inter\|^ face" /proc/net/dev | while read -r line; do
            iface=$(echo "$line" | awk -F: '{print $1}' | tr -d ' ')
            if [ -n "$iface" ]; then
                echo "Interface: $iface"
                # Try to get IP address from /sys/class/net
                if [ -f "/sys/class/net/$iface/address" ]; then
                    mac=$(cat "/sys/class/net/$iface/address" 2>/dev/null)
                    echo "  MAC: $mac"
                fi
                # Try to get IP from /sys/class/net
                if [ -d "/sys/class/net/$iface/ipv4" ]; then
                    for ip_file in /sys/class/net/$iface/ipv4/addr_*; do
                        if [ -f "$ip_file" ]; then
                            ip=$(cat "$ip_file" 2>/dev/null)
                            echo "  IP: $ip"
                        fi
                    done
                fi
                # Get interface state
                if [ -f "/sys/class/net/$iface/operstate" ]; then
                    state=$(cat "/sys/class/net/$iface/operstate" 2>/dev/null)
                    echo "  State: $state"
                fi
                echo ""
            fi
        done
    fi

    # Try to get additional info from /proc/net/fib_trie
    if [ -f "/proc/net/fib_trie" ]; then
        echo "Additional IP Information from fib_trie:"
        echo "----------------------------------------"
        grep -A1 "Main" /proc/net/fib_trie | grep -v "\-\-" | while read -r line; do
            if echo "$line" | grep -q "Main"; then
                echo "Network: $(echo "$line" | awk '{print $2}')"
            elif echo "$line" | grep -q "/"; then
                echo "  IP: $(echo "$line" | awk '{print $2}')"
            fi
        done
    fi
}

print_2title "Interfaces"
cat /etc/networks 2>/dev/null

# Try standard tools first, then fall back to our custom function
if command -v ifconfig >/dev/null 2>&1; then
    ifconfig 2>/dev/null
elif command -v ip >/dev/null 2>&1; then
    ip a 2>/dev/null
else
    parse_network_interfaces
fi

echo ""