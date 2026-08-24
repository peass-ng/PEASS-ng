# Title: Functions - checkCIFSwitchCVE202646243
# ID: checkCIFSwitchCVE202646243
# Author: Chack Agent
# Last Update: 24-08-2026
# Description: Passively identify systems exposing the complete CIFSwitch (CVE-2026-46243) cifs.spnego upcall chain.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_LIGHT_CYAN, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $cs46243_config, $cs46243_helper, $cs46243_helper_host, $cs46243_kallsyms, $cs46243_kernel, $cs46243_major, $cs46243_minor, $cs46243_modules, $cs46243_patch, $cs46243_request_key, $cs46243_root
# Fat linpeas: 0
# Small linpeas: 1


cs46243_kernel_is_fixed() {
  cs46243_kernel="${1%%-*}"
  cs46243_major="$(printf '%s' "$cs46243_kernel" | cut -d. -f1)"
  cs46243_minor="$(printf '%s' "$cs46243_kernel" | cut -d. -f2)"
  cs46243_patch="$(printf '%s' "$cs46243_kernel" | cut -d. -f3)"

  case "$cs46243_major:$cs46243_minor:$cs46243_patch" in
    *[!0-9:]*|::*|*:|:*) return 1 ;;
  esac

  [ "$cs46243_major" -gt 7 ] && return 0
  if [ "$cs46243_major" -eq 7 ]; then
    [ "$cs46243_minor" -ge 1 ] && return 0
    [ "$cs46243_minor" -eq 0 ] && [ "$cs46243_patch" -ge 11 ] && return 0
    return 1
  fi

  [ "$cs46243_major" -lt 2 ] && return 0
  if [ "$cs46243_major" -eq 2 ]; then
    [ "$cs46243_minor" -lt 6 ] && return 0
    [ "$cs46243_minor" -eq 6 ] && [ "$cs46243_patch" -lt 24 ] && return 0
    return 1
  fi

  case "$cs46243_major.$cs46243_minor" in
    5.10) [ "$cs46243_patch" -ge 258 ] ;;
    5.15) [ "$cs46243_patch" -ge 209 ] ;;
    6.1) [ "$cs46243_patch" -ge 175 ] ;;
    6.6) [ "$cs46243_patch" -ge 142 ] ;;
    6.12) [ "$cs46243_patch" -ge 92 ] ;;
    6.18) [ "$cs46243_patch" -ge 34 ] ;;
    *) return 1 ;;
  esac
}

checkCIFSwitchCVE202646243() {
  [ "$(uname -s 2>/dev/null)" = "Linux" ] || return 0

  cs46243_root="${ROOT_FOLDER:-/}"
  case "$cs46243_root" in
    */) ;;
    *) cs46243_root="${cs46243_root}/" ;;
  esac

  cs46243_modules="${cs46243_root}proc/modules"
  if ! [ -d "${cs46243_root}sys/module/cifs" ]; then
    [ -r "$cs46243_modules" ] && grep -q '^cifs[[:space:]]' "$cs46243_modules" 2>/dev/null || return 0
  fi

  cs46243_config=""
  cs46243_helper=""
  # request-key reads the drop-in directory before the main file.
  for cs46243_config in "${cs46243_root}"etc/request-key.d/*.conf "${cs46243_root}etc/request-key.conf"; do
    [ -r "$cs46243_config" ] || continue
    cs46243_helper="$(awk '$1 == "create" && $2 == "cifs.spnego" && $5 ~ /(^|\/)cifs\.upcall$/ { print $5; exit }' "$cs46243_config" 2>/dev/null)"
    [ "$cs46243_helper" ] && break
  done
  [ "$cs46243_helper" ] || return 0

  case "$cs46243_helper" in
    /*) cs46243_helper_host="${cs46243_root}${cs46243_helper#/}" ;;
    *) cs46243_helper_host="$cs46243_helper" ;;
  esac
  [ -x "$cs46243_helper_host" ] || return 0

  cs46243_request_key="${cs46243_root}sbin/request-key"
  [ -x "$cs46243_request_key" ] || return 0

  cs46243_kallsyms="${cs46243_root}proc/kallsyms"
  if [ -r "$cs46243_kallsyms" ] && grep -q '[[:space:]]cifs_spnego_key_vet_description' "$cs46243_kallsyms" 2>/dev/null; then
    return
  fi

  cs46243_kernel="$(cat "${cs46243_root}proc/sys/kernel/osrelease" 2>/dev/null)"
  [ "$cs46243_kernel" ] || cs46243_kernel="$(uname -r 2>/dev/null)"
  cs46243_kernel_is_fixed "$cs46243_kernel" && return

  print_3title "CIFSwitch attack chain (CVE-2026-46243)" "T1068"
  print_info "https://access.redhat.com/security/vulnerabilities/RHSB-2026-005"
  echo "Loaded CIFS module + active cifs.spnego rule + executable request-key/cifs.upcall helpers" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  echo "Kernel $cs46243_kernel does not expose the upstream cifs.spnego validation marker; vendor backports should be verified" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
  echo "Rule: $cs46243_config -> $cs46243_helper"
}
