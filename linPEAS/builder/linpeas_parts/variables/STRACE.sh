# Title: Variables - STRACE
# ID: STRACE
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Find strace
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables:
# Initial Functions:
# Generated Global Variables: $STRACE
# Fat linpeas: 0
# Small linpeas: 1


STRACE="$(command -v strace 2>/dev/null || echo -n '')"