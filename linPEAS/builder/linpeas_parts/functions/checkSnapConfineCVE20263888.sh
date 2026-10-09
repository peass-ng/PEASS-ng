# Title: Functions - checkSnapConfineCVE20263888
# ID: checkSnapConfineCVE20263888
# Author: Chack Agent
# Last Update: 09-10-2026
# Description: Passive prerequisites for the snap-confine and tmpfiles race (CVE-2026-3888).
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $ROOT_FOLDER, $E, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $sc3888_root, $sc3888_path, $sc3888_priv, $sc3888_dir, $sc3888_version, $sc3888_release, $sc3888_fixed, $sc3888_state, $sc3888_rule, $sc3888_age, $sc3888_timer
# Fat linpeas: 0
# Small linpeas: 1

checkSnapConfineCVE20263888() {
  sc3888_root="${ROOT_FOLDER:-/}"
  case "$sc3888_root" in */) ;; *) sc3888_root="${sc3888_root}/" ;; esac
  sc3888_path="${sc3888_root}usr/lib/snapd/snap-confine"
  [ -f "$sc3888_path" ] || return 0

  sc3888_priv=""
  if [ -u "$sc3888_path" ]; then
    sc3888_priv="setuid"
  elif command -v getcap >/dev/null 2>&1 &&
       getcap "$sc3888_path" 2>/dev/null | grep -q 'cap_sys_admin'; then
    sc3888_priv="file capabilities"
  fi
  [ -n "$sc3888_priv" ] || return 0

  sc3888_release="$(sed -n 's/^VERSION_ID="\{0,1\}\([0-9][0-9.]*\)"\{0,1\}$/\1/p' "${sc3888_root}etc/os-release" 2>/dev/null | head -n 1)"
  sc3888_version=""
  if command -v dpkg-query >/dev/null 2>&1; then
    sc3888_version="$(dpkg-query --admindir="${sc3888_root}var/lib/dpkg" -W -f='$''{Status} $''{Version}\n' snapd 2>/dev/null | awk 'NR==1 && $1=="install" && $2=="ok" && $3=="installed" {print $4; exit}')"
  fi
  sc3888_fixed=""
  case "$sc3888_release" in
    16.04) sc3888_fixed="2.61.4ubuntu0.16.04.1+esm2" ;;
    18.04) sc3888_fixed="2.61.4ubuntu0.18.04.1+esm2" ;;
    20.04) sc3888_fixed="2.67.1+20.04ubuntu1~esm1" ;;
    22.04) sc3888_fixed="2.73+ubuntu22.04.1" ;;
    24.04) sc3888_fixed="2.73+ubuntu24.04.2" ;;
    25.10) sc3888_fixed="2.73+ubuntu25.10.1" ;;
    26.04) sc3888_fixed="2.74.1+ubuntu26.04.3" ;;
  esac
  sc3888_state="unknown"
  # Compare complete Debian versions only for Ubuntu packages on a known release.
  # A local rebuild, epoch, or unexpected suffix can contain a backported fix.
  if grep -q '^ID=\("\{0,1\}\)ubuntu\1$' "${sc3888_root}etc/os-release" 2>/dev/null &&
     [ -n "$sc3888_version" ] && [ -n "$sc3888_fixed" ] &&
     command -v dpkg >/dev/null 2>&1; then
    case "$sc3888_version" in
      *"$sc3888_release"*|*"ubuntu${sc3888_release}"*)
        if dpkg --compare-versions "$sc3888_version" ge "$sc3888_fixed"; then
          sc3888_state="at-or-above-fix"
        else
          sc3888_state="below-fix"
        fi
        ;;
    esac
  fi

  # Inspect the usual effective tmp.conf by directory precedence. Other rule
  # names and runtime unit state need a full tmpfiles/systemd evaluation.
  sc3888_rule=""
  for sc3888_dir in etc run usr/local/lib usr/lib lib; do
    if [ -r "${sc3888_root}${sc3888_dir}/tmpfiles.d/tmp.conf" ]; then
      sc3888_rule="${sc3888_root}${sc3888_dir}/tmpfiles.d/tmp.conf"
      break
    fi
  done
  sc3888_age=""
  if [ -n "$sc3888_rule" ]; then
    sc3888_age="$(awk 'NR > 100 {exit} $1 ~ /^[dDqQ][!~+=-]*$/ && $2 == "/tmp" && $6 ~ /^[0-9]+[smhdwMy]*$/ {print $6; exit}' "$sc3888_rule" 2>/dev/null)"
  fi
  sc3888_timer="unit not found"
  if [ -e "${sc3888_root}usr/lib/systemd/system/systemd-tmpfiles-clean.timer" ] ||
     [ -e "${sc3888_root}lib/systemd/system/systemd-tmpfiles-clean.timer" ]; then
    sc3888_timer="unit present; runtime activation unverified"
  fi
  if [ -e "${sc3888_root}etc/systemd/system/timers.target.wants/systemd-tmpfiles-clean.timer" ]; then
    sc3888_timer="enable link present; runtime activation unverified"
  fi

  print_3title "snap-confine/tmpfiles race prerequisites (CVE-2026-3888)" "T1068"
  print_info "https://ubuntu.com/security/CVE-2026-3888"
  echo "snap-confine: $sc3888_priv ($sc3888_path)"
  echo "snapd package: ${sc3888_version:-unknown}; Ubuntu release: ${sc3888_release:-unknown}"
  case "$sc3888_state" in
    at-or-above-fix) echo "Package version is at or above the Ubuntu advisory fix ($sc3888_fixed); verify local package provenance" ;;
    below-fix) echo "Package version is below the Ubuntu advisory fix ($sc3888_fixed); verify backports" ;;
    *) echo "Package fix status unclassified; verify distribution patches and package provenance" ;;
  esac
  if [ -n "$sc3888_age" ]; then
    echo "/tmp cleanup rule: age $sc3888_age ($sc3888_rule)"
  else
    echo "/tmp cleanup rule: not confirmed in standard tmp.conf locations"
  fi
  echo "systemd-tmpfiles-clean.timer: $sc3888_timer"
  if [ "$sc3888_state" = "below-fix" ] && [ -n "$sc3888_age" ] &&
     [ "$sc3888_timer" != "unit not found" ]; then
    echo "Potential CVE-2026-3888 prerequisites observed; timer activity, snap layout, and backports remain unverified" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  fi
  echo ""
}
