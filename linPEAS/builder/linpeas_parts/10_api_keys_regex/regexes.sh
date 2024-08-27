# Title: API Keys Regex - Regexes
# ID: RX_regexes
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Regexes
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, search_for_regex
# Global Variables: $REGEXES, $TIMEOUT
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$REGEXES" ] && [ "$TIMEOUT" ]; then
    peass{REGEXES}
else
    echo "Regexes to search for API keys aren't activated, use param '-r' "
fi