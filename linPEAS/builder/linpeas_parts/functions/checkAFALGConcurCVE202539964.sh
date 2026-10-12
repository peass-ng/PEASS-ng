# Title: Function - checkAFALGConcurCVE202539964
# ID: checkAFALGConcurCVE202539964
# Author: HT Bot
# Last Update: 12-10-2026
# Description: Passively assess AF_ALG skcipher concurrent-writer exposure (CVE-2025-39964), without opening sockets or loading modules.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: lp_version_lt, print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_GREEN, $SED_LIGHT_CYAN, $SED_RED_YELLOW, $SED_YELLOW
# Initial Functions:
# Generated Global Variables: $af39964_root, $af39964_release, $af39964_base, $af39964_build, $af39964_osrelease, $af39964_os_id, $af39964_os_version, $af39964_status, $af39964_fixed, $af39964_config, $af39964_cfg, $af39964_api, $af39964_skcipher, $af39964_module, $af39964_rule, $af39964_exposure, $af39964_container, $af39964_package, $af39964_package_version, $af39964_vendor_fixed, $af39964_vendor_status, $af39964_reason
# Fat linpeas: 0
# Small linpeas: 1


af39964_upstream_status() {
  af39964_base="${af39964_release%%-*}"
  if ! printf '%s\n' "$af39964_base" | grep -Eq '^[0-9]+\.[0-9]+(\.[0-9]+)?$'; then
    af39964_status="unknown"
    return
  fi
  if lp_version_lt "$af39964_base" 2.6.38; then
    af39964_status="predates"
    return
  fi
  # The fix first reached mainline 6.17-rc7. Do not classify earlier RCs as fixed.
  case "$af39964_release" in
    6.17*rc[1-6]*) af39964_status="affected"; return ;;
  esac
  if ! lp_version_lt "$af39964_base" 6.17; then
    af39964_status="fixed"
    return
  fi
  af39964_fixed=""
  case "$af39964_base" in
    5.10.*) af39964_fixed=5.10.245 ;;
    5.15.*) af39964_fixed=5.15.194 ;;
    6.1.*) af39964_fixed=6.1.154 ;;
    6.6.*) af39964_fixed=6.6.108 ;;
    6.12.*) af39964_fixed=6.12.49 ;;
    6.16.*) af39964_fixed=6.16.9 ;;
  esac
  if [ -n "$af39964_fixed" ] && ! lp_version_lt "$af39964_base" "$af39964_fixed"; then
    af39964_status="fixed"
  else
    af39964_status="affected"
  fi
}

