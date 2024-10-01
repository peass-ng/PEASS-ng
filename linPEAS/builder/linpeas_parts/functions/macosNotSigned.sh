# Title: LinPeasBase - macosNotSigned
# ID: macosNotSigned
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get the macOS unsigned applications
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables: 
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


macosNotSigned(){
  for f in $1/*; do
    if codesign -vv -d \"$f\" 2>&1 | grep -q 'not signed'; then
      echo "$f isn't signed" | sed -${E} "s,.*,${SED_RED},"
    fi
  done
}