# Title: Functions - checkGlibcTunablesCVE20234911
# ID: checkGlibcTunablesCVE20234911
# Author: PEASS contributors
# Last Update: 09-10-2026
# Description: Review installed GNU C Library package against vendor fixes for CVE-2023-4911 without executing a malformed tunables probe.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $ROOT_FOLDER
# Initial Functions:
# Generated Global Variables: $gt4911_root, $gt4911_release, $gt4911_id, $gt4911_codename, $gt4911_fixed, $gt4911_record, $gt4911_version
# Fat linpeas: 0
# Small linpeas: 1

checkGlibcTunablesCVE20234911() {
  command -v dpkg-query >/dev/null 2>&1 || return 0
  command -v dpkg >/dev/null 2>&1 || return 0

  gt4911_root="${ROOT_FOLDER:-/}"
  case "$gt4911_root" in */) ;; *) gt4911_root="$gt4911_root/" ;; esac
  gt4911_release="${gt4911_root}etc/os-release"
  [ -r "$gt4911_release" ] || return 0
  gt4911_id="$(sed -n 's/^ID=//p' "$gt4911_release" | head -n 1 | tr -d '"')"
  gt4911_codename="$(sed -n 's/^VERSION_CODENAME=//p' "$gt4911_release" | head -n 1 | tr -d '"')"

  # Compare vendor package releases, not upstream glibc versions: Debian
  # bullseye backported the affected parser into its older 2.31 package.
  case "$gt4911_id:$gt4911_codename" in
    ubuntu:jammy) gt4911_fixed="2.35-0ubuntu3.4" ;;
    ubuntu:lunar) gt4911_fixed="2.37-0ubuntu2.1" ;;
    ubuntu:mantic|ubuntu:noble) gt4911_fixed="2.38-1ubuntu6" ;;
    debian:bullseye) gt4911_fixed="2.31-13+deb11u7" ;;
    debian:bookworm) gt4911_fixed="2.36-9+deb12u3" ;;
    *) return 0 ;;
  esac

  gt4911_record="$(dpkg-query --admindir="${gt4911_root}var/lib/dpkg" -W -f='$''{Status}|$''{Version}\n' libc6 2>/dev/null | head -n 1)"
  case "$gt4911_record" in
    'install ok installed|'*) gt4911_version="${gt4911_record#*|}" ;;
    *) return 0 ;;
  esac
  [ -n "$gt4911_version" ] || return 0

  print_3title "GNU C Library tunables package review (CVE-2023-4911)" "T1068"
  print_info "https://ubuntu.com/security/CVE-2023-4911"
  print_info "https://security-tracker.debian.org/tracker/CVE-2023-4911"
  echo "Installed libc6: $gt4911_version; vendor fixed threshold for $gt4911_id $gt4911_codename: $gt4911_fixed"
  if dpkg --compare-versions "$gt4911_version" lt "$gt4911_fixed"; then
    echo "Package below vendor fix: review candidate only. Confirm installed loader, package patch state, a reachable SUID target, and NoNewPrivs/nosuid or other mitigations; no exploit probe was run."
  else
    echo "Installed package meets this vendor's fixed threshold; confirm the running loader after package updates."
  fi
}
