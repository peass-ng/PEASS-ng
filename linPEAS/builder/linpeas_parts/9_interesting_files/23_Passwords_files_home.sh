# Title: Interesting Files - Passwords files in home
# ID: IF_Passwords_files_home
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Passwords files in home
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if [ "$PSTORAGE_PASSWORD_FILES" ] || [ "$DEBUG" ]; then
  print_2title "Searching *password* or *credential* files in home (limit 70)"
  (printf "%s\n" "$PSTORAGE_PASSWORD_FILES" | grep -v "/snap/" | awk -F/ '{line_init=$0; if (!cont){ cont=0 }; $NF=""; act=$0; if (cont < 3){ print line_init; } if (cont == "3"){print "  #)There are more creds/passwds files in the previous parent folder\n"}; if (act == pre){(cont += 1)} else {cont=0}; pre=act }' | head -n 70 | sed -${E} "s,password|credential,${SED_RED}," | sed "s,There are more creds/passwds files in the previous parent folder,${C}[3m&${C}[0m,") || echo_not_found
  echo ""
fi