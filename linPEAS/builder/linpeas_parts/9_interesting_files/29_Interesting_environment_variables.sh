# Title: Interesting Files - Interesting Environment Variables
# ID: IF_Interesting_environment_variables
# Author: Jack Vaughn
# Last Update: 25-05-2025
# Description: Searching possible sensitive environment variables inside of /proc/*/environ
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $MACPEAS, $NoEnvVars, $EnvVarsRed
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

if [ -z "$MACPEAS" ]; then
  print_2title "Checking all env variables in /proc/*/environ removing duplicates and filtering out useless env vars"
  cat /proc/[0-9]*/environ 2>/dev/null | \
  tr '\0' '\n' | \
  grep -Eiv "$NoEnvVars" | \
  sort -u | \
  sed -${E} "s,$EnvVarsRed,${SED_RED},g"
fi
