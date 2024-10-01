# Title: Software Information - Splunk
# ID: SI_Splunk
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: passwd files (splunk)
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables: $SPLUNK_BIN
# Fat linpeas: 0
# Small linpeas: 1


SPLUNK_BIN="$(command -v splunk 2>/dev/null || echo -n '')"
if [ "$PSTORAGE_SPLUNK" ] || [ "$SPLUNK_BIN" ] || [ "$DEBUG" ]; then
  print_2title "Searching uncommon passwd files (splunk)"
  if [ "$SPLUNK_BIN" ]; then echo "splunk binary was found installed on $SPLUNK_BIN" | sed "s,.*,${SED_RED},"; fi
  printf "%s\n" "$PSTORAGE_SPLUNK" | grep -v ".htpasswd" | sort | uniq | while read f; do
    if [ -f "$f" ] && ! [ -x "$f" ]; then
      echo "passwd file: $f" | sed "s,$f,${SED_RED},"
      cat "$f" 2>/dev/null | grep "'pass'|'password'|'user'|'database'|'host'|\$" | sed -${E} "s,password|pass|user|database|host|\$,${SED_RED},"
    fi
  done
  echo ""
fi