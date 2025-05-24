# Title: Users Information - MacOS SystemKey
# ID: UG_Macos_systemkey
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get macOS SystemKey information (used for FileVault encryption)
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $MACPEAS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ];then
  print_2title "SystemKey"
  echo "The SystemKey is used by FileVault to encrypt/decrypt the volume. If you can read it, you might be able to decrypt the disk."
  echo -e "\nSystemKey file permissions:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
  ls -l /var/db/SystemKey 2>/dev/null | sed -${E} "s,.*,${SED_RED_YELLOW},g"
  
  if [ -r "/var/db/SystemKey" ]; then
    echo -e "\nWARNING: You can read /var/db/SystemKey!" | sed -${E} "s,.*,${SED_RED},g"
    echo "SystemKey content (first 24 bytes after header):" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    hexdump -s 8 -n 24 -e '1/1 "%.2x"' /var/db/SystemKey | sed -${E} "s,.*,${SED_RED_YELLOW},g"
  fi
  echo ""
fi