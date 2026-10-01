# Title: Functions - checkNfTablesCVE202623111
# ID: checkNfTablesCVE202623111
# Author: HT Bot
# Last Update: 01-10-2026
# Description: Passively identify Linux systems that may expose the nftables catchall verdict-map UAF in CVE-2026-23111.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_GREEN, $SED_LIGHT_CYAN, $SED_RED_YELLOW, $SED_YELLOW
# Initial Functions:
# Generated Global Variables: $nft23111_apparmor, $nft23111_apparmor_restrict, $nft23111_base, $nft23111_build, $nft23111_candidate, $nft23111_caps, $nft23111_caps_decoded, $nft23111_cfg, $nft23111_cfg_data, $nft23111_codename, $nft23111_distro, $nft23111_fixed_pkg, $nft23111_kernel, $nft23111_kernel_status, $nft23111_major, $nft23111_minor, $nft23111_mod, $nft23111_mod_state, $nft23111_modules_builtin, $nft23111_modules_dep, $nft23111_modules_disabled, $nft23111_netns, $nft23111_netns_max, $nft23111_nf_tables, $nft23111_os_id, $nft23111_os_release, $nft23111_package, $nft23111_package_source, $nft23111_package_status, $nft23111_package_version, $nft23111_patch, $nft23111_pipapo, $nft23111_rc, $nft23111_root, $nft23111_rule, $nft23111_seccomp, $nft23111_selinux, $nft23111_status_file, $nft23111_uid, $nft23111_userns, $nft23111_userns_clone, $nft23111_userns_max
# Fat linpeas: 0
# Small linpeas: 1


nft23111_kernel_status_for_release() {
  nft23111_kernel="$1"
  nft23111_base="${nft23111_kernel%%-*}"
  nft23111_major="$(printf '%s' "$nft23111_base" | cut -d. -f1)"
  nft23111_minor="$(printf '%s' "$nft23111_base" | cut -d. -f2)"
  nft23111_patch="$(printf '%s' "$nft23111_base" | cut -d. -f3)"
  [ -n "$nft23111_patch" ] || nft23111_patch=0
  nft23111_rc="$(printf '%s' "$nft23111_kernel" | sed -n 's/.*-rc\([0-9][0-9]*\).*/\1/p')"

  case "$nft23111_major:$nft23111_minor:$nft23111_patch" in
    *[!0-9:]*|::*|*:|:*) echo "unknown"; return ;;
  esac

  # The Linux kernel CVE record lists backported introductions on old stable
  # branches, so comparing only against upstream 6.19 would be inaccurate.
  case "$nft23111_major.$nft23111_minor" in
    4.19) [ "$nft23111_patch" -ge 316 ] && echo "affected" || echo "unaffected" ;;
    5.4)  [ "$nft23111_patch" -ge 262 ] && echo "affected" || echo "unaffected" ;;
    5.10) [ "$nft23111_patch" -ge 188 ] && echo "affected" || echo "unaffected" ;;
    5.15)
      if [ "$nft23111_patch" -lt 121 ]; then echo "unaffected"
      elif [ "$nft23111_patch" -lt 200 ]; then echo "affected"
      else echo "fixed"; fi
      ;;
    6.1)
      if [ "$nft23111_patch" -lt 36 ]; then echo "unaffected"
      elif [ "$nft23111_patch" -lt 163 ]; then echo "affected"
      else echo "fixed"; fi
      ;;
    6.2) echo "unaffected" ;;
    6.3) [ "$nft23111_patch" -ge 10 ] && echo "affected" || echo "unaffected" ;;
    6.4|6.5|6.7|6.8|6.9|6.10|6.11|6.13|6.14|6.15|6.16|6.17) echo "affected" ;;
    6.6)  [ "$nft23111_patch" -ge 124 ] && echo "fixed" || echo "affected" ;;
    6.12) [ "$nft23111_patch" -ge 70 ] && echo "fixed" || echo "affected" ;;
    6.18) [ "$nft23111_patch" -ge 10 ] && echo "fixed" || echo "affected" ;;
    *)
      if [ "$nft23111_major" -lt 4 ] \
        || { [ "$nft23111_major" -eq 4 ] && [ "$nft23111_minor" -lt 19 ]; }; then
        echo "unaffected"
      elif [ "$nft23111_major" -eq 6 ] && [ "$nft23111_minor" -eq 19 ] && [ -n "$nft23111_rc" ]; then
        echo "affected"
      elif [ "$nft23111_major" -gt 6 ] \
        || { [ "$nft23111_major" -eq 6 ] && [ "$nft23111_minor" -ge 19 ]; }; then
        echo "fixed"
      else
        echo "unaffected"
      fi
      ;;
  esac
}

