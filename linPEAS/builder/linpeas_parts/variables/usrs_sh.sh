# Title: Variables - Users with and withuot shell
# ID: usrs_sh
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check for users with and without shell
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables: $MACPEAS
# Initial Functions:
# Generated Global Variables: $sh_usrs, $nosh_usrs, $ushell, $uname
# Fat linpeas: 0
# Small linpeas: 1


if [ "$MACPEAS" ]; then
  sh_usrs="ImPoSSssSiBlEee"
  nosh_usrs="ImPoSSssSiBlEee"
  dscl . list /Users | while read uname; do
    ushell=$(dscl . -read "/Users/$uname" UserShell | cut -d " " -f2)
    if  grep -q \"$ushell\" /etc/shells; then sh_usrs="$sh_usrs|$uname"; else nosh_usrs="$nosh_usrs|$uname"; fi
  done
else
  sh_usrs=$(cat /etc/passwd 2>/dev/null | grep -v "^root:" | grep -i "sh$" | cut -d ":" -f 1 | tr '\n' '|' | sed 's/|bin|/|bin[\\\s:]|^bin$|/' | sed 's/|sys|/|sys[\\\s:]|^sys$|/' | sed 's/|daemon|/|daemon[\\\s:]|^daemon$|/')"ImPoSSssSiBlEee" #Modified bin, sys and daemon so they are not colored everywhere
  nosh_usrs=$(cat /etc/passwd 2>/dev/null | grep -i -v "sh$" | sort | cut -d ":" -f 1 | tr '\n' '|' | sed 's/|bin|/|bin[\\\s:]|^bin$|/')"ImPoSSssSiBlEee"
fi