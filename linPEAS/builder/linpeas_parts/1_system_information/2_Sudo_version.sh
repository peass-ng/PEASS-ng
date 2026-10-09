# Title: System Information - Sudo Version
# ID: SY_Sudo_version
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: Inspect bounded, known sudo paths for upstream version candidates.
# License: GNU GPL
# Version: 1.1
# Mitre: T1548.003,T1068
# Functions Used: echo_not_found, lp_trusted_version_path, print_2title, print_info
# Global Variables: $PATH
# Initial Functions:
# Generated Global Variables: $sudo_trusted_candidate, $sudo_seen, $sudo_found, $sudo_timeout, $sudo_path_left, $sudo_path_count, $sudo_dir, $sudo_last, $sudo_candidate, $sudo_identity, $sudo_line, $sudo_version, $sudo_parts, $sudo_major, $sudo_minor, $sudo_patch, $sudo_revision, $sudo_host_candidate, $sudo_chroot_candidate
# Fat linpeas: 0
# Small linpeas: 1
# shellcheck shell=sh


print_2title "Sudo version" "T1548.003,T1068"
print_info "https://www.sudo.ws/security/advisories/"

# Only execute a root-owned setuid sudo target. A user-controlled PATH entry may
# contain an arbitrary program named sudo, so do not run it just to get a version.
sudo_version_check() {
  sudo_candidate=$1
  [ -f "$sudo_candidate" ] && [ -x "$sudo_candidate" ] || return 0
  sudo_identity=$(stat -Lc '%d:%i:%u' "$sudo_candidate" 2>/dev/null) ||
    sudo_identity=$(stat -L -f '%d:%i:%u' "$sudo_candidate" 2>/dev/null) || return 0
  case "
$sudo_seen
" in *"
$sudo_identity
"*) return 0 ;; esac
  sudo_seen="${sudo_seen}
${sudo_identity}"
  sudo_found=1

  case "$sudo_identity" in
    *:0) [ -u "$sudo_candidate" ] || {
      printf '  %s: root-owned, but not setuid; version query skipped\n' "$sudo_candidate"
      return 0
    } ;;
    *) printf '  %s: not root-owned setuid; version query skipped\n' "$sudo_candidate"
       return 0 ;;
  esac

  if [ -z "$sudo_timeout" ]; then
    printf '  %s: root-owned setuid; version query skipped (timeout unavailable)\n' "$sudo_candidate"
    return 0
  fi
  sudo_trusted_candidate=$(lp_trusted_version_path "$sudo_candidate") || {
    printf '  %s: version query skipped (executable or ancestor permissions are untrusted)\n' "$sudo_candidate"
    return 0
  }
  sudo_line=$("$sudo_timeout" -k 1 2 "$sudo_trusted_candidate" -V 2>/dev/null | head -c 256 | head -n 1)
  sudo_version=$(printf '%s\n' "$sudo_line" | awk '$1 == "Sudo" && $2 == "version" { print $3 }')
  if [ -z "$sudo_version" ]; then
    printf '  %s: root-owned setuid; version unavailable or query timed out\n' "$sudo_candidate"
    return 0
  fi
  printf '  %s: Sudo version %s\n' "$sudo_candidate" "$sudo_version"
  sudo_parts=$(printf '%s\n' "$sudo_version" | sed -nE 's/^([0-9]+)\.([0-9]+)\.([0-9]+)(p([0-9]+))?$/\1 \2 \3 \5/p')
  if [ -z "$sudo_parts" ]; then
    printf '    Upstream candidate range unknown; check the installed package advisory.\n'
    return 0
  fi
  # Split only the four numeric fields produced by the anchored expression.
  set -- $sudo_parts
  sudo_major=$1 sudo_minor=$2 sudo_patch=$3 sudo_revision=${4:-0}
  sudo_host_candidate=0 sudo_chroot_candidate=0
  if [ "$sudo_major" -eq 1 ]; then
    if [ "$sudo_minor" -eq 8 ] && [ "$sudo_patch" -ge 8 ]; then
      sudo_host_candidate=1
    elif [ "$sudo_minor" -eq 9 ]; then
      if [ "$sudo_patch" -le 16 ] || { [ "$sudo_patch" -eq 17 ] && [ "$sudo_revision" -eq 0 ]; }; then
        sudo_host_candidate=1
      fi
      if { [ "$sudo_patch" -ge 14 ] && [ "$sudo_patch" -le 16 ]; } ||
         { [ "$sudo_patch" -eq 17 ] && [ "$sudo_revision" -eq 0 ]; }; then
        sudo_chroot_candidate=1
      fi
    fi
  fi
  if [ "$sudo_host_candidate" -eq 1 ]; then
    printf '    CVE-2025-32462 upstream candidate (1.8.8-1.9.17); requires a host-specific sudoers or LDAP rule.\n'
  fi
  if [ "$sudo_chroot_candidate" -eq 1 ]; then
    printf '    CVE-2025-32463 upstream candidate (1.9.14-1.9.17); chroot/NSS behavior is relevant.\n'
  fi
  if [ "$sudo_host_candidate" -eq 1 ] || [ "$sudo_chroot_candidate" -eq 1 ]; then
    printf '    Version alone cannot confirm exposure: vendors backport fixes; check the installed package advisory.\n'
  else
    printf '    No CVE-2025-32462/32463 upstream candidate by version.\n'
  fi
}

sudo_seen=''
sudo_found=''
sudo_timeout=''
if command -v timeout >/dev/null 2>&1; then
  sudo_timeout=$(command -v timeout)
elif command -v gtimeout >/dev/null 2>&1; then
  sudo_timeout=$(command -v gtimeout)
fi

# Probe each PATH entry in order, then the bounded standard locations. Inode
# identity deduplicates aliases (including symlinks) without traversing the disk.
sudo_path_left=$PATH
sudo_path_count=0
while :; do
  sudo_path_count=$((sudo_path_count + 1))
  [ "$sudo_path_count" -le 24 ] || break
  sudo_last=
  case "$sudo_path_left" in
    *:*) sudo_dir=${sudo_path_left%%:*}; sudo_path_left=${sudo_path_left#*:} ;;
    *) sudo_dir=$sudo_path_left; sudo_last=1 ;;
  esac
  [ -n "$sudo_dir" ] || sudo_dir=.
  case "$sudo_dir" in /*) ;; *) sudo_dir="$(pwd -P)/$sudo_dir" ;; esac
  sudo_version_check "$sudo_dir/sudo"
  [ -n "$sudo_last" ] && break
done
for sudo_dir in /usr/bin /bin /usr/local/bin /usr/local/sbin; do
  sudo_version_check "$sudo_dir/sudo"
done
[ -n "$sudo_found" ] || echo_not_found "sudo"
echo ""
