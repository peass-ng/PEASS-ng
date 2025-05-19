# Title: System Information - MacOS OS checks
# ID: SY_Macos_os_checks
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for MacOS-specific vulnerabilities and misconfigurations that could lead to privilege escalation:
#   - Unsigned kernel extensions
#   - Non-Apple kernel extensions
#   - System Integrity Protection (SIP) status
#   - Gatekeeper status
#   - Common vulnerable scenarios:
#     * Disabled SIP
#     * Unsigned kernel extensions
#     * Third-party kernel extensions
#     * Disabled Gatekeeper
#   - Exploitation methods:
#     * Kernel extension injection: Load malicious kernel extensions
#     * Common attack vectors:
#       - SIP bypass
#       - Kernel extension loading
#       - Gatekeeper bypass
#       - System modification
#     * Exploit techniques:
#       - Kernel extension injection
#       - SIP bypass
#       - Gatekeeper bypass
#       - System modification
# License: GNU GPL
# Version: 1.0
# Functions Used:macosNotSigned, print_2title
# Global Variables: $MACPEAS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ]; then
    print_2title "Kernel Extensions not belonging to apple"
    kextstat 2>/dev/null | grep -Ev " com.apple."
    echo ""

    print_2title "Unsigned Kernel Extensions"
    macosNotSigned /Library/Extensions
    macosNotSigned /System/Library/Extensions
    echo ""
fi

if [ "$MACPEAS" ] && [ "$(command -v brew 2>/dev/null || echo -n '')" ]; then
    print_2title "Brew Doctor Suggestions"
    brew doctor
    echo ""
fi