# Title: Users Information - MacOS my user hooks
# ID: UG_Macos_my_user_hooks
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get current user Login and Logout hooks
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $HOME, $MACPEAS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ];then
  print_2title "Current user Login and Logout hooks"
  defaults read $HOME/Library/Preferences/com.apple.loginwindow.plist 2>/dev/null | grep -e "Hook"
  echo ""
fi