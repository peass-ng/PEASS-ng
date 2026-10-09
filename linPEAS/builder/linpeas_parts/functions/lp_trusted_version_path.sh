# Title: Function - lp_trusted_version_path
# ID: lp_trusted_version_path
# Author: PEASS-ng contributors
# Last Update: 09-10-2026
# Description: Resolve configuration-supplied version probes to protected root-owned executables.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $lp_version_path, $lp_version_uid, $lp_version_walk, $lp_version_count
# Fat linpeas: 0
# Small linpeas: 1

lp_trusted_version_path() { (
  # Never execute a program solely because a readable configuration names it.
  # Execute the checked canonical path, not a replaceable symlink spelling.
  case "$1" in /*) ;; *) exit 1 ;; esac
  lp_version_path=$(readlink -f "$1" 2>/dev/null) ||
    lp_version_path=$(realpath "$1" 2>/dev/null) || exit 1
  case "$lp_version_path" in /*) ;; *) exit 1 ;; esac
  [ "${#lp_version_path}" -le 4096 ] &&
    [ -f "$lp_version_path" ] && [ -x "$lp_version_path" ] || exit 1
  lp_version_uid=$(id -u 2>/dev/null) || exit 1
  lp_version_walk=$lp_version_path
  lp_version_count=0
  while :; do
    lp_version_count=$((lp_version_count + 1))
    [ "$lp_version_count" -le 64 ] && [ ! -L "$lp_version_walk" ] || exit 1
    # Numeric ownership and symbolic mode work on GNU, BSD and BusyBox ls.
    LC_ALL=C ls -ldn "$lp_version_walk" 2>/dev/null | awk '
      NR == 1 && $3 == "0" && length($1) >= 10 &&
          substr($1, 6, 1) != "w" && substr($1, 9, 1) != "w" { safe = 1 }
      END { exit !safe }
    ' || exit 1
    if [ "$lp_version_uid" != 0 ] && [ -w "$lp_version_walk" ]; then exit 1; fi
    [ "$lp_version_walk" != / ] || break
    lp_version_walk=${lp_version_walk%/*}
    [ -n "$lp_version_walk" ] || lp_version_walk=/
  done
  printf '%s\n' "$lp_version_path"
) }
