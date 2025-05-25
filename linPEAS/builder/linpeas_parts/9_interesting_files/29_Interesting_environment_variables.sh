# Title: Interesting Files - Interesting Environment Variables
# ID: IF_Interesting_environment_variables
# Author: Jack Vaughn
# Last Update: 25-05-2025
# Description: Searching possible sensitive environment variables inside of /proc/*/environ
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $MACPEAS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

if [ -z "$MACPEAS" ]; then
  print_2title "Searching possible sensitive environment variables inside of /proc/*/environ"
  for f in /proc/[0-9]*/environ; do
      [ -r "$f" ] || continue
      tr '\0' '\n' < "$f" | \
      grep -aEi "(token|password|secret|aws|azure|gcp|api|key|jwt|session|cookie|database|sql|mongo|postgres)" | \
      grep -avEi '(XDG_SESSION|DBUS_SESSION|systemd\/sessions)' | \
      while read -r g; do
          echo "$f: $g"
      done
  done
fi
