# Title: Users Information - Home SSH permissions
# ID: UG_Home_ssh_permissions
# Author: Carlos Polop
# Last Update: 13-02-2026
# Description: Enumerate .ssh directories and key file permissions in user homes.
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $MACPEAS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Home .ssh directories and key permissions"
if [ "$MACPEAS" ]; then
  for d in /Users/*/.ssh; do
    [ -d "$d" ] || continue
    ls -ld "$d" 2>/dev/null
    ls -l "$d"/authorized_keys "$d"/id_* "$d"/*.pub 2>/dev/null | sed "s,^,  ,"
  done
else
  for d in /home/*/.ssh /root/.ssh; do
    [ -d "$d" ] || continue
    ls -ld "$d" 2>/dev/null
    ls -l "$d"/authorized_keys "$d"/id_* "$d"/*.pub 2>/dev/null | sed "s,^,  ,"
  done
fi
echo ""
