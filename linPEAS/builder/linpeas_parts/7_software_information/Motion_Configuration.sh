# Title: Software Information - Motion configuration
# ID: SI_Motion_Configuration
# Author: PEASS-ng contributors
# Last Update: 08-10-2026
# Description: Read-only inventory of local motionEye and Motion control settings without printing credentials
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.001,T1068
# Functions Used: print_2title
# Global Variables: $MACPEAS, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $lp_motion_conf_dir, $lp_motion_custom_dir, $motion_config, $motion_dir, $motion_camera, $motion_camera_count, $motion_hook, $motion_filename, $motion_camera_unreadable, $motion_camera_flags
# Fat linpeas: 0
# Small linpeas: 1

lp_motion_readable() {
  [ -r "$1" ]
}

lp_motion_check_config() {
  local motion_config="$1" motion_dir motion_camera motion_camera_count=0
  local motion_hook=0 motion_filename=0 motion_camera_unreadable=0 motion_camera_flags

  [ -e "$motion_config" ] || return 0
  print_2title "Motion configuration: $motion_config" "T1552.001"
  if [ ! -f "$motion_config" ] || ! lp_motion_readable "$motion_config"; then
    echo "Configuration exists but is not readable by the current user"
    echo ""
    return 0
  fi

  # Values are reduced to fixed labels or validated numbers. In particular,
  # motionEye stores the admin hash in a comment, which must never be printed.
  dd if="$motion_config" bs=65536 count=1 2>/dev/null | awk '
    /^[[:space:]]*#[[:space:]]*@admin_password([[:space:]]|$)/ {
      admin_seen = 1
      admin_present = ($3 != "")
      next
    }
    /^[[:space:]]*#/ { next }
    $1 == "webcontrol_port" { port = ($2 ~ /^[0-9]+$/ && $2 + 0 <= 65535 ? $2 : "invalid") }
    $1 == "webcontrol_parms" { parms = ($2 ~ /^[0-3]$/ ? $2 : "invalid") }
    $1 == "webcontrol_auth_method" {
      method = tolower($2)
      if (method == "0" || method == "none") auth = "disabled"
      else if (method == "1" || method == "2" || method == "basic" || method == "digest") auth = "enabled"
      else auth = "unknown"
    }
    $1 == "webcontrol_localhost" { local_only = ($2 == "on" || $2 == "off" ? $2 : "invalid") }
    END {
      if (admin_seen) print "motionEye admin hash: " (admin_present ? "present (redacted)" : "empty")
      print "Motion webcontrol port: " (port == "" ? "not configured" : port)
      print "Motion webcontrol parameter level: " (parms == "" ? "default (0)" : parms)
      print "Motion webcontrol authentication: " (auth == "" ? "disabled/default" : auth)
      print "Motion webcontrol localhost restriction: " (local_only == "" ? "default (on)" : local_only)
      if (port ~ /^[0-9]+$/ && port + 0 > 0 && parms ~ /^[2-3]$/ && (auth == "" || auth == "disabled"))
        print "Review: Motion webcontrol may expose advanced settings without authentication to local users"
    }
  ' 2>/dev/null

  # motionEye normally places per-camera settings beside motion.conf. Bound
  # this local glob so an unusual directory cannot turn the check into a scan.
  motion_dir="${motion_config%/*}"
  for motion_camera in "$motion_dir"/camera-*.conf; do
    [ -e "$motion_camera" ] || continue
    motion_camera_count=$((motion_camera_count + 1))
    [ "$motion_camera_count" -le 10 ] || break
    if [ ! -f "$motion_camera" ] || ! lp_motion_readable "$motion_camera"; then
      motion_camera_unreadable=1
      continue
    fi
    motion_camera_flags="$(dd if="$motion_camera" bs=65536 count=1 2>/dev/null | awk '
      /^[[:space:]]*#/ { next }
      $1 == "on_picture_save" && NF > 1 { hook = 1 }
      $1 == "picture_filename" && NF > 1 { filename = 1 }
      END { print hook + 0, filename + 0 }
    ' 2>/dev/null)"
    case "$motion_camera_flags" in "1 "*) motion_hook=1 ;; esac
    case "$motion_camera_flags" in *" 1") motion_filename=1 ;; esac
  done
  [ "$motion_hook" -eq 1 ] && echo "Camera picture-save event hook: configured (value redacted)"
  [ "$motion_filename" -eq 1 ] && echo "Camera picture filename: configured (value redacted)"
  [ "$motion_camera_unreadable" -eq 1 ] && echo "Some camera configuration files are not readable"
  echo "Configuration alone does not establish that Motion is running or its service user"
  echo ""
}

if ! [ "$SEARCH_IN_FOLDER" ] && ! [ "$MACPEAS" ]; then
  lp_motion_conf_dir="/etc/motioneye"
  if lp_motion_readable /etc/motioneye/motioneye.conf; then
    lp_motion_custom_dir="$(awk '$1 == "conf_path" { path = $2 } END { print path }' /etc/motioneye/motioneye.conf 2>/dev/null)"
    case "$lp_motion_custom_dir" in /*) lp_motion_conf_dir="$lp_motion_custom_dir" ;; esac
  fi
  lp_motion_check_config "$lp_motion_conf_dir/motion.conf"
  lp_motion_check_config /etc/motion/motion.conf
  lp_motion_check_config /usr/local/etc/motion/motion.conf
fi
