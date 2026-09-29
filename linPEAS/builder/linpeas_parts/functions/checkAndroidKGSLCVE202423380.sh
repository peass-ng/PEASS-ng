# Title: Function - checkAndroidKGSLCVE202423380
# ID: checkAndroidKGSLCVE202423380
# Author: HT Bot
# Last Update: 29-09-2026
# Description: Passively assess Qualcomm KGSL exposure and Android security patch levels for CVE-2024-23380 without interacting with the GPU driver.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_3title, print_info
# Global Variables: $E, $ROOT_FOLDER, $SED_GREEN, $SED_LIGHT_CYAN, $SED_RED_YELLOW, $SED_YELLOW
# Initial Functions:
# Generated Global Variables: $kg23380_access, $kg23380_android_release, $kg23380_board, $kg23380_candidate, $kg23380_device, $kg23380_device_details, $kg23380_driver_evidence, $kg23380_driver_version, $kg23380_effective_num, $kg23380_effective_spl, $kg23380_file, $kg23380_fingerprint, $kg23380_gpu_model, $kg23380_hardware, $kg23380_key, $kg23380_kernel, $kg23380_kernel_build, $kg23380_platform_num, $kg23380_platform_spl, $kg23380_qualcomm_evidence, $kg23380_root, $kg23380_sdk, $kg23380_soc_manufacturer, $kg23380_soc_model, $kg23380_sysfs, $kg23380_value, $kg23380_vendor_fingerprint, $kg23380_vendor_num, $kg23380_vendor_spl
# Fat linpeas: 0
# Small linpeas: 1

kg23380_getprop() {
  kg23380_key="$1"

  if [ "$kg23380_root" = "/" ] && command -v getprop >/dev/null 2>&1; then
    kg23380_value="$(getprop "$kg23380_key" 2>/dev/null)"
    [ "$kg23380_value" ] && { printf '%s' "$kg23380_value"; return 0; }
  fi

  for kg23380_file in \
    "${kg23380_root}system/build.prop" \
    "${kg23380_root}system/system/build.prop" \
    "${kg23380_root}vendor/build.prop" \
    "${kg23380_root}product/build.prop" \
    "${kg23380_root}odm/build.prop" \
    "${kg23380_root}default.prop"; do
    [ -r "$kg23380_file" ] || continue
    kg23380_value="$(awk -F= -v key="$kg23380_key" '$1 == key { sub(/^[^=]*=/, ""); print; exit }' "$kg23380_file" 2>/dev/null)"
    [ "$kg23380_value" ] && { printf '%s' "$kg23380_value"; return 0; }
  done

  return 1
}

kg23380_valid_spl() {
  printf '%s' "$1" | grep -Eq '^[0-9]{4}-(0[1-9]|1[0-2])-([0-2][0-9]|3[01])$'
}

