# Title: System Information - Date
# ID: SY_Date
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get Information about the Date
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
