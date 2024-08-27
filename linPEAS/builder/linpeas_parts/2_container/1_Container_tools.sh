# Title: Container - Container Tools
# ID: CT_Container_tools
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Find container related tools in the PATH of the system
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Container related tools present (if any):"
command -v docker || echo -n ''
command -v lxc || echo -n ''
command -v rkt || echo -n ''
command -v kubectl || echo -n ''
command -v podman || echo -n ''
command -v runc || echo -n ''