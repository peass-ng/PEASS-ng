# Title: Users Information - Users with console
# ID: UG_Users_with_console
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Users with console
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $MACPEAS, $sh_usrs, $USER 
# Initial Functions:
# Generated Global Variables: $ushell, $no_shells, $unexpected_shells
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Users with console"
if [ "$MACPEAS" ]; then
  dscl . list /Users | while read un; do
    ushell=$(dscl . -read "/Users/$un" UserShell | cut -d " " -f2)
    if grep -q "$ushell" /etc/shells; then #Shell user
      dscl . -read "/Users/$un" UserShell RealName RecordName Password NFSHomeDirectory 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
      echo ""
    fi
  done
else
  no_shells=$(grep -Ev "sh$" /etc/passwd 2>/dev/null | cut -d ':' -f 7 | sort | uniq)
  unexpected_shells=""
  printf "%s\n" "$no_shells" | while read f; do
    if $f -c 'whoami' 2>/dev/null | grep -q "$USER"; then
      unexpected_shells="$f\n$unexpected_shells"
    fi
  done
  grep "sh$" /etc/passwd 2>/dev/null | sort | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
  if [ "$unexpected_shells" ]; then
    printf "%s" "These unexpected binaries are acting like shells:\n$unexpected_shells" | sed -${E} "s,/.*,${SED_RED},g"
    echo "Unexpected users with shells:"
    printf "%s\n" "$unexpected_shells" | while read f; do
      if [ "$f" ]; then
        grep -E "${f}$" /etc/passwd | sed -${E} "s,/.*,${SED_RED},g"
      fi
    done
  fi
fi
echo ""