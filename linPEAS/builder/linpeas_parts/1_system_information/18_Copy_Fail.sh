# Title: System Information - Copy Fail (CVE-2026-31431)
# ID: SY_Copy_Fail
# Author: GitHub Copilot
# Last Update: 30-04-2026
# Description: Check whether the running Linux kernel is vulnerable to Copy Fail (CVE-2026-31431).
# Description: Prefer a non-destructive Python runtime probe against the AF_ALG authencesn path; if Python is unavailable or inconclusive, fall back to kernel-version and exposure heuristics.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: checkCopyFail, print_2title, print_info
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Checking for Copy Fail (CVE-2026-31431)" "T1068"
print_info "https://copy.fail/"
print_info "https://www.cve.org/CVERecord?id=CVE-2026-31431"
checkCopyFail
echo ""