# Title: Software Information - Dovecot
# ID: SI_Dovecot
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Dovecot
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables: $dovecotpass, $dp, $df
# Fat linpeas: 0
# Small linpeas: 1


# Needs testing
dovecotpass=$(grep -r "PLAIN" /etc/dovecot 2>/dev/null)
if [ "$dovecotpass" ] || [ "$DEBUG" ]; then
  print_2title "Searching dovecot files"
  if [ -z "$dovecotpass" ]; then
    echo_not_found "dovecot credentials"
  else
    printf "%s\n" "$dovecotpass" | while read d; do
      df=$(echo $d |cut -d ':' -f1)
      dp=$(echo $d |cut -d ':' -f2-)
      echo "Found possible PLAIN text creds in $df"
      echo "$dp" | sed -${E} "s,.*,${SED_RED}," 2>/dev/null
    done
  fi
  echo ""
fi