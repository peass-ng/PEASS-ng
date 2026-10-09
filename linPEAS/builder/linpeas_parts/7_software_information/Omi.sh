# Title: Software Information - OMI management listener
# ID: SI_Omi
# Author: PEASS-ng contributors
# Last Update: 09-10-2026
# Description: Passively correlate a standard OMI installation, process identity, and local WS-Management listener.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables:
# Initial Functions:
# Generated Global Variables: $omi_proc, $omi_port, $omi_pkg, $omi_timeout
# Fat linpeas: 0
# Small linpeas: 1

omi_bounded() {
  if [ -n "$omi_timeout" ]; then
    "$omi_timeout" 2 "$@"
  else
    "$@"
  fi
}

omi_process_user() {
  LC_ALL=C awk '
    NR > 4096 { print "#limit"; exit }
    $2 == "omiengine" { print $1; exit }
  '
}

omi_listener() {
  LC_ALL=C awk '
    NR > 4096 { print "#limit"; exit }
    /LISTEN/ {
      local_address = $4
      if (local_address ~ /[.:]598[56]$/) { print local_address; exit }
    }
  '
}

omi_report_evidence() {
  printf 'OMI package version: %s (vendor backports/build provenance unverified).\n' "${3:-unknown}"
  case "$1" in
    '#limit') echo 'OMI process context: unknown (4,096-line process limit).' ;;
    '') echo 'OMI process context: not visible; service may be stopped or hidden.' ;;
    *) printf 'OMI process owner: %s (effective service identity needs confirmation).\n' "$1" ;;
  esac
  case "$2" in
    '#limit') echo 'WS-Management listener: unknown (4,096-line socket limit).' ;;
    '') echo 'WS-Management listener on 5985/5986: not visible; custom ports or Unix socket remain possible.' ;;
    *) printf 'WS-Management listener co-observed: %s (socket ownership unproven).\n' "$2" ;;
  esac
  if [ "$1" = root ] && [ -n "$2" ] && [ "$2" != '#limit' ]; then
    echo 'OMI review candidate: root-owned omiengine and a 5985/5986 listener are visible; verify socket ownership, installed security fix/backports, access policy, and actual SSRF/network reachability.'
  fi
}

check_omi_exposure() {
  # Fixed standard paths gate all commands; no directory walk or service request.
  [ -e /opt/omi/bin/omiengine ] || [ -e /etc/opt/omi/conf/omiserver.conf ] || return 0
  print_3title 'OMI management listener review' 'T1068'
  print_info 'https://github.com/microsoft/omi/security/advisories/GHSA-fmh7-p6gp-8xpj'
  print_info 'https://book.hacktricks.wiki/en/network-services-pentesting/5985-5986-pentesting-omi.html'

  omi_timeout=$(command -v timeout 2>/dev/null || command -v gtimeout 2>/dev/null)
  omi_proc=$(omi_bounded ps -eo user=,comm= 2>/dev/null | omi_process_user)
  if command -v ss >/dev/null 2>&1; then
    omi_port=$(omi_bounded ss -ltn 2>/dev/null | omi_listener)
  elif command -v netstat >/dev/null 2>&1; then
    omi_port=$(omi_bounded netstat -an 2>/dev/null | omi_listener)
  else
    omi_port=
  fi
  omi_pkg=
  if command -v dpkg-query >/dev/null 2>&1; then
    omi_pkg=$(omi_bounded dpkg-query -W -f='$''{Version}\n' omi 2>/dev/null | LC_ALL=C head -n1 | cut -c1-64)
  elif command -v rpm >/dev/null 2>&1; then
    omi_pkg=$(omi_bounded rpm -q --qf '%{VERSION}-%{RELEASE}\n' omi 2>/dev/null | LC_ALL=C head -n1 | cut -c1-64)
  fi
  case "$omi_pkg" in [0-9]* ) ;; * ) omi_pkg= ;; esac
  omi_report_evidence "$omi_proc" "$omi_port" "$omi_pkg"
}

check_omi_exposure
