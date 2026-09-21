# Title: Functions - checkDiagSpillCVE202674469
# ID: checkDiagSpillCVE202674469
# Author: Chack Agent
# Last Update: 21-09-2026
# Description: Passively identify kernels with reachable SCTP diagnostics that may be exposed to DiagSpill (CVE-2026-74469).
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_LIGHT_CYAN, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $ds74469_base, $ds74469_config, $ds74469_config_data, $ds74469_diag_state, $ds74469_disabled, $ds74469_kernel, $ds74469_major, $ds74469_minor, $ds74469_mod, $ds74469_modules_builtin, $ds74469_modules_dep, $ds74469_modules_disabled, $ds74469_patch, $ds74469_rc, $ds74469_root, $ds74469_rule, $ds74469_sctp_state
# Fat linpeas: 0
# Small linpeas: 1


ds74469_kernel_is_affected() {
  ds74469_kernel="$1"
  ds74469_base="${ds74469_kernel%%-*}"
  ds74469_major="$(printf '%s' "$ds74469_base" | cut -d. -f1)"
  ds74469_minor="$(printf '%s' "$ds74469_base" | cut -d. -f2)"
  ds74469_patch="$(printf '%s' "$ds74469_base" | cut -d. -f3)"
  [ -n "$ds74469_patch" ] || ds74469_patch=0
  ds74469_rc="$(printf '%s' "$ds74469_kernel" | sed -n 's/.*-rc\([0-9][0-9]*\).*/\1/p')"

  case "$ds74469_major:$ds74469_minor:$ds74469_patch" in
    *[!0-9:]*|::*|*:|:*) return 1 ;;
  esac

  if [ "$ds74469_major" -lt 4 ] \
    || { [ "$ds74469_major" -eq 4 ] && [ "$ds74469_minor" -lt 7 ]; }; then
    return 1
  fi

  if [ "$ds74469_major" -gt 7 ] \
    || { [ "$ds74469_major" -eq 7 ] && [ "$ds74469_minor" -gt 2 ]; }; then
    return 1
  fi

  if [ "$ds74469_major" -eq 7 ] && [ "$ds74469_minor" -eq 2 ]; then
    [ -n "$ds74469_rc" ] && [ "$ds74469_rc" -lt 6 ] && return 0
    return 1
  fi

  case "$ds74469_major.$ds74469_minor" in
    5.10) [ "$ds74469_patch" -lt 265 ] ;;
    5.15) [ "$ds74469_patch" -lt 216 ] ;;
    6.1)  [ "$ds74469_patch" -lt 183 ] ;;
    6.6)  [ "$ds74469_patch" -lt 151 ] ;;
    6.12) [ "$ds74469_patch" -lt 103 ] ;;
    6.18) [ "$ds74469_patch" -lt 44 ] ;;
    7.1)  [ "$ds74469_patch" -lt 8 ] ;;
    *) return 0 ;;
  esac
}

