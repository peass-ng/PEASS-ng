# Title: Function - checkUMPConversionCVE202537891
# ID: checkUMPConversionCVE202537891
# Author: HT Bot
# Last Update: 27-10-2026
# Description: Passively assess the ALSA UMP SysEx conversion overflow (CVE-2025-37891) using the running kernel, selected vendor fixes, and MIDI configuration.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: lp_version_lt, print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_GREEN, $SED_RED_YELLOW, $SED_YELLOW
# Initial Functions:
# Generated Global Variables: $ump37891_release, $ump37891_base, $ump37891_status, $ump37891_os, $ump37891_os_version, $ump37891_package, $ump37891_package_version, $ump37891_fixed_package, $ump37891_vendor, $ump37891_config, $ump37891_lines, $ump37891_ump, $ump37891_legacy, $ump37891_usb, $ump37891_node, $ump37891_nodes
# Fat linpeas: 0
# Small linpeas: 1

checkUMPConversionCVE202537891() {
  [ "$(uname -s 2>/dev/null)" = Linux ] || return 0
  [ "${ROOT_FOLDER:-/}" = / ] || return 0 # uname and /dev describe the live host only.

  ump37891_release="$(uname -r 2>/dev/null)"
  ump37891_base="${ump37891_release%%-*}"
  ump37891_status="candidate"
  # Linux CVE record: stable fixes 6.6.90, 6.12.28, 6.14.6; mainline 6.15-rc5.
  # https://kernel.googlesource.com/pub/scm/linux/security/vulns/+/refs/heads/master/cve/published/2025/CVE-2025-37891.json
  if ! printf '%s\n' "$ump37891_base" | grep -Eq '^[0-9]+\.[0-9]+(\.[0-9]+)?$'; then
    ump37891_status="unknown"
  elif lp_version_lt "$ump37891_base" 6.5; then
    ump37891_status="predates"
  else
    case "$ump37891_release" in
      6.15*rc[1-4]*) ump37891_status="candidate" ;;
      *)
        if ! lp_version_lt "$ump37891_base" 6.15; then
          ump37891_status="fixed"
        else
          case "$ump37891_base" in
            6.6.*) lp_version_lt "$ump37891_base" 6.6.90 || ump37891_status="fixed" ;;
            6.12.*) lp_version_lt "$ump37891_base" 6.12.28 || ump37891_status="fixed" ;;
            6.14.*) lp_version_lt "$ump37891_base" 6.14.6 || ump37891_status="fixed" ;;
          esac
        fi
        ;;
    esac
  fi

  print_3title "ALSA UMP SysEx conversion overflow (CVE-2025-37891)" "T1068"
  print_info "https://git.kernel.org/stable/c/ce4f77bef276e7d2eb7ab03a5d08bcbaa40710ec"
  case "$ump37891_status" in
    predates)
      echo "Kernel $ump37891_release predates the vulnerable UMP conversion code (introduced in 6.5)" | sed -${E} "s,.*,${SED_GREEN},"
      return 0 ;;
    fixed)
      echo "Kernel $ump37891_release includes the upstream stable/mainline fix by version comparison" | sed -${E} "s,.*,${SED_GREEN},"
      return 0 ;;
  esac

  ump37891_os="$(sed -n 's/^ID=//p' /etc/os-release 2>/dev/null | head -n1 | tr -d '"')"
  ump37891_os_version="$(sed -n 's/^VERSION_ID=//p' /etc/os-release 2>/dev/null | head -n1 | tr -d '"')"
  ump37891_vendor=""
  ump37891_fixed_package=""
  # A container's installed packages cannot establish its host kernel patch state.
  if [ "$ump37891_status" = candidate ] && [ ! -e /.dockerenv ] && [ ! -e /run/.containerenv ] \
    && ! grep -Eq '(docker|kubepods|containerd|libpod|lxc)' /proc/self/cgroup /proc/1/cgroup 2>/dev/null; then
    # Published fixed source-package versions; do not extrapolate to other flavors.
    # https://ubuntu.com/security/CVE-2025-37891
    # https://security-tracker.debian.org/tracker/CVE-2025-37891
    case "$ump37891_os:$ump37891_os_version:$ump37891_release" in
      ubuntu:24.04:6.8.*-generic) ump37891_fixed_package="6.8.0-100.100" ;;
      ubuntu:22.04:6.8.*-generic) ump37891_fixed_package="6.8.0-100.100~22.04.1" ;;
      ubuntu:25.04:6.14.*-generic) ump37891_fixed_package="6.14.0-24.24" ;;
      debian:*:6.12.*) ump37891_fixed_package="6.12.29-1" ;;
    esac
    if [ -n "$ump37891_fixed_package" ] && command -v dpkg-query >/dev/null 2>&1 && command -v dpkg >/dev/null 2>&1; then
      ump37891_package="$(dpkg-query -S "/boot/vmlinuz-$ump37891_release" 2>/dev/null | sed 's/: .*//' | head -n1)"
      case "$ump37891_package" in
        linux-image-*)
          ump37891_package_version="$(dpkg-query -W -f='$''{Version}' "$ump37891_package" 2>/dev/null)"
          if [ -n "$ump37891_package_version" ]; then
            if dpkg --compare-versions "$ump37891_package_version" ge "$ump37891_fixed_package"; then
              ump37891_vendor="fixed: $ump37891_package $ump37891_package_version (vendor threshold $ump37891_fixed_package)"
              ump37891_status="fixed-vendor"
            else
              ump37891_vendor="below published fix: $ump37891_package $ump37891_package_version (threshold $ump37891_fixed_package)"
            fi
          fi ;;
      esac
    fi
  fi

  if [ "$ump37891_status" = fixed-vendor ]; then
    echo "Kernel $ump37891_release: $ump37891_vendor" | sed -${E} "s,.*,${SED_GREEN},"
    return 0
  fi
  if [ "$ump37891_status" = unknown ]; then
    echo "Kernel $ump37891_release: upstream fix status could not be determined" | sed -${E} "s,.*,${SED_YELLOW},"
  else
    echo "Kernel $ump37891_release: possible CVE-2025-37891 candidate; vendor backports may change the result" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  fi
  [ -n "$ump37891_vendor" ] && echo "$ump37891_vendor"

  ump37891_config=""
  for ump37891_node in /proc/config.gz "/boot/config-$ump37891_release"; do
    [ -r "$ump37891_node" ] && { ump37891_config="$ump37891_node"; break; }
  done
  ump37891_lines=""
  if [ -n "$ump37891_config" ]; then
    case "$ump37891_config" in
      *.gz) command -v gzip >/dev/null 2>&1 && ump37891_lines="$(gzip -dc "$ump37891_config" 2>/dev/null | grep -E '^(CONFIG_SND_UMP|CONFIG_SND_UMP_LEGACY_RAWMIDI|CONFIG_SND_USB_AUDIO_MIDI_V2)=|^# CONFIG_SND_(UMP|UMP_LEGACY_RAWMIDI|USB_AUDIO_MIDI_V2) is not set$')" ;;
      *) ump37891_lines="$(grep -E '^(CONFIG_SND_UMP|CONFIG_SND_UMP_LEGACY_RAWMIDI|CONFIG_SND_USB_AUDIO_MIDI_V2)=|^# CONFIG_SND_(UMP|UMP_LEGACY_RAWMIDI|USB_AUDIO_MIDI_V2) is not set$' "$ump37891_config" 2>/dev/null)" ;;
    esac
  fi
  ump37891_ump="unknown"
  ump37891_legacy="unknown"
  ump37891_usb="unknown"
  printf '%s\n' "$ump37891_lines" | grep -Eq '^CONFIG_SND_UMP=[ym]$' && ump37891_ump="enabled"
  printf '%s\n' "$ump37891_lines" | grep -Eq '^# CONFIG_SND_UMP is not set$|^CONFIG_SND_UMP=n$' && ump37891_ump="disabled"
  printf '%s\n' "$ump37891_lines" | grep -Eq '^CONFIG_SND_UMP_LEGACY_RAWMIDI=[ym]$' && ump37891_legacy="enabled"
  printf '%s\n' "$ump37891_lines" | grep -Eq '^# CONFIG_SND_UMP_LEGACY_RAWMIDI is not set$|^CONFIG_SND_UMP_LEGACY_RAWMIDI=n$' && ump37891_legacy="disabled"
  printf '%s\n' "$ump37891_lines" | grep -Eq '^CONFIG_SND_USB_AUDIO_MIDI_V2=[ym]$' && ump37891_usb="enabled"
  printf '%s\n' "$ump37891_lines" | grep -Eq '^# CONFIG_SND_USB_AUDIO_MIDI_V2 is not set$|^CONFIG_SND_USB_AUDIO_MIDI_V2=n$' && ump37891_usb="disabled"
  echo "UMP config: $ump37891_ump; legacy rawmidi: $ump37891_legacy; USB MIDI2: $ump37891_usb"

  ump37891_nodes="none visible"
  for ump37891_node in /dev/snd/midiC*D*; do
    [ -c "$ump37891_node" ] || continue
    ump37891_nodes="present (not necessarily UMP)"
    [ -w "$ump37891_node" ] && { ump37891_nodes="writable by current user (not necessarily UMP)"; break; }
  done
  echo "Rawmidi device nodes: $ump37891_nodes"
  echo "A UMP endpoint and MIDI access are needed; a USB MIDI2 device can supply an endpoint. Check vendor backports/Livepatch and upgrade to a vendor-fixed kernel."
}
