# Title: Network Information - Tcpdump
# ID: NT_Tcpdump
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Can I sniff with tcpdump?
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_no, print_2title, print_info
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Can I sniff with tcpdump?"
timeout 1 tcpdump >/dev/null 2>&1
if [ $? -eq 124 ]; then #If 124, then timed out == It worked
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#sniffing"
    echo "You can sniff with tcpdump!" | sed -${E} "s,.*,${SED_RED},"
else echo_no
fi
echo ""
