# Title: Functions - checkNeedrestartCVE202448990
# ID: checkNeedrestartCVE202448990
# Author: Chack Agent
# Last Update: 24-08-2026
# Description: Passively identify needrestart interpreter-scanner local privilege-escalation exposure, including CVE-2024-48990.
# License: GNU GPL
# Version: 1.1
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_GREEN, $SED_LIGHT_CYAN, $SED_RED_YELLOW, $SED_YELLOW
# Initial Functions:
# Generated Global Variables: $nr48990_binary, $nr48990_codename, $nr48990_config, $nr48990_config_file, $nr48990_config_file_value, $nr48990_distro_id, $nr48990_dpkg_fixed, $nr48990_dpkg_record, $nr48990_full_version, $nr48990_interpscan, $nr48990_manager, $nr48990_os_release, $nr48990_root, $nr48990_status, $nr48990_ubuntu_codename, $nr48990_upstream_version
# Fat linpeas: 0
# Small linpeas: 1


nr48990_extract_upstream_version() {
  printf '%s' "$1" | sed -E 's/^[0-9]+://; s/^[^0-9]*//; s/[^0-9.].*$//'
}

nr48990_version_lt() {
  [ -n "$1" ] && [ -n "$2" ] || return 1
  awk -v nr48990_a="$1" -v nr48990_b="$2" 'BEGIN {
    nr48990_na = split(nr48990_a, nr48990_av, ".")
    nr48990_nb = split(nr48990_b, nr48990_bv, ".")
    nr48990_n = nr48990_na > nr48990_nb ? nr48990_na : nr48990_nb
    for (nr48990_i = 1; nr48990_i <= nr48990_n; nr48990_i++) {
      nr48990_ai = nr48990_av[nr48990_i] + 0
      nr48990_bi = nr48990_bv[nr48990_i] + 0
      if (nr48990_ai < nr48990_bi) exit 0
      if (nr48990_ai > nr48990_bi) exit 1
    }
    exit 1
  }'
}

nr48990_fixed_dpkg_version() {
  # Vendor backports from Ubuntu CVE-2024-48990 and Debian DSA-5815-1 /
  # DLA-3957-1; comparing only the upstream 3.8 version would misclassify them.
  case "$1:$2" in
    debian:bullseye|raspbian:bullseye) echo "3.5-4+deb11u4" ;;
    debian:bookworm|raspbian:bookworm) echo "3.6-4+deb12u2" ;;
    ubuntu:xenial) echo "2.6-1ubuntu0.1~esm1" ;;
    ubuntu:bionic) echo "3.1-1ubuntu0.1+esm1" ;;
    ubuntu:focal) echo "3.4-6ubuntu0.1+esm1" ;;
    ubuntu:jammy) echo "3.5-5ubuntu2.2" ;;
    ubuntu:noble) echo "3.6-7ubuntu4.3" ;;
    ubuntu:oracular) echo "3.6-8ubuntu4.2" ;;
    ubuntu:plucky) echo "3.6-8ubuntu6" ;;
  esac
}

nr48990_effective_interpscan() {
  nr48990_config="1"
  for nr48990_config_file in "$1"etc/needrestart/needrestart.conf "$1"etc/needrestart/conf.d/*.conf; do
    [ -r "$nr48990_config_file" ] || continue
    nr48990_config_file_value="$(awk '
      /^[[:space:]]*#/ { next }
      {
        nr48990_line = $0
        sub(/[[:space:]]*#.*/, "", nr48990_line)
        if (nr48990_line ~ /^[[:space:]]*[$]nrconf[[:space:]]*\{[[:space:]]*[\047\042]?interpscan[\047\042]?[[:space:]]*\}[[:space:]]*=[[:space:]]*[01][[:space:]]*;/) {
          sub(/^.*=[[:space:]]*/, "", nr48990_line)
          sub(/[[:space:]]*;.*/, "", nr48990_line)
          print nr48990_line
        }
      }
    ' "$nr48990_config_file" 2>/dev/null | tail -n1)"
    [ -n "$nr48990_config_file_value" ] && nr48990_config="$nr48990_config_file_value"
  done
  printf '%s' "$nr48990_config"
}

