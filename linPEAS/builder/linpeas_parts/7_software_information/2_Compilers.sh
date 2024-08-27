# Title: Software Information - Compilers
# ID: SI_Compilers
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Search for compilers
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Installed Compilers"
  (dpkg --list 2>/dev/null | grep "compiler" | grep -v "decompiler\|lib" 2>/dev/null || yum list installed 'gcc*' 2>/dev/null | grep gcc 2>/dev/null; command -v gcc g++ 2>/dev/null || locate -r "/gcc[0-9\.-]\+$" 2>/dev/null | grep -v "/doc/");
  echo ""

  if [ "$(command -v pkg 2>/dev/null || echo -n '')" ]; then
      print_2title "Vulnerable Packages"
      pkg audit -F | sed -${E} "s,vulnerable,${SED_RED},g"
      echo ""
  fi

  if [ "$(command -v brew 2>/dev/null || echo -n '')" ]; then
      print_2title "Brew Installed Packages"
      brew list
      echo ""
  fi
fi