# Title: System Information - CPU info
# ID: SY_CPU_info
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get Information about the CPU
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, warn_exec
# Global Variables: $DEBUG, $EXTRA_CHECKS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$EXTRA_CHECKS" ] || [ "$DEBUG" ]; then
    print_2title "CPU info"
    warn_exec lscpu 2>/dev/null
    echo ""
fi