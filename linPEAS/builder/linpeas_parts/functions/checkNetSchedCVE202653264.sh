# Title: Functions - checkNetSchedCVE202653264
# ID: checkNetSchedCVE202653264
# Author: HT Bot
# Last Update: 25-09-2026
# Description: Passively identify systems exposing the unprivileged net/sched action UAF path in CVE-2026-53264.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_LIGHT_CYAN, $SED_RED_YELLOW, $SED_YELLOW
# Initial Functions:
# Generated Global Variables: $ns53264_base, $ns53264_cfg, $ns53264_cfg_data, $ns53264_codename, $ns53264_fixed_pkg, $ns53264_gact, $ns53264_kernel, $ns53264_major, $ns53264_minor, $ns53264_mod, $ns53264_mod_state, $ns53264_modules_builtin, $ns53264_modules_dep, $ns53264_modules_disabled, $ns53264_os_id, $ns53264_os_release, $ns53264_package, $ns53264_package_source, $ns53264_package_version, $ns53264_patch, $ns53264_rc, $ns53264_root, $ns53264_rule, $ns53264_status, $ns53264_userns, $ns53264_flower
# Fat linpeas: 0
# Small linpeas: 1


ns53264_kernel_status() {
  ns53264_kernel="$1"
  ns53264_base="${ns53264_kernel%%-*}"
  ns53264_major="$(printf '%s' "$ns53264_base" | cut -d. -f1)"
  ns53264_minor="$(printf '%s' "$ns53264_base" | cut -d. -f2)"
  ns53264_patch="$(printf '%s' "$ns53264_base" | cut -d. -f3)"
  [ -n "$ns53264_patch" ] || ns53264_patch=0
  ns53264_rc="$(printf '%s' "$ns53264_kernel" | sed -n 's/.*-rc\([0-9][0-9]*\).*/\1/p')"

  case "$ns53264_major:$ns53264_minor:$ns53264_patch" in
    *[!0-9:]*|::*|*:|:*) echo unknown; return ;;
  esac

  if [ "$ns53264_major" -lt 4 ] \
    || { [ "$ns53264_major" -eq 4 ] && [ "$ns53264_minor" -lt 14 ]; }; then
    echo predates
    return
  fi

  if [ "$ns53264_major" -gt 7 ] \
    || { [ "$ns53264_major" -eq 7 ] && [ "$ns53264_minor" -gt 1 ]; }; then
    echo fixed
    return
  fi

  if [ "$ns53264_major" -eq 7 ] && [ "$ns53264_minor" -eq 1 ]; then
    if [ -n "$ns53264_rc" ] && [ "$ns53264_rc" -lt 7 ]; then
      echo affected
    else
      echo fixed
    fi
    return
  fi

  case "$ns53264_major.$ns53264_minor" in
    5.10) [ "$ns53264_patch" -ge 259 ] && echo fixed || echo affected ;;
    5.15) [ "$ns53264_patch" -ge 210 ] && echo fixed || echo affected ;;
    6.1)  [ "$ns53264_patch" -ge 176 ] && echo fixed || echo affected ;;
    6.6)  [ "$ns53264_patch" -ge 143 ] && echo fixed || echo affected ;;
    6.12) [ "$ns53264_patch" -ge 94 ] && echo fixed || echo affected ;;
    6.18) [ "$ns53264_patch" -ge 36 ] && echo fixed || echo affected ;;
    7.0)  [ "$ns53264_patch" -ge 13 ] && echo fixed || echo affected ;;
    *) echo affected ;;
  esac
}

