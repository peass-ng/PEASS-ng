# Title: Functions - checkUDisksCVE20267867
# ID: checkUDisksCVE20267867
# Author: Chack Agent
# Last Update: 05-09-2026
# Description: Passively identify complete udisks2 as-user authorization-bypass exposure (CVE-2026-7867).
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_LIGHT_CYAN, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $ud7867_binary, $ud7867_binary_candidate, $ud7867_codename, $ud7867_distro_id, $ud7867_dpkg_fixed, $ud7867_dpkg_record, $ud7867_fstab, $ud7867_fstab_matches, $ud7867_full_version, $ud7867_manager, $ud7867_os_release, $ud7867_root, $ud7867_rpm_record, $ud7867_service_file, $ud7867_status, $ud7867_ubuntu_codename, $ud7867_upstream_version
# Fat linpeas: 0
# Small linpeas: 1


ud7867_extract_upstream_version() {
  printf '%s' "$1" | sed -E 's/^[0-9]+://; s/^[^0-9]*//; s/[^0-9.].*$//'
}

ud7867_version_lt() {
  [ -n "$1" ] && [ -n "$2" ] || return 1
  awk -v ud7867_a="$1" -v ud7867_b="$2" 'BEGIN {
    ud7867_na = split(ud7867_a, ud7867_av, ".")
    ud7867_nb = split(ud7867_b, ud7867_bv, ".")
    ud7867_n = ud7867_na > ud7867_nb ? ud7867_na : ud7867_nb
    for (ud7867_i = 1; ud7867_i <= ud7867_n; ud7867_i++) {
      ud7867_ai = ud7867_av[ud7867_i] + 0
      ud7867_bi = ud7867_bv[ud7867_i] + 0
      if (ud7867_ai < ud7867_bi) exit 0
      if (ud7867_ai > ud7867_bi) exit 1
    }
    exit 1
  }'
}

ud7867_fixed_dpkg_version() {
  # Known vendor backports from Debian DSA-6414-1 and Ubuntu USN-8701-1.
  case "$1:$2" in
    debian:trixie) echo "2.10.1-12.1+deb13u2" ;;
    debian:forky|debian:sid) echo "2.11.2-1" ;;
    ubuntu:noble) echo "2.10.1-6ubuntu1.5" ;;
    ubuntu:resolute) echo "2.10.91-1ubuntu2.1" ;;
  esac
}

