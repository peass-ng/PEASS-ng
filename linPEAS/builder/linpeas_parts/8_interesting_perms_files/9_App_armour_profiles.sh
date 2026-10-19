# Title: Interesting Permissions Files - AppArmor profiles
# ID: IP_App_armour_profiles
# Author: Carlos Polop
# Last Update: 19-10-2026
# Description: AppArmor profiles and Ubuntu CrackArmor host-path prerequisites
# License: GNU GPL
# Version: 1.1
# Mitre: T1518.001,T1068
# Functions Used: print_2title, print_3title, print_info
# Global Variables: $SEARCH_IN_FOLDER, $E, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $apparmor_ca_root, $apparmor_ca_release, $apparmor_ca_version, $apparmor_ca_fixed, $apparmor_ca_policy, $apparmor_ca_mode, $apparmor_ca_su, $apparmor_ca_owner
# Fat linpeas: 0
# Small linpeas: 0


# A patched kernel or another userspace mitigation can block this chain. Only
# report the documented Ubuntu host path when its su mitigation is absent.
apparmor_crackarmor_review() (
  apparmor_ca_root=$1
  apparmor_ca_release=$2
  apparmor_ca_version=$3
  case "$apparmor_ca_root" in */) ;; *) apparmor_ca_root="${apparmor_ca_root}/" ;; esac
  case "$apparmor_ca_release" in
    20.04) apparmor_ca_fixed=2.34-0.1ubuntu9.6+esm1 ;;
    22.04) apparmor_ca_fixed=2.37.2-4ubuntu3.5 ;;
    24.04) apparmor_ca_fixed=2.39.3-9ubuntu6.5 ;;
    25.10) apparmor_ca_fixed=2.41-4ubuntu4.2 ;;
    *) return 0 ;;
  esac
  [ -n "$apparmor_ca_version" ] && command -v dpkg >/dev/null 2>&1 || return 0
  dpkg --compare-versions "$apparmor_ca_version" lt "$apparmor_ca_fixed" 2>/dev/null || return 0
  [ "$(cat "${apparmor_ca_root}sys/module/apparmor/parameters/enabled" 2>/dev/null)" = Y ] || return 0
  for apparmor_ca_policy in .load .replace .remove; do
    [ -f "${apparmor_ca_root}sys/kernel/security/apparmor/${apparmor_ca_policy}" ] || return 0
    apparmor_ca_mode=$(LC_ALL=C ls -ld "${apparmor_ca_root}sys/kernel/security/apparmor/${apparmor_ca_policy}" 2>/dev/null | awk 'NR==1 {print $1}')
    [ "$(printf '%s' "$apparmor_ca_mode" | cut -c 9)" = w ] || return 0
  done
  apparmor_ca_su=
  for apparmor_ca_policy in usr/bin/su bin/su; do
    if [ -f "${apparmor_ca_root}${apparmor_ca_policy}" ] &&
       [ -x "${apparmor_ca_root}${apparmor_ca_policy}" ] &&
       [ -u "${apparmor_ca_root}${apparmor_ca_policy}" ]; then
      apparmor_ca_owner=$(stat -Lc %u "${apparmor_ca_root}${apparmor_ca_policy}" 2>/dev/null) ||
        apparmor_ca_owner=$(stat -L -f %u "${apparmor_ca_root}${apparmor_ca_policy}" 2>/dev/null)
      if [ "$apparmor_ca_owner" = 0 ]; then
        apparmor_ca_su="${apparmor_ca_root}${apparmor_ca_policy}"
        break
      fi
    fi
  done
  [ -n "$apparmor_ca_su" ] || return 0
  print_3title "CrackArmor host-path prerequisites (CVE-2026-23268)" "T1068"
  print_info "https://ubuntu.com/blog/apparmor-vulnerability-fixes-available"
  printf '%s\n' "Active AppArmor, world-writable policy files, root-setuid su, and util-linux $apparmor_ca_version below Ubuntu's $apparmor_ca_fixed su mitigation" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  printf '%s\n' 'Potential local root escalation; verify the running kernel fix, su --pty support, account password, and other mitigations. No policy write was attempted.'
)

if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ -d "/etc/apparmor.d/" ] && [ -r "/etc/apparmor.d/" ]; then
    print_2title "AppArmor binary profiles" "T1518.001"
    ls -l /etc/apparmor.d/ 2>/dev/null | grep -E "^-" | grep "\."
    echo ""
  fi
  if [ "$(uname -s 2>/dev/null)" = Linux ] &&
     grep -Eq '^ID=("ubuntu"|ubuntu)$' /etc/os-release 2>/dev/null &&
     command -v dpkg-query >/dev/null 2>&1; then
    apparmor_ca_release=$(sed -n 's/^VERSION_ID="\{0,1\}\([0-9][0-9.]*\)"\{0,1\}$/\1/p' /etc/os-release 2>/dev/null | head -n 1)
    apparmor_ca_version=$(dpkg-query -W -f='$''{Status} $''{Version}\n' util-linux 2>/dev/null | awk '$1=="install" && $2=="ok" && $3=="installed" {print $4; exit}')
    apparmor_crackarmor_review / "$apparmor_ca_release" "$apparmor_ca_version"
  fi
fi
