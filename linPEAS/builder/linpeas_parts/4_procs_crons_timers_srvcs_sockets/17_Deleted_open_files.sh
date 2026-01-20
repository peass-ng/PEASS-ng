# Title: Processes & Cron & Services & Timers - Deleted open files
# ID: PR_Deleted_open_files
# Author: Carlos Polop
# Last Update: 2025-01-07
# Description: Identify deleted files still held open by running processes
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $EXTRA_CHECKS, $E, $SED_RED
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

if [ "$(command -v lsof 2>/dev/null || echo -n '')" ] || [ "$DEBUG" ]; then
    print_2title "Deleted files still open"
    print_info "Open deleted files can hide tools and still consume disk space"
    lsof +L1 2>/dev/null | sed -${E} "s,\\(deleted\\),${SED_RED},g"
    echo ""
elif [ "$EXTRA_CHECKS" ] || [ "$DEBUG" ]; then
    print_2title "Deleted files still open"
    print_info "lsof not found, scanning /proc for deleted file descriptors"
    ls -l /proc/[0-9]*/fd 2>/dev/null | grep "(deleted)" | sed -${E} "s,\\(deleted\\),${SED_RED},g" | head -n 200
    echo ""
fi
