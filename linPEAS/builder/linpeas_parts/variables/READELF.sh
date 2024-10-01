# Title: Variables - READELF
# ID: READELF
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Find readelf
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables:
# Initial Functions:
# Generated Global Variables: $READELF
# Fat linpeas: 0
# Small linpeas: 1


READELF="$(command -v readelf 2>/dev/null || echo -n '')"