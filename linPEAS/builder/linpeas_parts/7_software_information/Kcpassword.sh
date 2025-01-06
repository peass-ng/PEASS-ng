# Title: Software Information - kcpassword
# ID: SI_Kcpassword
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Analyzing kcpassword files
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if [ "$PSTORAGE_KCPASSWORD" ] || [ "$DEBUG" ]; then
  print_2title "Analyzing kcpassword files"
  print_info "https://book.hacktricks.wiki/en/macos-hardening/macos-security-and-privilege-escalation/macos-files-folders-and-binaries/macos-sensitive-locations.html#kcpassword"
  printf "%s\n" "$PSTORAGE_KCPASSWORD" | while read f; do
    echo "$f" | sed -${E} "s,.*,${SED_RED},"
    base64 "$f" 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  done
  echo ""
fi