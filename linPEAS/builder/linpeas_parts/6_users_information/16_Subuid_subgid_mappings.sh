# Title: Users Information - subuid/subgid mappings
# ID: UG_Subuid_subgid_mappings
# Author: Carlos Polop
# Last Update: 13-02-2026
# Description: Show delegated user namespace ID ranges from /etc/subuid and /etc/subgid.
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $MACPEAS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "User namespace mappings (subuid/subgid)"
if [ "$MACPEAS" ]; then
  echo "Not applicable on macOS"
else
  if [ -r /etc/subuid ]; then
    echo "subuid:"
    grep -v -E '^\s*#|^\s*$' /etc/subuid 2>/dev/null
  else
    echo "/etc/subuid not readable or not present"
  fi

  if [ -r /etc/subgid ]; then
    echo ""
    echo "subgid:"
    grep -v -E '^\s*#|^\s*$' /etc/subgid 2>/dev/null
  else
    echo "/etc/subgid not readable or not present"
  fi
fi
echo ""

