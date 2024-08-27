# Title: Software Information - Vault-ssh
# ID: SI_Vault_ssh
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching Vault-ssh files
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if [ "$PSTORAGE_VAULT_SSH_HELPER" ] || [ "$DEBUG" ]; then
  print_2title "Searching Vault-ssh files"
  printf "$PSTORAGE_VAULT_SSH_HELPER\n"
  printf "%s\n" "$PSTORAGE_VAULT_SSH_HELPER" | while read f; do cat "$f" 2>/dev/null; vault-ssh-helper -verify-only -config "$f" 2>/dev/null; done
  echo ""
  vault secrets list 2>/dev/null
  printf "%s\n" "$PSTORAGE_VAULT_SSH_TOKEN" | sed -${E} "s,.*,${SED_RED}," 2>/dev/null
fi
echo ""