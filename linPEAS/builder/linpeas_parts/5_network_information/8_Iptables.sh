# Title: Network Information - Firewall Rules Analysis
# ID: NT_Iptables
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Analyze firewall rules and configurations
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, warn_exec, echo_not_found
# Global Variables: $EXTRA_CHECKS, $E, $SED_RED, $SED_GREEN, $SED_YELLOW
# Initial Functions:
# Generated Global Variables: $rules_file, $cmd, $tool, $config_file
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

# Function to analyze iptables rules
analyze_iptables() {
    echo ""
    print_3title "Iptables Rules"
    
    # Check if iptables is available
    if ! check_command iptables; then
        echo_not_found "iptables"
        return
    fi
    
    # Check if we have permission to list rules
    if ! timeout 1 iptables -L >/dev/null 2>&1; then
        echo "No permission to list iptables rules" | sed -${E} "s,.*,${SED_RED},g"
        return
    fi
    
    # Get iptables version
    warn_exec iptables --version 2>/dev/null
    
    # List all chains and rules
    echo -e "\nFilter Table Rules:"
    warn_exec iptables -L -v -n 2>/dev/null
    
    echo -e "\nNAT Table Rules:"
    warn_exec iptables -t nat -L -v -n 2>/dev/null
    
    echo -e "\nMangle Table Rules:"
    warn_exec iptables -t mangle -L -v -n 2>/dev/null
    
    # Check for custom chains
    echo -e "\nCustom Chains:"
    warn_exec iptables -L -v -n | grep -E "^Chain [A-Za-z]" | grep -v "INPUT\|OUTPUT\|FORWARD\|PREROUTING\|POSTROUTING" 2>/dev/null
    
    # Check for saved rules
    echo -e "\nSaved Rules:"
    for rules_file in /etc/iptables/* /etc/iptables/rules.v4 /etc/iptables/rules.v6 /etc/iptables-save /etc/iptables.save; do
        if [ -f "$rules_file" ]; then
            echo "Found rules in $rules_file:"
            warn_exec cat "$rules_file" | grep -v "^#" | grep -Ev "\W+\#|^#" 2>/dev/null
        fi
    done
}

# Function to analyze nftables rules
analyze_nftables() {
    echo ""
    print_3title "Nftables Rules"
    
    # Check if nft is available
    if ! check_command nft; then
        echo_not_found "nftables"
        return
    fi
    
    # Check if we have permission to list rules
    if ! timeout 1 nft list ruleset >/dev/null 2>&1; then
        echo "No permission to list nftables rules" | sed -${E} "s,.*,${SED_RED},g"
        return
    fi
    
    # Get nftables version
    warn_exec nft --version 2>/dev/null
    
    # List all rules
    echo -e "\nNftables Ruleset:"
    warn_exec nft list ruleset 2>/dev/null
    
    # Check for saved rules
    echo -e "\nSaved Rules:"
    for rules_file in /etc/nftables.conf /etc/sysconfig/nftables.conf; do
        if [ -f "$rules_file" ]; then
            echo "Found rules in $rules_file:"
            warn_exec cat "$rules_file" | grep -v "^#" | grep -Ev "\W+\#|^#" 2>/dev/null
        fi
    done
}

# Function to analyze firewalld rules
analyze_firewalld() {
    echo ""
    print_3title "Firewalld Rules"
    
    # Check if firewall-cmd is available
    if ! check_command firewall-cmd; then
        echo_not_found "firewalld"
        return
    fi
    
    # Check if firewalld is running
    if ! systemctl is-active firewalld >/dev/null 2>&1; then
        echo "Firewalld is not running" | sed -${E} "s,.*,${SED_YELLOW},g"
        return
    fi
    
    # Get firewalld version
    warn_exec firewall-cmd --version 2>/dev/null
    
    # List all zones
    echo -e "\nFirewalld Zones:"
    warn_exec firewall-cmd --list-all-zones 2>/dev/null
    
    # List active zones
    echo -e "\nActive Zones:"
    warn_exec firewall-cmd --get-active-zones 2>/dev/null
    
    # List services
    echo -e "\nAvailable Services:"
    warn_exec firewall-cmd --list-services 2>/dev/null
    
    # List ports
    echo -e "\nOpen Ports:"
    warn_exec firewall-cmd --list-ports 2>/dev/null
    
    # List rich rules
    echo -e "\nRich Rules:"
    warn_exec firewall-cmd --list-rich-rules 2>/dev/null
}

# Function to analyze UFW rules
analyze_ufw() {
    echo ""
    print_3title "UFW Rules"
    
    # Check if ufw is available
    if ! check_command ufw; then
        echo_not_found "ufw"
        return
    fi
    
    # Check if UFW is running
    if ! ufw status >/dev/null 2>&1; then
        echo "UFW is not running" | sed -${E} "s,.*,${SED_YELLOW},g"
        return
    fi
    
    # Get UFW version
    warn_exec ufw version 2>/dev/null
    
    # List rules
    echo -e "\nUFW Rules:"
    warn_exec ufw status verbose 2>/dev/null
    
    # List numbered rules
    echo -e "\nNumbered Rules:"
    warn_exec ufw status numbered 2>/dev/null
}

# Main function to analyze firewall rules
analyze_firewall_rules() {
    print_2title "Firewall Rules Analysis"
    
    # Analyze different firewall systems
    analyze_iptables
    analyze_nftables
    analyze_firewalld
    analyze_ufw
    
    # Additional checks if EXTRA_CHECKS is enabled
    if [ "$EXTRA_CHECKS" ]; then
        echo ""
        print_3title "Additional Firewall Information"
        
        # Check for common firewall configuration files
        echo "Checking for firewall configuration files..."
        for config_file in /etc/sysconfig/iptables /etc/sysconfig/ip6tables /etc/iptables/rules.v4 /etc/iptables/rules.v6 /etc/nftables.conf /etc/ufw/user.rules /etc/ufw/user6.rules; do
            if [ -f "$config_file" ]; then
                echo "Found configuration file: $config_file" | sed -${E} "s,.*,${SED_GREEN},g"
            fi
        done
        
        # Check for firewall management tools
        echo -e "\nChecking for firewall management tools..."
        for tool in shorewall shorewall6 ferm; do
            if check_command "$tool"; then
                echo "$tool is available" | sed -${E} "s,.*,${SED_GREEN},g"
            fi
        done
    fi
    
    echo ""
}

# Run the main function
analyze_firewall_rules