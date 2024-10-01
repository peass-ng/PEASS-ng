# Title: Software Information - Check aws-vault
# ID: SI_Awsvault
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check aws-vault
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables: $AWSVAULT
# Fat linpeas: 0
# Small linpeas: 1


AWSVAULT="$(command -v aws-vault 2>/dev/null || echo -n '')"
if [ "$AWSVAULT" ] || [ "$DEBUG" ]; then
  print_2title "Check aws-vault"
  aws-vault list
fi
