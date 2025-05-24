# Title: Network Information - Hostname, hosts and DNS
# ID: NT_Hostname_hosts_dns
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get hostname, hosts and DNS
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, warn_exec
# Global Variables:
# Initial Functions:
# Generated Global Variables: $conf, $line
# Fat linpeas: 0
# Small linpeas: 1

# Function to get hostname using multiple methods
get_hostname_info() {
    print_3title "Hostname Information"
    # Try multiple methods to get hostname
    if command -v hostname >/dev/null 2>&1; then
        echo "System hostname: $(hostname 2>/dev/null)"
        echo "FQDN: $(hostname -f 2>/dev/null)"
    else
        # Fallback methods
        if [ -f "/proc/sys/kernel/hostname" ]; then
            echo "System hostname: $(cat /proc/sys/kernel/hostname 2>/dev/null)"
        fi
        if [ -f "/etc/hostname" ]; then
            echo "Hostname from /etc/hostname: $(cat /etc/hostname 2>/dev/null)"
        fi
    fi
    echo ""
}

# Function to get hosts file information
get_hosts_info() {
    print_3title "Hosts File Information"
    if [ -f "/etc/hosts" ]; then
        echo "Contents of /etc/hosts:"
        grep -v "^#" /etc/hosts 2>/dev/null | grep -v "^$" | while read -r line; do
            echo "  $line"
        done
    fi
    echo ""
}

# Function to get DNS information
get_dns_info() {
    print_3title "DNS Configuration"
    
    # Get resolv.conf information
    if [ -f "/etc/resolv.conf" ]; then
        echo "DNS Servers (resolv.conf):"
        grep -v "^#" /etc/resolv.conf 2>/dev/null | grep -v "^$" | while read -r line; do
            if echo "$line" | grep -q "nameserver"; then
                echo "  $(echo "$line" | awk '{print $2}')"
            elif echo "$line" | grep -q "search\|domain"; then
                echo "  $line"
            fi
        done
    fi

    # Check for systemd-resolved configuration
    if [ -f "/etc/systemd/resolved.conf" ]; then
        echo -e "\nSystemd-resolved configuration:"
        grep -v "^#" /etc/systemd/resolved.conf 2>/dev/null | grep -v "^$" | while read -r line; do
            echo "  $line"
        done
    fi

    # Check for NetworkManager DNS settings
    if [ -d "/etc/NetworkManager" ]; then
        echo -e "\nNetworkManager DNS settings:"
        find /etc/NetworkManager -type f -name "*.conf" 2>/dev/null | while read -r conf; do
            if grep -q "dns=" "$conf" 2>/dev/null; then
                echo "  From $conf:"
                grep "dns=" "$conf" 2>/dev/null | while read -r line; do
                    echo "    $line"
                done
            fi
        done
    fi

    # Try to get DNS domain name
    echo -e "\nDNS Domain Information:"
    if command -v dnsdomainname >/dev/null 2>&1; then
        warn_exec dnsdomainname 2>/dev/null
    fi
    if command -v domainname >/dev/null 2>&1; then
        warn_exec domainname 2>/dev/null
    fi

    # Check for DNS cache status
    if command -v systemd-resolve >/dev/null 2>&1; then
        echo -e "\nDNS Cache Status (systemd-resolve):"
        systemd-resolve --status 2>/dev/null | grep -A5 "DNS Servers" | grep -v "\-\-" | while read -r line; do
            echo "  $line"
        done
    fi
    echo ""
}

print_2title "Hostname, hosts and DNS"

# Execute all information gathering functions
get_hostname_info
get_hosts_info
get_dns_info