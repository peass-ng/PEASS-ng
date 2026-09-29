# Title: Function - checkAndroidBinderCVE202320938
# ID: checkAndroidBinderCVE202320938
# Author: HT Bot
# Last Update: 29-09-2026
# Description: Passively assess Android GKI 5.4/5.10 Binder exposure and the security patch levels for CVE-2023-20938 and its incomplete fix, CVE-2023-21255.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_GREEN, $SED_LIGHT_CYAN, $SED_RED_YELLOW, $SED_YELLOW
# Initial Functions:
# Generated Global Variables: $ab20938_access, $ab20938_android_release, $ab20938_candidate, $ab20938_device, $ab20938_devices, $ab20938_effective_num, $ab20938_effective_spl, $ab20938_file, $ab20938_fingerprint, $ab20938_first_api, $ab20938_gki, $ab20938_kernel, $ab20938_kernel_branch, $ab20938_kernel_build, $ab20938_key, $ab20938_patch_state, $ab20938_platform_num, $ab20938_platform_spl, $ab20938_root, $ab20938_rw_devices, $ab20938_sdk, $ab20938_value, $ab20938_vendor_fingerprint, $ab20938_vendor_num, $ab20938_vendor_spl
# Fat linpeas: 0
# Small linpeas: 1

ab20938_getprop() {
  ab20938_key="$1"

  if [ "$ab20938_root" = "/" ] && command -v getprop >/dev/null 2>&1; then
    ab20938_value="$(getprop "$ab20938_key" 2>/dev/null)"
    [ "$ab20938_value" ] && { printf '%s' "$ab20938_value"; return 0; }
  fi

  for ab20938_file in \
    "${ab20938_root}system/build.prop" \
    "${ab20938_root}system/system/build.prop" \
    "${ab20938_root}vendor/build.prop" \
    "${ab20938_root}product/build.prop" \
    "${ab20938_root}odm/build.prop" \
    "${ab20938_root}default.prop"; do
    [ -r "$ab20938_file" ] || continue
    ab20938_value="$(awk -F= -v key="$ab20938_key" '$1 == key { sub(/^[^=]*=/, ""); print; exit }' "$ab20938_file" 2>/dev/null)"
    [ "$ab20938_value" ] && { printf '%s' "$ab20938_value"; return 0; }
  done

  return 1
}

ab20938_valid_spl() {
  printf '%s' "$1" | grep -Eq '^[0-9]{4}-(0[1-9]|1[0-2])-([0-2][0-9]|3[01])$'
}

