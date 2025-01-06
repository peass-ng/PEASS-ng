# Title: System Information - Dmesg
# ID: SY_Dmesg
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching Signature verification failed in dmesg
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