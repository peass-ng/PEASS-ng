# Title: System Information - Sudo Version
# ID: SY_Sudo_version
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get Information about the Sudo Version
# License: GNU GPL
# Version: 1.0 
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $sudovB
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Sudo version"
if [ "$(command -v sudo 2>/dev/null || echo -n '')" ]; then
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#sudo-version"
sudo -V 2>/dev/null | grep "Sudo ver" | sed -${E} "s,$sudovB,${SED_RED},"
else echo_not_found "sudo"
fi
echo ""