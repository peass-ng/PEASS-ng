# Title: Variables - mygroups
# ID: mygroups
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: My groups
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $mygroups
# Fat linpeas: 0
# Small linpeas: 1


mygroups=$(groups 2>/dev/null | tr " " "|")
