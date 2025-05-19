# Title: System Information - Dmesg
# ID: SY_Dmesg
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for kernel signature verification failures that could lead to privilege escalation:
#   - Failed kernel module signature verifications
#   - Common vulnerable scenarios:
#     * Disabled kernel module signing
#     * Failed signature verifications
#     * Unsigned kernel modules
#   - Exploitation methods:
#     * Kernel module injection: Load malicious kernel modules
#     * Common attack vectors:
#       - Kernel module loading
#       - Kernel module replacement
#       - Kernel module modification
#     * Exploit techniques:
#       - Module signing bypass
#       - Kernel module injection
#       - Kernel module modification
#       - Kernel module replacement
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$(command -v dmesg 2>/dev/null || echo -n '')" ] || [ "$DEBUG" ]; then
    print_2title "Searching Signature verification failed in dmesg"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#dmesg-signature-verification-failed"
    (dmesg 2>/dev/null | grep "signature") || echo_not_found "dmesg"
    echo ""
fi