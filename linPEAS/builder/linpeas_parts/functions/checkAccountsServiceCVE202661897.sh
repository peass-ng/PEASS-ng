# Title: Functions - checkAccountsServiceCVE202661897
# ID: checkAccountsServiceCVE202661897
# Author: PEASS-ng
# Last Update: 16-11-2026
# Description: Passive Ubuntu AccountsService SetLanguage privilege-drop candidate (CVE-2026-61897).
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $ROOT_FOLDER
# Initial Functions:
# Generated Global Variables: $as61897_root, $as61897_release, $as61897_fixed, $as61897_daemon, $as61897_path, $as61897_service, $as61897_package
# Fat linpeas: 0
# Small linpeas: 1

checkAccountsServiceCVE202661897() { (
  [ "$(uname -s 2>/dev/null)" = Linux ] || return 0
  as61897_root=${ROOT_FOLDER:-/}
  case "$as61897_root" in */) ;; *) as61897_root="$as61897_root/" ;; esac
  [ -f "${as61897_root}etc/os-release" ] && [ -r "${as61897_root}etc/os-release" ] || return 0
  # Do not source os-release: it is only data, including in offline fixtures.
  as61897_release=$(awk -F= '
    NR > 80 { exit }
    $1 == "ID" { id=$2; gsub(/"/, "", id) }
    $1 == "VERSION_ID" { version=$2; gsub(/"/, "", version) }
    END { if (id == "ubuntu") print version }
  ' "${as61897_root}etc/os-release")
  case "$as61897_release" in
    26.04) as61897_fixed=23.13.9-8ubuntu5.2 ;;
    24.04) as61897_fixed=23.13.9-2ubuntu6.1 ;;
    22.04) as61897_fixed=22.07.5-2ubuntu1.6 ;;
    20.04) as61897_fixed='0.6.55-0ubuntu12~20.04.7+esm1' ;;
    18.04) as61897_fixed=0.6.45-1ubuntu1.3+esm2 ;;
    16.04) as61897_fixed=0.6.40-2ubuntu11.6+esm2 ;;
    14.04) as61897_fixed=0.6.35-0ubuntu7.3+esm4 ;;
    *) return 0 ;;
  esac

  # An installed package alone is not enough: require the Ubuntu language
  # helper and the system-bus activation path used by the vulnerable daemon.
  as61897_service="${as61897_root}usr/share/dbus-1/system-services/org.freedesktop.Accounts.service"
  [ -f "$as61897_service" ] && [ -r "$as61897_service" ] || return 0
  sed -n '1,40p;41q' "$as61897_service" | grep -Eq '^Name=org[.]freedesktop[.]Accounts$' || return 0
  [ -f "${as61897_root}usr/share/language-tools/set-language-helper" ] &&
    [ -r "${as61897_root}usr/share/language-tools/set-language-helper" ] || return 0
  as61897_daemon=
  for as61897_path in usr/libexec/accounts-daemon usr/lib/accountsservice/accounts-daemon; do
    if [ -f "${as61897_root}${as61897_path}" ] && [ -x "${as61897_root}${as61897_path}" ]; then
      as61897_daemon="${as61897_root}${as61897_path}"
      break
    fi
  done
  [ -n "$as61897_daemon" ] || return 0

  [ -x /usr/bin/dpkg-query ] && [ -x /usr/bin/dpkg ] || return 0
  [ -r "${as61897_root}var/lib/dpkg/status" ] || return 0
  as61897_package=$(/usr/bin/dpkg-query --admindir="${as61897_root}var/lib/dpkg" \
    -W -f='$''{db:Status-Status} $''{Version}\n' accountsservice 2>/dev/null |
    awk 'NR == 1 && $1 == "installed" && NF == 2 { print $2 }')
  [ -n "$as61897_package" ] || return 0
  /usr/bin/dpkg --compare-versions "$as61897_package" lt "$as61897_fixed" || return 0

  print_3title "Ubuntu AccountsService SetLanguage root escalation candidate (CVE-2026-61897)" "T1068"
  print_info "https://ubuntu.com/security/CVE-2026-61897"
  printf 'Ubuntu %s accountsservice %s is below the vendor fixed package %s.\n' \
    "$as61897_release" "$as61897_package" "$as61897_fixed"
  printf 'AccountsService D-Bus activation, daemon, and SetLanguage helper are present; verify the installed vendor package and whether the service is reachable.\n'
  echo ""
) ; }
