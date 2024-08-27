# Title: Variables - notmounted
# ID: notmounted
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Not mounted folders
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $mountG, $mounted
# Initial Functions:
# Generated Global Variables: $notmounted
# Fat linpeas: 0
# Small linpeas: 1


notmounted=$(cat /etc/fstab 2>/dev/null | grep "^/" | grep -Ev "$mountG" | awk '{print $1}' | grep -Ev "$mounted" | tr '\n' '|')"ImPoSSssSiBlEee"