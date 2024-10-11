# Title: System Information - CVE_2021_3560
# ID: SY_CVE_2021_3560
# Author: Carlos Polop
# Last Update: 07-10-2024
# Description: CVE-2021-3560 - paper box from HTB
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0

if apt list --installed 2>/dev/null | grep -q 'polkit.*0\.105-26' || \
   yum list installed 2>/dev/null | grep -q 'polkit.*\(0\.117-2\|0\.115-6\)' || \
   rpm -qa 2>/dev/null | grep -q 'polkit.*\(0\.117-2\|0\.115-6\)'; then
    echo "Vulnerable to CVE-2021-3560" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    echo ""
fi