ns53264_module_is_disabled() {
  ns53264_mod="$1"
  for ns53264_rule in \
    "${ns53264_root}"etc/modprobe.d/*.conf \
    "${ns53264_root}"run/modprobe.d/*.conf \
    "${ns53264_root}"usr/lib/modprobe.d/*.conf \
    "${ns53264_root}"lib/modprobe.d/*.conf; do
    [ -r "$ns53264_rule" ] || continue
    grep -Eq "^[[:space:]]*install[[:space:]]+${ns53264_mod}[[:space:]]+(/usr)?/bin/(true|false)([[:space:]#]|$)" "$ns53264_rule" 2>/dev/null \
      && return 0
  done
  return 1
}

ns53264_module_state() {
  ns53264_mod="$1"
  ns53264_cfg="$2"
  ns53264_mod_state="unknown"

  if printf '%s\n' "$ns53264_cfg_data" | grep -q "^${ns53264_cfg}=y$"; then
    ns53264_mod_state="built-in"
  elif printf '%s\n' "$ns53264_cfg_data" | grep -q "^${ns53264_cfg}=m$"; then
    ns53264_mod_state="module"
  elif printf '%s\n' "$ns53264_cfg_data" | grep -Eq "^# ${ns53264_cfg} is not set$|^${ns53264_cfg}=n$"; then
    ns53264_mod_state="disabled"
  fi

  if [ -d "${ns53264_root}sys/module/${ns53264_mod}" ] \
    || grep -q "^${ns53264_mod}[[:space:]]" "${ns53264_root}proc/modules" 2>/dev/null; then
    ns53264_mod_state="loaded"
  elif [ "$ns53264_mod_state" = "unknown" ] \
    && grep -Eq "(^|/)${ns53264_mod}\.ko(\.(gz|xz|zst))?:" "$ns53264_modules_dep" 2>/dev/null; then
    ns53264_mod_state="module"
  elif [ "$ns53264_mod_state" = "unknown" ] \
    && grep -Eq "(^|/)${ns53264_mod}\.ko(\.(gz|xz|zst))?$" "$ns53264_modules_builtin" 2>/dev/null; then
    ns53264_mod_state="built-in"
  fi

  if [ "$ns53264_mod_state" = "module" ]; then
    if [ "$ns53264_modules_disabled" = "1" ] || ns53264_module_is_disabled "$ns53264_mod"; then
      ns53264_mod_state="unavailable"
    fi
  fi

  printf '%s' "$ns53264_mod_state"
}

ns53264_running_package_is_fixed() {
  ns53264_fixed_pkg="no"
  ns53264_package=""
  ns53264_package_source=""
  ns53264_package_version=""

  # Package-manager queries only describe the live root.  For an alternate
  # ROOT_FOLDER, retain the conservative upstream-version result.
  [ "$ns53264_root" = "/" ] || return 1

  if command -v dpkg-query >/dev/null 2>&1; then
    ns53264_package="$(dpkg-query -S "/boot/vmlinuz-$ns53264_kernel" 2>/dev/null | sed 's/: .*//' | head -n1)"
    if [ -z "$ns53264_package" ]; then
      ns53264_package="$(dpkg-query -S "/lib/modules/$ns53264_kernel" 2>/dev/null | sed 's/: .*//' | head -n1)"
    fi
    if [ -n "$ns53264_package" ]; then
      ns53264_package_version="$(dpkg-query -W -f='$''{Version}\n' "$ns53264_package" 2>/dev/null | head -n1)"
      ns53264_package_source="$(dpkg-query -W -f='$''{source:Package}\n' "$ns53264_package" 2>/dev/null | head -n1 | sed 's/^src://')"

      for ns53264_rule in /usr/share/doc/"$ns53264_package"/changelog*; do
        [ -r "$ns53264_rule" ] || continue
        case "$ns53264_rule" in
          *.gz) command -v gzip >/dev/null 2>&1 && gzip -cd "$ns53264_rule" 2>/dev/null ;;
          *) cat "$ns53264_rule" 2>/dev/null ;;
        esac
      done | grep -Eiq 'CVE-2026-53264|5057e1aca011|net/sched: act_api: use RCU with deferred freeing' \
        && ns53264_fixed_pkg="yes"

      # Debian Security Tracker fixed source-package versions.
      if [ "$ns53264_fixed_pkg" = "no" ] && [ "$ns53264_os_id" = "debian" ] \
        && command -v dpkg >/dev/null 2>&1; then
        case "$ns53264_codename:$ns53264_package_source" in
          bullseye:linux)
            dpkg --compare-versions "$ns53264_package_version" ge '5.10.259-1' && ns53264_fixed_pkg="yes"
            ;;
          bullseye:linux-6.1)
            dpkg --compare-versions "$ns53264_package_version" ge '6.1.176-1~deb11u1' && ns53264_fixed_pkg="yes"
            ;;
          bookworm:linux)
            dpkg --compare-versions "$ns53264_package_version" ge '6.1.176-1' && ns53264_fixed_pkg="yes"
            ;;
          trixie:linux)
            dpkg --compare-versions "$ns53264_package_version" ge '6.12.94-1' && ns53264_fixed_pkg="yes"
            ;;
        esac
      fi

      # Canonical lists this standard Resolute kernel package as fixed.
      if [ "$ns53264_fixed_pkg" = "no" ] && [ "$ns53264_os_id" = "ubuntu" ] \
        && [ "$ns53264_codename" = "resolute" ] && [ "$ns53264_package_source" = "linux" ] \
        && command -v dpkg >/dev/null 2>&1; then
        dpkg --compare-versions "$ns53264_package_version" ge '7.0.0-31.31' \
          && ns53264_fixed_pkg="yes"
      fi
    fi
  fi

  if [ "$ns53264_fixed_pkg" = "no" ] && command -v rpm >/dev/null 2>&1; then
    ns53264_package="$(rpm -q --whatprovides "kernel-uname-r = $ns53264_kernel" 2>/dev/null | head -n1)"
    case "$ns53264_package" in
      ''|no\ package*) ;;
      *)
        ns53264_package_version="$(rpm -q --qf '%{VERSION}-%{RELEASE}\n' "$ns53264_package" 2>/dev/null | head -n1)"
        rpm -q --changelog "$ns53264_package" 2>/dev/null \
          | grep -Eiq 'CVE-2026-53264|5057e1aca011|net/sched: act_api: use RCU with deferred freeing' \
          && ns53264_fixed_pkg="yes"
        ;;
    esac
  fi

  [ "$ns53264_fixed_pkg" = "yes" ]
}

