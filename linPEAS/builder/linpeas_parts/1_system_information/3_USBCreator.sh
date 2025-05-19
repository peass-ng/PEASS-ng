# Title: System Information - USBCreator
# ID: SY_USBCreator
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for USBCreator vulnerabilities that could lead to privilege escalation:
#   - Vulnerable policykit-desktop-privileges versions
#   - Common vulnerable versions:
#     * policykit-desktop-privileges < 0.21
#   - Exploitation methods:
#     * D-Bus command injection through USBCreator
#     * Abuse of policykit privileges
#     * Common attack vectors:
#       - D-Bus method call injection
#       - PolicyKit authentication bypass
#       - Command execution through USB creation
#     * Exploit techniques:
#       - D-Bus method spoofing
#       - PolicyKit privilege escalation
#       - USB device creation abuse
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables: $pc_version, $pc_length, $pc_major, $pc_minor
# Fat linpeas: 0
# Small linpeas: 0


if (busctl list 2>/dev/null | grep -q com.ubuntu.USBCreator) || [ "$DEBUG" ]; then
    print_2title "USBCreator"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/d-bus-enumeration-and-command-injection-privilege-escalation.html"

    pc_version=$(dpkg -l 2>/dev/null | grep policykit-desktop-privileges | grep -oP "[0-9][0-9a-zA-Z\.]+")
    if [ -z "$pc_version" ]; then
        pc_version=$(apt-cache policy policykit-desktop-privileges 2>/dev/null | grep -oP "\*\*\*.*" | cut -d" " -f2)
    fi
    if [ -n "$pc_version" ]; then
        pc_length=${#pc_version}
        pc_major=$(echo "$pc_version" | cut -d. -f1)
        pc_minor=$(echo "$pc_version" | cut -d. -f2)
        if [ "$pc_length" -eq 4 ] && [ "$pc_major" -eq 0 ] && [ "$pc_minor"  -lt 21 ]; then
            echo "Vulnerable!!" | sed -${E} "s,.*,${SED_RED},"
        fi
    fi
fi
echo ""