# Title: Software Information - Cached AD Hashes
# ID: SI_Cached_AD_hashes
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: Passive metadata for local domain credential-cache candidates
# License: GNU GPL
# Version: 1.1
# Mitre: T1003.003
# Functions Used: print_2title
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables: $adhashes, $adhash, $lp_ad_cache_count
# Fat linpeas: 0
# Small linpeas: 1


adhashes=""
lp_ad_cache_count=0
for adhash in /var/lib/samba/private/secrets.tdb /var/lib/samba/passdb.tdb /var/opt/quest/vas/authcache/vas_auth.vdb /var/lib/sss/db/cache_*.ldb; do
  [ -f "$adhash" ] || continue
  if [ -z "$adhashes" ]; then
    print_2title "Local domain credential-cache candidates" "T1003.003"
    adhashes=found
  fi
  ls -ld "$adhash" 2>/dev/null
  if [ -r "$adhash" ]; then
    printf '  readable by current user (contents not inspected)\n'
  else
    printf '  not readable by current user\n'
  fi
  lp_ad_cache_count=$((lp_ad_cache_count + 1))
  if [ "$lp_ad_cache_count" -ge 32 ]; then
    printf '  additional cache files omitted after 32 entries\n'
    break
  fi
done
if [ -z "$adhashes" ] && [ "$DEBUG" ]; then
  print_2title "Local domain credential-cache candidates" "T1003.003"
fi
[ -z "$adhashes" ] || printf '%s\n\n' 'Cache-file presence alone does not prove cached passwords; check domain cache_credentials and prior logins.'
