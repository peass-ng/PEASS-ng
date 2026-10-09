# Title: Processes & Cron & Services & Timers - Cross-user Xvfb framebuffer files
# ID: PR_Xvfb_fbdir
# Author: PEASS-ng contributors
# Last Update: 08-10-2026
# Description: Report readable framebuffer files from another user's running Xvfb without reading their contents
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.001
# Functions Used: print_2title
# Global Variables: $MACPEAS, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $comm, $current_uid, $cwd, $fbdir, $found, $metadata, $mode, $owner_uid, $pid, $printed, $proc, $process_uid, $proc_root, $screen, $screens
# Fat linpeas: 0
# Small linpeas: 1

lp_xvfb_fbdir_from_cmdline() {
  tr '\000' '\n' < "$1" 2>/dev/null | awk '
    NR == 1 {
      name = $0
      sub(/^.*\//, "", name)
      if (name != "Xvfb") exit
    }
    want_dir { if ($0 != "") print; exit }
    $0 == "-fbdir" { want_dir = 1 }
  '
}

lp_xvfb_candidate_pids() {
  local proc_root="$1" proc comm found=0
  if [ "$proc_root" = /proc ] && command -v pgrep >/dev/null 2>&1; then
    pgrep -x Xvfb 2>/dev/null | head -n 8
    return
  fi
  # Fallback for systems without pgrep. Inspect only comm for other processes;
  # stop after eight actual Xvfb candidates, regardless of PID ordering.
  for proc in "$proc_root"/[0-9]*; do
    [ -r "$proc/comm" ] || continue
    IFS= read -r comm < "$proc/comm" || continue
    [ "$comm" = Xvfb ] || continue
    printf '%s\n' "${proc##*/}"
    found=$((found + 1))
    [ "$found" -lt 8 ] || break
  done
}

lp_check_xvfb_fbdir() {
  local proc_root="${1:-/proc}" current_uid="${2:-$(id -u)}"
  local proc pid fbdir comm process_uid metadata owner_uid mode screen screens cwd printed=0

  [ -d "$proc_root" ] || return 0
  # Root can normally read every user's framebuffer, so this is useful for
  # unprivileged cross-user access only.
  [ "$current_uid" = 0 ] && return 0
  for pid in $(lp_xvfb_candidate_pids "$proc_root"); do
    proc="$proc_root/$pid"
    [ -r "$proc/comm" ] && [ -r "$proc/cmdline" ] && [ -r "$proc/status" ] || continue
    IFS= read -r comm < "$proc/comm" || continue
    [ "$comm" = Xvfb ] || continue
    process_uid=$(awk '$1 == "Uid:" { print $3; exit }' "$proc/status" 2>/dev/null)
    case "$process_uid" in ''|*[!0-9]*) continue ;; esac
    [ "$process_uid" != "$current_uid" ] || continue
    fbdir=$(lp_xvfb_fbdir_from_cmdline "$proc/cmdline")
    [ -n "$fbdir" ] || continue
    case "$fbdir" in
      /*) ;;
      *) cwd=$(readlink "$proc/cwd" 2>/dev/null) || continue
         [ -n "$cwd" ] || continue
         fbdir="$cwd/$fbdir" ;;
    esac
    [ -d "$fbdir" ] || continue
    screens=$(find "$fbdir" -maxdepth 1 -type f -name 'Xvfb_screen*' -print 2>/dev/null | head -n 8)
    [ -n "$screens" ] || continue
    while IFS= read -r screen; do
      [ -f "$screen" ] && [ -r "$screen" ] || continue
      metadata=$(stat -c '%u %a' -- "$screen" 2>/dev/null) || continue
      owner_uid=${metadata%% *}
      mode=${metadata#* }
      [ "$owner_uid" = "$process_uid" ] || continue
      [ "$owner_uid" != "$current_uid" ] || continue
      if [ "$printed" -eq 0 ]; then
        print_2title "Readable cross-user Xvfb framebuffer files" "T1552.001"
        printed=1
      fi
      printf 'Review: running Xvfb PID %s exposes readable framebuffer: %s (owner UID %s, mode 0%s)\n' "$pid" "$screen" "$owner_uid" "$mode"
    done <<EOF
$screens
EOF
  done
  [ "$printed" -eq 0 ] || echo ""
}

if ! [ "$SEARCH_IN_FOLDER" ] && ! [ "$MACPEAS" ] && [ -d /proc ]; then
  lp_check_xvfb_fbdir
fi