checkAndroidKGSLCVE202423380() {
  kg23380_root="${ROOT_FOLDER:-/}"
  case "$kg23380_root" in
    */) ;;
    *) kg23380_root="${kg23380_root}/" ;;
  esac

  kg23380_android_release="$(kg23380_getprop ro.build.version.release)"
  kg23380_sdk="$(kg23380_getprop ro.build.version.sdk)"
  kg23380_platform_spl="$(kg23380_getprop ro.build.version.security_patch)"
  kg23380_vendor_spl="$(kg23380_getprop ro.vendor.build.security_patch)"
  kg23380_fingerprint="$(kg23380_getprop ro.build.fingerprint)"
  kg23380_vendor_fingerprint="$(kg23380_getprop ro.vendor.build.fingerprint)"

  if [ -z "$kg23380_android_release$kg23380_sdk$kg23380_platform_spl$kg23380_fingerprint" ] \
    && [ ! -e "${kg23380_root}system/bin/app_process" ] \
    && [ ! -e "${kg23380_root}system/framework/framework.jar" ]; then
    return 0
  fi

  kg23380_hardware="$(kg23380_getprop ro.hardware)"
  [ "$kg23380_hardware" ] || kg23380_hardware="$(kg23380_getprop ro.boot.hardware)"
  kg23380_board="$(kg23380_getprop ro.board.platform)"
  [ "$kg23380_board" ] || kg23380_board="$(kg23380_getprop ro.product.board)"
  kg23380_soc_manufacturer="$(kg23380_getprop ro.soc.manufacturer)"
  kg23380_soc_model="$(kg23380_getprop ro.soc.model)"

  if [ -r "${kg23380_root}proc/sys/kernel/osrelease" ]; then
    kg23380_kernel="$(cat "${kg23380_root}proc/sys/kernel/osrelease" 2>/dev/null)"
  elif [ "$kg23380_root" = "/" ]; then
    kg23380_kernel="$(uname -r 2>/dev/null)"
  else
    kg23380_kernel="$(kg23380_getprop ro.kernel.version)"
  fi

  if [ -r "${kg23380_root}proc/version" ]; then
    kg23380_kernel_build="$(cat "${kg23380_root}proc/version" 2>/dev/null)"
  elif [ "$kg23380_root" = "/" ]; then
    kg23380_kernel_build="$(uname -v 2>/dev/null)"
  else
    kg23380_kernel_build="unknown"
  fi

  kg23380_device="${kg23380_root}dev/kgsl-3d0"
  kg23380_sysfs=""
  for kg23380_candidate in \
    "${kg23380_root}sys/class/kgsl/kgsl-3d0" \
    "${kg23380_root}sys/class/misc/kgsl-3d0" \
    "${kg23380_root}sys/devices/platform/"*kgsl* \
    "${kg23380_root}sys/devices/platform/soc/"*kgsl*; do
    [ -e "$kg23380_candidate" ] || continue
    kg23380_sysfs="$kg23380_candidate"
    break
  done

  kg23380_driver_evidence=""
  [ -e "$kg23380_device" ] && kg23380_driver_evidence="device node"
  if [ "$kg23380_sysfs" ]; then
    if [ "$kg23380_driver_evidence" ]; then
      kg23380_driver_evidence="$kg23380_driver_evidence and sysfs"
    else
      kg23380_driver_evidence="sysfs"
    fi
  fi

  kg23380_qualcomm_evidence="unknown"
  if [ "$kg23380_driver_evidence" ] \
    || printf '%s\n' "$kg23380_hardware $kg23380_board $kg23380_soc_manufacturer $kg23380_soc_model $kg23380_fingerprint $kg23380_vendor_fingerprint" \
      | grep -Eqi 'qualcomm|qcom|(^|[^[:alnum:]])(msm|sdm|sm)[0-9]'; then
    kg23380_qualcomm_evidence="detected"
  fi

  kg23380_gpu_model=""
  for kg23380_candidate in \
    "${kg23380_root}sys/class/kgsl/kgsl-3d0/gpu_model" \
    "${kg23380_root}sys/class/kgsl/kgsl-3d0/gpu_model_name" \
    "${kg23380_root}sys/class/misc/kgsl-3d0/device/gpu_model"; do
    [ -r "$kg23380_candidate" ] || continue
    kg23380_gpu_model="$(cat "$kg23380_candidate" 2>/dev/null)"
    [ "$kg23380_gpu_model" ] && break
  done

  kg23380_driver_version=""
  for kg23380_candidate in \
    "${kg23380_root}sys/module/kgsl/version" \
    "${kg23380_root}sys/module/msm_kgsl/version" \
    "${kg23380_root}sys/class/kgsl/kgsl-3d0/device/driver/module/version"; do
    [ -r "$kg23380_candidate" ] || continue
    kg23380_driver_version="$(cat "$kg23380_candidate" 2>/dev/null)"
    [ "$kg23380_driver_version" ] && break
  done

  print_3title "Android Qualcomm KGSL VBO LPE (CVE-2024-23380)" "T1068"
  print_info "https://androidoffsec.withgoogle.com/posts/a-technical-deep-dive-into-cve-2024-23380-exploiting-gpu-memory-corruption-to-android-root/"
  print_info "https://source.android.com/docs/security/bulletin/2024-07-01"
  print_info "https://git.codelinaro.org/clo/la/platform/vendor/qcom/opensource/graphics-kernel/-/commit/919306871384731b35cbfafb208bbd13bff08605"

  echo "Android release: ${kg23380_android_release:-unknown} (SDK ${kg23380_sdk:-unknown})"
  echo "Platform security patch level: ${kg23380_platform_spl:-unknown}"
  echo "Vendor security patch level: ${kg23380_vendor_spl:-unknown}"
  echo "Hardware: ${kg23380_hardware:-unknown}; board platform: ${kg23380_board:-unknown}"
  echo "SoC: ${kg23380_soc_manufacturer:-unknown} ${kg23380_soc_model:-unknown}"
  echo "Qualcomm/Adreno evidence: $kg23380_qualcomm_evidence"
  echo "Kernel release: ${kg23380_kernel:-unknown}"
  echo "Kernel build: ${kg23380_kernel_build:-unknown}"
  echo "KGSL evidence: ${kg23380_driver_evidence:-none}"
  echo "KGSL/Adreno model: ${kg23380_gpu_model:-unknown}"
  echo "KGSL module version: ${kg23380_driver_version:-unknown (often not exported separately from the vendor kernel)}"

  kg23380_access="missing"
  if [ -e "$kg23380_device" ]; then
    kg23380_device_details="$(ls -ld "$kg23380_device" 2>/dev/null | awk '{ print $1, $3, $4 }')"
    echo "KGSL device: /dev/kgsl-3d0${kg23380_device_details:+ ($kg23380_device_details)}"
    if [ "$kg23380_root" != "/" ]; then
      kg23380_access="present (offline image; runtime access and SELinux policy unknown)"
    elif [ -r "$kg23380_device" ] && [ -w "$kg23380_device" ]; then
      kg23380_access="read/write accessible to the current context"
    elif [ -r "$kg23380_device" ]; then
      kg23380_access="read-only accessible to the current context"
    elif [ -w "$kg23380_device" ]; then
      kg23380_access="write-only accessible to the current context"
    else
      kg23380_access="not accessible to the current context"
    fi
  else
    echo "KGSL device: /dev/kgsl-3d0 not found"
  fi
  echo "KGSL device access: $kg23380_access"
  [ "$kg23380_sysfs" ] && echo "KGSL sysfs: /${kg23380_sysfs#${kg23380_root}}"

  kg23380_effective_spl=""
  kg23380_effective_num=""
  kg23380_platform_num=""
  kg23380_vendor_num=""
  if kg23380_valid_spl "$kg23380_platform_spl"; then
    kg23380_platform_num="$(printf '%s' "$kg23380_platform_spl" | tr -d '-')"
    kg23380_effective_spl="$kg23380_platform_spl"
    kg23380_effective_num="$kg23380_platform_num"
  fi
  if kg23380_valid_spl "$kg23380_vendor_spl"; then
    kg23380_vendor_num="$(printf '%s' "$kg23380_vendor_spl" | tr -d '-')"
    if [ -z "$kg23380_effective_spl" ] || [ "$kg23380_vendor_num" -lt "$kg23380_effective_num" ]; then
      kg23380_effective_spl="$kg23380_vendor_spl"
      kg23380_effective_num="$kg23380_vendor_num"
    fi
  fi
  [ "$kg23380_effective_spl" ] && echo "Conservative patch level used for assessment: $kg23380_effective_spl (oldest reported platform/vendor level)"

  if [ -z "$kg23380_driver_evidence" ]; then
    echo "Current Qualcomm KGSL attack surface not found; CVE-2024-23380 is hardware- and Android-build-specific" | sed -${E} "s,.*,${SED_GREEN},"
    return 0
  fi

  if [ "$kg23380_root" = "/" ] && [ ! -e "$kg23380_device" ]; then
    echo "KGSL driver evidence exists, but the required /dev/kgsl-3d0 entry point is absent in this runtime" | sed -${E} "s,.*,${SED_GREEN},"
    return 0
  fi

  if [ "$kg23380_effective_spl" ] && [ "$kg23380_effective_num" -ge 20240705 ]; then
    echo "No vulnerable patch-level indication: the reported patch level includes the July 5, 2024 Qualcomm fixes" | sed -${E} "s,.*,${SED_GREEN},"
    echo "Patch-level matching cannot establish the exact KGSL driver revision; OEM backports and incomplete vendor updates should be verified separately" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
    return 0
  fi

  if [ "$kg23380_effective_spl" ]; then
    echo "POTENTIAL ANDROID KERNEL LPE: Qualcomm KGSL is present and patch level $kg23380_effective_spl predates the 2024-07-05 fix for CVE-2024-23380" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  else
    echo "POTENTIAL ANDROID KERNEL LPE: Qualcomm KGSL is present but no valid Android platform/vendor security patch level is available for CVE-2024-23380" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  fi
  echo "This is a passive heuristic: confirm Qualcomm fix 919306871384731b35cbfafb208bbd13bff08605 or an OEM-equivalent backport before treating the device as vulnerable" | sed -${E} "s,.*,${SED_LIGHT_CYAN},"
  if [ "$kg23380_root" = "/" ] && [ "$kg23380_access" != "read/write accessible to the current context" ]; then
    echo "The current shell cannot read and write /dev/kgsl-3d0; Android application domains can have different SELinux access" | sed -${E} "s,.*,${SED_YELLOW},"
  fi
}
