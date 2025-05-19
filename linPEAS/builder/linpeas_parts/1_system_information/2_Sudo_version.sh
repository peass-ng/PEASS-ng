# Title: System Information - Sudo Version
# ID: SY_Sudo_version
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for sudo vulnerabilities and misconfigurations that could lead to privilege escalation:
#   - Vulnerable sudo versions with known exploits
#   - Common vulnerable versions and CVEs:
#     * CVE-2021-3156 (Baron Samedit): Heap overflow in sudo
#     * CVE-2021-23239: Potential privilege escalation
#     * CVE-2021-23240: Potential privilege escalation
#     * CVE-2021-23241: Potential privilege escalation
#   - Exploitation methods:
#     * Version exploits: Use known exploits for vulnerable sudo versions
#     * Common targets: sudo < 1.9.5p2 (Baron Samedit)
#     * Exploit techniques:
#       - Heap overflow exploitation
#       - Race conditions
#       - Memory corruption
#       - Command injection
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