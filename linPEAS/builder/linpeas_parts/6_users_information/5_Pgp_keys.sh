# Title: Users Information - PGP keys
# ID: UG_Pgp_keys
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check for PGP keys and related files that might contain sensitive information
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.004
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $HOME
# Initial Functions:
# Generated Global Variables: $pgp_file, $pgp_walk, $pgp_key_count, $pgp_home_count, $pgp_home, $pgp_gnupg, $pgp_keydir, $pgp_nearby, $pgp_ciphertext, $pgp_key
# Fat linpeas: 0
# Small linpeas: 1


print_2title "PGP Keys and Related Files" "T1552.004"
print_info "https://book.hacktricks.wiki/en/linux-hardening/user-information/user-and-session-triage.html#review-user-artifacts"

# Review direct GnuPG private-key paths in login homes. Never open key material.
# Input is a newline-separated list of home paths; the caller caps that list.
pgp_path_has_no_symlinks() {
  pgp_walk=$1
  while [ "$pgp_walk" != / ]; do
    [ ! -L "$pgp_walk" ] || return 1
    pgp_walk=${pgp_walk%/*}
    [ -n "$pgp_walk" ] || pgp_walk=/
  done
}

pgp_private_key_candidates() {
  pgp_key_count=0
  pgp_home_count=0
  while IFS= read -r pgp_home; do
    [ "$pgp_key_count" -lt 24 ] && [ "$pgp_home_count" -lt 64 ] || break
    pgp_home_count=$((pgp_home_count + 1))
    case "$pgp_home" in
      /*) ;;
      *) continue ;;
    esac
    case "$pgp_home/" in
      *'/../'*|*'/./'*|*'//'*) continue ;;
    esac
    [ "$pgp_home" != "$HOME" ] || continue
    pgp_gnupg="$pgp_home/.gnupg"
    pgp_keydir="$pgp_gnupg/private-keys-v1.d"
    # Reject symlinks at each path component we intentionally enter.
    [ -d "$pgp_home" ] && [ -x "$pgp_home" ] && pgp_path_has_no_symlinks "$pgp_home" || continue
    [ -d "$pgp_gnupg" ] && [ -x "$pgp_gnupg" ] && [ ! -L "$pgp_gnupg" ] || continue
    [ -d "$pgp_keydir" ] && [ -x "$pgp_keydir" ] && [ ! -L "$pgp_keydir" ] || continue

    pgp_nearby="no"
    for pgp_ciphertext in "$pgp_home"/*.gpg "$pgp_home"/backup/*.gpg; do
      case "$pgp_ciphertext" in
        "$pgp_home"/backup/*)
          [ -d "$pgp_home/backup" ] && [ -x "$pgp_home/backup" ] && [ ! -L "$pgp_home/backup" ] || continue ;;
      esac
      if [ -f "$pgp_ciphertext" ] && [ -r "$pgp_ciphertext" ] && [ ! -L "$pgp_ciphertext" ]; then
        pgp_nearby="yes: $pgp_ciphertext"
        break
      fi
    done

    # Only direct children, with a global output cap; do not traverse homes.
    for pgp_key in "$pgp_keydir"/*.key; do
      [ "$pgp_key_count" -lt 24 ] || break
      [ -f "$pgp_key" ] && [ -r "$pgp_key" ] && [ ! -L "$pgp_key" ] || continue
      printf 'Readable GnuPG private key (review candidate): %s\n' "$pgp_key"
      ls -ld "$pgp_key" 2>/dev/null
      printf '  Readable nearby .gpg ciphertext: %s\n' "$pgp_nearby"
      pgp_key_count=$((pgp_key_count + 1))
    done
  done
}

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

# Only login homes from the account database; at most 64 distinct homes.
echo "Cross-user GnuPG private-key paths (metadata only, limit 24):" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
awk -F: '$6 ~ /^\// && $7 !~ /(nologin|false)$/ && $6 != "/" && $6 != "/nonexistent" && !seen[$6]++ { print $6; if (++n == 64) exit }' /etc/passwd 2>/dev/null | pgp_private_key_candidates
echo ""
