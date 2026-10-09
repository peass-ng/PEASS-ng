# Title: Software Information - Below log directory
# ID: SI_Below_Log_Directory
# Author: PEASS-ng contributors
# Last Update: 09-10-2026
# Description: Passive triage of a writable Below log directory with a privileged execution path
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_2title
# Global Variables: $MACPEAS, $SEARCH_IN_FOLDER, $sudo_l_cached_output, $sudo_l_password_output, $sudo_l_output
# Initial Functions:
# Generated Global Variables: $lp_below_binary, $lp_below_candidate, $lp_below_dir_mode, $lp_below_privileged_path
# Fat linpeas: 0
# Small linpeas: 1

lp_below_effective_access() {
  [ -w "$1" ] && [ -x "$1" ]
}

lp_below_is_symlink() {
  [ -L "$1" ]
}

lp_below_check_log_directory() {
  # The arguments permit metadata-only fixtures; the live call uses fixed paths.
  [ -f "$1" ] && [ -x "$1" ] || return 0
  [ -d "$2" ] && ! lp_below_is_symlink "$2" && lp_below_effective_access "$2" || return 0

  # Only the mode token for the fixed Below directory is used.
  # shellcheck disable=SC2012
  lp_below_dir_mode=$(ls -ld "$2" 2>/dev/null | awk 'NR == 1 { print $1 }')
  case "$lp_below_dir_mode" in
    d????????t*|d????????T*) return 0 ;;
    d????????[x-]*) ;;
    *) return 0 ;; # Unknown metadata must not become a positive finding.
  esac

  lp_below_privileged_path=""
  # These values are collected by the preceding sudo module when available.
  # shellcheck disable=SC2154
  if printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" |
    awk -v path="$1" '
      /^[[:space:]]*\((root|ALL)([[:space:]]*:[[:space:]]*ALL)?\)/ && index($0, "!") == 0 {
        at = index($0, path)
        if (at && (at == 1 || substr($0, at - 1, 1) ~ /[[:space:],:]/) &&
            (substr($0, at + length(path), 1) ~ /[[:space:]]/ || at + length(path) > length($0)))
          found = 1
      }
      END { exit !found }
    '; then
    lp_below_privileged_path="sudo rule lists this Below executable for root"
  elif command -v ps >/dev/null 2>&1 && ps -eo user=,comm= 2>/dev/null |
    awk '$1 == "root" && $2 == "below" { found = 1 } END { exit !found }'; then
    lp_below_privileged_path="root-owned Below process observed"
  fi
  [ -n "$lp_below_privileged_path" ] || return 0

  print_2title "Below log directory: potential privileged log-file exposure" "T1068"
  printf 'Installed Below executable: %s\n' "$1"
  printf 'Log directory: %s (effective write/search access; nonsticky mode %s)\n' "$2" "$lp_below_dir_mode"
  printf 'Privileged execution evidence: %s\n' "$lp_below_privileged_path"
  if lp_below_is_symlink "$3"; then
    printf 'Log entry: %s is a symlink (target not read)\n' "$3"
  elif [ -e "$3" ]; then
    printf 'Log entry: %s exists\n' "$3"
  else
    printf 'Log entry: %s absent\n' "$3"
  fi
  printf '%s\n' 'Patch status: unknown; package version and backports were not verified.'
  printf '%s\n\n' 'Review ACLs, filesystem behavior, and the actual privileged launch before treating this as CVE-2025-27591 exposure.'
}

if [ -z "$SEARCH_IN_FOLDER" ] && [ -z "$MACPEAS" ] && [ "$(id -u 2>/dev/null)" != 0 ]; then
  lp_below_binary=""
  for lp_below_candidate in /usr/bin/below /usr/local/bin/below; do
    if [ -f "$lp_below_candidate" ] && [ -x "$lp_below_candidate" ]; then
      lp_below_binary="$lp_below_candidate"
      break
    fi
  done
  [ -n "$lp_below_binary" ] && lp_below_check_log_directory "$lp_below_binary" /var/log/below /var/log/below/error_root.log
fi
