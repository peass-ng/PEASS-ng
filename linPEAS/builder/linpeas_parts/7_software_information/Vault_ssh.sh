# Title: Software Information - Vault-ssh
# ID: SI_Vault_ssh
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: Report cached Vault CLI token paths independently of SSH helper configuration
# License: GNU GPL
# Version: 1.1
# Mitre: T1552.004
# Functions Used: print_2title
# Global Variables: $DEBUG, $E, $SED_RED
# Cached storage inputs (builder): $PSTORAGE_VAULT_SSH_HELPER, $PSTORAGE_VAULT_SSH_TOKEN
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if [ "$PSTORAGE_VAULT_SSH_TOKEN" ]; then
  print_2title "Vault CLI token file paths" "T1552.004"
  printf '%s\n' "$PSTORAGE_VAULT_SSH_TOKEN" | awk 'NR <= 10 { print } END { if (NR > 10) print "Additional token paths omitted (partial inventory)" }' | sed -${E} "1,10s,.*,${SED_RED},"
  echo "Paths only; token policy, SSH role, and login reachability require separate review."
  echo ""
fi

if [ "$PSTORAGE_VAULT_SSH_HELPER" ] || [ "$DEBUG" ]; then
  print_2title "Searching Vault-ssh files" "T1552.004"
  printf "$PSTORAGE_VAULT_SSH_HELPER\n"
  printf "%s\n" "$PSTORAGE_VAULT_SSH_HELPER" | while read f; do cat "$f" 2>/dev/null; vault-ssh-helper -verify-only -config "$f" 2>/dev/null; done
  echo ""
  vault secrets list 2>/dev/null
fi
echo ""
