# Title: Interesting Files - Executable files with passwords
# ID: IF_Files_with_passwords
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching possible password variables inside key folders and config files
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $HOMESEARCH,$ITALIC, $pwd_in_variables1, $pwd_in_variables2, $pwd_in_variables3, $pwd_in_variables4, $pwd_in_variables5, $pwd_in_variables6, $pwd_in_variables7, $pwd_in_variables8, $pwd_in_variables9, $pwd_in_variables10, $pwd_in_variables11, $SEARCH_IN_FOLDER, $TIMEOUT, $backup_folders_row
# Initial Functions:
# Generated Global Variables: $ppicf
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && [ "$TIMEOUT" ]; then

  ##-- IF) Find possible files with passwords
  print_2title "Searching possible password variables inside key folders (limit 140)"
  if ! [ "$SEARCH_IN_FOLDER" ]; then
    timeout 150 find $HOMESEARCH -exec grep -HnRiIE "($pwd_in_variables1|$pwd_in_variables2|$pwd_in_variables3|$pwd_in_variables4|$pwd_in_variables5|$pwd_in_variables6|$pwd_in_variables7|$pwd_in_variables8|$pwd_in_variables9|$pwd_in_variables10|$pwd_in_variables11).*[=:].+" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | grep -Ev "^#" | grep -iv "linpeas" | sort | uniq | head -n 70 | sed -${E} "s,$pwd_in_variables1,${SED_RED},g" | sed -${E} "s,$pwd_in_variables2,${SED_RED},g" | sed -${E} "s,$pwd_in_variables3,${SED_RED},g" | sed -${E} "s,$pwd_in_variables4,${SED_RED},g" | sed -${E} "s,$pwd_in_variables5,${SED_RED},g" | sed -${E} "s,$pwd_in_variables6,${SED_RED},g" | sed -${E} "s,$pwd_in_variables7,${SED_RED},g" | sed -${E} "s,$pwd_in_variables8,${SED_RED},g" | sed -${E} "s,$pwd_in_variables9,${SED_RED},g" | sed -${E} "s,$pwd_in_variables10,${SED_RED},g" | sed -${E} "s,$pwd_in_variables11,${SED_RED},g" &
    timeout 150 find /var/www $backup_folders_row /tmp /etc /mnt /private grep -HnRiIE "($pwd_in_variables1|$pwd_in_variables2|$pwd_in_variables3|$pwd_in_variables4|$pwd_in_variables5|$pwd_in_variables6|$pwd_in_variables7|$pwd_in_variables8|$pwd_in_variables9|$pwd_in_variables10|$pwd_in_variables11).*[=:].+" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | grep -Ev "^#" | grep -iv "linpeas" | sort | uniq | head -n 70 | sed -${E} "s,$pwd_in_variables1,${SED_RED},g" | sed -${E} "s,$pwd_in_variables2,${SED_RED},g" | sed -${E} "s,$pwd_in_variables3,${SED_RED},g" | sed -${E} "s,$pwd_in_variables4,${SED_RED},g" | sed -${E} "s,$pwd_in_variables5,${SED_RED},g" | sed -${E} "s,$pwd_in_variables6,${SED_RED},g" | sed -${E} "s,$pwd_in_variables7,${SED_RED},g" | sed -${E} "s,$pwd_in_variables8,${SED_RED},g" | sed -${E} "s,$pwd_in_variables9,${SED_RED},g" | sed -${E} "s,$pwd_in_variables10,${SED_RED},g" | sed -${E} "s,$pwd_in_variables11,${SED_RED},g" &
  else
    timeout 150 find $SEARCH_IN_FOLDER -exec grep -HnRiIE "($pwd_in_variables1|$pwd_in_variables2|$pwd_in_variables3|$pwd_in_variables4|$pwd_in_variables5|$pwd_in_variables6|$pwd_in_variables7|$pwd_in_variables8|$pwd_in_variables9|$pwd_in_variables10|$pwd_in_variables11).*[=:].+" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | grep -Ev "^#" | grep -iv "linpeas" | sort | uniq | head -n 70 | sed -${E} "s,$pwd_in_variables1,${SED_RED},g" | sed -${E} "s,$pwd_in_variables2,${SED_RED},g" | sed -${E} "s,$pwd_in_variables3,${SED_RED},g" | sed -${E} "s,$pwd_in_variables4,${SED_RED},g" | sed -${E} "s,$pwd_in_variables5,${SED_RED},g" | sed -${E} "s,$pwd_in_variables6,${SED_RED},g" | sed -${E} "s,$pwd_in_variables7,${SED_RED},g" | sed -${E} "s,$pwd_in_variables8,${SED_RED},g" | sed -${E} "s,$pwd_in_variables9,${SED_RED},g" | sed -${E} "s,$pwd_in_variables10,${SED_RED},g" | sed -${E} "s,$pwd_in_variables11,${SED_RED},g" &
  fi
  wait
  echo ""

  ##-- IF) Find possible conf files with passwords
  print_2title "Searching possible password in config files (if k8s secrets are found you need to read the file)"
  if ! [ "$SEARCH_IN_FOLDER" ]; then
    ppicf=$(timeout 150 find $HOMESEARCH /var/www/ /usr/local/www/ /etc /opt /tmp /private /Applications /mnt -name "*.conf" -o -name "*.cnf" -o -name "*.config" -name "*.json" -name "*.yml" -name "*.yaml" 2>/dev/null)
  else
    ppicf=$(timeout 150 find $SEARCH_IN_FOLDER -name "*.conf" -o -name "*.cnf" -o -name "*.config" -name "*.json" -name "*.yml" -name "*.yaml" 2>/dev/null)
  fi
  printf "%s\n" "$ppicf" | while read f; do
    if grep -qEiI 'passwd.*|creden.*|^kind:\W?Secret|\Wenv:|\Wsecret:|\WsecretName:|^kind:\W?EncryptionConfiguration|\-\-encryption\-provider\-config' \"$f\" 2>/dev/null; then
      echo "$ITALIC $f$NC"
      grep -HnEiIo 'passwd.*|creden.*|^kind:\W?Secret|\Wenv:|\Wsecret:|\WsecretName:|^kind:\W?EncryptionConfiguration|\-\-encryption\-provider\-config' "$f" 2>/dev/null | sed -${E} "s,[pP][aA][sS][sS][wW]|[cC][rR][eE][dD][eE][nN],${SED_RED},g"
    fi
  done
  echo ""
fi
