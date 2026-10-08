# Title: Processes & Cron & Services & Timers - Erlang SSH daemons
# ID: PR_Erlang_ssh_daemons
# Author: PEASS-ng contributors
# Last Update: 2026-10-09
# Description: Correlate running Erlang source files with OTP SSH markers without disclosing authentication values
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.001
# Functions Used: print_2title
# Global Variables: $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $snapshot, $proc_root, $ps_rows, $pid, $uid, $command, $source, $physical_dir, $canonical, $size, $markers, $auth, $shell, $candidates, $files, $printed, $unknowns, $seen_files
# Fat linpeas: 0
# Small linpeas: 1

lp_erlang_ssh_source_from_ps() {
  printf '%s\n' "$1" | awk '
    {
      n = split($0, a, /[[:space:]]+/)
      executable = a[1]
      sub(/^.*\//, "", executable)
      if (executable != "escript") exit
      for (i = 2; i <= n; i++)
        if (a[i] ~ /^\/[^[:space:]]+\.(escript|erl)$/) { print a[i]; exit }
    }'
}

lp_erlang_ssh_source_from_proc() {
  [ -r "$1" ] || return 1
  tr '\000' '\n' < "$1" 2>/dev/null | awk '
    NR == 1 {
      executable = $0
      sub(/^.*\//, "", executable)
      if (executable != "escript") exit
      next
    }
    /^\/.*\.(escript|erl)$/ { print; exit }'
}

lp_erlang_ssh_markers() {
  awk '
    NR > 4096 { exit }
    {
      # Ignore comments and quoted strings, including markers placed in examples.
      line = $0 "\n"
      for (i = 1; i <= length(line); i++) {
        c = substr(line, i, 1)
        if (comment) { if (c == "\n") comment = 0; continue }
        if (quoted) {
          if (escape) { escape = 0; continue }
          if (c == "\\") { escape = 1; continue }
          if (c == quote) quoted = 0
          continue
        }
        if (c == "%") { comment = 1; continue }
        if (c == "\"" || c == "\047") { quoted = 1; quote = c; continue }
        code = code c
      }
    }
    END {
      gsub(/[[:space:]]+/, " ", code)
      if (code !~ /ssh[[:space:]]*:[[:space:]]*daemon[[:space:]]*\(/) exit
      auth = "none"
      if (code ~ /[{][[:space:]]*user_passwords[[:space:]]*,[[:space:]]*\[/) auth = "literal_user_passwords"
      else if (code ~ /[{][[:space:]]*(user_passwords|password|pwdfun|user_dir_fun|publickey|public_key)[[:space:]]*,/) auth = "auth_option"
      shell = "unknown"
      if (code ~ /[{][[:space:]]*shell[[:space:]]*,[[:space:]]*disabled/ || code ~ /[{][[:space:]]*ssh_cli[[:space:]]*,[[:space:]]*no_cli/) shell = "disabled_marker"
      else if (code ~ /[{][[:space:]]*(shell|ssh_cli)[[:space:]]*,/) shell = "explicit_option"
      print auth, shell
    }' "$1" 2>/dev/null
}

lp_check_erlang_ssh_daemons() {
  # Optional arguments let focused fixtures supply a ps snapshot and proc root.
  local snapshot="$1" proc_root="${2:-/proc}" ps_rows pid uid command source physical_dir canonical size markers auth shell
  local candidates=0 files=0 printed=0 unknowns=0 seen_files='|'
  if [ -n "$snapshot" ]; then
    [ -r "$snapshot" ] || return 0
    ps_rows=$(cat "$snapshot" 2>/dev/null)
  else
    ps_rows=$(ps -eo pid=,uid=,args= 2>/dev/null) || ps_rows=$(ps -axo pid=,uid=,command= 2>/dev/null) || return 0
  fi
  [ -n "$ps_rows" ] || return 0
  while read -r pid uid command; do
    case "$pid:$uid" in *[!0-9:]*|:*|*:) continue ;; esac
    case "$command" in
      erl|erlang|escript|beam|beam.smp|*/erl|*/erlang|*/escript|*/beam|*/beam.smp|erl\ *|erlang\ *|escript\ *|beam\ *|beam.smp\ *|*/erl\ *|*/erlang\ *|*/escript\ *|*/beam\ *|*/beam.smp\ *) ;;
      *) continue ;;
    esac
    candidates=$((candidates + 1))
    [ "$candidates" -le 64 ] || break
    source=''
    if [ -r "$proc_root/$pid/cmdline" ]; then
      source=$(lp_erlang_ssh_source_from_proc "$proc_root/$pid/cmdline")
    fi
    [ -n "$source" ] || source=$(lp_erlang_ssh_source_from_ps "$command")
    if [ -z "$source" ]; then
      if [ "$unknowns" -lt 8 ]; then
        [ "$printed" -eq 1 ] || { print_2title 'Running Erlang SSH source candidates' 'T1552.001'; printed=1; }
        printf 'Candidate: Erlang PID %s UID %s; absolute launched source unavailable (SSH status unknown).\n' "$pid" "$uid"
        unknowns=$((unknowns + 1))
      fi
      continue
    fi
    # Resolve parent directories physically; refuse a symlink at the final component.
    case "$source" in /*.escript|/*.erl) ;; *) continue ;; esac
    if [ -L "$source" ]; then
      canonical=''
    else
      physical_dir=$(cd -P "${source%/*}" 2>/dev/null && pwd -P) || physical_dir=''
      canonical="$physical_dir/${source##*/}"
    fi
    if [ -z "$physical_dir" ] || [ ! -f "$canonical" ] || [ ! -r "$canonical" ]; then
      if [ "$unknowns" -lt 8 ]; then
        [ "$printed" -eq 1 ] || { print_2title 'Running Erlang SSH source candidates' 'T1552.001'; printed=1; }
        printf 'Candidate: Erlang PID %s UID %s; launched source inaccessible or symlinked: %s (SSH status unknown).\n' "$pid" "$uid" "$source"
        unknowns=$((unknowns + 1))
      fi
      continue
    fi
    case "$seen_files" in
      *"|$canonical|"*) ;;
      *) seen_files="$seen_files$canonical|"
         files=$((files + 1))
         [ "$files" -le 32 ] || break ;;
    esac
    size=$(wc -c < "$canonical" 2>/dev/null) || continue
    case "$size" in ''|*[!0-9]*) continue ;; esac
    if [ "$size" -gt 131072 ]; then
      if [ "$unknowns" -lt 8 ]; then
        [ "$printed" -eq 1 ] || { print_2title 'Running Erlang SSH source candidates' 'T1552.001'; printed=1; }
        printf 'Candidate: Erlang PID %s UID %s; launched source exceeds 128 KiB: %s (SSH status unknown).\n' "$pid" "$uid" "$canonical"
        unknowns=$((unknowns + 1))
      fi
      continue
    fi
    markers=$(lp_erlang_ssh_markers "$canonical")
    [ -n "$markers" ] || continue
    auth=${markers%% *}
    shell=${markers#* }
    [ "$auth" != none ] || [ "$shell" != unknown ] || continue
    [ "$printed" -eq 1 ] || { print_2title 'Running Erlang SSH source candidates' 'T1552.001'; printed=1; }
    printf 'Candidate: Erlang SSH PID %s UID %s source %s; auth=%s, shell=%s.\n' "$pid" "$uid" "$canonical" "$auth" "$shell"
  done <<EOF
$ps_rows
EOF
  if [ "$printed" -eq 1 ]; then
    printf '%s\n' 'Review the live daemon options and OTP version: readable source markers do not prove accepted credentials or an Erlang shell. No authentication was attempted.'
    echo ''
  fi
}

if ! [ "$SEARCH_IN_FOLDER" ]; then
  lp_check_erlang_ssh_daemons
fi
