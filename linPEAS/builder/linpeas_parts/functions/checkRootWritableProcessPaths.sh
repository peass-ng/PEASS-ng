# Title: Function - checkRootWritableProcessPaths
# ID: checkRootWritableProcessPaths
# Author: PEASS-ng
# Last Update: 2026-10-09
# Description: Passively correlate observed UID 0 commands with modifiable absolute executable or interpreter script paths.
# License: GNU GPL
# Version: 1.0
# Mitre: T1574
# Functions Used: print_3title
# Global Variables:
# Initial Functions:
# Generated Global Variables: $rwpp_proc_root, $rwpp_uid, $rwpp_seen, $rwpp_found, $rwpp_entry, $rwpp_pid, $rwpp_effective_uid, $rwpp_kind, $rwpp_path, $rwpp_cmd, $rwpp_parent, $rwpp_reason, $rwpp_parent_uid, $rwpp_entry_uid
# Fat linpeas: 0
# Small linpeas: 1

checkRootWritableProcessPaths() {
  [ "$(uname -s 2>/dev/null)" = Linux ] || return 0
  rwpp_uid=$(id -u 2>/dev/null) || return 0
  [ "$rwpp_uid" != 0 ] || return 0
  rwpp_proc_root=${1:-/proc}
  [ -d "$rwpp_proc_root" ] || return 0
  rwpp_seen=0
  rwpp_found=0

  for rwpp_entry in "$rwpp_proc_root"/[0-9]*; do
    [ -d "$rwpp_entry" ] || continue
    rwpp_pid=${rwpp_entry##*/}
    case "$rwpp_pid" in *[!0-9]*|'') continue ;; esac
    rwpp_seen=$((rwpp_seen + 1))
    [ "$rwpp_seen" -le 200 ] || break
    [ -r "$rwpp_entry/status" ] && [ -r "$rwpp_entry/cmdline" ] || continue
    rwpp_effective_uid=$(awk '$1 == "Uid:" { print $3; exit }' "$rwpp_entry/status" 2>/dev/null)
    [ "$rwpp_effective_uid" = 0 ] || continue

    # od preserves NUL argument boundaries. Reading 1025 bytes lets awk reject
    # a truncated command instead of interpreting an incomplete script operand.
    while IFS='|' read -r rwpp_kind rwpp_path rwpp_cmd; do
      [ -f "$rwpp_path" ] || continue
      rwpp_reason=''
      if [ -w "$rwpp_path" ]; then
        rwpp_reason='file writable'
      fi
      rwpp_parent=${rwpp_path%/*}
      [ -n "$rwpp_parent" ] || rwpp_parent=/
      if [ -d "$rwpp_parent" ] && [ -w "$rwpp_parent" ] && [ -x "$rwpp_parent" ]; then
        # For sticky directories either the directory or the entry must belong
        # to the caller. stat without -L gets the entry owner for symlinks.
        if [ -k "$rwpp_parent" ]; then
          rwpp_parent_uid=$(stat -Lc '%u' "$rwpp_parent" 2>/dev/null)
          rwpp_entry_uid=$(stat -c '%u' "$rwpp_path" 2>/dev/null)
          if [ "$rwpp_parent_uid" = "$rwpp_uid" ] || [ "$rwpp_entry_uid" = "$rwpp_uid" ]; then
            rwpp_reason="${rwpp_reason:+$rwpp_reason; }parent allows replacement"
          fi
        else
          rwpp_reason="${rwpp_reason:+$rwpp_reason; }parent allows replacement"
        fi
      fi
      [ -n "$rwpp_reason" ] || continue
      if [ "$rwpp_found" -eq 0 ]; then
        print_3title 'Observed UID 0 process paths modifiable by current user' 'T1574'
      fi
      printf 'PID %s: %s | %s: %s (%s)\n' "$rwpp_pid" "$rwpp_cmd" "$rwpp_kind" "$rwpp_path" "$rwpp_reason"
      rwpp_found=$((rwpp_found + 1))
      [ "$rwpp_found" -lt 12 ] || break
    done <<EOF
$(dd if="$rwpp_entry/cmdline" bs=1025 count=1 2>/dev/null | od -An -v -tu1 | awk '
  {
    for (i = 1; i <= NF; i++) {
      byte = $i + 0; count++
      if (count > 1024) invalid = 1
      if (byte == 0) {
        argc++; arg[argc] = current; current = ""; ended = 1
      } else {
        ended = 0
        if (byte < 32 || byte > 126) invalid = 1
        current = current sprintf("%c", byte)
      }
    }
  }
  END {
    if (invalid || !ended || !argc) exit
    exe = arg[1]
    name = exe; sub(/^.*\//, "", name)
    command = exe
    for (i = 2; i <= argc && i <= 3; i++) command = command " " arg[i]
    gsub(/\|/, "?", command)
    if (exe ~ /^\// && exe !~ /\|/) print "executable|" exe "|" command
    if (name ~ /^(sh|bash|dash|ash|ksh|zsh|perl|ruby|php)$/ || name ~ /^python([23]([.][0-9]+)?)?$/) {
      i = 2
      if (arg[i] == "--") i++
      if (arg[i] ~ /^\// && arg[i] !~ /\|/) print "script|" arg[i] "|" command
    }
  }')
EOF
    [ "$rwpp_found" -lt 12 ] || break
  done
  [ "$rwpp_found" -eq 0 ] || echo ''
}
