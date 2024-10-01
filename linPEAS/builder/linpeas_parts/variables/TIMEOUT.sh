# Title: Variables - TIMEOUT
# ID: TIMEOUT
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Find timeout
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables:
# Initial Functions:
# Generated Global Variables: $TIMEOUT
# Fat linpeas: 0
# Small linpeas: 1


TIMEOUT="$(command -v timeout 2>/dev/null || echo -n '')"