# Title: System Information - Date
# ID: SY_Date
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for system date and uptime information relevant to privilege escalation:
#   - System uptime
#   - Last boot time
#   - System time
#   - Common vulnerable scenarios:
#     * Long uptime (unpatched systems)
#     * Time-based vulnerabilities
#     * Scheduled tasks timing
#     * Cron job timing
#   - Exploitation methods:
#     * Timing attacks: Abuse time-based vulnerabilities
#     * Common attack vectors:
#       - Race conditions
#       - Time-of-check to time-of-use (TOCTOU)
#       - Scheduled task abuse
#       - Cron job timing
#     * Exploit techniques:
#       - Race condition exploitation
#       - TOCTOU attacks
#       - Scheduled task manipulation
#       - Cron job abuse
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, warn_exec
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


print_2title "Date & uptime"
warn_exec date 2>/dev/null
warn_exec uptime 2>/dev/null
echo ""
