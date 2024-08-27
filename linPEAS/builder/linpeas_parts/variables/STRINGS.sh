# Title: Variables - STRINGS
# ID: STRINGS
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Find strings
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables:
# Initial Functions:
# Generated Global Variables: $STRINGS
# Fat linpeas: 0
# Small linpeas: 1


STRINGS="$(command -v strings 2>/dev/null || echo -n '')"