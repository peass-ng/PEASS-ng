# Title: Software Information - Cached AD Hashes
# ID: SI_Cached_AD_hashes
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Cached AD Hashes
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables: $adhashes
# Fat linpeas: 0
# Small linpeas: 1


adhashes=$(ls "/var/lib/samba/private/secrets.tdb" "/var/lib/samba/passdb.tdb" "/var/opt/quest/vas/authcache/vas_auth.vdb" "/var/lib/sss/db/cache_*" 2>/dev/null)
if [ "$adhashes" ] || [ "$DEBUG" ]; then
  print_2title "Searching AD cached hashes"
  ls -l "/var/lib/samba/private/secrets.tdb" "/var/lib/samba/passdb.tdb" "/var/opt/quest/vas/authcache/vas_auth.vdb" "/var/lib/sss/db/cache_*" 2>/dev/null
  echo ""
fi