# Title: Functions - checkXFSTangoCVE202680530
# ID: checkXFSTangoCVE202680530
# Author: Chack Agent
# Last Update: 07-09-2026
# Description: Passively identify mounted XFS filesystems exposed to the XFSTango reflink local privilege-escalation condition (CVE-2026-80530).
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $E, $SED_GREEN, $SED_LIGHT_CYAN, $SED_RED_YELLOW, $SED_YELLOW, $TIMEOUT
# Initial Functions:
# Generated Global Variables: $xft80530_distro_id, $xft80530_distro_version, $xft80530_info, $xft80530_kernel, $xft80530_mount_file, $xft80530_mountpoint, $xft80530_mountpoint_escaped, $xft80530_mounts, $xft80530_os_release, $xft80530_rc, $xft80530_reflink_enabled, $xft80530_reflink_status, $xft80530_reflink_unknown, $xft80530_status, $xft80530_version
# Fat linpeas: 0
# Small linpeas: 1


xft80530_version_lt() {
  [ -n "$1" ] && [ -n "$2" ] || return 1
  awk -v xft80530_a="$1" -v xft80530_b="$2" 'BEGIN {
    xft80530_na = split(xft80530_a, xft80530_av, ".")
    xft80530_nb = split(xft80530_b, xft80530_bv, ".")
    xft80530_n = xft80530_na > xft80530_nb ? xft80530_na : xft80530_nb
    for (xft80530_i = 1; xft80530_i <= xft80530_n; xft80530_i++) {
      xft80530_ai = xft80530_av[xft80530_i] + 0
      xft80530_bi = xft80530_bv[xft80530_i] + 0
      if (xft80530_ai < xft80530_bi) exit 0
      if (xft80530_ai > xft80530_bi) exit 1
    }
    exit 1
  }'
}

xft80530_kernel_status() {
  case "$1" in
    7.2.0-rc*)
      xft80530_rc="$(printf '%s' "$1" | sed -nE 's/^7\.2\.0-rc([0-9]+).*/\1/p')"
      if [ -n "$xft80530_rc" ] && [ "$xft80530_rc" -ge 7 ]; then
        echo "fixed"
      else
        echo "affected"
      fi
      return
      ;;
  esac
  xft80530_version="$(printf '%s' "$1" | sed -nE 's/^([0-9]+\.[0-9]+\.[0-9]+).*/\1/p')"
  if [ -z "$xft80530_version" ]; then
    echo "unknown"
  elif xft80530_version_lt "$xft80530_version" "6.10.0"; then
    echo "predates"
  elif ! xft80530_version_lt "$xft80530_version" "7.2.0"; then
    echo "fixed"
  else
    case "$xft80530_version" in
      6.12.*)
        if ! xft80530_version_lt "$xft80530_version" "6.12.105"; then
          echo "fixed"
        else
          echo "affected"
        fi
        ;;
      6.18.*)
        if ! xft80530_version_lt "$xft80530_version" "6.18.46"; then
          echo "fixed"
        else
          echo "affected"
        fi
        ;;
      7.1.*)
        if ! xft80530_version_lt "$xft80530_version" "7.1.10"; then
          echo "fixed"
        else
          echo "affected"
        fi
        ;;
      *) echo "affected" ;;
    esac
  fi
}