nft23111_module_is_hard_disabled() {
  nft23111_mod="$1"
  for nft23111_rule in \
    "${nft23111_root}"etc/modprobe.d/*.conf \
    "${nft23111_root}"run/modprobe.d/*.conf \
    "${nft23111_root}"usr/lib/modprobe.d/*.conf \
    "${nft23111_root}"lib/modprobe.d/*.conf; do
    [ -r "$nft23111_rule" ] || continue
    grep -Eq "^[[:space:]]*install[[:space:]]+${nft23111_mod}[[:space:]]+(/usr)?/bin/(true|false)([[:space:]#]|$)" "$nft23111_rule" 2>/dev/null \
      && return 0
  done
  return 1
}

nft23111_module_state() {
  nft23111_mod="$1"
  nft23111_rule="$2"
  nft23111_mod_state="unknown"

  if printf '%s\n' "$nft23111_cfg_data" | grep -q "^${nft23111_rule}=y$"; then
    nft23111_mod_state="built-in"
  elif printf '%s\n' "$nft23111_cfg_data" | grep -q "^${nft23111_rule}=m$"; then
    nft23111_mod_state="module"
  elif printf '%s\n' "$nft23111_cfg_data" | grep -Eq "^# ${nft23111_rule} is not set$|^${nft23111_rule}=n$"; then
    nft23111_mod_state="disabled"
  fi

  if [ -d "${nft23111_root}sys/module/${nft23111_mod}" ] \
    || grep -q "^${nft23111_mod}[[:space:]]" "${nft23111_root}proc/modules" 2>/dev/null; then
    nft23111_mod_state="loaded"
  elif grep -Eq "(^|/)${nft23111_mod}\.ko(\.(gz|xz|zst))?$" "$nft23111_modules_builtin" 2>/dev/null; then
    nft23111_mod_state="built-in"
  elif grep -Eq "(^|/)${nft23111_mod}\.ko(\.(gz|xz|zst))?:" "$nft23111_modules_dep" 2>/dev/null; then
    nft23111_mod_state="module"
  elif [ "$nft23111_mod_state" = "unknown" ] && [ "$nft23111_root" = "/" ] \
    && command -v modinfo >/dev/null 2>&1; then
    nft23111_candidate="$(modinfo -n "$nft23111_mod" 2>/dev/null)"
    case "$nft23111_candidate" in
      "") ;;
      "(builtin)") nft23111_mod_state="built-in" ;;
      *) nft23111_mod_state="module" ;;
    esac
  fi

  if [ "$nft23111_mod_state" = "module" ] \
    && { [ "$nft23111_modules_disabled" = "1" ] || nft23111_module_is_hard_disabled "$nft23111_mod"; }; then
    nft23111_mod_state="unavailable"
  fi

  printf '%s' "$nft23111_mod_state"
}

nft23111_collect_package_status() {
  nft23111_fixed_pkg="no"
  nft23111_package=""
  nft23111_package_source=""
  nft23111_package_status="unknown"
  nft23111_package_version=""

  # Package-manager databases below describe only the live root.
  [ "$nft23111_root" = "/" ] || return

  if command -v dpkg-query >/dev/null 2>&1; then
    nft23111_package="$(dpkg-query -S "/boot/vmlinuz-$nft23111_kernel" 2>/dev/null | sed 's/: .*//' | head -n1)"
    [ -n "$nft23111_package" ] || nft23111_package="$(dpkg-query -S "/lib/modules/$nft23111_kernel" 2>/dev/null | sed 's/: .*//' | head -n1)"
    if [ -n "$nft23111_package" ]; then
      nft23111_package_version="$(dpkg-query -W -f='$''{Version}\n' "$nft23111_package" 2>/dev/null | head -n1)"
      nft23111_package_source="$(dpkg-query -W -f='$''{source:Package}\n' "$nft23111_package" 2>/dev/null | head -n1 | sed 's/^src://')"

      for nft23111_candidate in \
        /usr/share/doc/"$nft23111_package"/changelog* \
        /usr/share/doc/"$nft23111_package_source"/changelog*; do
        [ -r "$nft23111_candidate" ] || continue
        case "$nft23111_candidate" in
          *.gz) command -v gzip >/dev/null 2>&1 && gzip -cd "$nft23111_candidate" 2>/dev/null ;;
          *) cat "$nft23111_candidate" 2>/dev/null ;;
        esac
      done | grep -Eiq 'CVE-2026-23111|f41c5d151078|8c760ba4e36c|b9b6573421de|42c574c1504a|1444ff890b46|8b68a45f9722|fix inverted genmask check in nft_map_catchall_activate' \
        && nft23111_fixed_pkg="yes"

      if [ "$nft23111_fixed_pkg" = "yes" ]; then
        nft23111_package_status="fixed"
        return
      fi

      if command -v dpkg >/dev/null 2>&1; then
        if [ "$nft23111_os_id" = "debian" ]; then
          case "$nft23111_codename:$nft23111_package_source" in
            bullseye:linux-6.1)
              dpkg --compare-versions "$nft23111_package_version" ge '6.1.164-1~deb11u1' \
                && nft23111_package_status="fixed" || nft23111_package_status="vulnerable"
              ;;
            bookworm:linux)
              dpkg --compare-versions "$nft23111_package_version" ge '6.1.164-1' \
                && nft23111_package_status="fixed" || nft23111_package_status="vulnerable"
              ;;
            trixie:linux)
              dpkg --compare-versions "$nft23111_package_version" ge '6.12.73-1' \
                && nft23111_package_status="fixed" || nft23111_package_status="vulnerable"
              ;;
            forky:linux|sid:linux)
              dpkg --compare-versions "$nft23111_package_version" ge '6.18.10-1' \
                && nft23111_package_status="fixed" || nft23111_package_status="vulnerable"
              ;;
          esac
        elif [ "$nft23111_os_id" = "ubuntu" ]; then
          case "$nft23111_codename:$nft23111_package_source" in
            focal:linux) nft23111_package_status="not-affected" ;;
            jammy:linux) nft23111_package_status="vulnerable" ;;
            noble:linux)
              dpkg --compare-versions "$nft23111_package_version" ge '6.8.0-107.107' \
                && nft23111_package_status="fixed" || nft23111_package_status="vulnerable"
              ;;
            questing:linux)
              dpkg --compare-versions "$nft23111_package_version" ge '6.17.0-20.20' \
                && nft23111_package_status="fixed" || nft23111_package_status="vulnerable"
              ;;
            resolute:linux) nft23111_package_status="not-affected" ;;
            jammy:linux-hwe-6.8)
              dpkg --compare-versions "$nft23111_package_version" ge '6.8.0-107.107~22.04.1' \
                && nft23111_package_status="fixed" || nft23111_package_status="vulnerable"
              ;;
          esac
        fi
      fi
    fi
  elif command -v rpm >/dev/null 2>&1; then
    nft23111_package="$(rpm -q --whatprovides "kernel-uname-r = $nft23111_kernel" 2>/dev/null | head -n1)"
    case "$nft23111_package" in
      ''|no\ package*) nft23111_package="" ;;
      *)
        nft23111_package_version="$(rpm -q --qf '%{VERSION}-%{RELEASE}\n' "$nft23111_package" 2>/dev/null | head -n1)"
        nft23111_package_source="kernel"
        rpm -q --changelog "$nft23111_package" 2>/dev/null \
          | grep -Eiq 'CVE-2026-23111|f41c5d151078|8c760ba4e36c|b9b6573421de|42c574c1504a|1444ff890b46|8b68a45f9722|fix inverted genmask check in nft_map_catchall_activate' \
          && nft23111_package_status="fixed"
        ;;
    esac
  fi
}

