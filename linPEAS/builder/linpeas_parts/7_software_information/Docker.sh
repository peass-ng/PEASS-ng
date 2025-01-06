# Title: Software Information - Docker
# ID: SI_Docker
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Docker
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $IAMROOT
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if [ "$PSTORAGE_DOCKER" ] || [ "$DEBUG" ]; then
  print_2title "Searching docker files (limit 70)"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/docker-security/index.html#docker-breakout--privilege-escalation"
  printf "%s\n" "$PSTORAGE_DOCKER" | head -n 70 | while read f; do
    ls -l "$f" 2>/dev/null
    if ! [ "$IAMROOT" ] && [ -S "$f" ] && [ -w "$f" ]; then
      echo "Docker related socket ($f) is writable" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    fi
  done
  echo ""
fi