checkXFSTangoCVE202680530() {
  [ "$(uname -s 2>/dev/null)" = "Linux" ] || return 0

  xft80530_kernel="${1:-$(uname -r 2>/dev/null)}"
  xft80530_mount_file="${2:-/proc/self/mounts}"
  xft80530_os_release="${3:-/etc/os-release}"
  [ -r "$xft80530_mount_file" ] || return 0

  # Limit filesystem queries on hosts with very large mount namespaces.
  xft80530_mounts="$(awk '$3 == "xfs" { print $2 }' "$xft80530_mount_file" 2>/dev/null | head -n 20)"
  [ -n "$xft80530_mounts" ] || return 0

  xft80530_status="$(xft80530_kernel_status "$xft80530_kernel")"
  xft80530_distro_id="$(sed -nE 's/^ID="?([^" ]+)"?$/\1/p' "$xft80530_os_release" 2>/dev/null | head -n1)"
  xft80530_distro_version="$(sed -nE 's/^VERSION_ID="?([^" ]+)"?$/\1/p' "$xft80530_os_release" 2>/dev/null | head -n1)"
  # Red Hat confirms that its 5.14-based RHEL 9 kernel backported the
  # vulnerable code, while RHEL 10 did not.  Do not trust upstream version
  # boundaries alone for these vendor kernels.
  case "$xft80530_distro_id:$xft80530_distro_version" in
    rhel:9*|centos:9*|rocky:9*|almalinux:9*) xft80530_status="vendor_affected" ;;
    rhel:10*) xft80530_status="vendor_unaffected" ;;
  esac
  xft80530_reflink_enabled=0
  xft80530_reflink_unknown=0

  print_3title "XFSTango XFS reflink LPE (CVE-2026-80530)" "T1068"
  print_info "https://seclists.org/oss-sec/2026/q3/641"
  echo "Kernel: $xft80530_kernel" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
  echo "Mounted XFS filesystems (maximum 20):"

  while IFS= read -r xft80530_mountpoint_escaped; do
    [ -n "$xft80530_mountpoint_escaped" ] || continue
    # /proc/self/mounts represents whitespace as octal escapes.
    xft80530_mountpoint="$(printf '%b' "$xft80530_mountpoint_escaped")"
    xft80530_info=""
    if [ -n "$TIMEOUT" ] && command -v xfs_info >/dev/null 2>&1; then
      xft80530_info="$("$TIMEOUT" 2 xfs_info "$xft80530_mountpoint" 2>/dev/null)"
    fi

    if printf '%s\n' "$xft80530_info" | grep -Eq '(^|[[:space:]])reflink=1([[:space:]]|$)'; then
      xft80530_reflink_status="enabled"
      xft80530_reflink_enabled=$((xft80530_reflink_enabled + 1))
    elif printf '%s\n' "$xft80530_info" | grep -Eq '(^|[[:space:]])reflink=0([[:space:]]|$)'; then
      xft80530_reflink_status="disabled"
    else
      xft80530_reflink_status="unknown (xfs_info unavailable or inconclusive)"
      xft80530_reflink_unknown=$((xft80530_reflink_unknown + 1))
    fi
    printf '  %s - reflink %s\n' "$xft80530_mountpoint" "$xft80530_reflink_status"
  done <<EOF
$xft80530_mounts
EOF

  case "$xft80530_status" in
    predates)
      echo "NOT VULNERABLE: upstream kernel $xft80530_kernel predates the vulnerable code introduced in 6.10" | sed -${E} "s,.*,${SED_GREEN},"
      ;;
    fixed)
      echo "NOT VULNERABLE by upstream version: kernel $xft80530_kernel is at or after a known fixed release" | sed -${E} "s,.*,${SED_GREEN},"
      ;;
    vendor_unaffected)
      echo "NOT VULNERABLE according to the Red Hat advisory: this RHEL 10-family kernel did not include the vulnerable code" | sed -${E} "s,.*,${SED_GREEN},"
      ;;
    affected|vendor_affected)
      if [ "$xft80530_reflink_enabled" -gt 0 ]; then
        if [ "$xft80530_status" = "vendor_affected" ]; then
          echo "POTENTIALLY VULNERABLE to CVE-2026-80530: this is a RHEL 9-family kernel (Red Hat lists RHEL 9 affected despite its 5.14 base), and XFS reflink is enabled" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        else
          echo "POTENTIALLY VULNERABLE to CVE-2026-80530: affected upstream kernel range and an XFS filesystem with reflink enabled" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        fi
        echo "Exploitation also requires an attacker-writable location and a readable target on the same XFS filesystem"
        echo "Vendor kernels may contain a backported fix; verify the installed kernel package advisory" | sed -${E} "s,.*,${SED_YELLOW},"
      elif [ "$xft80530_reflink_unknown" -gt 0 ]; then
        echo "POTENTIALLY VULNERABLE to CVE-2026-80530: affected upstream kernel range; confirm whether reflink is enabled on the XFS filesystem(s)" | sed -${E} "s,.*,${SED_YELLOW},"
      else
        echo "Mitigated: the mounted XFS filesystem(s) report reflink disabled" | sed -${E} "s,.*,${SED_GREEN},"
      fi
      ;;
    *)
      echo "XFS is mounted, but kernel version could not be assessed for CVE-2026-80530" | sed -${E} "s,.*,${SED_YELLOW},"
      ;;
  esac
  echo ""
}
