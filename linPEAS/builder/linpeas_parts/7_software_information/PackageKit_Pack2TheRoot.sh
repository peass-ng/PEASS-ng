# Title: Software Information - PackageKit Pack2TheRoot (CVE-2026-41651)
# ID: SI_PackageKit_Pack2TheRoot
# Author: Samuel Monsempes
# Last Update: 29-06-2026
# Description: Check for the Pack2TheRoot vulnerability (CVE-2026-41651) in PackageKit:
#   - Cross-distro local privilege escalation in the PackageKit daemon
#   - Affects all PackageKit versions >= 1.0.2 and <= 1.3.4
#   - Allows any unprivileged local user to install/remove packages and gain root
#   - Confirmed on default installs of Ubuntu (18.04 - 26.04), Debian Trixie 13.4,
#     RockyLinux 10.1, Fedora 43 (Desktop & Server). Cockpit installs may also be affected.
#   - Exploitation methods:
#     * Trigger an unauthorized package install/removal via the PackageKit D-Bus API
#     * Achieve arbitrary root code execution from a low-privileged session
#   - IOC: PackageKit daemon crashes with an "emitted_finished" assertion failure
#     after successful exploitation (visible in journalctl).
#   - Auto-verification: when network access and curl are available, queries
#     https://api.osv.dev/v1/query with the installed package coordinates and
#     greps the response for CVE-2026-41651. Falls back to a local version-range
#     heuristic when offline or when curl is unavailable.
# License: GNU GPL
# Version: 1.1
# Mitre: T1068
# Functions Used: print_2title, print_3title, print_info, echo_not_found
# Global Variables:
# Initial Functions:
# Generated Global Variables: $pk_full, $pk_version, $pk_osv_eco, $pk_osv_resp, $pk_osv_status, $pk_debian_backport, $pk_deb_backported, $pk_min_vuln, $pk_max_vuln, $pk_lower, $pk_higher, $pk_ioc_count
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Checking for PackageKit Pack2TheRoot (CVE-2026-41651)" "T1068"
print_info "https://github.security.telekom.com/2026/04/pack2theroot-linux-local-privilege-escalation.html"

pk_full=""
pk_version=""
if command -v dpkg >/dev/null 2>&1; then
  # Keep the full Debian version (including epoch, revision, +debNNu suffix) for backport detection
  pk_full="$(dpkg -l 2>/dev/null | grep -iE '^ii\s+packagekit\s' | awk '{print $3}' | head -n1)"
  pk_version="$(printf '%s' "$pk_full" | sed -E 's/^[0-9]+://; s/-.*$//')"
fi
if [ -z "$pk_version" ] && command -v rpm >/dev/null 2>&1; then
  pk_full="$(rpm -qa 2>/dev/null | grep -iE '^PackageKit-[0-9]' | head -n1)"
  pk_version="$(printf '%s' "$pk_full" | sed -E 's/^[Pp]ackage[Kk]it-([0-9.]+)-.*/\1/')"
fi

if [ -z "$pk_version" ]; then
  echo_not_found "PackageKit"
