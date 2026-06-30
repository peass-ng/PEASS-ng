# Title: Software Information - PackageKit Pack2TheRoot (CVE-2026-41651)
# ID: SI_PackageKit_Pack2TheRoot
# Author: Samuel Monsempes
# Contributor: Arjay Saguisa
# Last Update: 29-06-2026
# Description: Check for the Pack2TheRoot vulnerability (CVE-2026-41651) in PackageKit:
#   - Cross-distro local privilege escalation in the PackageKit daemon
#   - Affects PackageKit versions >= 1.0.2 and <= 1.3.4 unless a distro fix was backported
#   - Allows any unprivileged local user to install/remove packages and gain root
#   - Confirmed on default installs of Ubuntu (18.04 - 26.04), Debian Trixie 13.4,
#     RockyLinux 10.1, Fedora 43 (Desktop & Server). Cockpit installs may also be affected.
#   - Exploitation methods:
#     * Trigger an unauthorized package install/removal via the PackageKit D-Bus API
#     * Achieve arbitrary root code execution from a low-privileged session
#   - IOC: PackageKit daemon crashes with an "emitted_finished" assertion failure
#     after successful exploitation (visible in journalctl).
# License: GNU GPL
# Version: 1.1
# Mitre: T1068
# Functions Used: print_2title, print_3title, print_info, echo_not_found
# Global Variables:
# Initial Functions:
# Generated Global Variables: $pk_full, $pk_version, $pk_pkg_manager, $pk_distro_id, $pk_distro_codename, $pk_fixed_version, $pk_fixed_label, $pk_min_vuln, $pk_max_vuln, $pk_lower, $pk_higher, $pk_vulnerable, $pk_ioc_count, $VERSION_CODENAME, $Version
# Fat linpeas: 0
# Small linpeas: 1

pk_dpkg_fixed_version() {
  pk_fixed_version=""
  pk_fixed_label=""

  [ -r /etc/os-release ] || return
  pk_distro_id=""
  pk_distro_codename=""
  # shellcheck disable=SC1091
  . /etc/os-release
  pk_distro_id="${ID:-}"
  pk_distro_codename="$(sed -nE 's/^VERSION_CODENAME=\"?([^"]*)\"?$/\1/p' /etc/os-release | head -n1)"

  case "${pk_distro_id}:${pk_distro_codename}" in
    debian:bullseye|raspbian:bullseye)
      pk_fixed_version="1.2.2-2+deb11u1"
      pk_fixed_label="Debian/Raspbian bullseye fixed version"
      ;;
    debian:bookworm|raspbian:bookworm)
      pk_fixed_version="1.2.6-5+deb12u1"
      pk_fixed_label="Debian/Raspbian bookworm fixed version"
      ;;
    debian:trixie|raspbian:trixie)
      pk_fixed_version="1.3.1-1+deb13u1"
      pk_fixed_label="Debian/Raspbian trixie fixed version"
      ;;
    ubuntu:xenial)
      pk_fixed_version="0.8.17-4ubuntu6~gcc5.4ubuntu1.5+esm1"
      pk_fixed_label="Ubuntu 16.04 ESM fixed version"
      ;;
    ubuntu:bionic)
      pk_fixed_version="1.1.9-1ubuntu2.18.04.6+esm1"
      pk_fixed_label="Ubuntu 18.04 ESM fixed version"
      ;;
    ubuntu:focal)
      pk_fixed_version="1.1.13-2ubuntu1.1+esm1"
      pk_fixed_label="Ubuntu 20.04 ESM fixed version"
      ;;
    ubuntu:jammy)
      pk_fixed_version="1.2.5-2ubuntu3.1"
      pk_fixed_label="Ubuntu 22.04 fixed version"
      ;;
    ubuntu:noble)
      pk_fixed_version="1.2.8-2ubuntu1.5"
      pk_fixed_label="Ubuntu 24.04 fixed version"
      ;;
    ubuntu:questing)
      pk_fixed_version="1.3.1-1ubuntu1.1"
      pk_fixed_label="Ubuntu 25.10 fixed version"
      ;;
    ubuntu:resolute)
      pk_fixed_version="1.3.4-3ubuntu1"
      pk_fixed_label="Ubuntu 26.04 fixed version"
      ;;
  esac
}

