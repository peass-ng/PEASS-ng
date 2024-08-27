# Title: Users Information - PGP keys
# ID: UG_Pgp_keys
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: PGP keys
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title
# Global Variables: 
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Do I have PGP keys?"
command -v gpg 2>/dev/null || echo_not_found "gpg"
gpg --list-keys 2>/dev/null
command -v netpgpkeys 2>/dev/null || echo_not_found "netpgpkeys"
netpgpkeys --list-keys 2>/dev/null
command -v netpgp 2>/dev/null || echo_not_found "netpgp"
echo ""