# Title: Functions - checkLibblockdevCVE20256019
# ID: checkLibblockdevCVE20256019
# Author: PEASS-ng
# Last Update: 08-10-2026
# Description: Passive udisks/libblockdev and polkit evidence for CVE-2025-6019.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: lp_suse_numeric_version_lt, print_3title, print_info
# Global Variables: $ROOT_FOLDER
# Initial Functions:
# Generated Global Variables: $bd6019_root, $bd6019_daemon, $bd6019_candidate, $bd6019_service, $bd6019_policy, $bd6019_package, $bd6019_os, $bd6019_id, $bd6019_version
# Fat linpeas: 0
# Small linpeas: 1

checkLibblockdevCVE20256019() { (
  [ "$(uname -s 2>/dev/null)" = Linux ] || return 0
  bd6019_root="${ROOT_FOLDER:-/}"
  case "$bd6019_root" in */) ;; *) bd6019_root="$bd6019_root/" ;; esac
  bd6019_daemon=""
  for bd6019_candidate in usr/libexec/udisks2/udisksd usr/lib/udisks2/udisksd lib/udisks2/udisksd; do
    if [ -x "${bd6019_root}${bd6019_candidate}" ]; then
      bd6019_daemon="${bd6019_root}${bd6019_candidate}"
      break
    fi
  done
  [ -n "$bd6019_daemon" ] || return 0
  bd6019_service="${bd6019_root}usr/share/dbus-1/system-services/org.freedesktop.UDisks2.service"
  [ -r "$bd6019_service" ] || return 0
  sed -n '1,100p;101q' "$bd6019_service" | grep -Eq '^Name=org[.]freedesktop[.]UDisks2$' || return 0
  [ -x "${bd6019_root}usr/sbin/xfs_growfs" ] || [ -x "${bd6019_root}sbin/xfs_growfs" ] || return 0
  bd6019_policy="${bd6019_root}usr/share/polkit-1/actions/org.freedesktop.udisks2.policy"
  [ -r "$bd6019_policy" ] || return 0
  # The resize path is useful to an active user only when this action permits it.
  sed -n '1,1000p;1001q' "$bd6019_policy" | awk '
    /<action id="org[.]freedesktop[.]udisks2[.]modify-device">/ { in_action=1 }
    in_action && /<allow_active>[[:space:]]*yes[[:space:]]*<\/allow_active>/ { found=1 }
    in_action && /<\/action>/ { in_action=0 }
    END { exit !found }
  ' || return 0
  bd6019_os="${bd6019_root}etc/os-release"
  [ -r "$bd6019_os" ] || return 0
  bd6019_id="$(sed -n '1,100{s/^ID=[" ]*\([^" ]*\).*/\1/p;};101q' "$bd6019_os" | head -n 1)"
  bd6019_version="$(sed -n '1,100{s/^VERSION_ID=[" ]*\([^" ]*\).*/\1/p;};101q' "$bd6019_os" | head -n 1)"
  case "$bd6019_id:$bd6019_version" in
    sles:15.6|sles:15.7|sled:15.6|sled:15.7|opensuse-leap:15.6|opensuse:15.6) ;;
    *) return 0 ;;
  esac
  command -v timeout >/dev/null 2>&1 && command -v rpm >/dev/null 2>&1 || return 0
  bd6019_package="$(timeout 3 rpm --root "$bd6019_root" -q --qf '%{VERSION}-%{RELEASE}\n' libbd_fs2 2>/dev/null | awk 'NR == 1 && /^[0-9]/ { print }')"
  lp_suse_numeric_version_lt "$bd6019_package" "2.26-150400.3.5.1" || return 0
  print_3title "udisks/libblockdev XFS resize path (CVE-2025-6019)" "T1068"
  print_info "https://www.suse.com/security/cve/CVE-2025-6019.html"
  echo "udisks D-Bus activation, xfs_growfs, and allow_active=yes modify-device policy are present"
  printf 'libbd_fs2 package: %s (below SUSE fixed version 2.26-150400.3.5.1)\n' "$bd6019_package"
  echo "Effective patch status: unknown; verify vendor libblockdev advisory and local overrides"
  echo ""
) ; }
