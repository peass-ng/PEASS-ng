# Title: Variables - sudoB
# ID: sudoB
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Known dangerous sudoers configs
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $sudoB
# Fat linpeas: 0
# Small linpeas: 1


sudoB="$(whoami)|ALL:ALL|ALL : ALL|ALL|env_keep|NOPASSWD|SETENV|/apache2|/cryptsetup|/mount"