print_2title "Checking for PackageKit Pack2TheRoot (CVE-2026-41651)" "T1068"
print_info "https://github.security.telekom.com/2026/04/pack2theroot-linux-local-privilege-escalation.html"

pk_full=""
pk_version=""
pk_pkg_manager=""
if command -v dpkg-query >/dev/null 2>&1; then
  pk_full="$(dpkg-query -W -f='$''{Version}\n' packagekit 2>/dev/null | head -n1)"
  if [ -n "$pk_full" ]; then
    pk_pkg_manager="dpkg"
    pk_version="$(printf '%s' "$pk_full" | sed -E 's/^[0-9]+://; s/[-+~].*$//')"
  fi
fi
if [ -z "$pk_version" ] && command -v rpm >/dev/null 2>&1; then
  pk_full="$(rpm -qa 2>/dev/null | grep -iE '^PackageKit-[0-9]' | head -n1)"
  if [ -n "$pk_full" ]; then
    pk_pkg_manager="rpm"
    pk_version="$(printf '%s' "$pk_full" | sed -E 's/^[Pp]ackage[Kk]it-([0-9.]+)-.*/\1/')"
  fi
fi

if [ -z "$pk_version" ]; then
  echo_not_found "PackageKit"
else
  echo "PackageKit version detected: ${pk_full:-$pk_version}"

  pk_vulnerable="no"

  if [ "$pk_pkg_manager" = "dpkg" ] && command -v dpkg >/dev/null 2>&1; then
    pk_dpkg_fixed_version
    if [ -n "$pk_fixed_version" ]; then
      if dpkg --compare-versions "$pk_full" ge "$pk_fixed_version"; then
        echo "PackageKit $pk_full is at or above the ${pk_fixed_label}: $pk_fixed_version" | sed -${E} "s,.*,${SED_GREEN},"
      else
        echo "Vulnerable to CVE-2026-41651 (Pack2TheRoot) - PackageKit $pk_full is below the ${pk_fixed_label}: $pk_fixed_version" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        pk_vulnerable="yes"
      fi
    fi
  fi

  if [ -z "$pk_fixed_version" ]; then
    # Generic upstream range: >= 1.0.2 and <= 1.3.4. Distro backports are handled above.
    pk_min_vuln="1.0.2"
    pk_max_vuln="1.3.4"
    pk_lower="$(printf '%s\n%s\n' "$pk_min_vuln" "$pk_version" | sort -V | head -n1)"
    pk_higher="$(printf '%s\n%s\n' "$pk_version" "$pk_max_vuln" | sort -V | tail -n1)"

    if [ "$pk_lower" = "$pk_min_vuln" ] && [ "$pk_higher" = "$pk_max_vuln" ]; then
      echo "Vulnerable to CVE-2026-41651 (Pack2TheRoot) - PackageKit $pk_version is in the upstream vulnerable range >=1.0.2 <=1.3.4" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      pk_vulnerable="yes"
    else
      echo "PackageKit $pk_version is not in the upstream vulnerable range for CVE-2026-41651" | sed -${E} "s,.*,${SED_GREEN},"
    fi
  fi

  if [ "$pk_vulnerable" = "yes" ]; then
    echo ""
    print_3title "PackageKit daemon reachability"
    if command -v systemctl >/dev/null 2>&1 && systemctl status packagekit >/dev/null 2>&1; then
      echo "PackageKit service is loaded/running - exploitation likely possible" | sed -${E} "s,.*,${SED_RED},"
    elif command -v pkcon >/dev/null 2>&1 || command -v pkmon >/dev/null 2>&1; then
      echo "pkcon/pkmon present - daemon can be activated on demand via D-Bus" | sed -${E} "s,.*,${SED_RED},"
    else
      echo "PackageKit daemon does not appear to be reachable from this session" | sed -${E} "s,.*,${SED_GREEN},"
    fi

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