checkAFALGConcurCVE202539964() {
  [ "$(uname -s 2>/dev/null)" = Linux ] || return 0
  af39964_root="${ROOT_FOLDER:-/}"
  [ "$af39964_root" = / ] || return 0 # uname describes the host, not an offline root.
  af39964_release="$(uname -r 2>/dev/null)"
  af39964_build="$(uname -v 2>/dev/null)"
  af39964_osrelease="$(cat /proc/sys/kernel/osrelease 2>/dev/null)"
  af39964_os_id="$(sed -n 's/^ID=//p' /etc/os-release 2>/dev/null | head -n1 | tr -d '"')"
  af39964_os_version="$(sed -n 's/^VERSION_ID=//p' /etc/os-release 2>/dev/null | head -n1 | tr -d '"')"
  af39964_upstream_status

  af39964_container="no evidence"
  if [ -e /.dockerenv ] || [ -e /run/.containerenv ] \
    || grep -Eq '(docker|kubepods|containerd|libpod|lxc)' /proc/self/cgroup /proc/1/cgroup 2>/dev/null; then
    af39964_container="detected (the host kernel is shared)"
  fi

  af39964_package=""
  af39964_package_version=""
  af39964_vendor_status="unknown"
  # Container image packages cannot establish the host kernel's patch state.
  if [ "$af39964_container" = "no evidence" ] && command -v dpkg-query >/dev/null 2>&1; then
    af39964_package="$(dpkg-query -S "/boot/vmlinuz-$af39964_release" 2>/dev/null | sed 's/: .*//' | head -n1)"
    [ -n "$af39964_package" ] || af39964_package="$(dpkg-query -S "/lib/modules/$af39964_release" 2>/dev/null | sed 's/: .*//' | head -n1)"
    case "$af39964_package" in
      linux-image-*) af39964_package_version="$(dpkg-query -W -f='$''{Version}' "$af39964_package" 2>/dev/null)" ;;
      *) af39964_package="" ;;
    esac
    af39964_vendor_fixed=""
    case "$af39964_os_id:$af39964_release" in
      debian:5.10.*) af39964_vendor_fixed=5.10.247-1 ;;
      debian:6.1.*) af39964_vendor_fixed=6.1.158-1 ;;
      debian:6.12.*) af39964_vendor_fixed=6.12.57-1 ;;
      debian:6.16.*) af39964_vendor_fixed=6.16.9-1 ;;
      ubuntu:*-generic)
        case "$af39964_release" in
          4.4.*)
            af39964_vendor_fixed=4.4.0-276.310
            [ "$af39964_os_version" = 14.04 ] && af39964_vendor_fixed=4.4.0-276.310~14.04.1 ;;
          4.15.*)
            af39964_vendor_fixed=4.15.0-245.257
            [ "$af39964_os_version" = 16.04 ] && af39964_vendor_fixed=4.15.0-245.257~16.04.1 ;;
          5.4.*)
            af39964_vendor_fixed=5.4.0-224.244
            [ "$af39964_os_version" = 18.04 ] && af39964_vendor_fixed=5.4.0-224.244~18.04.1 ;;
          5.15.*)
            af39964_vendor_fixed=5.15.0-164.174
            [ "$af39964_os_version" = 20.04 ] && af39964_vendor_fixed=5.15.0-164.174~20.04.1 ;;
          6.8.*)
            af39964_vendor_fixed=6.8.0-90.91
            [ "$af39964_os_version" = 22.04 ] && af39964_vendor_fixed=6.8.0-90.91~22.04.1 ;;
          6.14.*)
            [ "$af39964_os_version" = 25.04 ] && af39964_vendor_fixed=6.14.0-37.37 ;;
        esac ;;
    esac
    if [ -n "$af39964_vendor_fixed" ] && [ -n "$af39964_package_version" ] && command -v dpkg >/dev/null 2>&1; then
      if dpkg --compare-versions "$af39964_package_version" ge "$af39964_vendor_fixed"; then
        af39964_vendor_status="fixed (vendor package >= $af39964_vendor_fixed)"
      else
        af39964_vendor_status="below published vendor fix $af39964_vendor_fixed"
      fi
    fi
  fi

  af39964_config=""
  for af39964_cfg in /proc/config.gz "/boot/config-$af39964_release"; do
    [ -r "$af39964_cfg" ] && { af39964_config="$af39964_cfg"; break; }
  done
  af39964_api="unknown"
  af39964_skcipher="unknown"
  if [ -n "$af39964_config" ]; then
    case "$af39964_config" in
      *.gz) command -v gzip >/dev/null 2>&1 && af39964_cfg="$(gzip -dc "$af39964_config" 2>/dev/null | grep -E '^(CONFIG_CRYPTO_USER_API|CONFIG_CRYPTO_USER_API_SKCIPHER)=|^# CONFIG_CRYPTO_USER_API(_SKCIPHER)? is not set$')" ;;
      *) af39964_cfg="$(grep -E '^(CONFIG_CRYPTO_USER_API|CONFIG_CRYPTO_USER_API_SKCIPHER)=|^# CONFIG_CRYPTO_USER_API(_SKCIPHER)? is not set$' "$af39964_config" 2>/dev/null)" ;;
    esac
    printf '%s\n' "$af39964_cfg" | grep -Eq '^CONFIG_CRYPTO_USER_API=[ym]$' && af39964_api="enabled"
    printf '%s\n' "$af39964_cfg" | grep -Eq '^CONFIG_CRYPTO_USER_API_SKCIPHER=[ym]$' && af39964_skcipher="enabled"
    printf '%s\n' "$af39964_cfg" | grep -Eq '^# CONFIG_CRYPTO_USER_API is not set$|^CONFIG_CRYPTO_USER_API=n$' && af39964_api="disabled"
    printf '%s\n' "$af39964_cfg" | grep -Eq '^# CONFIG_CRYPTO_USER_API_SKCIPHER is not set$|^CONFIG_CRYPTO_USER_API_SKCIPHER=n$' && af39964_skcipher="disabled"
  fi
  af39964_module="unknown"
  if [ -d /sys/module/algif_skcipher ] || grep -q '^algif_skcipher[[:space:]]' /proc/modules 2>/dev/null; then
    af39964_module="loaded/built-in"
  elif grep -Eq '(^|/)algif_skcipher\.ko(\.(gz|xz|zst))?:' "/lib/modules/$af39964_release/modules.dep" 2>/dev/null; then
    af39964_module="installed (autoload possible)"
  elif grep -Eq '(^|/)algif_skcipher\.ko(\.(gz|xz|zst))?$' "/lib/modules/$af39964_release/modules.builtin" 2>/dev/null; then
    af39964_module="built-in"
  fi
  if [ "$af39964_module" = "installed (autoload possible)" ]; then
    if [ "$(cat /proc/sys/kernel/modules_disabled 2>/dev/null)" = 1 ]; then
      af39964_module="installed, but loading disabled"
    else
      for af39964_rule in /etc/modprobe.d/*.conf /run/modprobe.d/*.conf /usr/lib/modprobe.d/*.conf /lib/modprobe.d/*.conf; do
        [ -r "$af39964_rule" ] || continue
        if grep -Eq '^[[:space:]]*install[[:space:]]+algif_skcipher[[:space:]]+(/usr)?/bin/(true|false)([[:space:]#]|$)' "$af39964_rule" 2>/dev/null; then
          af39964_module="installed, but autoload blocked"
          break
        fi
      done
    fi
  fi
  af39964_exposure="unknown"
  if [ "$af39964_api" = disabled ] || [ "$af39964_skcipher" = disabled ]; then
    af39964_exposure="disabled by kernel configuration"
  elif [ "$af39964_module" = "installed, but loading disabled" ] || [ "$af39964_module" = "installed, but autoload blocked" ]; then
    af39964_exposure="not currently reachable (module loading blocked)"
  elif [ "$af39964_module" != unknown ]; then
    af39964_exposure="AF_ALG skcipher present or installed"
  elif [ "$af39964_api" = enabled ] && [ "$af39964_skcipher" = enabled ]; then
    af39964_exposure="enabled by kernel configuration"
  fi

  print_3title "AF_ALG concurrent-writer LPE (CVE-2025-39964)" "T1068"
  print_info "https://www.cve.org/CVERecord?id=CVE-2025-39964"
  printf 'Kernel: %s | build: %s | /proc osrelease: %s\n' "$af39964_release" "$af39964_build" "$af39964_osrelease" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
  printf 'Distribution: %s | running-kernel package: %s %s | vendor evidence: %s\n' "${af39964_os_id:-unknown}" "${af39964_package:-unknown}" "$af39964_package_version" "$af39964_vendor_status"
  printf 'AF_ALG: user API %s, skcipher %s, algif_skcipher %s; exposure: %s\n' "$af39964_api" "$af39964_skcipher" "$af39964_module" "$af39964_exposure"
  printf 'Container: %s\n' "$af39964_container"

  if [ "$af39964_exposure" = "disabled by kernel configuration" ] || [ "$af39964_exposure" = "not currently reachable (module loading blocked)" ]; then
    af39964_reason="NOT EXPOSED from this context: $af39964_exposure"
    echo "$af39964_reason" | sed -${E} "s,.*,${SED_GREEN},"
  elif [ "$af39964_vendor_status" != unknown ] && [ "${af39964_vendor_status%% *}" = fixed ]; then
    af39964_reason="FIXED by published vendor package threshold; no race was attempted"
    echo "$af39964_reason" | sed -${E} "s,.*,${SED_GREEN},"
  elif [ "$af39964_status" = fixed ]; then
    af39964_reason="At or beyond an upstream fixed release; vendor patch status still requires confirmation"
    echo "$af39964_reason" | sed -${E} "s,.*,${SED_GREEN},"
  elif [ "$af39964_status" = predates ]; then
    echo "NOT VULNERABLE by upstream introduction: this kernel predates Linux 2.6.38" | sed -${E} "s,.*,${SED_GREEN},"
  elif [ "$af39964_status" = affected ] && [ "$af39964_exposure" != unknown ]; then
    echo "POTENTIALLY VULNERABLE: affected upstream version and AF_ALG skcipher exposure; vendor backports may fix it" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  else
    echo "UNKNOWN: kernel patch or AF_ALG skcipher reachability cannot be established non-invasively" | sed -${E} "s,.*,${SED_YELLOW},"
  fi
  [ "$af39964_container" = "no evidence" ] || echo "Container-to-host impact is possible if the host kernel is vulnerable and AF_ALG is reachable."
  echo "This read-only check never confirms vulnerability; verify the host kernel's vendor advisory or patch commit 1b34cbbf4f01."
}
