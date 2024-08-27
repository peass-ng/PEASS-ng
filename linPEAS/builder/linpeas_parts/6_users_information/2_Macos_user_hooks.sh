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
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ];then
  print_2title "All Login and Logout hooks"
  defaults read /Users/*/Library/Preferences/com.apple.loginwindow.plist 2>/dev/null | grep -e "Hook"
  defaults read /private/var/root/Library/Preferences/com.apple.loginwindow.plist
  echo ""

fi