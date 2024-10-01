# Title: Software Information - Pam.d
# ID: SI_Pamd
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Passwords inside pam.d
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables: $pamdpass
# Fat linpeas: 0
# Small linpeas: 1


pamdpass=$(grep -Ri "passwd"  ${ROOT_FOLDER}etc/pam.d/ 2>/dev/null | grep -v ":#")
if [ "$pamdpass" ] || [ "$DEBUG" ]; then
  print_2title "Passwords inside pam.d"
  grep -Ri "passwd"  ${ROOT_FOLDER}etc/pam.d/ 2>/dev/null | grep -v ":#" | sed "s,passwd,${SED_RED},"
  echo ""
fi