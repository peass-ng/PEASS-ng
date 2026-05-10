# Title: System Information - Dirty Frag (CVE-2026-43284 / CVE-2026-43500)
# ID: SY_Dirty_Frag
# Author: Samuel Monsempes
# Last Update: 10-05-2026
# Description: Check whether the running Linux kernel is exposed to Dirty Frag (CVE-2026-43284 and CVE-2026-43500).
# Description: Inspects xfrm-ESP and rxrpc module state, built-in kernel config, modprobe.d mitigation, user-namespace mitigation, CAP_NET_ADMIN on the current process, and a kernel build-date heuristic. Read-only.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: checkDirtyFrag, print_2title, print_info
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1
print_2title "Checking for Dirty Frag (CVE-2026-43284 / CVE-2026-43500)" "T1068"
print_info "https://ubuntu.com/blog/dirty-frag-linux-vulnerability-fixes-available"
print_info "https://www.cve.org/CVERecord?id=CVE-2026-43284"
print_info "https://www.cve.org/CVERecord?id=CVE-2026-43500"
checkDirtyFrag
echo ""
