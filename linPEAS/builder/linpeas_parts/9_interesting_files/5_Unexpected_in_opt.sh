# Title: Interesting Files - Unexpected in /opt
# ID: IF_Unexpected_in_opt
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Unexpected in /opt
# License: GNU GPL
# Version: 1.0
# Mitre: T1083
# Functions Used: print_2title
# Global Variables: $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0

if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ "$(ls /opt 2>/dev/null)" ]; then
    print_2title "Unexpected in /opt (usually empty)" "T1083"
    ls -la /opt
    echo ""
  fi
fi