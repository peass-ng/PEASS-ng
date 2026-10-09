# Title: Function - check_pg_basebackup_boundary
# ID: check_pg_basebackup_boundary
# Author: PEASS-ng
# Last Update: 2026-10-09
# Description: Correlate bounded root cron backups with writable PostgreSQL data and executable output.
# License: GNU GPL
# Version: 1.0
# Functions Used: pgbb_mount_options
# Global Variables: $PG_BASEBACKUP_DESTS
# Initial Functions:
# Generated Global Variables: $pgbb_command, $pgbb_count, $pgbb_cron, $pgbb_depth, $pgbb_dest, $pgbb_dest_meta, $pgbb_file, $pgbb_file_count, $pgbb_host, $pgbb_line, $pgbb_line_count, $pgbb_meta, $pgbb_opts, $pgbb_parent, $pgbb_path, $pgbb_size, $pgbb_source, $pgbb_source_meta, $pgbb_total, $pgbb_wrapper
# Fat linpeas: 0
# Small linpeas: 1

pgbb_metadata() {
  stat -c '%u %a' "$1" 2>/dev/null || stat -f '%u %Lp' "$1" 2>/dev/null
}

pgbb_literal_path() {
  [ "${#1}" -le 512 ] || return 1
  case "$1" in
    /*) case "$1" in *[!a-zA-Z0-9_./+-]*|*//*|*/../*|*/./*|*/..|*/.) return 1;; esac ;;
    *) return 1 ;;
  esac
  pgbb_path=$1
  pgbb_depth=0
  while [ "$pgbb_path" != / ]; do
    pgbb_depth=$((pgbb_depth + 1))
    [ "$pgbb_depth" -le 32 ] || return 1
    pgbb_path=${pgbb_path%/*}
    [ -n "$pgbb_path" ] || pgbb_path=/
  done
  return 0
}

pgbb_no_symlinks() {
  pgbb_path=$1
  while [ "$pgbb_path" != / ] && [ "${pgbb_path%/}" != "$pgbb_path" ]; do pgbb_path=${pgbb_path%/}; done
  while [ "$pgbb_path" != / ]; do
    [ ! -L "$pgbb_path" ] || return 1
    pgbb_path=${pgbb_path%/*}
    [ -n "$pgbb_path" ] || pgbb_path=/
  done
}

pgbb_parse_command() (
  case "$1" in
    *\$*|*\`*|*\;*|*\&*|*\|*|*\(*|*\)*|*\<*|*\>*|*\\*|*\"*|*\'*|*\**|*\?*|*\[*|*\]*) return 1 ;;
  esac
  set -f
  set -- $1
  [ "$1" = exec ] && shift
  case "$1" in /usr/bin/pg_basebackup|/usr/local/bin/pg_basebackup|pg_basebackup) shift ;; *) return 1 ;; esac
  pgbb_dest=
  pgbb_host=
  while [ "$#" -gt 0 ]; do
    case "$1" in
      -D|--pgdata) shift; [ "$#" -gt 0 ] || return 1; [ -z "$pgbb_dest" ] || return 1; pgbb_dest=$1 ;;
      --pgdata=*) [ -z "$pgbb_dest" ] || return 1; pgbb_dest=${1#*=} ;;
      -h|--host) shift; [ "$#" -gt 0 ] || return 1; [ -z "$pgbb_host" ] || return 1; pgbb_host=$1 ;;
      --host=*) [ -z "$pgbb_host" ] || return 1; pgbb_host=${1#*=} ;;
      -d|--dbname|--dbname=*) return 1 ;;
      -F|--format) shift; [ "$1" = p ] || [ "$1" = plain ] || return 1 ;;
      --format=plain|--format=p) ;;
      -Fp) ;;
      -F*|--format=*) return 1 ;;
      -D*) return 1 ;;
    esac
    shift
  done
  [ -n "$pgbb_dest" ] || return 1
  pgbb_literal_path "$pgbb_dest" || return 1
  pgbb_literal_path "$pgbb_host" || return 1
  while [ "$pgbb_dest" != / ] && [ "${pgbb_dest%/}" != "$pgbb_dest" ]; do pgbb_dest=${pgbb_dest%/}; done
  [ "$pgbb_dest" != / ] || return 1
  printf '%s\n' "$pgbb_dest"
)

pgbb_source_from_process() {
  ps -eo args 2>/dev/null | awk '
    NR > 256 { exit }
    { for (i=1; i<NF-1; i++) if ($i ~ /(^|\/)postgres$/ && $(i+1)=="-D" && $(i+2) ~ /^\/[a-zA-Z0-9_.\/+\-]+$/) { found++; source=$(i+2) } }
    END { if (found == 1) print source }
  '
}

pgbb_review_job() {
  pgbb_cron=$1
  pgbb_command=$2
  pgbb_source=$3
  pgbb_dest=$(pgbb_parse_command "$pgbb_command") || {
    printf 'PostgreSQL base-backup review lead: %s: literal plain destination could not be established\n' "$pgbb_cron"
    return 0
  }
  pgbb_literal_path "$pgbb_source" || return 0
  pgbb_no_symlinks "$pgbb_source" || return 0
  pgbb_no_symlinks "$pgbb_dest" || {
    printf 'PostgreSQL base-backup review lead: %s: destination contains a symlink\n' "$pgbb_cron"
    return 0
  }
  [ -d "$pgbb_source" ] && [ -w "$pgbb_source" ] && [ -x "$pgbb_source" ] || return 0
  pgbb_source_meta=$(pgbb_metadata "$pgbb_source") || return 0
  pgbb_parent=$pgbb_dest
  if [ -d "$pgbb_dest" ]; then
    [ -x "$pgbb_dest" ] && [ ! -w "$pgbb_dest" ] || return 0
    pgbb_dest_meta=$(pgbb_metadata "$pgbb_dest") || return 0
  elif [ ! -e "$pgbb_dest" ]; then
    pgbb_parent=${pgbb_dest%/*}
    [ -n "$pgbb_parent" ] || pgbb_parent=/
    [ -d "$pgbb_parent" ] && [ -x "$pgbb_parent" ] && [ ! -w "$pgbb_parent" ] || return 0
    pgbb_dest_meta=$(pgbb_metadata "$pgbb_parent") || return 0
    [ "${pgbb_dest_meta%% *}" = 0 ] || return 0
    printf 'PostgreSQL base-backup review lead: %s: output does not exist; resulting directory mode cannot be confirmed\n' "$pgbb_cron"
    return 0
  else
    return 0
  fi
  [ "${pgbb_dest_meta%% *}" = 0 ] || return 0
  pgbb_path=$pgbb_parent
  while [ "$pgbb_path" != / ]; do
    [ -x "$pgbb_path" ] || return 0
    pgbb_path=${pgbb_path%/*}
    [ -n "$pgbb_path" ] || pgbb_path=/
  done
  pgbb_opts=$(pgbb_mount_options "$pgbb_dest")
  case ",$pgbb_opts," in
    ,,) printf 'PostgreSQL base-backup review lead: %s: destination mount options unknown\n' "$pgbb_cron"; return 0 ;;
    *,nosuid,*|*,noexec,*|*,ro,*) return 0 ;;
  esac
  case ",$pgbb_opts," in *,rw,*) : ;; *) return 0 ;; esac
  printf 'PostgreSQL base-backup review candidate: %s\n' "$pgbb_cron"
  printf '  command: %s\n  writable source: %s (uid mode %s)\n  root output: %s (uid mode %s; mount %s)\n' "$pgbb_command" "$pgbb_source" "$pgbb_source_meta" "$pgbb_dest" "$pgbb_dest_meta" "$pgbb_opts"
  printf '  reason: root plain-format backup may copy lower-trust files and set-ID modes into an executable path; verify mode preservation\n'
  PG_BASEBACKUP_DESTS="${PG_BASEBACKUP_DESTS}${PG_BASEBACKUP_DESTS:+
}$pgbb_dest"
}

pgbb_scan_file() {
  pgbb_file=$1
  pgbb_source=$2
  [ -f "$pgbb_file" ] && [ -r "$pgbb_file" ] && [ ! -L "$pgbb_file" ] && [ ! -w "$pgbb_file" ] || return 0
  pgbb_meta=$(pgbb_metadata "$pgbb_file") || return 0
  [ "${pgbb_meta%% *}" = 0 ] || return 0
  pgbb_size=$(stat -c %s "$pgbb_file" 2>/dev/null || stat -f %z "$pgbb_file" 2>/dev/null) || return 0
  case "$pgbb_size" in ''|*[!0-9]*) return 0 ;; esac
  [ "$pgbb_size" -le 65536 ] || return 0
  pgbb_line_count=0
  while IFS= read -r pgbb_line; do
    pgbb_line_count=$((pgbb_line_count + 1))
    [ "$pgbb_line_count" -le 200 ] || break
    [ "${#pgbb_line}" -le 1024 ] || continue
    case "$pgbb_line" in ''|\#*|[[:space:]]\#*) continue ;; esac
    pgbb_total=$((pgbb_total + 1))
    [ "$pgbb_total" -le 400 ] || return 0
    set -f
    set -- $pgbb_line
    set +f
    case "$pgbb_file" in
      */cron.d/*|/etc/crontab) [ "$#" -ge 7 ] && [ "$6" = root ] || continue; shift 6 ;;
      */crontabs/root|*/cron/root) [ "$#" -ge 6 ] || continue; shift 5 ;;
      /etc/anacrontab) [ "$#" -ge 4 ] || continue; shift 3 ;;
      *) continue ;;
    esac
    pgbb_command="$*"
    case "$pgbb_command" in *pg_basebackup*) : ;; *)
      set -- $pgbb_command
      case "$1" in /bin/sh|/bin/bash|/usr/bin/sh|/usr/bin/bash) shift ;; esac
      [ "$#" -eq 1 ] || continue
      pgbb_wrapper=$1
      pgbb_literal_path "$pgbb_wrapper" || continue
      pgbb_no_symlinks "$pgbb_wrapper" || continue
      [ -f "$pgbb_wrapper" ] && [ -r "$pgbb_wrapper" ] && [ ! -L "$pgbb_wrapper" ] && [ ! -w "$pgbb_wrapper" ] || continue
      pgbb_meta=$(pgbb_metadata "$pgbb_wrapper") || continue
      [ "${pgbb_meta%% *}" = 0 ] || continue
      pgbb_size=$(stat -c %s "$pgbb_wrapper" 2>/dev/null || stat -f %z "$pgbb_wrapper" 2>/dev/null) || continue
      case "$pgbb_size" in ''|*[!0-9]*) continue ;; esac
      [ "$pgbb_size" -le 8192 ] || continue
      pgbb_command=$(head -c 8192 "$pgbb_wrapper" 2>/dev/null | awk 'NR<=80 && /(^|[[:space:]\/])pg_basebackup([[:space:]]|$)/ { print; exit }')
      [ -n "$pgbb_command" ] || continue
      ;;
    esac
    pgbb_count=$((pgbb_count + 1))
    [ "$pgbb_count" -le 20 ] || return 0
    pgbb_review_job "$pgbb_file:$pgbb_line_count" "$pgbb_command" "$pgbb_source"
  done < "$pgbb_file"
}

pgbb_scan_files() {
  pgbb_source=$1
  shift
  pgbb_count=0
  pgbb_total=0
  pgbb_file_count=0
  for pgbb_file do
    pgbb_file_count=$((pgbb_file_count + 1))
    [ "$pgbb_file_count" -le 64 ] || break
    pgbb_scan_file "$pgbb_file" "$pgbb_source"
    [ "$pgbb_count" -lt 20 ] && [ "$pgbb_total" -lt 400 ] || break
  done
}

check_pg_basebackup_boundary() {
  [ "$(id -u 2>/dev/null)" != 0 ] || return 0
  pgbb_source=$(pgbb_source_from_process)
  [ -n "$pgbb_source" ] || return 0
  pgbb_scan_files "$pgbb_source" /etc/crontab /var/spool/cron/crontabs/root /var/spool/cron/root /etc/anacrontab /etc/cron.d/*
}
