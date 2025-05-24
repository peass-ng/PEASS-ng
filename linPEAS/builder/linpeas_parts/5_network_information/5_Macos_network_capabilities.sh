# Title: Network Information - MacOS network capabilities
# ID: NT_Macos_network_capabilities
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: MacOS network Capabilities
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, warn_exec
# Global Variables: $MACPEAS, $EXTRA_CHECKS
# Initial Functions:
# Generated Global Variables: $net_service
# Fat linpeas: 0
# Small linpeas: 0

# Function to get network capabilities information
get_macos_network_capabilities() {
    print_2title "Network Capabilities"

    # Basic network information
    echo ""
    print_3title "Network Interfaces and Configuration"
    warn_exec system_profiler SPNetworkDataType

    # Network locations
    echo ""
    print_3title "Network Locations"
    warn_exec system_profiler SPNetworkLocationDataType

    # Network extensions
    echo ""
    print_3title "Network Extensions"
    if [ -d "/Library/SystemExtensions" ]; then
        warn_exec systemextensionsctl list
    fi

    # Network security
    echo ""
    print_3title "Network Security"
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
        print_3title "Network Preferences"
        if [ -f "/Library/Preferences/SystemConfiguration/preferences.plist" ]; then
            warn_exec plutil -p /Library/Preferences/SystemConfiguration/preferences.plist | grep -A 5 "NetworkServices"
        fi
        
        # Network statistics
        echo ""
        print_3title "Network Statistics"
        warn_exec netstat -s
        
        # Network routes
        echo ""
        print_3title "Network Routes"
        warn_exec netstat -rn
        
        # Network interfaces details
        echo ""
        print_3title "Network Interfaces Details"
        warn_exec ifconfig -a
        
        # Network kernel extensions
        echo ""
        print_3title "Network Kernel Extensions"
        warn_exec kextstat | grep -i network
    fi

    echo ""
}

if [ "$MACPEAS" ]; then
    get_macos_network_capabilities
fi