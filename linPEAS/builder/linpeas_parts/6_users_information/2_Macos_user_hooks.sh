# Title: Users Information - MacOS user hooks
# ID: UG_Macos_user_hooks
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Enumerate all users login and logout hooks
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $MACPEAS 
# Initial Functions:
# Generated Global Variables: $user_home
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ];then
  print_2title "All Login and Logout hooks"
  for user_home in /Users/*/ /private/var/root/; do
    if [ -f "${user_home}Library/Preferences/com.apple.loginwindow.plist" ]; then
      echo "User: $(basename "$user_home")" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
      defaults read "${user_home}Library/Preferences/com.apple.loginwindow.plist" 2>/dev/null | grep -e "Hook" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
    fi
  done
  echo ""
fi