# Title: Users Information - Login info
# ID: UG_Login_info
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Last time logon each user
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $knw_usrs, $MACPEAS, $nosh_usrs, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables: EXISTS_FINGER, ushell
# Fat linpeas: 0
# Small linpeas: 0


print_2title "Last time logon each user"
lastlog 2>/dev/null | grep -v "Never" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"

EXISTS_FINGER="$(command -v finger 2>/dev/null || echo -n '')"
if [ "$MACPEAS" ] && [ "$EXISTS_FINGER" ]; then
  dscl . list /Users | while read un; do
    ushell=$(dscl . -read "/Users/$un" UserShell | cut -d " " -f2)
    if grep -q "$ushell" /etc/shells; then #Shell user
      finger "$un" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
      echo ""
    fi
  done
fi
echo ""