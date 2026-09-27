# Title: Functions - checkOVSPageCacheCVE2026
# ID: checkOVSPageCacheCVE2026
# Author: HT Bot
# Last Update: 27-09-2026
# Description: Passively correlate the kernel, Open vSwitch, ESP/XFRM, user-namespace, and setuid prerequisites for the CVE-2026-80977/CVE-2026-89487/CVE-2026-90049 page-cache LPE chain.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: lp_version_lt, print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_GREEN, $SED_LIGHT_CYAN, $SED_RED_YELLOW, $SED_YELLOW
# Initial Functions:
# Generated Global Variables: $ovspc_affected, $ovspc_apparmor, $ovspc_base, $ovspc_candidate, $ovspc_candidates, $ovspc_config, $ovspc_config_data, $ovspc_esp4_reachable, $ovspc_esp4_state, $ovspc_esp6_reachable, $ovspc_esp6_state, $ovspc_fixed, $ovspc_genl_state, $ovspc_kernel, $ovspc_kernel_status, $ovspc_magic, $ovspc_major, $ovspc_minor, $ovspc_mod, $ovspc_modules_builtin, $ovspc_modules_dep, $ovspc_modules_disabled, $ovspc_openvswitch_reachable, $ovspc_openvswitch_state, $ovspc_patch, $ovspc_root, $ovspc_rule, $ovspc_symbol, $ovspc_target, $ovspc_userns_clone, $ovspc_userns_max, $ovspc_userns_state, $ovspc_xfrm_reachable, $ovspc_xfrm_state
# Fat linpeas: 0
# Small linpeas: 1


ovspc_kernel_status_for_release() {
  ovspc_kernel="$1"
  ovspc_base="${ovspc_kernel%%-*}"
  ovspc_major="$(printf '%s' "$ovspc_base" | cut -d. -f1)"
  ovspc_minor="$(printf '%s' "$ovspc_base" | cut -d. -f2)"
  ovspc_patch="$(printf '%s' "$ovspc_base" | cut -d. -f3)"
  [ -n "$ovspc_patch" ] || ovspc_patch=0

  case "$ovspc_major:$ovspc_minor:$ovspc_patch" in
    *[!0-9:]*|::*|*:|:*) echo "unknown"; return ;;
  esac

  # The underlying destructive zerocopy handling predates current kernels.
  # Old 3.10/3.12 backports are deliberately not flagged: they predate the
  # shared-frag/ESP consumer that turns this specific bug into the described LPE.
  if lp_version_lt "$ovspc_base" "3.14.0"; then
    echo "predates"
    return
  fi

  # All three fixes are present from 7.3-rc1.  On stable branches the last
  # member of the series (CVE-2026-90049) determines the safe release.
  if ! lp_version_lt "$ovspc_base" "7.3.0"; then
    echo "fixed"
    return
  fi

  ovspc_fixed=""
  case "$ovspc_major.$ovspc_minor" in
    5.10) [ "$ovspc_patch" -ge 270 ] && ovspc_fixed="yes" ;;
    5.15) [ "$ovspc_patch" -ge 221 ] && ovspc_fixed="yes" ;;
    6.1)  [ "$ovspc_patch" -ge 188 ] && ovspc_fixed="yes" ;;
    6.6)  [ "$ovspc_patch" -ge 157 ] && ovspc_fixed="yes" ;;
    6.12) [ "$ovspc_patch" -ge 110 ] && ovspc_fixed="yes" ;;
    6.18) [ "$ovspc_patch" -ge 51 ] && ovspc_fixed="yes" ;;
    7.2)  [ "$ovspc_patch" -ge 5 ] && ovspc_fixed="yes" ;;
  esac

  if [ "$ovspc_fixed" ]; then
    echo "fixed"
  else
    echo "affected"
  fi
}

