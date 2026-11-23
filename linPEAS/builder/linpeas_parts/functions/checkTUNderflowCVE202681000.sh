# Title: Functions - checkTUNderflowCVE202681000
# ID: checkTUNderflowCVE202681000
# Author: Chack Agent
# Last Update: 23-11-2026
# Description: Passively correlate the running kernel, TUN/OVS/VXLAN/netkit support, and namespace or CAP_NET_ADMIN reachability for TUNderflow (CVE-2026-81000).
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $td81000_root, $td81000_kernel, $td81000_base, $td81000_major, $td81000_minor, $td81000_patch, $td81000_rc, $td81000_config, $td81000_config_data, $td81000_modules_dep, $td81000_modules_builtin, $td81000_modules_disabled, $td81000_mod, $td81000_symbol, $td81000_tun_state, $td81000_ovs_state, $td81000_vxlan_state, $td81000_rule, $td81000_rule_count, $td81000_userns_max, $td81000_userns_clone, $td81000_apparmor_userns, $td81000_cap_hex, $td81000_cap_nibble, $td81000_access
# Fat linpeas: 0
# Small linpeas: 1

# These are upstream stable boundaries, not distribution package verdicts.
td81000_kernel_is_affected() {
  td81000_kernel=$1
  td81000_base=${td81000_kernel%%-*}
  td81000_major=${td81000_base%%.*}
  td81000_minor=${td81000_base#*.}
  td81000_minor=${td81000_minor%%.*}
  td81000_patch=${td81000_base#*.*.}
  [ "$td81000_patch" = "$td81000_base" ] && td81000_patch=0
  case "$td81000_major:$td81000_minor:$td81000_patch" in
    *[!0-9:]*|::*|*:|:*) return 1 ;;
  esac
  [ "$td81000_major" -ge 4 ] || return 1
  if [ "$td81000_major" -eq 4 ]; then
    [ "$td81000_minor" -ge 6 ] || return 1
  fi
  [ "$td81000_major" -le 7 ] || return 1
  if [ "$td81000_major" -eq 7 ]; then
    [ "$td81000_minor" -le 3 ] || return 1
    if [ "$td81000_minor" -eq 3 ]; then
      # The fix entered 7.3-rc1. Reject all 7.3 releases, including rc1.
      return 1
    fi
  fi
  case "$td81000_major.$td81000_minor" in
    5.10) [ "$td81000_patch" -lt 270 ] ;;
    5.15) [ "$td81000_patch" -lt 221 ] ;;
    6.1)  [ "$td81000_patch" -lt 188 ] ;;
    6.6)  [ "$td81000_patch" -lt 157 ] ;;
    6.12) [ "$td81000_patch" -lt 109 ] ;;
    6.18) [ "$td81000_patch" -lt 50 ] ;;
    7.2)
      td81000_rc=${td81000_kernel#*'-rc'}
      if [ "$td81000_rc" != "$td81000_kernel" ]; then
        return 0
      fi
      [ "$td81000_patch" -lt 4 ]
      ;;
    *) return 0 ;;
  esac
}

td81000_module_disabled() {
  td81000_mod=$1
  td81000_rule_count=0
  for td81000_rule in \
    "${td81000_root}etc/modprobe.d/"*.conf \
    "${td81000_root}run/modprobe.d/"*.conf \
    "${td81000_root}usr/lib/modprobe.d/"*.conf \
    "${td81000_root}lib/modprobe.d/"*.conf; do
    [ -f "$td81000_rule" ] && [ -r "$td81000_rule" ] || continue
    td81000_rule_count=$((td81000_rule_count + 1))
    [ "$td81000_rule_count" -le 64 ] || break
    # A hard-disable rule blocks autoload, but not a module already loaded.
    [ "$(wc -c < "$td81000_rule" 2>/dev/null)" -le 65536 ] 2>/dev/null || continue
    grep -Eq "^[[:space:]]*install[[:space:]]+${td81000_mod}[[:space:]]+(/usr)?/bin/(true|false)([[:space:]#]|$)" "$td81000_rule" 2>/dev/null && return 0
  done
  return 1
}

td81000_module_state() {
  td81000_mod=$1
  td81000_symbol=$2
  if [ -d "${td81000_root}sys/module/${td81000_mod}" ] ||
     grep -q "^${td81000_mod}[[:space:]]" "${td81000_root}proc/modules" 2>/dev/null; then
    printf 'loaded\n'
    return 0
  fi
  if printf '%s\n' "$td81000_config_data" | grep -q "^${td81000_symbol}=y$" ||
     grep -Eq "(^|/)${td81000_mod}[.]ko([.](gz|xz|zst))?$" "$td81000_modules_builtin" 2>/dev/null; then
    printf 'built-in\n'
    return 0
  fi
  # CONFIG_FOO=m alone does not establish that the module is still installed.
  if grep -Eq "(^|/)${td81000_mod}[.]ko([.](gz|xz|zst))?:" "$td81000_modules_dep" 2>/dev/null; then
    if [ "$td81000_modules_disabled" = 1 ] || td81000_module_disabled "$td81000_mod"; then
      printf 'unavailable\n'
    else
      printf 'module\n'
    fi
    return 0
  fi
  printf 'unavailable\n'
}