checkNfTablesCVE202623111() {
  [ "$(uname -s 2>/dev/null)" = "Linux" ] || return 0

  nft23111_root="${ROOT_FOLDER:-/}"
  case "$nft23111_root" in
    */) ;;
    *) nft23111_root="${nft23111_root}/" ;;
  esac

  nft23111_kernel="$(cat "${nft23111_root}proc/sys/kernel/osrelease" 2>/dev/null)"
  [ -n "$nft23111_kernel" ] || nft23111_kernel="$(uname -r 2>/dev/null)"
  nft23111_build="$(cat "${nft23111_root}proc/version" 2>/dev/null)"
  [ -n "$nft23111_build" ] || nft23111_build="$(uname -a 2>/dev/null)"
  nft23111_kernel_status="$(nft23111_kernel_status_for_release "$nft23111_kernel")"

  nft23111_os_release="${nft23111_root}etc/os-release"
  nft23111_os_id="$(sed -nE 's/^ID="?([^" ]+)"?$/\1/p' "$nft23111_os_release" 2>/dev/null | head -n1)"
  nft23111_codename="$(sed -nE 's/^VERSION_CODENAME="?([^" ]+)"?$/\1/p' "$nft23111_os_release" 2>/dev/null | head -n1)"
  nft23111_distro="$(sed -nE 's/^PRETTY_NAME="?([^"].*)"?$/\1/p' "$nft23111_os_release" 2>/dev/null | head -n1 | sed 's/"$//')"
  nft23111_collect_package_status

  print_3title "nftables catchall verdict-map UAF (CVE-2026-23111)" "T1068"
  print_info "https://www.cve.org/CVERecord?id=CVE-2026-23111"
  print_info "https://git.kernel.org/linus/f41c5d151078c5348271ffaf8e7410d96f2d82f8"
  print_info "https://blog.exodusintel.com/2026/06/08/off-by-exploiting-a-use-after-free-in-the-linux-kernel/"
  echo "Kernel release: $nft23111_kernel"
  echo "Kernel build: ${nft23111_build:-unknown}"
  echo "Distribution: ${nft23111_distro:-unknown}${nft23111_codename:+ ($nft23111_codename)}"
  if [ -n "$nft23111_package" ]; then
    echo "Running kernel package: $nft23111_package${nft23111_package_source:+ (source: $nft23111_package_source)} ${nft23111_package_version:-unknown}"
  else
    echo "Running kernel package: not identified"
  fi

  case "$nft23111_package_status:$nft23111_kernel_status" in
    fixed:*)
      echo "Vendor package evidence: FIXED for CVE-2026-23111" | sed -${E} "s,.*,${SED_GREEN},"
      return 0
      ;;
    not-affected:*)
      echo "Vendor status: this kernel package line is NOT AFFECTED by CVE-2026-23111" | sed -${E} "s,.*,${SED_GREEN},"
      return 0
      ;;
    *:fixed)
      echo "Upstream status: kernel $nft23111_kernel contains the fix by stable-version comparison" | sed -${E} "s,.*,${SED_GREEN},"
      return 0
      ;;
    *:unaffected)
      echo "Upstream status: kernel $nft23111_kernel is outside the introduced ranges" | sed -${E} "s,.*,${SED_GREEN},"
      return 0
      ;;
    vulnerable:affected)
      echo "VULNERABLE to CVE-2026-23111 according to vendor package data; practical exploitability depends on the prerequisites below" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      ;;
    *:affected)
      echo "POTENTIALLY VULNERABLE to CVE-2026-23111 by upstream range; vendor backport status is unavailable" | sed -${E} "s,.*,${SED_YELLOW},"
      ;;
    *)
      echo "Kernel vulnerability status is unknown; prerequisite exposure follows" | sed -${E} "s,.*,${SED_YELLOW},"
      ;;
  esac

  nft23111_cfg=""
  for nft23111_candidate in \
    "${nft23111_root}proc/config.gz" \
    "${nft23111_root}boot/config-${nft23111_kernel}" \
    "${nft23111_root}lib/modules/${nft23111_kernel}/config" \
    "${nft23111_root}lib/modules/${nft23111_kernel}/build/.config" \
    "${nft23111_root}usr/lib/modules/${nft23111_kernel}/build/.config"; do
    [ -r "$nft23111_candidate" ] && { nft23111_cfg="$nft23111_candidate"; break; }
  done

  nft23111_cfg_data=""
  if [ -n "$nft23111_cfg" ]; then
    case "$nft23111_cfg" in
      *.gz)
        if command -v gzip >/dev/null 2>&1; then
          nft23111_cfg_data="$(gzip -cd "$nft23111_cfg" 2>/dev/null | grep -E '^((CONFIG_NF_TABLES|CONFIG_NFT_SET_PIPAPO|CONFIG_USER_NS|CONFIG_NET_NS)=|# (CONFIG_NF_TABLES|CONFIG_NFT_SET_PIPAPO|CONFIG_USER_NS|CONFIG_NET_NS) is not set)')"
        fi
        ;;
      *)
        nft23111_cfg_data="$(grep -E '^((CONFIG_NF_TABLES|CONFIG_NFT_SET_PIPAPO|CONFIG_USER_NS|CONFIG_NET_NS)=|# (CONFIG_NF_TABLES|CONFIG_NFT_SET_PIPAPO|CONFIG_USER_NS|CONFIG_NET_NS) is not set)' "$nft23111_cfg" 2>/dev/null)"
        ;;
    esac
  fi

  nft23111_modules_dep="${nft23111_root}lib/modules/${nft23111_kernel}/modules.dep"
  nft23111_modules_builtin="${nft23111_root}lib/modules/${nft23111_kernel}/modules.builtin"
  nft23111_modules_disabled="$(cat "${nft23111_root}proc/sys/kernel/modules_disabled" 2>/dev/null)"
  nft23111_nf_tables="$(nft23111_module_state nf_tables CONFIG_NF_TABLES)"
  nft23111_pipapo="$(nft23111_module_state nft_set_pipapo CONFIG_NFT_SET_PIPAPO)"

  echo "nftables kernel support: nf_tables=$nft23111_nf_tables, nft_set_pipapo=$nft23111_pipapo" \
    | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
  case "$nft23111_nf_tables:$nft23111_pipapo" in
    *disabled*|*unavailable*)
      echo "Required nftables/pipapo kernel attack surface is unavailable" | sed -${E} "s,.*,${SED_GREEN},"
      return 0
      ;;
  esac

  nft23111_userns_clone="$(cat "${nft23111_root}proc/sys/kernel/unprivileged_userns_clone" 2>/dev/null)"
  nft23111_userns_max="$(cat "${nft23111_root}proc/sys/user/max_user_namespaces" 2>/dev/null)"
  nft23111_netns_max="$(cat "${nft23111_root}proc/sys/user/max_net_namespaces" 2>/dev/null)"
  nft23111_userns="unknown"
  nft23111_netns="unknown"

  if printf '%s\n' "$nft23111_cfg_data" | grep -Eq '^# CONFIG_USER_NS is not set$|^CONFIG_USER_NS=n$' \
    || [ "$nft23111_userns_clone" = "0" ] || [ "$nft23111_userns_max" = "0" ]; then
    nft23111_userns="blocked"
  elif printf '%s\n' "$nft23111_cfg_data" | grep -q '^CONFIG_USER_NS=y$' \
    || [ "$nft23111_userns_clone" = "1" ] \
    || { [ -n "$nft23111_userns_max" ] && [ "$nft23111_userns_max" != "0" ]; }; then
    nft23111_userns="enabled"
  fi

  if printf '%s\n' "$nft23111_cfg_data" | grep -Eq '^# CONFIG_NET_NS is not set$|^CONFIG_NET_NS=n$' \
    || [ "$nft23111_netns_max" = "0" ]; then
    nft23111_netns="blocked"
  elif printf '%s\n' "$nft23111_cfg_data" | grep -q '^CONFIG_NET_NS=y$' \
    || { [ -n "$nft23111_netns_max" ] && [ "$nft23111_netns_max" != "0" ]; }; then
    nft23111_netns="enabled"
  fi

  echo "Namespace prerequisites: user_ns=$nft23111_userns (unprivileged_userns_clone=${nft23111_userns_clone:-unset}, max=${nft23111_userns_max:-unknown}); net_ns=$nft23111_netns (max=${nft23111_netns_max:-unknown})"

  nft23111_status_file="${nft23111_root}proc/self/status"
  nft23111_uid="$(id -u 2>/dev/null)"
  nft23111_caps="$(sed -n 's/^CapEff:[[:space:]]*//p' "$nft23111_status_file" 2>/dev/null | head -n1)"
  nft23111_caps_decoded=""
  if [ "$nft23111_root" = "/" ] && [ -n "$nft23111_caps" ] && command -v capsh >/dev/null 2>&1; then
    nft23111_caps_decoded="$(capsh --decode="0x$nft23111_caps" 2>/dev/null | sed 's/^[^=]*=//')"
  fi
  echo "Current context: uid=${nft23111_uid:-unknown}, CapEff=${nft23111_caps:-unknown}${nft23111_caps_decoded:+ ($nft23111_caps_decoded)}"

  nft23111_apparmor="$(cat "${nft23111_root}proc/self/attr/current" 2>/dev/null)"
  nft23111_apparmor_restrict="$(cat "${nft23111_root}proc/sys/kernel/apparmor_restrict_unprivileged_userns" 2>/dev/null)"
  nft23111_selinux="unknown"
  if [ -r "${nft23111_root}sys/fs/selinux/enforce" ]; then
    nft23111_rule="$(cat "${nft23111_root}sys/fs/selinux/enforce" 2>/dev/null)"
    [ "$nft23111_rule" = "1" ] && nft23111_selinux="enforcing"
    [ "$nft23111_rule" = "0" ] && nft23111_selinux="permissive"
  elif [ ! -d "${nft23111_root}sys/fs/selinux" ]; then
    nft23111_selinux="disabled"
  fi
  nft23111_seccomp="$(sed -n 's/^Seccomp:[[:space:]]*//p' "$nft23111_status_file" 2>/dev/null | head -n1)"
  echo "Policy context: AppArmor=${nft23111_apparmor:-unconfined/unknown}, AppArmor unprivileged-userns restriction=${nft23111_apparmor_restrict:-unset}, SELinux=$nft23111_selinux, Seccomp=${nft23111_seccomp:-unknown}"

  case "$nft23111_userns:$nft23111_netns" in
    *blocked*)
      echo "Unprivileged namespace creation appears blocked, reducing exploitability (privileged capabilities or policy bypasses may change this)" | sed -${E} "s,.*,${SED_YELLOW},"
      ;;
    enabled:enabled)
      echo "Required user and network namespace primitives appear available" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      ;;
    *)
      echo "Namespace reachability could not be confirmed passively" | sed -${E} "s,.*,${SED_YELLOW},"
      ;;
  esac

  echo "Remediation: install a vendor kernel containing upstream commit f41c5d151078c5348271ffaf8e7410d96f2d82f8; restrict unnecessary unprivileged user/network namespaces as defense in depth."
}