checkNeedrestartCVE202448990() {
  nr48990_root="${ROOT_FOLDER:-/}"
  case "$nr48990_root" in
    */) ;;
    *) nr48990_root="${nr48990_root}/" ;;
  esac

  nr48990_binary="$(command -v needrestart 2>/dev/null)"
  nr48990_full_version=""
  nr48990_manager=""
  if command -v dpkg-query >/dev/null 2>&1; then
    nr48990_dpkg_record="$(dpkg-query --admindir="${nr48990_root}var/lib/dpkg" -W -f='$''{Status}|$''{Version}\n' needrestart 2>/dev/null | head -n1)"
    case "$nr48990_dpkg_record" in
      "install ok installed|"*)
        nr48990_full_version="${nr48990_dpkg_record#*|}"
        [ -n "$nr48990_full_version" ] && nr48990_manager="dpkg"
        ;;
    esac
  fi
  if [ -z "$nr48990_full_version" ] && command -v rpm >/dev/null 2>&1; then
    nr48990_full_version="$(rpm --root "$nr48990_root" -q --qf '%{VERSION}-%{RELEASE}\n' needrestart 2>/dev/null | head -n1)"
    [ -n "$nr48990_full_version" ] && nr48990_manager="rpm"
  fi
  [ -n "$nr48990_binary" ] || [ -n "$nr48990_full_version" ] || return 0

  print_3title "Needrestart interpreter-scanner LPE (CVE-2024-48990)" "T1068"
  print_info "https://ubuntu.com/security/CVE-2024-48990"

  nr48990_os_release="${nr48990_root}etc/os-release"
  nr48990_distro_id="$(sed -nE 's/^ID="?([^" ]+)"?$/\1/p' "$nr48990_os_release" 2>/dev/null | head -n1)"
  nr48990_codename="$(sed -nE 's/^VERSION_CODENAME="?([^" ]+)"?$/\1/p' "$nr48990_os_release" 2>/dev/null | head -n1)"
  nr48990_ubuntu_codename="$(sed -nE 's/^UBUNTU_CODENAME="?([^" ]+)"?$/\1/p' "$nr48990_os_release" 2>/dev/null | head -n1)"
  if [ -n "$nr48990_ubuntu_codename" ]; then
    nr48990_distro_id="ubuntu"
    nr48990_codename="$nr48990_ubuntu_codename"
  fi

  nr48990_upstream_version="$(nr48990_extract_upstream_version "$nr48990_full_version")"
  nr48990_interpscan="$(nr48990_effective_interpscan "$nr48990_root")"
  nr48990_dpkg_fixed=""
  nr48990_status="unknown"

  if [ "$nr48990_manager" = "dpkg" ] && command -v dpkg >/dev/null 2>&1; then
    nr48990_dpkg_fixed="$(nr48990_fixed_dpkg_version "$nr48990_distro_id" "$nr48990_codename")"
    if [ -n "$nr48990_dpkg_fixed" ]; then
      if dpkg --compare-versions "$nr48990_full_version" lt "$nr48990_dpkg_fixed"; then
        nr48990_status="affected"
      else
        nr48990_status="fixed"
      fi
    fi
  fi
  if [ "$nr48990_status" = "unknown" ] && [ -n "$nr48990_upstream_version" ]; then
    if nr48990_version_lt "$nr48990_upstream_version" "3.8"; then
      nr48990_status="potential"
    else
      nr48990_status="fixed"
    fi
  fi

  echo "needrestart package: ${nr48990_full_version:-version unknown}${nr48990_manager:+ ($nr48990_manager)}" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
  if [ -n "$nr48990_dpkg_fixed" ]; then
    echo "Vendor fixed version for ${nr48990_distro_id:-unknown} ${nr48990_codename:-unknown}: $nr48990_dpkg_fixed"
  fi

  case "$nr48990_status:$nr48990_interpscan" in
    affected:0|potential:0)
      echo "Affected needrestart version detected, but the official interpscan=0 mitigation is active; update is still recommended" | sed -${E} "s,.*,${SED_YELLOW},"
      ;;
    affected:*)
      echo "VULNERABLE to CVE-2024-48990: needrestart $nr48990_full_version is below the vendor fixed version and interpreter scanning is enabled" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      ;;
    potential:*)
      echo "Potentially vulnerable to CVE-2024-48990: upstream needrestart $nr48990_upstream_version is before 3.8 and interpreter scanning is enabled; verify distro backports" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      ;;
    fixed:*)
      echo "needrestart $nr48990_full_version is not vulnerable to CVE-2024-48990 according to the known vendor/upstream fixed version" | sed -${E} "s,.*,${SED_GREEN},"
      ;;
    unknown:0)
      echo "needrestart is present; version is unknown, but the interpscan=0 mitigation is active" | sed -${E} "s,.*,${SED_YELLOW},"
      ;;
    *)
      echo "needrestart is present with interpreter scanning enabled, but its version could not be assessed" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      ;;
  esac
  echo "Effective interpreter scanning: $nr48990_interpscan (0=disabled mitigation, 1=enabled/default)"
  echo ""
}