else
  echo "PackageKit version detected: $pk_version"

  # OSV.dev authoritative cross-check (network + curl required)
  pk_osv_eco=""
  pk_osv_resp=""
  pk_osv_status="skipped"

  # Auto-detect distro ecosystem for OSV.dev
  if [ -r /etc/os-release ]; then
    # shellcheck disable=SC1091
    . /etc/os-release
    case "${ID:-}:${VERSION_CODENAME:-}" in
      debian:trixie|raspbian:trixie) pk_osv_eco="Debian:13" ;;
      debian:bookworm|raspbian:bookworm) pk_osv_eco="Debian:12" ;;
      debian:bullseye|raspbian:bullseye) pk_osv_eco="Debian:11" ;;
      debian:*) pk_osv_eco="Debian:13" ;;
      ubuntu:jammy) pk_osv_eco="Ubuntu:22.04" ;;
      ubuntu:noble) pk_osv_eco="Ubuntu:24.04" ;;
      ubuntu:*) pk_osv_eco="Ubuntu:24.04" ;;
      fedora:*) pk_osv_eco="Fedora:43" ;;
      rocky:*|almalinux:*|rhel:*|centos:*) pk_osv_eco="Red Hat Enterprise Linux:10" ;;
    esac
  fi

  if [ -z "$pk_osv_eco" ] && command -v rpm >/dev/null 2>&1; then
    # Fallback for RPM systems without an /etc/os-release ID we recognize
    pk_osv_eco="Red Hat Enterprise Linux:10"
  fi
  if [ -z "$pk_osv_eco" ]; then
    pk_osv_eco="Debian:13"
  fi

  # Direct OSV.dev query (5-second timeout). No caching, no python parsing -
  # the response body is grep'd for CVE-2026-41651 only.
  if command -v curl >/dev/null 2>&1; then
    pk_osv_payload=$(printf '{"package":{"name":"packagekit","ecosystem":"%s"},"version":"%s"}' \
      "$pk_osv_eco" "$pk_full")
    pk_osv_resp=$(curl -sS --max-time 5 \
      -H "Content-Type: application/json" \
      -X POST \
      --data "$pk_osv_payload" \
      https://api.osv.dev/v1/query 2>/dev/null)
    if [ -n "$pk_osv_resp" ] && printf '%s' "$pk_osv_resp" | grep -q '"vulns"'; then
      pk_osv_status="queried"
    else
      pk_osv_resp=""
      pk_osv_status="offline"
    fi
  else
    pk_osv_status="no_curl"
  fi

  # Emit OSV.dev status block
  print_3title "OSV.dev cross-check"
  case "$pk_osv_status" in
    queried)
      echo "OSV.dev query succeeded (ecosystem: ${pk_osv_eco}, version: ${pk_full})" | sed -${E} "s,.*,${SED_GREEN},"
      ;;
    offline)
      echo "OSV.dev unreachable (network error or timeout); falling back to heuristic check" | sed -${E} "s,.*,${SED_YELLOW},"
      ;;
    no_curl)
      echo "curl not available; OSV.dev cross-check skipped, falling back to heuristic check" | sed -${E} "s,.*,${SED_YELLOW},"
      ;;
  esac

  if [ "$pk_osv_status" = "queried" ]; then
    # Bare-minimum cross-match: does the OSV response mention CVE-2026-41651?
    if printf '%s' "$pk_osv_resp" | grep -q 'CVE-2026-41651'; then
      echo "CVE-2026-41651 (Pack2TheRoot): VULNERABLE per OSV.dev" | sed -${E} "s,.*,${SED_RED},"
      pk_osv_vulnerable="yes"
    else
      echo "CVE-2026-41651 (Pack2TheRoot): NOT VULNERABLE per OSV.dev (not in affected versions)" | sed -${E} "s,.*,${SED_GREEN},"
      pk_osv_vulnerable="no"
    fi
  else
    pk_osv_vulnerable="unknown"
  fi

  # Daemon-reachability and IOC checks fire if EITHER the OSV cross-check or the
  # local heuristic flags the host as vulnerable.
  pk_heuristic_vulnerable="no"
  pk_debian_backport="$(printf '%s' "$pk_full" | grep -Eo '\+deb[0-9]+u[0-9]+$' || true)"
  pk_deb_backported=""
  if [ -n "$pk_debian_backport" ]; then
    pk_deb_backported="yes"
  fi

  # Vulnerable range: >= 1.0.2 and <= 1.3.4
  pk_min_vuln="1.0.2"
  pk_max_vuln="1.3.4"
  pk_lower="$(printf '%s\n%s\n' "$pk_min_vuln" "$pk_version" | sort -V | head -n1)"
  pk_higher="$(printf '%s\n%s\n' "$pk_version" "$pk_max_vuln" | sort -V | tail -n1)"

  if [ "$pk_deb_backported" = "yes" ]; then
    echo "PackageKit $pk_full carries a Debian security backport (${pk_debian_backport}); CVE-2026-41651 likely patched (heuristic - confirm via OSV.dev when online)." | sed -${E} "s,.*,${SED_GREEN},"
  elif [ "$pk_lower" = "$pk_min_vuln" ] && [ "$pk_higher" = "$pk_max_vuln" ]; then
    echo "Vulnerable to CVE-2026-41651 (Pack2TheRoot) - PackageKit $pk_version is in the vulnerable range >=1.0.2 <=1.3.4" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    pk_heuristic_vulnerable="yes"
  else
    echo "PackageKit $pk_version is not in the vulnerable range for CVE-2026-41651" | sed -${E} "s,.*,${SED_GREEN},"
  fi

  # Daemon reachability + IOC only matter when the host is actually vulnerable.
  # OSV is authoritative when it succeeded; otherwise trust the heuristic.
  if [ "$pk_osv_vulnerable" = "yes" ] || { [ "$pk_osv_vulnerable" = "unknown" ] && [ "$pk_heuristic_vulnerable" = "yes" ]; }; then
    echo ""
    print_3title "PackageKit daemon reachability"
    if command -v systemctl >/dev/null 2>&1 && systemctl status packagekit >/dev/null 2>&1; then
      echo "PackageKit service is loaded/running - exploitation likely possible" | sed -${E} "s,.*,${SED_RED},"
    elif command -v pkcon >/dev/null 2>&1 || command -v pkmon >/dev/null 2>&1; then
      echo "pkcon/pkmon present - daemon can be activated on demand via D-Bus" | sed -${E} "s,.*,${SED_RED},"
    else
      echo "PackageKit daemon does not appear to be reachable from this session" | sed -${E} "s,.*,${SED_GREEN},"
    fi

    # IOC: emitted_finished assertion failures
    echo ""
    print_3title "IOC: emitted_finished assertion failures"
    if command -v journalctl >/dev/null 2>&1; then
      pk_ioc_count="$(journalctl --no-pager -u packagekit 2>/dev/null | grep -c emitted_finished)"
      if [ "${pk_ioc_count:-0}" -gt 0 ] 2>/dev/null; then
        echo "Found ${pk_ioc_count} 'emitted_finished' crashes in PackageKit logs - possible prior exploitation" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      else
        echo "No emitted_finished assertion failures found in PackageKit logs"
      fi
    else
      echo "journalctl not available - cannot check IOC"
    fi
  fi
fi
echo ""