ovspc_module_state() {
  ovspc_mod="$1"
  ovspc_symbol="$2"

  if [ -d "${ovspc_root}sys/module/${ovspc_mod}" ] \
    || grep -q "^${ovspc_mod}[[:space:]]" "${ovspc_root}proc/modules" 2>/dev/null; then
    echo "loaded"
    return
  fi

  if printf '%s\n' "$ovspc_config_data" | grep -q "^${ovspc_symbol}=y$" \
    || grep -Eq "(^|/)${ovspc_mod}\.ko(\.(gz|xz|zst))?$" "$ovspc_modules_builtin" 2>/dev/null; then
    echo "built-in"
    return
  fi

  if printf '%s\n' "$ovspc_config_data" | grep -q "^${ovspc_symbol}=m$" \
    || grep -Eq "(^|/)${ovspc_mod}\.ko(\.(gz|xz|zst))?:" "$ovspc_modules_dep" 2>/dev/null; then
    echo "module"
    return
  fi

  # modinfo does not load a module.  Only use it for the live root because its
  # path lookup cannot be redirected reliably to ROOT_FOLDER on all systems.
  if [ "$ovspc_root" = "/" ] && command -v modinfo >/dev/null 2>&1; then
    ovspc_candidate="$(modinfo -n "$ovspc_mod" 2>/dev/null)"
    case "$ovspc_candidate" in
      "") ;;
      "(builtin)") echo "built-in"; return ;;
      *) echo "module"; return ;;
    esac
  fi

  echo "unavailable"
}