checkNetSchedCVE202653264() {
  [ "$(uname -s 2>/dev/null)" = "Linux" ] || return 0

  ns53264_root="${ROOT_FOLDER:-/}"
  case "$ns53264_root" in
    */) ;;
    *) ns53264_root="${ns53264_root}/" ;;
  esac

  ns53264_kernel="$(cat "${ns53264_root}proc/sys/kernel/osrelease" 2>/dev/null)"
  [ -n "$ns53264_kernel" ] || ns53264_kernel="$(uname -r 2>/dev/null)"
  ns53264_status="$(ns53264_kernel_status "$ns53264_kernel")"
  [ "$ns53264_status" = "affected" ] || return 0

  ns53264_os_release="${ns53264_root}etc/os-release"
  ns53264_os_id="$(sed -nE 's/^ID="?([^" ]+)"?$/\1/p' "$ns53264_os_release" 2>/dev/null | head -n1)"
  ns53264_codename="$(sed -nE 's/^VERSION_CODENAME="?([^" ]+)"?$/\1/p' "$ns53264_os_release" 2>/dev/null | head -n1)"
  ns53264_running_package_is_fixed && return 0

  ns53264_cfg=""
  for ns53264_cfg in \
    "${ns53264_root}proc/config.gz" \
    "${ns53264_root}boot/config-${ns53264_kernel}" \
    "${ns53264_root}lib/modules/${ns53264_kernel}/config" \
    "${ns53264_root}lib/modules/${ns53264_kernel}/build/.config" \
    "${ns53264_root}usr/lib/modules/${ns53264_kernel}/build/.config"; do
    [ -r "$ns53264_cfg" ] && break
    ns53264_cfg=""
  done

  ns53264_cfg_data=""
  if [ -n "$ns53264_cfg" ]; then
    case "$ns53264_cfg" in
      *.gz)
        if command -v gzip >/dev/null 2>&1; then
          ns53264_cfg_data="$(gzip -cd "$ns53264_cfg" 2>/dev/null | grep -E '^(CONFIG_USER_NS|CONFIG_NET_ACT_GACT|CONFIG_NET_CLS_FLOWER)=|^# (CONFIG_USER_NS|CONFIG_NET_ACT_GACT|CONFIG_NET_CLS_FLOWER) is not set$')"
        fi
        ;;
      *)
        ns53264_cfg_data="$(grep -E '^(CONFIG_USER_NS|CONFIG_NET_ACT_GACT|CONFIG_NET_CLS_FLOWER)=|^# (CONFIG_USER_NS|CONFIG_NET_ACT_GACT|CONFIG_NET_CLS_FLOWER) is not set$' "$ns53264_cfg" 2>/dev/null)"
        ;;
    esac
  fi

  if printf '%s\n' "$ns53264_cfg_data" | grep -Eq '^# CONFIG_USER_NS is not set$|^CONFIG_USER_NS=n$'; then
    return 0
  fi

  ns53264_userns="unknown"
  if printf '%s\n' "$ns53264_cfg_data" | grep -q '^CONFIG_USER_NS=y$'; then
    ns53264_userns="enabled"
  fi
  if [ -r "${ns53264_root}proc/sys/user/max_user_namespaces" ]; then
    ns53264_rule="$(cat "${ns53264_root}proc/sys/user/max_user_namespaces" 2>/dev/null)"
    case "$ns53264_rule" in
      ''|*[!0-9]*) ;;
      0) return 0 ;;
      *) ns53264_userns="enabled" ;;
    esac
  fi
  if [ -r "${ns53264_root}proc/sys/kernel/unprivileged_userns_clone" ]; then
    ns53264_rule="$(cat "${ns53264_root}proc/sys/kernel/unprivileged_userns_clone" 2>/dev/null)"
    [ "$ns53264_rule" = "0" ] && return 0
    [ "$ns53264_rule" = "1" ] && ns53264_userns="enabled"
  fi

  ns53264_modules_dep="${ns53264_root}lib/modules/${ns53264_kernel}/modules.dep"
  ns53264_modules_builtin="${ns53264_root}lib/modules/${ns53264_kernel}/modules.builtin"
  ns53264_modules_disabled="$(cat "${ns53264_root}proc/sys/kernel/modules_disabled" 2>/dev/null)"
  ns53264_gact="$(ns53264_module_state act_gact CONFIG_NET_ACT_GACT)"
  ns53264_flower="$(ns53264_module_state cls_flower CONFIG_NET_CLS_FLOWER)"

  case "$ns53264_gact:$ns53264_flower" in
    *disabled*|*unavailable*) return 0 ;;
  esac

  print_3title "net/sched action UAF exposure (CVE-2026-53264)" "T1068"
  print_info "https://www.cve.org/CVERecord?id=CVE-2026-53264"
  if [ "$ns53264_userns" = "enabled" ] \
    && [ "$ns53264_gact" != "unknown" ] && [ "$ns53264_flower" != "unknown" ]; then
    echo "HIGH-RISK: potentially vulnerable kernel $ns53264_kernel with unprivileged user namespaces and the required traffic-control components reachable" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  else
    echo "Potential CVE-2026-53264 exposure on kernel $ns53264_kernel; one or more prerequisites could not be confirmed" | sed -${E} "s,.*,${SED_YELLOW},"
  fi
  echo "Prerequisites: user namespaces: $ns53264_userns; act_gact: $ns53264_gact; cls_flower: $ns53264_flower" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
  if [ -n "$ns53264_package" ]; then
    echo "Running kernel package: $ns53264_package${ns53264_package_version:+ ($ns53264_package_version)}; no local fix marker or known fixed package boundary was found"
  else
    echo "Running kernel package could not be identified; vendor backports must be verified"
  fi
  echo "The public exploit is build-specific. Install the vendor kernel update; disabling unprivileged user namespaces is only a configuration mitigation."
}
