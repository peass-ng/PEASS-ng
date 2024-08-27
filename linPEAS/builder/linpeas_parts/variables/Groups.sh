# Title: Variables - Groups
# ID: Groups
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get groups of the current user
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables: $USER
# Initial Functions:
# Generated Global Variables: $Groups
# Fat linpeas: 0
# Small linpeas: 1


Groups="ImPoSSssSiBlEee"$(groups "$USER" 2>/dev/null | cut -d ":" -f 2 | tr ' ' '|')