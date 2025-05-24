# Title: Users Information - Last logons
# ID: UG_Last_logons
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check last logons and login history
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $knw_usrs, $nosh_usrs, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables: $EXISTS_FINGER, $ushell
# Fat linpeas: 0
# Small linpeas: 1

print_2title "Last Logons and Login History"

# Check last logins
echo ""
print_3title "Last logins"
if command -v last >/dev/null 2>&1; then
  last -n 20 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed "s,root,${SED_RED},g"
fi

# Check failed login attempts
echo ""
print_3title "Failed login attempts"
if command -v lastb >/dev/null 2>&1; then
  lastb -n 20 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed "s,root,${SED_RED},g"
fi

# Check auth logs for recent logins
echo ""
print_3title "Recent logins from auth.log (limit 20)"
if [ -f "/var/log/auth.log" ]; then
  grep -i "login\|authentication\|accepted" /var/log/auth.log 2>/dev/null | tail -n 20 | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed "s,root,${SED_RED},g"
fi

# Last time logon each user
echo ""
if command -v lastlog >/dev/null 2>&1; then
  print_3title "Last time logon each user"
  lastlog 2>/dev/null | grep -v "Never" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
fi

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