checkUDisksCVE20267867() {
  [ "$(uname -s 2>/dev/null)" = "Linux" ] || return 0

  ud7867_root="${ROOT_FOLDER:-/}"
  case "$ud7867_root" in
    */) ;;
    *) ud7867_root="${ud7867_root}/" ;;
  esac

  ud7867_fstab="${ud7867_root}etc/fstab"
  [ -r "$ud7867_fstab" ] || return 0
  ud7867_fstab_matches="$(awk '
    /^[[:space:]]*#/ || NF < 4 { next }
    {
      ud7867_count = split($4, ud7867_options, ",")
      for (ud7867_i = 1; ud7867_i <= ud7867_count; ud7867_i++) {
        if (ud7867_options[ud7867_i] == "user" ||
            ud7867_options[ud7867_i] == "users" ||
            ud7867_options[ud7867_i] == "x-udisks-auth") {
          print
          next
        }
      }
    }
  ' "$ud7867_fstab" 2>/dev/null | head -n 10)"
  [ -n "$ud7867_fstab_matches" ] || return 0

  ud7867_binary=""
  for ud7867_binary_candidate in usr/libexec/udisks2/udisksd usr/lib/udisks2/udisksd lib/udisks2/udisksd; do
    if [ -x "${ud7867_root}${ud7867_binary_candidate}" ]; then
      ud7867_binary="${ud7867_root}${ud7867_binary_candidate}"
      break
    fi
  done
  [ -n "$ud7867_binary" ] || return 0

  ud7867_service_file="${ud7867_root}usr/share/dbus-1/system-services/org.freedesktop.UDisks2.service"
  [ -r "$ud7867_service_file" ] || return 0

  ud7867_full_version=""
  ud7867_manager=""
  if command -v dpkg-query >/dev/null 2>&1; then
    ud7867_dpkg_record="$(dpkg-query --admindir="${ud7867_root}var/lib/dpkg" -W -f='$''{Status}|$''{Version}\n' udisks2 2>/dev/null | head -n1)"
    case "$ud7867_dpkg_record" in
      "install ok installed|"*)
        ud7867_full_version="${ud7867_dpkg_record#*|}"
        [ -n "$ud7867_full_version" ] && ud7867_manager="dpkg"
        ;;
    esac
  fi
  if [ -z "$ud7867_full_version" ] && command -v rpm >/dev/null 2>&1; then
    if ud7867_rpm_record="$(rpm --root "$ud7867_root" -q --qf '%{VERSION}-%{RELEASE}\n' udisks2 2>/dev/null)"; then
      ud7867_full_version="$(printf '%s\n' "$ud7867_rpm_record" | head -n1)"
      [ -n "$ud7867_full_version" ] && ud7867_manager="rpm"
    fi
  fi
  [ -n "$ud7867_full_version" ] || return 0

  ud7867_os_release="${ud7867_root}etc/os-release"
  ud7867_distro_id="$(sed -nE 's/^ID="?([^" ]+)"?$/\1/p' "$ud7867_os_release" 2>/dev/null | head -n1)"
  ud7867_codename="$(sed -nE 's/^VERSION_CODENAME="?([^" ]+)"?$/\1/p' "$ud7867_os_release" 2>/dev/null | head -n1)"
  ud7867_ubuntu_codename="$(sed -nE 's/^UBUNTU_CODENAME="?([^" ]+)"?$/\1/p' "$ud7867_os_release" 2>/dev/null | head -n1)"
  if [ -n "$ud7867_ubuntu_codename" ]; then
    ud7867_distro_id="ubuntu"
    ud7867_codename="$ud7867_ubuntu_codename"
  fi

  ud7867_upstream_version="$(ud7867_extract_upstream_version "$ud7867_full_version")"
  ud7867_dpkg_fixed=""
  ud7867_status="unknown"

  if [ "$ud7867_manager" = "dpkg" ] && command -v dpkg >/dev/null 2>&1; then
    ud7867_dpkg_fixed="$(ud7867_fixed_dpkg_version "$ud7867_distro_id" "$ud7867_codename")"
    if [ -n "$ud7867_dpkg_fixed" ]; then
      if dpkg --compare-versions "$ud7867_full_version" lt "$ud7867_dpkg_fixed"; then
        ud7867_status="affected"
      else
        ud7867_status="fixed"
      fi
    fi
  fi

  # RPM vendors commonly retain their old upstream version after backporting.
  if [ "$ud7867_status" = "unknown" ] && [ "$ud7867_manager" = "rpm" ]; then
    if rpm --root "$ud7867_root" -q --changelog udisks2 2>/dev/null | grep -q 'CVE-2026-7867'; then
      ud7867_status="fixed"
    fi
  fi

  if [ "$ud7867_status" = "unknown" ] && [ -n "$ud7867_upstream_version" ]; then
    if ud7867_version_lt "$ud7867_upstream_version" "2.10.0"; then
      ud7867_status="fixed"
    elif ud7867_version_lt "$ud7867_upstream_version" "2.11.2"; then
      ud7867_status="potential"
    else
      ud7867_status="fixed"
    fi
  fi
  [ "$ud7867_status" = "affected" ] || [ "$ud7867_status" = "potential" ] || return 0

  print_3title "UDisks as-user mount authorization bypass (CVE-2026-7867)" "T1068"
  print_info "https://github.com/storaged-project/udisks/security/advisories/GHSA-j42g-v9jw-6ph3"
  echo "udisks2 package: $ud7867_full_version ($ud7867_manager)" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
  if [ "$ud7867_status" = "affected" ]; then
    echo "VULNERABLE to CVE-2026-7867: package is below the known vendor fixed version and a qualifying fstab entry is present" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  else
    echo "Potentially vulnerable to CVE-2026-7867: upstream version is in the affected range 2.10.0 through 2.11.1; verify vendor backports" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  fi
  echo "Qualifying /etc/fstab entries (maximum 10):"
  printf '%s\n' "$ud7867_fstab_matches"
  echo ""
}
