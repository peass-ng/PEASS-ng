# Title: Function - checkGameOverLay515
# ID: checkGameOverLay515
# Author: HT Bot
# Last Update: 17-11-2026
# Description: Passively identify Ubuntu/Trisquel 5.15 OverlayFS GameOver(lay) candidates using the running kernel package.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $gol515_release, $gol515_id, $gol515_distro_version, $gol515_package, $gol515_package_version, $gol515_package_state, $gol515_userns, $gol515_status
# Fat linpeas: 0
# Small linpeas: 1

checkGameOverLay515() {
  [ "$(uname -s 2>/dev/null)" = Linux ] || return 0
  [ "${ROOT_FOLDER:-/}" = / ] || return 0 # uname describes the live kernel.

  gol515_release="$(uname -r 2>/dev/null)"
  printf '%s\n' "$gol515_release" | grep -Eq '^5[.]15[.]0-[0-9]+-(generic|lowlatency)(-64k|-lpae)?$' || return 0
  [ -r /etc/os-release ] || return 0
  gol515_id="$(sed -n 's/^ID=//p' /etc/os-release | head -n1 | tr -d '"')"
  gol515_distro_version="$(sed -n 's/^VERSION_ID=//p' /etc/os-release | head -n1 | tr -d '"')"
  case "$gol515_id:$gol515_distro_version" in
    ubuntu:22.04|trisquel:11|trisquel:11.0) ;;
    *) return 0 ;;
  esac

  # A container's dpkg database does not establish the host kernel's patch state.
  [ -e /.dockerenv ] && return 0
  [ -e /run/.containerenv ] && return 0
  grep -Eq '(docker|kubepods|containerd|libpod|lxc)' /proc/self/cgroup /proc/1/cgroup 2>/dev/null && return 0
  command -v dpkg-query >/dev/null 2>&1 || return 0
  command -v dpkg >/dev/null 2>&1 || return 0
  gol515_package_version=""
  for gol515_package in "linux-image-$gol515_release" "linux-image-unsigned-$gol515_release"; do
    gol515_package_state="$(dpkg-query -W -f='$''{db:Status-Abbrev}' "$gol515_package" 2>/dev/null)"
    case "$gol515_package_state" in ii*) ;; *) continue ;; esac
    gol515_package_version="$(dpkg-query -W -f='$''{Version}' "$gol515_package" 2>/dev/null)"
    [ -n "$gol515_package_version" ] && break
  done
  [ -n "$gol515_package_version" ] || return 0

  # USN-8255-1 fixes Ubuntu 22.04's generic/lowlatency linux at 5.15.0-177.187.
  # Trisquel rebuilds Ubuntu kernels: this is a reference floor, not a
  # Trisquel-specific fixed-version claim. Earlier vendor backports may exist.
  dpkg --compare-versions "$gol515_package_version" lt 5.15.0-177.187 || return 0
  gol515_userns="$(cat /proc/sys/kernel/unprivileged_userns_clone 2>/dev/null)"
  [ "$gol515_userns" = 1 ] || return 0

  gol515_status="Candidate: running kernel package predates Ubuntu's GameOver(lay) fix; verify vendor patches and Livepatch before exploitation."
  [ "$gol515_id" = trisquel ] && gol515_status="Candidate: Trisquel linux-libre build is below Ubuntu's reference fix; verify Trisquel's own patch history (Ubuntu's version is not its fixed version)."
  print_3title "GameOver(lay) OverlayFS LPE (CVE-2023-2640, CVE-2023-32629)" "T1068"
  print_info "https://ubuntu.com/security/notices/USN-8255-1"
  printf 'Distribution: %s %s | kernel: %s | build: %s\n' "$gol515_id" "$gol515_distro_version" "$gol515_release" "$(uname -v 2>/dev/null)"
  printf 'Kernel version record: %s\n' "$(head -n1 /proc/version 2>/dev/null)"
  printf 'Running-kernel package: %s %s | unprivileged user namespaces: %s\n' "$gol515_package" "$gol515_package_version" "$gol515_userns"
  printf '%s\n' "$gol515_status" | sed -${E} "s,.*,${SED_RED_YELLOW},"
}
