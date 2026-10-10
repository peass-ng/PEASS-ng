# Title: Interesting Files - Executable files with passwords
# ID: IF_Files_with_passwords
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching possible password variables inside key folders and config files
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.001
# Functions Used: print_2title
# Global Variables: $HOMESEARCH,$ITALIC, $pwd_in_variables1, $pwd_in_variables2, $pwd_in_variables3, $pwd_in_variables4, $pwd_in_variables5, $pwd_in_variables6, $pwd_in_variables7, $pwd_in_variables8, $pwd_in_variables9, $pwd_in_variables10, $pwd_in_variables11, $SEARCH_IN_FOLDER, $TIMEOUT, $backup_folders_row
# Initial Functions:
# Generated Global Variables: $password_find_exec_end, $password_variable_regex
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && [ "$TIMEOUT" ]; then

  ##-- IF) Find possible files with passwords
  print_2title "Searching possible password variables inside key folders (limit 140)" "T1552.001"
  # Probe batching once; use the per-file terminator on older find implementations.
  password_find_exec_end='+'
  find /dev/null -exec true '{}' + >/dev/null 2>&1 || password_find_exec_end=';'
  password_variable_regex="($pwd_in_variables1|$pwd_in_variables2|$pwd_in_variables3|$pwd_in_variables4|$pwd_in_variables5|$pwd_in_variables6|$pwd_in_variables7|$pwd_in_variables8|$pwd_in_variables9|$pwd_in_variables10|$pwd_in_variables11).*[=:].+"
  if ! [ "$SEARCH_IN_FOLDER" ]; then
    # shellcheck disable=SC2067
    "$TIMEOUT" 150 find -L $HOMESEARCH -type f -exec grep -HniIE "$password_variable_regex" '{}' "$password_find_exec_end" 2>/dev/null | sed '/^.\{150\}./d' | grep -Ev "^#" | grep -iv "linpeas" | sort | uniq | head -n 70 | sed -${E} "s,$pwd_in_variables1,${SED_RED},g" | sed -${E} "s,$pwd_in_variables2,${SED_RED},g" | sed -${E} "s,$pwd_in_variables3,${SED_RED},g" | sed -${E} "s,$pwd_in_variables4,${SED_RED},g" | sed -${E} "s,$pwd_in_variables5,${SED_RED},g" | sed -${E} "s,$pwd_in_variables6,${SED_RED},g" | sed -${E} "s,$pwd_in_variables7,${SED_RED},g" | sed -${E} "s,$pwd_in_variables8,${SED_RED},g" | sed -${E} "s,$pwd_in_variables9,${SED_RED},g" | sed -${E} "s,$pwd_in_variables10,${SED_RED},g" | sed -${E} "s,$pwd_in_variables11,${SED_RED},g" &
    # shellcheck disable=SC2067
    "$TIMEOUT" 150 find -L /var/www $backup_folders_row /tmp /etc /mnt /private -type f -exec grep -HniIE "$password_variable_regex" '{}' "$password_find_exec_end" 2>/dev/null | sed '/^.\{150\}./d' | grep -Ev "^#" | grep -iv "linpeas" | sort | uniq | head -n 70 | sed -${E} "s,$pwd_in_variables1,${SED_RED},g" | sed -${E} "s,$pwd_in_variables2,${SED_RED},g" | sed -${E} "s,$pwd_in_variables3,${SED_RED},g" | sed -${E} "s,$pwd_in_variables4,${SED_RED},g" | sed -${E} "s,$pwd_in_variables5,${SED_RED},g" | sed -${E} "s,$pwd_in_variables6,${SED_RED},g" | sed -${E} "s,$pwd_in_variables7,${SED_RED},g" | sed -${E} "s,$pwd_in_variables8,${SED_RED},g" | sed -${E} "s,$pwd_in_variables9,${SED_RED},g" | sed -${E} "s,$pwd_in_variables10,${SED_RED},g" | sed -${E} "s,$pwd_in_variables11,${SED_RED},g" &
  else
    # shellcheck disable=SC2067
    "$TIMEOUT" 150 find -L $SEARCH_IN_FOLDER -type f -exec grep -HniIE "$password_variable_regex" '{}' "$password_find_exec_end" 2>/dev/null | sed '/^.\{150\}./d' | grep -Ev "^#" | grep -iv "linpeas" | sort | uniq | head -n 70 | sed -${E} "s,$pwd_in_variables1,${SED_RED},g" | sed -${E} "s,$pwd_in_variables2,${SED_RED},g" | sed -${E} "s,$pwd_in_variables3,${SED_RED},g" | sed -${E} "s,$pwd_in_variables4,${SED_RED},g" | sed -${E} "s,$pwd_in_variables5,${SED_RED},g" | sed -${E} "s,$pwd_in_variables6,${SED_RED},g" | sed -${E} "s,$pwd_in_variables7,${SED_RED},g" | sed -${E} "s,$pwd_in_variables8,${SED_RED},g" | sed -${E} "s,$pwd_in_variables9,${SED_RED},g" | sed -${E} "s,$pwd_in_variables10,${SED_RED},g" | sed -${E} "s,$pwd_in_variables11,${SED_RED},g" &
  fi
  wait
  echo ""

  ##-- IF) Find possible conf files with passwords
  print_2title "Searching possible password in config files (if k8s secrets are found you need to read the file)" "T1552.001"
  (
  if ! [ "$SEARCH_IN_FOLDER" ]; then
    # shellcheck disable=SC2086
    set -- $HOMESEARCH /var/www/ /usr/local/www/ /etc /opt /tmp /private /Applications /mnt
  else
    set -- "$SEARCH_IN_FOLDER"
  fi
  "$TIMEOUT" 150 find -L "$@" -type f '(' -name "*.conf" -o -name "*.cnf" -o -name "*.config" -o -name "*.json" -o -name "*.yml" -o -name "*.yaml" ')' \
    -exec grep -HnEiIo 'passwd.*|creden.*|^kind:\W?Secret|\Wenv:|\Wsecret:|\WsecretName:|^kind:\W?EncryptionConfiguration|\-\-encryption\-provider\-config' '{}' "$password_find_exec_end" 2>/dev/null |
    awk -v italic="$ITALIC " -v nc="$NC" '{ path=$0; sub(/:[0-9]+:.*/, "", path); if (path != previous) { print italic path nc; previous=path } print }' |
    sed -${E} "s,[pP][aA][sS][sS][wW]|[cC][rR][eE][dD][eE][nN],${SED_RED},g"
  )
  echo ""
fi
