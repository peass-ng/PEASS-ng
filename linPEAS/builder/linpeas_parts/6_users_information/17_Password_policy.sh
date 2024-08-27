# Title: Users Information - Password policy
# ID: UG_Password_policy
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get assword policy
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title
# Global Variables: $EXTRA_CHECKS, $MACPEAS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$EXTRA_CHECKS" ]; then
  print_2title "Password policy"
  grep "^PASS_MAX_DAYS\|^PASS_MIN_DAYS\|^PASS_WARN_AGE\|^ENCRYPT_METHOD" /etc/login.defs 2>/dev/null || echo_not_found "/etc/login.defs"
  echo ""

  if [ "$MACPEAS" ]; then
    print_2title "Relevant last user info and user configs"
    defaults read /Library/Preferences/com.apple.loginwindow.plist 2>/dev/null
    echo ""

    print_2title "Guest user status"
    sysadminctl -afpGuestAccess status | sed -${E} "s,enabled,${SED_RED}," | sed -${E} "s,disabled,${SED_GREEN},"
    sysadminctl -guestAccount status | sed -${E} "s,enabled,${SED_RED}," | sed -${E} "s,disabled,${SED_GREEN},"
    sysadminctl -smbGuestAccess status | sed -${E} "s,enabled,${SED_RED}," | sed -${E} "s,disabled,${SED_GREEN},"
    echo ""
  fi
fi