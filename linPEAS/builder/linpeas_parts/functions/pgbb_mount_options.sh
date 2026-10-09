# Title: Function - pgbb_mount_options
# ID: pgbb_mount_options
# Author: PEASS-ng
# Last Update: 2026-10-09
# Description: Read the effective Linux mount options for a literal path.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

pgbb_mount_options() {
  [ -r /proc/mounts ] || return 1
  awk -v path="$1" '
    $2 !~ /\\\\/ && (path == $2 || $2 == "/" || index(path, $2 "/") == 1) {
      if (length($2) > best) { best=length($2); opts=$4 }
    }
    END { if (best) print opts }
  ' /proc/mounts
}
