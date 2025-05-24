# Title: Users Information - Login now
# ID: UG_Login_now
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check currently logged in users and their sessions
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $knw_usrs, $nosh_usrs, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Currently Logged in Users"

# Check basic user information
echo ""
print_3title "Basic user information"
(w || who || finger || users) 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed "s,root,${SED_RED},g"

# Check for active sessions
echo ""
print_3title "Active sessions"
if command -v w >/dev/null 2>&1; then
  w 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed "s,root,${SED_RED},g"
fi

# Check for logged in users via utmp
echo ""
print_3title "Logged in users (utmp)"
if [ -f "/var/run/utmp" ]; then
  who -a 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed "s,root,${SED_RED},g"
fi

# Check for SSH sessions
echo ""
print_3title "SSH sessions"
if command -v ss >/dev/null 2>&1; then
  ss -tnp | grep ":22" 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed "s,root,${SED_RED},g"
fi

# Check for screen sessions
echo ""
print_3title "Screen sessions"
if command -v screen >/dev/null 2>&1; then
  screen -ls 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed "s,root,${SED_RED},g"
fi

# Check for tmux sessions
echo ""
print_3title "Tmux sessions"
if command -v tmux >/dev/null 2>&1; then
  tmux list-sessions 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed "s,root,${SED_RED},g"
fi
echo ""