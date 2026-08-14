# Title: Functions - checkSnapConfineCVE20268933
# ID: checkSnapConfineCVE20268933
# Author: Chack Agent
# Last Update: 14-08-2026
# Description: Passively identify set-capabilities snap-confine binaries exposed to CVE-2026-8933.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_GREEN, $SED_LIGHT_CYAN, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $sc8933_caps, $sc8933_fixed, $sc8933_kind, $sc8933_os_id, $sc8933_os_release, $sc8933_path, $sc8933_reported, $sc8933_root, $sc8933_upstream, $sc8933_version, $sc8933_yaml
# Fat linpeas: 0
# Small linpeas: 1


sc8933_extract_upstream_version() {
  printf '%s' "$1" | sed -E 's/^[0-9]+://; s/^[^0-9]*//; s/[^0-9.].*$//'
}

sc8933_version_ge() {
  [ -n "$1" ] && [ -n "$2" ] || return 1
  if command -v dpkg >/dev/null 2>&1; then
    dpkg --compare-versions "$1" ge "$2"
  else
    [ "$(printf '%s\n%s\n' "$1" "$2" | sort -V 2>/dev/null | tail -n1)" = "$1" ]
  fi
}

sc8933_version_lt() {
  [ -n "$1" ] && [ -n "$2" ] || return 1
  if command -v dpkg >/dev/null 2>&1; then
    dpkg --compare-versions "$1" lt "$2"
  else
    [ "$1" != "$2" ] && [ "$(printf '%s\n%s\n' "$1" "$2" | sort -V 2>/dev/null | head -n1)" = "$1" ]
  fi
}

sc8933_version_is_vulnerable() {
  sc8933_version="$1"
  sc8933_kind="$2"
  sc8933_os_release="$3"
  sc8933_upstream="$(sc8933_extract_upstream_version "$sc8933_version")"

  # The upstream affected range is >= 2.75.0 and < 2.76.1. Ubuntu fixed
  # 2.76 with release-specific backports, so compare the complete dpkg version.
  sc8933_version_ge "$sc8933_upstream" "2.75" || return 1
  if [ "$sc8933_kind" = "deb" ]; then
    sc8933_fixed=""
    case "$sc8933_os_release" in
      22.04) sc8933_fixed="2.76+ubuntu22.04.1" ;;
      24.04) sc8933_fixed="2.76+ubuntu24.04.1" ;;
      26.04) sc8933_fixed="2.76+ubuntu26.04.3" ;;
    esac
    if [ -n "$sc8933_fixed" ]; then
      sc8933_version_lt "$sc8933_version" "$sc8933_fixed"
      return
    fi
  fi

  sc8933_version_lt "$sc8933_upstream" "2.76.1"
}

checkSnapConfineCVE20268933() {
  command -v getcap >/dev/null 2>&1 || return

  sc8933_root="${ROOT_FOLDER:-/}"
  case "$sc8933_root" in
    */) ;;
    *) sc8933_root="${sc8933_root}/" ;;
  esac
  sc8933_os_id="$(sed -nE 's/^ID="?([^" ]+)"?$/\1/p' "${sc8933_root}etc/os-release" 2>/dev/null | head -n1)"
  sc8933_os_release="$(sed -nE 's/^VERSION_ID="?([^" ]+)"?$/\1/p' "${sc8933_root}etc/os-release" 2>/dev/null | head -n1)"
  sc8933_reported=""

  for sc8933_path in \
    "${sc8933_root}usr/lib/snapd/snap-confine" \
    "${sc8933_root}snap/snapd/current/usr/lib/snapd/snap-confine"; do
    [ -f "$sc8933_path" ] || continue
    [ -u "$sc8933_path" ] && continue
    sc8933_caps="$(getcap "$sc8933_path" 2>/dev/null)"
    printf '%s' "$sc8933_caps" | grep -q 'cap_sys_admin' || continue

    if [ -z "$sc8933_reported" ]; then
      print_3title "Set-capabilities snap-confine (CVE-2026-8933)" "T1068"
      print_info "https://ubuntu.com/security/CVE-2026-8933"
      sc8933_reported="1"
    fi

    sc8933_kind="snap"
    sc8933_yaml="${sc8933_root}snap/snapd/current/meta/snap.yaml"
    sc8933_version=""
    case "$sc8933_path" in
      */usr/lib/snapd/snap-confine)
        if [ "$sc8933_path" = "${sc8933_root}usr/lib/snapd/snap-confine" ]; then
          sc8933_kind="deb"
          if command -v dpkg-query >/dev/null 2>&1; then
            sc8933_version="$(dpkg-query --admindir="${sc8933_root}var/lib/dpkg" -W -f='$''{Version}\n' snapd 2>/dev/null | head -n1)"
          fi
        elif [ -r "$sc8933_yaml" ]; then
          sc8933_version="$(sed -nE "s/^version:[[:space:]]*['\"]?([^'\"[:space:]]+).*/\1/p" "$sc8933_yaml" 2>/dev/null | head -n1)"
        fi
        ;;
    esac

    echo "$sc8933_caps" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
    if [ -z "$sc8933_version" ]; then
      echo "Potential CVE-2026-8933 exposure: privileged snap-confine uses file capabilities; version could not be determined" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    elif [ "$sc8933_os_id" != "ubuntu" ] && [ "$sc8933_kind" = "deb" ]; then
      if sc8933_version_is_vulnerable "$sc8933_version" "$sc8933_kind" "$sc8933_os_release"; then
        echo "snap-confine version $sc8933_version uses file capabilities and is in the upstream CVE-2026-8933 range; verify distro backports" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      else
        echo "snap-confine version $sc8933_version is not in the known CVE-2026-8933 vulnerable range" | sed -${E} "s,.*,${SED_GREEN},"
      fi
    elif sc8933_version_is_vulnerable "$sc8933_version" "$sc8933_kind" "$sc8933_os_release"; then
      echo "Vulnerable to CVE-2026-8933: set-capabilities snap-confine version $sc8933_version permits local privilege escalation to root" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    else
      echo "snap-confine version $sc8933_version is not in the known CVE-2026-8933 vulnerable range" | sed -${E} "s,.*,${SED_GREEN},"
    fi
  done

  if [ -n "$sc8933_reported" ]; then
    echo ""
  fi
}