ds74469_module_is_disabled() {
  ds74469_mod="$1"
  for ds74469_rule in \
    "${ds74469_root}"etc/modprobe.d/*.conf \
    "${ds74469_root}"run/modprobe.d/*.conf \
    "${ds74469_root}"usr/lib/modprobe.d/*.conf \
    "${ds74469_root}"lib/modprobe.d/*.conf; do
    [ -r "$ds74469_rule" ] || continue
    grep -Eq "^[[:space:]]*install[[:space:]]+${ds74469_mod}[[:space:]]+(/usr)?/bin/(true|false)([[:space:]#]|$)" "$ds74469_rule" 2>/dev/null \
      && return 0
  done
  return 1
}

checkDiagSpillCVE202674469() {
  [ "$(uname -s 2>/dev/null)" = "Linux" ] || return 0

  ds74469_root="${ROOT_FOLDER:-/}"
  case "$ds74469_root" in
    */) ;;
    *) ds74469_root="${ds74469_root}/" ;;
  esac

  ds74469_kernel="$(cat "${ds74469_root}proc/sys/kernel/osrelease" 2>/dev/null)"
  [ "$ds74469_kernel" ] || ds74469_kernel="$(uname -r 2>/dev/null)"
  ds74469_kernel_is_affected "$ds74469_kernel" || return 0

  ds74469_sctp_state=""
  ds74469_diag_state=""
  if [ -d "${ds74469_root}sys/module/sctp" ] \
    || grep -q '^sctp[[:space:]]' "${ds74469_root}proc/modules" 2>/dev/null; then
    ds74469_sctp_state="loaded"
  fi
  if [ -d "${ds74469_root}sys/module/sctp_diag" ] \
    || grep -q '^sctp_diag[[:space:]]' "${ds74469_root}proc/modules" 2>/dev/null; then
    ds74469_diag_state="loaded"
  fi

  ds74469_config=""
  for ds74469_config in \
    "${ds74469_root}proc/config.gz" \
    "${ds74469_root}boot/config-${ds74469_kernel}" \
    "${ds74469_root}lib/modules/${ds74469_kernel}/config"; do
    [ -r "$ds74469_config" ] && break
    ds74469_config=""
  done

  ds74469_config_data=""
  if [ "$ds74469_config" ]; then
    case "$ds74469_config" in
      *.gz)
        if command -v gzip >/dev/null 2>&1; then
          ds74469_config_data="$(gzip -cd "$ds74469_config" 2>/dev/null | grep -E '^(CONFIG_IP_SCTP|CONFIG_INET_SCTP_DIAG)=')"
        fi
        ;;
      *) ds74469_config_data="$(grep -E '^(CONFIG_IP_SCTP|CONFIG_INET_SCTP_DIAG)=' "$ds74469_config" 2>/dev/null)" ;;
    esac
  fi

  if [ -z "$ds74469_sctp_state" ] \
    && printf '%s\n' "$ds74469_config_data" | grep -q '^CONFIG_IP_SCTP=y$'; then
    ds74469_sctp_state="built-in"
  fi
  if [ -z "$ds74469_diag_state" ] \
    && printf '%s\n' "$ds74469_config_data" | grep -q '^CONFIG_INET_SCTP_DIAG=y$'; then
    ds74469_diag_state="built-in"
  fi

  ds74469_modules_dep="${ds74469_root}lib/modules/${ds74469_kernel}/modules.dep"
  ds74469_modules_builtin="${ds74469_root}lib/modules/${ds74469_kernel}/modules.builtin"

  if [ -z "$ds74469_sctp_state" ] \
    && grep -Eq '(^|/)sctp\.ko(\.(gz|xz|zst))?:' "$ds74469_modules_dep" 2>/dev/null; then
    ds74469_sctp_state="module"
  fi
  if [ -z "$ds74469_diag_state" ] \
    && grep -Eq '(^|/)sctp_diag\.ko(\.(gz|xz|zst))?:' "$ds74469_modules_dep" 2>/dev/null; then
    ds74469_diag_state="module"
  fi
  if [ -z "$ds74469_sctp_state" ] \
    && grep -Eq '(^|/)sctp\.ko(\.(gz|xz|zst))?$' "$ds74469_modules_builtin" 2>/dev/null; then
    ds74469_sctp_state="built-in"
  fi
  if [ -z "$ds74469_diag_state" ] \
    && grep -Eq '(^|/)sctp_diag\.ko(\.(gz|xz|zst))?$' "$ds74469_modules_builtin" 2>/dev/null; then
    ds74469_diag_state="built-in"
  fi

  [ "$ds74469_sctp_state" ] && [ "$ds74469_diag_state" ] || return 0

  ds74469_modules_disabled="$(cat "${ds74469_root}proc/sys/kernel/modules_disabled" 2>/dev/null)"
  if [ "$ds74469_modules_disabled" = "1" ]; then
    [ "$ds74469_sctp_state" = "module" ] && return 0
    [ "$ds74469_diag_state" = "module" ] && return 0
  fi

  ds74469_disabled=""
  if [ "$ds74469_sctp_state" = "module" ] && ds74469_module_is_disabled sctp; then
    ds74469_disabled="yes"
  fi
  if [ "$ds74469_diag_state" = "module" ] && ds74469_module_is_disabled sctp_diag; then
    ds74469_disabled="yes"
  fi
  [ "$ds74469_disabled" ] && return 0

  print_3title "DiagSpill SCTP kernel exposure (CVE-2026-74469)" "T1068"
  print_info "https://access.redhat.com/security/vulnerabilities/RHSB-2026-011"
  echo "Potentially vulnerable kernel $ds74469_kernel with SCTP diagnostics reachable (sctp: $ds74469_sctp_state; sctp_diag: $ds74469_diag_state)" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  echo "Local exploitation needs no capability or unprivileged user namespace; verify vendor backports" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
  echo "Mitigation: update the kernel, or hard-disable both sctp and sctp_diag if SCTP is unused"
}
