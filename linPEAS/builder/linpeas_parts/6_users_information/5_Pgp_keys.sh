# Title: Users Information - PGP keys
# ID: UG_Pgp_keys
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check for PGP keys and related files that might contain sensitive information
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $HOME
# Initial Functions:
# Generated Global Variables: $pgp_file
# Fat linpeas: 0
# Small linpeas: 1


print_2title "PGP Keys and Related Files"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#pgp-keys"

# Check for GPG
echo "GPG:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
if command -v gpg >/dev/null 2>&1; then
  echo "GPG is installed, listing keys:"
  gpg --list-keys 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
  # Check for private keys
  gpg --list-secret-keys 2>/dev/null | sed -${E} "s,.*,${SED_RED_YELLOW},g"
else
  echo_not_found "gpg"
fi

# Check for NetPGP
echo -e "\nNetPGP:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
if command -v netpgpkeys >/dev/null 2>&1; then
  echo "NetPGP is installed" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
  netpgpkeys --list-keys 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
else
  echo_not_found "netpgpkeys"
fi

# Check for common PGP files
echo -e "\nPGP Related Files:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
for pgp_file in "$HOME/.gnupg" "$HOME/.pgp" "$HOME/.openpgp" "$HOME/.ssh/gpg-agent.conf" "$HOME/.config/gpg"; do
  if [ -e "$pgp_file" ]; then
    echo "Found: $pgp_file"
    if [ -d "$pgp_file" ]; then
      ls -la "$pgp_file" 2>/dev/null
    fi
  fi
done
echo ""