checkTUNderflowCVE202681000() {
  [ "$(uname -s 2>/dev/null)" = Linux ] || return 0
  td81000_root=${ROOT_FOLDER:-/}
  case "$td81000_root" in */) ;; *) td81000_root="${td81000_root}/" ;; esac
  td81000_kernel=$(cat "${td81000_root}proc/sys/kernel/osrelease" 2>/dev/null)
  [ -n "$td81000_kernel" ] || td81000_kernel=$(uname -r 2>/dev/null)
  td81000_kernel_is_affected "$td81000_kernel" || return 0

  # Do not open the device: merely establishing its access is sufficient.
  [ -r "${td81000_root}dev/net/tun" ] && [ -w "${td81000_root}dev/net/tun" ] || return 0

  td81000_config=
  for td81000_config in \
    "${td81000_root}boot/config-${td81000_kernel}" \
    "${td81000_root}lib/modules/${td81000_kernel}/config"; do
    [ -r "$td81000_config" ] && break
    td81000_config=
  done
  td81000_config_data=
  if [ -n "$td81000_config" ] && [ "$(wc -c < "$td81000_config" 2>/dev/null)" -le 1048576 ] 2>/dev/null; then
    td81000_config_data=$(grep -E '^(CONFIG_TUN|CONFIG_OPENVSWITCH|CONFIG_VXLAN|CONFIG_NETKIT)=' "$td81000_config" 2>/dev/null)
  elif [ -f "${td81000_root}proc/config.gz" ] && [ -r "${td81000_root}proc/config.gz" ] &&
       [ "$(wc -c < "${td81000_root}proc/config.gz" 2>/dev/null)" -le 262144 ] 2>/dev/null &&
       command -v gzip >/dev/null 2>&1; then
    td81000_config_data=$(gzip -cd "${td81000_root}proc/config.gz" 2>/dev/null | grep -E '^(CONFIG_TUN|CONFIG_OPENVSWITCH|CONFIG_VXLAN|CONFIG_NETKIT)=')
  fi

  # The public chain's oversized headroom source is netkit (built in only).
  # Other headroom sources may exist, so a missing netkit suppresses only this
  # evidence-backed candidate, not the underlying CVE globally.
  printf '%s\n' "$td81000_config_data" | grep -q '^CONFIG_NETKIT=y$' || return 0

  td81000_modules_dep="${td81000_root}lib/modules/${td81000_kernel}/modules.dep"
  td81000_modules_builtin="${td81000_root}lib/modules/${td81000_kernel}/modules.builtin"
  td81000_modules_disabled=$(cat "${td81000_root}proc/sys/kernel/modules_disabled" 2>/dev/null)
  td81000_tun_state=$(td81000_module_state tun CONFIG_TUN)
  td81000_ovs_state=$(td81000_module_state openvswitch CONFIG_OPENVSWITCH)
  td81000_vxlan_state=$(td81000_module_state vxlan CONFIG_VXLAN)
  [ "$td81000_tun_state" != unavailable ] && [ "$td81000_ovs_state" != unavailable ] && [ "$td81000_vxlan_state" != unavailable ] || return 0

  td81000_access=
  td81000_userns_max=$(cat "${td81000_root}proc/sys/user/max_user_namespaces" 2>/dev/null)
  td81000_userns_clone=$(cat "${td81000_root}proc/sys/kernel/unprivileged_userns_clone" 2>/dev/null)
  td81000_apparmor_userns=$(cat "${td81000_root}proc/sys/kernel/apparmor_restrict_unprivileged_userns" 2>/dev/null)
  case "$td81000_userns_max" in
    ''|*[!0-9]*) ;;
    *) if [ "$td81000_userns_max" -gt 0 ] && [ "$td81000_userns_clone" != 0 ] && [ "$td81000_apparmor_userns" != 1 ]; then
         td81000_access='unprivileged user namespaces appear enabled'
       fi ;;
  esac
  td81000_cap_hex=$(awk '/^CapEff:/ && !seen++ { print $2 }' "${td81000_root}proc/self/status" 2>/dev/null)
  # CAP_NET_ADMIN is bit 12, the low bit of the fourth hex digit from right.
  td81000_cap_nibble=$(printf '%s\n' "$td81000_cap_hex" | awk 'length($0) >= 4 { print substr($0, length($0)-3, 1) }')
  case "$td81000_cap_nibble" in
    [13579bBdDfF])
      if [ -n "$td81000_access" ]; then td81000_access="$td81000_access or CAP_NET_ADMIN is effective"
      else td81000_access='CAP_NET_ADMIN is effective'; fi ;;
  esac
  [ -n "$td81000_access" ] || return 0

  print_3title 'TUNderflow TUN/OVS headroom LPE (CVE-2026-81000)' 'T1068'
  print_info 'https://github.com/manizada/TUNderflow'
  printf 'Upstream affected kernel candidate: %s (distribution backports and live patching require verification).\n' "$td81000_kernel" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  printf 'Evidence: accessible /dev/net/tun; TUN=%s, Open vSwitch=%s, VXLAN=%s, netkit=built-in; %s.\n' "$td81000_tun_state" "$td81000_ovs_state" "$td81000_vxlan_state" "$td81000_access"
  printf 'This is a passive prerequisite match, not proof of exploitability; seccomp/LSM and module-loading policy may further restrict the path.\n'
}