checkAndroidBinderCVE202320938() {
  ab20938_root="${ROOT_FOLDER:-/}"
  case "$ab20938_root" in
    */) ;;
    *) ab20938_root="${ab20938_root}/" ;;
  esac

  ab20938_android_release="$(ab20938_getprop ro.build.version.release)"
  ab20938_sdk="$(ab20938_getprop ro.build.version.sdk)"
  ab20938_platform_spl="$(ab20938_getprop ro.build.version.security_patch)"
  ab20938_vendor_spl="$(ab20938_getprop ro.vendor.build.security_patch)"
  ab20938_fingerprint="$(ab20938_getprop ro.build.fingerprint)"
  ab20938_vendor_fingerprint="$(ab20938_getprop ro.vendor.build.fingerprint)"
  ab20938_first_api="$(ab20938_getprop ro.product.first_api_level)"

  if [ -z "$ab20938_android_release$ab20938_sdk$ab20938_platform_spl$ab20938_fingerprint" ] \
    && [ ! -e "${ab20938_root}system/bin/app_process" ] \
    && [ ! -e "${ab20938_root}system/framework/framework.jar" ]; then
    return 0
  fi

  if [ -r "${ab20938_root}proc/sys/kernel/osrelease" ]; then
    ab20938_kernel="$(cat "${ab20938_root}proc/sys/kernel/osrelease" 2>/dev/null)"
  elif [ "$ab20938_root" = "/" ]; then
    ab20938_kernel="$(uname -r 2>/dev/null)"
  else
    ab20938_kernel="$(ab20938_getprop ro.kernel.version)"
  fi

  if [ -r "${ab20938_root}proc/version" ]; then
    ab20938_kernel_build="$(cat "${ab20938_root}proc/version" 2>/dev/null)"
  elif [ "$ab20938_root" = "/" ]; then
    ab20938_kernel_build="$(uname -v 2>/dev/null)"
  else
    ab20938_kernel_build="unknown"
  fi

  print_3title "Android Binder LPE (CVE-2023-20938 / CVE-2023-21255)" "T1068"
  print_info "https://androidoffsec.withgoogle.com/posts/attacking-android-binder-analysis-and-exploitation-of-cve-2023-20938/"
  print_info "https://source.android.com/docs/security/bulletin/2023-02-01"
  print_info "https://source.android.com/docs/security/bulletin/2023-07-01"
  print_info "https://source.android.com/docs/core/architecture/kernel/gki-versioning"

  echo "Android release: ${ab20938_android_release:-unknown} (SDK ${ab20938_sdk:-unknown}; first API ${ab20938_first_api:-unknown})"
  echo "Platform security patch level: ${ab20938_platform_spl:-unknown}"
  echo "Vendor security patch level: ${ab20938_vendor_spl:-unknown}"
  echo "Kernel release: ${ab20938_kernel:-unknown}"
  echo "Kernel build: ${ab20938_kernel_build:-unknown}"
  echo "Build fingerprint: ${ab20938_fingerprint:-unknown}"
  [ "$ab20938_vendor_fingerprint" ] && echo "Vendor fingerprint: $ab20938_vendor_fingerprint"

  ab20938_effective_spl=""
  ab20938_platform_num=""
  ab20938_vendor_num=""
  if ab20938_valid_spl "$ab20938_platform_spl"; then
    ab20938_platform_num="$(printf '%s' "$ab20938_platform_spl" | tr -d '-')"
    ab20938_effective_spl="$ab20938_platform_spl"
    ab20938_effective_num="$ab20938_platform_num"
  fi
  if ab20938_valid_spl "$ab20938_vendor_spl"; then
    ab20938_vendor_num="$(printf '%s' "$ab20938_vendor_spl" | tr -d '-')"
    if [ -z "$ab20938_effective_spl" ] || [ "$ab20938_vendor_num" -lt "$ab20938_effective_num" ]; then
      ab20938_effective_spl="$ab20938_vendor_spl"
      ab20938_effective_num="$ab20938_vendor_num"
    fi
  fi
  [ "$ab20938_effective_spl" ] && echo "Conservative patch level used for assessment: $ab20938_effective_spl (oldest reported platform/vendor level)"

  ab20938_kernel_branch=""
  case "$ab20938_kernel" in
    5.4|5.4.*) ab20938_kernel_branch="5.4" ;;
    5.10|5.10.*) ab20938_kernel_branch="5.10" ;;
  esac

  ab20938_gki="unknown"
  # Android documents the GKI release form as
  # w.x.y-android<release>-<KMI generation>-<suffix>.  Do not treat a generic
  # Android build-host marker as proof that a vendor kernel is GKI.
  if printf '%s' "$ab20938_kernel" | grep -Eq '^5\.(4|10)\.[0-9]+-android[0-9]+-[0-9]+([.-].*)?$' \
    || printf '%s' "$ab20938_kernel" | grep -Eqi '(^|[.-])gki([.-]|$)'; then
    ab20938_gki="detected"
  fi
  echo "GKI evidence: $ab20938_gki"

  ab20938_devices=""
  ab20938_rw_devices=""
  for ab20938_candidate in \
    dev/binder dev/hwbinder dev/vndbinder \
    dev/binderfs/binder dev/binderfs/hwbinder dev/binderfs/vndbinder; do
    ab20938_device="${ab20938_root}${ab20938_candidate}"
    [ -e "$ab20938_device" ] || continue
    ab20938_devices="$ab20938_devices /$ab20938_candidate"
    ab20938_access="present"
    if [ "$ab20938_root" != "/" ]; then
      ab20938_access="present (offline image; runtime access unknown)"
    elif [ -r "$ab20938_device" ] && [ -w "$ab20938_device" ]; then
      ab20938_access="read/write accessible to the current context"
      ab20938_rw_devices="$ab20938_rw_devices /$ab20938_candidate"
    elif [ -r "$ab20938_device" ]; then
      ab20938_access="read-only accessible to the current context"
    elif [ -w "$ab20938_device" ]; then
      ab20938_access="write-only accessible to the current context"
    else
      ab20938_access="not accessible to the current context"
    fi
    echo "Binder device /$ab20938_candidate: $ab20938_access"
  done
  [ "$ab20938_devices" ] || echo "Binder devices: none found"

  ab20938_patch_state="unknown"
  if [ "$ab20938_effective_spl" ]; then
    if [ "$ab20938_effective_num" -ge 20230705 ]; then
      ab20938_patch_state="fully-fixed"
      echo "Patch status: the reported patch level includes the July 5, 2023 Binder root-cause fix" | sed -${E} "s,.*,${SED_GREEN},"
    elif [ "$ab20938_effective_num" -ge 20230205 ]; then
      ab20938_patch_state="incomplete-fix"
      echo "Patch status: the February fix is reported, but the July root-cause fix for CVE-2023-21255 is not" | sed -${E} "s,.*,${SED_YELLOW},"
    else
      ab20938_patch_state="unfixed"
      echo "Patch status: predates the February 5, 2023 CVE-2023-20938 patch level" | sed -${E} "s,.*,${SED_YELLOW},"
    fi
  else
    echo "Patch status: no valid Android platform/vendor security patch level was available" | sed -${E} "s,.*,${SED_YELLOW},"
  fi

  if [ -z "$ab20938_kernel_branch" ]; then
    echo "NOT APPLICABLE: the published exploit affected Android GKI 5.4 and 5.10, not kernel ${ab20938_kernel:-unknown}" | sed -${E} "s,.*,${SED_GREEN},"
    return 0
  fi

  if [ "$ab20938_gki" != "detected" ]; then
    echo "No vulnerability verdict: kernel $ab20938_kernel is in a relevant version family, but GKI could not be confirmed" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
    return 0
  fi

  if [ -z "$ab20938_devices" ]; then
    echo "Current Binder attack surface not found" | sed -${E} "s,.*,${SED_GREEN},"
    return 0
  fi

  if [ "$ab20938_root" = "/" ] && [ -z "$ab20938_rw_devices" ]; then
    echo "Current context cannot read and write a detected Binder device; this execution context cannot use the described entry point" | sed -${E} "s,.*,${SED_GREEN},"
    echo "Other Android application domains can have different Binder access under SELinux" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
    return 0
  fi

  case "$ab20938_patch_state" in
    fully-fixed)
      echo "No vulnerable patch-level indication for CVE-2023-20938/CVE-2023-21255" | sed -${E} "s,.*,${SED_GREEN},"
      ;;
    incomplete-fix)
      echo "POTENTIAL ANDROID KERNEL LPE: GKI $ab20938_kernel_branch, a Binder device, and a pre-2023-07-05 patch level match CVE-2023-21255" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      echo "Confirm the vendor kernel contains commit 1ca1130ec62d (or an equivalent backport); patch-level matching alone cannot prove vulnerability" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
      ;;
    unfixed)
      echo "POTENTIAL ANDROID KERNEL LPE: GKI $ab20938_kernel_branch, a Binder device, and a pre-2023-02-05 patch level match CVE-2023-20938/CVE-2023-21255" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      echo "Confirm vendor backports before treating this heuristic as proof of vulnerability" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
      ;;
    *)
      echo "Relevant GKI/Binder attack surface detected, but patch status is unknown; verify both February and July 2023 Binder fixes manually" | sed -${E} "s,.*,${SED_YELLOW},"
      ;;
  esac
}
