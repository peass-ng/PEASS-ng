# Title: LinPeasBase - check_if_su_brute
# ID: check_if_su_brute
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Try to brute-force su
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables:
# Initial Functions:
# Generated Global Variables: $EXISTS_SU, $error
# Fat linpeas: 0
# Small linpeas: 1


check_if_su_brute(){
  EXISTS_SU="$(command -v su 2>/dev/null || echo -n '')"
  error=$(echo "" | timeout 1 su $(whoami) -c whoami 2>&1);
  if [ "$EXISTS_SU" ] && ! echo $error | grep -q "must be run from a terminal"; then
    echo "1"
  fi
}