ovspc_module_is_hard_disabled() {
  ovspc_mod="$1"
  for ovspc_rule in \
    "${ovspc_root}"etc/modprobe.d/*.conf \
    "${ovspc_root}"run/modprobe.d/*.conf \
    "${ovspc_root}"usr/lib/modprobe.d/*.conf \
    "${ovspc_root}"lib/modprobe.d/*.conf; do
    [ -r "$ovspc_rule" ] || continue
    grep -Eq "^[[:space:]]*install[[:space:]]+${ovspc_mod}[[:space:]]+(/usr)?/bin/(true|false)([[:space:]#]|$)" "$ovspc_rule" 2>/dev/null \
      && return 0
  done
  return 1
}

ovspc_module_is_reachable() {
  case "$2" in
    loaded|built-in) return 0 ;;
    module)
      [ "$ovspc_modules_disabled" = "1" ] && return 1
      ovspc_module_is_hard_disabled "$1" && return 1
      return 0
      ;;
  esac
  return 1
}

checkOVSPageCacheCVE2026() {
  [ "$(uname -s 2>/dev/null)" = "Linux" ] || return 0

  ovspc_root="${ROOT_FOLDER:-/}"
  case "$ovspc_root" in
    */) ;;
    *) ovspc_root="${ovspc_root}/" ;;
  esac

  ovspc_kernel="$(cat "${ovspc_root}proc/sys/kernel/osrelease" 2>/dev/null)"
  [ "$ovspc_kernel" ] || ovspc_kernel="$(uname -r 2>/dev/null)"
  ovspc_kernel_status="$(ovspc_kernel_status_for_release "$ovspc_kernel")"

  print_3title "Open vSwitch forwarded-SKB page-cache LPE (CVE-2026-80977 / CVE-2026-89487 / CVE-2026-90049)" "T1068"
  print_info "https://blog.doyensec.com/2026/09/17/ovs.html"
  print_info "https://lore.kernel.org/netdev/4B5CCA6E-2C49-4F86-8C4E-E1BE15C16C0A@doyensec.com/T/#t"

  case "$ovspc_kernel_status" in
    fixed)
      echo "Kernel $ovspc_kernel includes the complete fix series by upstream stable-version comparison" | sed -${E} "s,.*,${SED_GREEN},"
      return 0
      ;;
    predates)
      echo "Kernel $ovspc_kernel predates the known shared-frag/ESP form of this chain" | sed -${E} "s,.*,${SED_GREEN},"
      return 0
      ;;
    unknown)
      echo "Kernel release could not be compared; prerequisite exposure is reported without a vulnerability verdict" | sed -${E} "s,.*,${SED_YELLOW},"
      ;;
    affected)
      echo "Kernel $ovspc_kernel is in an upstream affected range; vendor backports may override this result" | sed -${E} "s,.*,${SED_YELLOW},"
      ;;
  esac

  ovspc_config=""
  for ovspc_candidate in \
    "${ovspc_root}proc/config.gz" \
    "${ovspc_root}boot/config-${ovspc_kernel}" \
    "${ovspc_root}lib/modules/${ovspc_kernel}/config"; do
    [ -r "$ovspc_candidate" ] && { ovspc_config="$ovspc_candidate"; break; }
  done

  ovspc_config_data=""
  if [ "$ovspc_config" ]; then
    case "$ovspc_config" in
      *.gz)
        if command -v gzip >/dev/null 2>&1; then
          ovspc_config_data="$(gzip -cd "$ovspc_config" 2>/dev/null | grep -E '^((CONFIG_OPENVSWITCH|CONFIG_INET_ESP|CONFIG_INET6_ESP|CONFIG_XFRM_USER|CONFIG_USER_NS|CONFIG_NET|CONFIG_GENERIC_NETLINK)=|# (CONFIG_OPENVSWITCH|CONFIG_INET_ESP|CONFIG_INET6_ESP|CONFIG_XFRM_USER|CONFIG_USER_NS|CONFIG_NET|CONFIG_GENERIC_NETLINK) is not set)')"
        fi
        ;;
      *)
        ovspc_config_data="$(grep -E '^((CONFIG_OPENVSWITCH|CONFIG_INET_ESP|CONFIG_INET6_ESP|CONFIG_XFRM_USER|CONFIG_USER_NS|CONFIG_NET|CONFIG_GENERIC_NETLINK)=|# (CONFIG_OPENVSWITCH|CONFIG_INET_ESP|CONFIG_INET6_ESP|CONFIG_XFRM_USER|CONFIG_USER_NS|CONFIG_NET|CONFIG_GENERIC_NETLINK) is not set)' "$ovspc_config" 2>/dev/null)"
        ;;
    esac
  fi

  ovspc_modules_dep="${ovspc_root}lib/modules/${ovspc_kernel}/modules.dep"
  ovspc_modules_builtin="${ovspc_root}lib/modules/${ovspc_kernel}/modules.builtin"
  ovspc_modules_disabled="$(cat "${ovspc_root}proc/sys/kernel/modules_disabled" 2>/dev/null)"

  ovspc_openvswitch_state="$(ovspc_module_state openvswitch CONFIG_OPENVSWITCH)"
  ovspc_esp4_state="$(ovspc_module_state esp4 CONFIG_INET_ESP)"
  ovspc_esp6_state="$(ovspc_module_state esp6 CONFIG_INET6_ESP)"
  ovspc_xfrm_state="$(ovspc_module_state xfrm_user CONFIG_XFRM_USER)"

  ovspc_openvswitch_reachable=""
  ovspc_esp4_reachable=""
  ovspc_esp6_reachable=""
  ovspc_xfrm_reachable=""
  ovspc_module_is_reachable openvswitch "$ovspc_openvswitch_state" && ovspc_openvswitch_reachable="yes"
  ovspc_module_is_reachable esp4 "$ovspc_esp4_state" && ovspc_esp4_reachable="yes"
  ovspc_module_is_reachable esp6 "$ovspc_esp6_state" && ovspc_esp6_reachable="yes"
  ovspc_module_is_reachable xfrm_user "$ovspc_xfrm_state" && ovspc_xfrm_reachable="yes"

  ovspc_genl_state="unknown"
  if printf '%s\n' "$ovspc_config_data" | grep -q '^CONFIG_NET=y$' \
    && printf '%s\n' "$ovspc_config_data" | grep -q '^CONFIG_GENERIC_NETLINK=y$'; then
    ovspc_genl_state="enabled"
  elif printf '%s\n' "$ovspc_config_data" | grep -Eq '^# (CONFIG_NET|CONFIG_GENERIC_NETLINK) is not set$'; then
    ovspc_genl_state="unavailable"
  elif [ "$ovspc_openvswitch_state" != "unavailable" ]; then
    # A present OVS datapath depends on generic netlink, even when the running
    # kernel configuration is not readable to the current user.
    ovspc_genl_state="inferred enabled"
  fi

  if [ "$ovspc_openvswitch_reachable" ]; then
    echo "Open vSwitch kernel datapath: $ovspc_openvswitch_state and reachable" | sed -${E} "s,.*,${SED_YELLOW},"
  else
    echo "Open vSwitch kernel datapath: $ovspc_openvswitch_state or blocked from loading" | sed -${E} "s,.*,${SED_GREEN},"
  fi
  echo "OVS generic-netlink support: $ovspc_genl_state"

  if [ "$ovspc_esp4_reachable$ovspc_esp6_reachable" ]; then
    echo "ESP in-place-decryption consumer: reachable (esp4: $ovspc_esp4_state; esp6: $ovspc_esp6_state)" | sed -${E} "s,.*,${SED_YELLOW},"
  else
    echo "ESP in-place-decryption consumer: unavailable or blocked (esp4: $ovspc_esp4_state; esp6: $ovspc_esp6_state)" | sed -${E} "s,.*,${SED_GREEN},"
  fi

  if [ "$ovspc_xfrm_reachable" ]; then
    echo "XFRM userspace state configuration: $ovspc_xfrm_state and reachable" | sed -${E} "s,.*,${SED_YELLOW},"
  else
    echo "XFRM userspace state configuration: $ovspc_xfrm_state or blocked from loading" | sed -${E} "s,.*,${SED_GREEN},"
  fi

  ovspc_userns_max="$(cat "${ovspc_root}proc/sys/user/max_user_namespaces" 2>/dev/null)"
  ovspc_userns_clone="$(cat "${ovspc_root}proc/sys/kernel/unprivileged_userns_clone" 2>/dev/null)"
  ovspc_apparmor="$(cat "${ovspc_root}proc/sys/kernel/apparmor_restrict_unprivileged_userns" 2>/dev/null)"
  ovspc_userns_state="unknown"
  if printf '%s\n' "$ovspc_config_data" | grep -Eq '^(CONFIG_USER_NS=n|# CONFIG_USER_NS is not set)$' \
    || [ "$ovspc_userns_max" = "0" ] \
    || [ "$ovspc_userns_clone" = "0" ] \
    || [ "$ovspc_apparmor" = "1" ]; then
    ovspc_userns_state="blocked"
  elif printf '%s\n' "$ovspc_userns_max" | grep -Eq '^[1-9][0-9]*$' \
    && [ "$ovspc_userns_clone" != "0" ] \
    && [ "$ovspc_apparmor" != "1" ]; then
    ovspc_userns_state="enabled"
  elif printf '%s\n' "$ovspc_config_data" | grep -q '^CONFIG_USER_NS=y$' \
    && [ "$ovspc_userns_clone" != "0" ] \
    && [ "$ovspc_apparmor" != "1" ]; then
    ovspc_userns_state="enabled"
  fi

  case "$ovspc_userns_state" in
    enabled) echo "Unprivileged user namespaces: enabled (namespace CAP_NET_ADMIN is obtainable)" | sed -${E} "s,.*,${SED_YELLOW}," ;;
    blocked) echo "Unprivileged user namespaces: disabled or AppArmor-restricted (default attack path blocked)" | sed -${E} "s,.*,${SED_GREEN}," ;;
    *) echo "Unprivileged user namespaces: could not be determined" | sed -${E} "s,.*,${SED_YELLOW}," ;;
  esac

  # Reuse the common setuid targets highlighted by linPEAS instead of another
  # filesystem-wide search.  The public exploit only needs one readable,
  # root-owned setuid executable as its transient page-cache target.
  ovspc_candidates=""
  for ovspc_target in \
    usr/bin/mount usr/bin/chfn usr/bin/chsh usr/bin/newgrp usr/bin/chage \
    usr/bin/gpasswd usr/bin/su usr/bin/passwd bin/mount bin/su; do
    ovspc_candidate="${ovspc_root}${ovspc_target}"
    ovspc_magic=""
    if command -v od >/dev/null 2>&1; then
      ovspc_magic="$(od -An -tx1 -N4 "$ovspc_candidate" 2>/dev/null | tr -d '[:space:]')"
    fi
    [ "$ovspc_magic" = "7f454c46" ] \
      && [ -f "$ovspc_candidate" ] && [ -r "$ovspc_candidate" ] && [ -x "$ovspc_candidate" ] && [ -u "$ovspc_candidate" ] \
      && [ "$(ls -dn "$ovspc_candidate" 2>/dev/null | awk '{print $3}')" = "0" ] \
      && ovspc_candidates="$ovspc_candidates /$ovspc_target"
  done

  if [ "$ovspc_candidates" ]; then
    echo "Readable root-owned setuid target(s):$ovspc_candidates" | sed -${E} "s,.*,${SED_YELLOW},"
  else
    echo "Readable root-owned setuid target: none found in common system paths" | sed -${E} "s,.*,${SED_GREEN},"
  fi

  ovspc_affected=""
  [ "$ovspc_kernel_status" = "affected" ] \
    && [ "$ovspc_openvswitch_reachable" ] \
    && [ "$ovspc_esp4_reachable$ovspc_esp6_reachable" ] \
    && [ "$ovspc_xfrm_reachable" ] \
    && [ "$ovspc_userns_state" = "enabled" ] \
    && [ "$ovspc_candidates" ] \
    && ovspc_affected="yes"

  if [ "$ovspc_affected" ]; then
    echo "POTENTIAL ROOT LPE: the affected kernel range and all known OVS/ESP/userns/setuid prerequisites were detected" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    echo "Confirm the running distribution kernel contains all three vendor backports before treating it as vulnerable" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
    echo "Mitigation: update the vendor kernel; if unused, hard-disable openvswitch (or esp4/esp6) with an install /bin/false rule"
  else
    echo "Complete public exploit chain not confirmed; one or more required conditions above is missing or unknown" | sed -${E} "s,.*,${SED_GREEN},"
  fi
}
