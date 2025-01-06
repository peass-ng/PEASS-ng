# Title: Software Information - containerd installed
# ID: SI_Containerd
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: containerd installed
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $containerd
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  containerd=$(command -v ctr || echo -n '')
  if [ "$containerd" ] || [ "$DEBUG" ]; then
    print_2title "Checking if containerd(ctr) is available"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#containerd-ctr-privilege-escalation"
    if [ "$containerd" ]; then
      echo "ctr was found in $containerd, you may be able to escalate privileges with it" | sed -${E} "s,.*,${SED_RED},"
      ctr image list 2>&1
    fi
    echo ""
  fi
fi
