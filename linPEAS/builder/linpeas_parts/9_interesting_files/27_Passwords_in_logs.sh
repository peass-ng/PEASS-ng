# Title: Interesting Files - Passwords inside logs
# ID: IF_Passwords_in_logs
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Passwords inside logs
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.001
# Functions Used: print_2title
# Global Variables: $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $log_find_exec_end
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Searching passwords inside logs (limit 70)" "T1552.001"
  # Probe batching once; use the per-file terminator on older find implementations.
  log_find_exec_end='+'
  find /dev/null -exec true '{}' + >/dev/null 2>&1 || log_find_exec_end=';'
  # shellcheck disable=SC2067
  (find /var/log/ /var/logs/ /private/var/log -type f -exec grep -R -H -i "pwd\|passw" "{}" "$log_find_exec_end") 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | grep -v "File does not exist:\|modules-config/config-set-passwords\|config-set-passwords already ran\|script not found or unable to stat:\|\"GET /.*\" 404" | head -n 70 | sed -${E} "s,pwd|passw,${SED_RED},"
  echo ""
fi
