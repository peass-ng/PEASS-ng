# Title: System Information - CVE_2021_3560
# ID: SY_CVE_2021_3560
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for Polkit vulnerability (CVE-2021-3560) that could lead to privilege escalation:
#   - Vulnerable Polkit versions:
#     * polkit 0.105-26 (Ubuntu)
#     * polkit 0.117-2 (RHEL)
#     * polkit 0.115-6 (RHEL)
#   - Common vulnerable scenarios:
#     * Unpatched Polkit versions
#     * Default Polkit configurations
#   - Exploitation methods:
#     * Race condition in Polkit authentication
#     * Common attack vectors:
#       - Authentication bypass
#       - Privilege escalation
#       - Root access acquisition
#     * Exploit techniques:
#       - Race condition exploitation
#       - Authentication bypass
#       - Privilege escalation
#       - System compromise
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0

if apt list --installed 2>/dev/null | grep -E 'polkit.*0\.105-26' | grep -qEv 'ubuntu1\.[1-9]' || \
   yum list installed 2>/dev/null | grep -qE 'polkit.*\(0\.117-2\|0\.115-6\|0\.11[3-9]\)' || \
   rpm -qa 2>/dev/null | grep -qE 'polkit.*\(0\.117-2\|0\.115-6\|0\.11[3-9]\)'; then
    echo "Vulnerable to CVE-2021-3560" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    echo ""
fi
