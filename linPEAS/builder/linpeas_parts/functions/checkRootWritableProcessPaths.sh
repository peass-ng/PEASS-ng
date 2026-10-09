# Title: Function - checkRootWritableProcessPaths
# ID: checkRootWritableProcessPaths
# Author: PEASS-ng
# Last Update: 2026-10-09
# Description: Passively correlate observed UID 0 commands with modifiable executable, script, or literal PHP include paths.
# License: GNU GPL
# Version: 1.0
# Mitre: T1574
# Functions Used: print_3title
# Global Variables:
# Initial Functions:
# Generated Global Variables: $rwpp_proc_root, $rwpp_uid, $rwpp_seen, $rwpp_found, $rwpp_php_seen, $rwpp_entry, $rwpp_pid, $rwpp_effective_uid, $rwpp_kind, $rwpp_path, $rwpp_cmd, $rwpp_parent, $rwpp_reason, $rwpp_parent_uid, $rwpp_entry_uid, $rwpp_php_base, $rwpp_php_script, $rwpp_php_pid, $rwpp_php_lines, $rwpp_php_relative, $rwpp_php_target, $rwpp_php_walk, $rwpp_php_rest, $rwpp_php_safe, $rwpp_php_part, $rwpp_php_found
# Fat linpeas: 0
# Small linpeas: 1

# Inspect literal includes in root PHP consumers. Do not follow dynamic
# expressions, symlinks, parent traversal, or more than four consumers and
# eight 8 KiB source prefixes per document root.
rwpp_php_literal_includes() (
  rwpp_php_base=$1
  rwpp_php_script=$2
  rwpp_php_pid=$3
  case "$rwpp_php_base:$rwpp_php_script" in *[!A-Za-z0-9_./:-]*) exit 0 ;; esac
  case "$rwpp_php_base:$rwpp_php_script" in *'//'*|*'/./'*|*'/../'*|*'/.'|*'/..') exit 0 ;; esac
  [ -f "$rwpp_php_script" ] && [ -r "$rwpp_php_script" ] || exit 0
  [ -L "$rwpp_php_script" ] && exit 0
  rwpp_php_lines=$(LC_ALL=C dd if="$rwpp_php_script" bs=8192 count=1 2>/dev/null | sed -n '1,64p' |
    sed -nE "s/^[[:space:]]*(include|include_once|require|require_once)[[:space:]]*\\(?[[:space:]]*['\"]([A-Za-z0-9_./-]+\\.php)['\"][[:space:]]*\\)?[[:space:]]*;.*/\\2/p" |
    sed -n '1,4p')
  [ -n "$rwpp_php_lines" ] || exit 0
  rwpp_php_found=0
  for rwpp_php_relative in $rwpp_php_lines; do
    case "$rwpp_php_relative" in *'//'*|*'/./'*|*'/../'*|*'/.'|*'/..'|/*) continue ;; esac
    rwpp_php_target=${rwpp_php_script%/*}/$rwpp_php_relative
    [ -f "$rwpp_php_target" ] && [ -w "$rwpp_php_target" ] || continue
    rwpp_php_walk=''
    rwpp_php_rest=${rwpp_php_target#/}
    rwpp_php_safe=1
    while [ -n "$rwpp_php_rest" ]; do
      rwpp_php_part=${rwpp_php_rest%%/*}
      rwpp_php_walk=$rwpp_php_walk/$rwpp_php_part
      if [ -L "$rwpp_php_walk" ]; then rwpp_php_safe=0; break; fi
      case "$rwpp_php_rest" in */*) rwpp_php_rest=${rwpp_php_rest#*/} ;; *) rwpp_php_rest='' ;; esac
    done
    [ "$rwpp_php_safe" -eq 1 ] || continue
    printf 'PID %s: root PHP source %s includes writable PHP file %s (literal path; execution path and access policy require review)\n' \
      "$rwpp_php_pid" "$rwpp_php_script" "$rwpp_php_target"
    rwpp_php_found=$((rwpp_php_found + 1))
    [ "$rwpp_php_found" -lt 2 ] || break
  done
)

checkRootWritableProcessPaths() {
  [ "$(uname -s 2>/dev/null)" = Linux ] || return 0
  rwpp_uid=$(id -u 2>/dev/null) || return 0
  [ "$rwpp_uid" != 0 ] || return 0
  rwpp_proc_root=${1:-/proc}
  [ -d "$rwpp_proc_root" ] || return 0
  rwpp_seen=0
  rwpp_found=0
  rwpp_php_seen=0

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
      case "$rwpp_kind" in
        phpdocroot)
          [ "$rwpp_php_seen" -lt 4 ] && [ -d "$rwpp_path" ] && [ ! -L "$rwpp_path" ] || continue
          rwpp_php_seen=$((rwpp_php_seen + 1))
          find "$rwpp_path" -maxdepth 1 -type f -name '*.php' -print 2>/dev/null | head -n 8 |
          while IFS= read -r rwpp_php_script; do
            rwpp_php_literal_includes "$rwpp_path" "$rwpp_php_script" "$rwpp_pid"
          done
          continue ;;
        phpscript)
          [ "$rwpp_php_seen" -lt 4 ] || continue
          rwpp_php_seen=$((rwpp_php_seen + 1))
          rwpp_php_literal_includes "${rwpp_path%/*}" "$rwpp_path" "$rwpp_pid"
          continue ;;
      esac
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
    if (name == "php" && exe ~ /^\//) {
      if (arg[2] == "-S" && arg[4] == "-t" && arg[5] ~ /^\/[A-Za-z0-9_.\/-]+$/)
        print "phpdocroot|" arg[5] "|" command
      else if (arg[2] == "-f" && arg[3] ~ /^\/[A-Za-z0-9_.\/-]+\.php$/)
        print "phpscript|" arg[3] "|" command
      else if (arg[2] ~ /^\/[A-Za-z0-9_.\/-]+\.php$/)
        print "phpscript|" arg[2] "|" command
    }
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
