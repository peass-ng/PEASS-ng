# Title: Software Information - Logstash
# ID: SI_Logstash
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching logstash files
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG, $knw_usrs, $nosh_usrs, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if [ "$PSTORAGE_LOGSTASH" ] || [ "$DEBUG" ]; then
  print_2title "Searching logstash files"
  printf "$PSTORAGE_LOGSTASH"
  printf "%s\n" "$PSTORAGE_LOGSTASH" | while read d; do
    if [ -r "$d/startup.options" ]; then
      echo "Logstash is running as user:"
      cat "$d/startup.options" 2>/dev/null | grep "LS_USER\|LS_GROUP" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed -${E} "s,$USER,${SED_LIGHT_MAGENTA}," | sed -${E} "s,root,${SED_RED},"
    fi
    cat "$d/conf.d/out*" | grep "exec\s*{\|command\s*=>" | sed -${E} "s,exec\W*\{|command\W*=>,${SED_RED},"
    cat "$d/conf.d/filt*" | grep "path\s*=>\|code\s*=>\|ruby\s*{" | sed -${E} "s,path\W*=>|code\W*=>|ruby\W*\{,${SED_RED},"
  done
fi
echo ""