# Title: Network Information - MacOS network capabilities
# ID: NT_Macos_network_capabilities
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: MacOS network Capabilities
# License: GNU GPL
# Version: 1.0
# Mitre: T1016
# Functions Used: print_2title, print_3title, warn_exec
# Global Variables: $MACPEAS, $EXTRA_CHECKS
# Initial Functions:
# Generated Global Variables: $net_service
# Fat linpeas: 0
# Small linpeas: 0

# Function to get network capabilities information
get_macos_network_capabilities() {
    print_2title "Network Capabilities" "T1016"
    # Basic network information
    echo ""
    print_3title "Network Interfaces and Configuration" "T1016"
    warn_exec system_profiler SPNetworkDataType

    # Network locations
    echo ""
    print_3title "Network Locations" "T1016"
    warn_exec system_profiler SPNetworkLocationDataType

    # Network extensions
    echo ""
    print_3title "Network Extensions" "T1016"
    if [ -d "/Library/SystemExtensions" ]; then
        warn_exec systemextensionsctl list
    fi

    # Network security
    echo ""
    print_3title "Network Security" "T1016"
    if command -v networksetup >/dev/null 2>&1; then
        echo "Firewall Status:"
        warn_exec networksetup -getglobalstate
        echo -e "\nFirewall Rules:"
        warn_exec networksetup -listallnetworkservices | while read -r net_service; do
            if [ -n "$net_service" ]; then
                echo "Service: $net_service"
                warn_exec networksetup -getwebproxy "$net_service"
                warn_exec networksetup -getsecurewebproxy "$net_service"
                warn_exec networksetup -getproxybypassdomains "$net_service"
            fi
        done
    fi

    # Additional network information if EXTRA_CHECKS is enabled
    if [ "$EXTRA_CHECKS" ]; then
        # Network preferences
        echo ""
        print_3title "Network Preferences" "T1016"
        if [ -f "/Library/Preferences/SystemConfiguration/preferences.plist" ]; then
            warn_exec plutil -p /Library/Preferences/SystemConfiguration/preferences.plist | grep -A 5 "NetworkServices"
        fi
        
        # Network statistics
        echo ""
        print_3title "Network Statistics" "T1016"
        warn_exec netstat -s
        
        # Network routes
        echo ""
        print_3title "Network Routes" "T1016"
        warn_exec netstat -rn
        
        # Network interfaces details
        echo ""
        print_3title "Network Interfaces Details" "T1016"
        warn_exec ifconfig -a
        
        # Network kernel extensions
        echo ""
        print_3title "Network Kernel Extensions" "T1016"
        warn_exec kextstat | grep -i network
    fi

    echo ""
}

if [ "$MACPEAS" ]; then
    get_macos_network_capabilities
fi