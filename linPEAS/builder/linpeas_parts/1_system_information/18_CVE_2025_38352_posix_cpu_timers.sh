# Title: System - CVE-2025-38352 POSIX CPU timers
# ID: SY_CVE_2025_38352_posix_cpu_timers
# Author: HT Bot
# Last Update: 09-09-2025
# Description: Quick checks related to potential exposure to CVE-2025-38352 (POSIX CPU timers).
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_list, echo_no
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

print_2title "CVE-2025-38352 - POSIX CPU timers"

print_list "Kernel version ................ "$NC
(uname -r 2>/dev/null || echo_no) | head -n 1

print_list "Kernel build info ............. "$NC
(uname -v 2>/dev/null || echo_no) | head -n 1

print_list "POSIX timers (hint) ........... "$NC
( (grep -qi posix /proc/timer_list 2>/dev/null && echo "present") || echo_no )

echo ""
