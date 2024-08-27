# Title: Variables - LDD
# ID: LDD
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Find ldd
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables:
# Initial Functions:
# Generated Global Variables: $LDD
# Fat linpeas: 0
# Small linpeas: 1


LDD="$(command -v ldd 2>/dev/null || echo -n '')"