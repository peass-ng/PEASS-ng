# Title: Function - check_privileged_file_location
# ID: check_privileged_file_location
# Author: HT Bot
# Last Update: 30-09-2026
# Description: Report risky placement, ownership, and parent permissions for SUID and SGID files.
# License: GNU GPL
# Version: 1.0
# Functions Used: pgbb_mount_options
# Global Variables: $E, $IAMROOT, $PG_BASEBACKUP_DESTS, $ROOT_FOLDER, $SED_RED, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $priv_file_name, $priv_file_owner, $priv_file_parent, $priv_file_type, $priv_file_unusual, $priv_backup_dest, $priv_backup_opts
# Fat linpeas: 0
# Small linpeas: 1


check_privileged_file_location() {
  priv_file_type="$1"
  priv_file_name="$2"
  priv_file_owner="$3"

  priv_file_unusual=""
  case "$priv_file_name" in
    "${ROOT_FOLDER}tmp/"*|"${ROOT_FOLDER}var/tmp/"*|"${ROOT_FOLDER}dev/shm/"*|"${ROOT_FOLDER}run/user/"*|"${ROOT_FOLDER}var/run/user/"*|"${ROOT_FOLDER}home/"*)
      echo "$priv_file_type file in a user-writable or unusual location: $priv_file_name" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      priv_file_unusual="1"
      ;;
  esac

  if [ "$priv_file_owner" = root ] && [ -f "$priv_file_name" ] && [ ! -L "$priv_file_name" ] && [ -x "$priv_file_name" ]; then
    while IFS= read -r priv_backup_dest; do
      [ -n "$priv_backup_dest" ] || continue
      case "$priv_file_name" in
        "$priv_backup_dest"/*)
          priv_backup_opts=$(pgbb_mount_options "$priv_file_name")
          case ",$priv_backup_opts," in ,,|*,nosuid,*|*,noexec,*) continue ;; esac
          echo "$priv_file_type root-owned file under a reviewed PostgreSQL backup destination: $priv_file_name" | sed -${E} "s,.*,${SED_RED_YELLOW},"
          priv_file_unusual=1
          break
          ;;
      esac
    done <<EOF_PRIV_BACKUP_DESTS
$PG_BASEBACKUP_DESTS
EOF_PRIV_BACKUP_DESTS
  fi

  priv_file_parent="$(dirname "$priv_file_name")"
  if ! [ "$IAMROOT" ] && [ -d "$priv_file_parent" ] && [ -w "$priv_file_parent" ] && [ -x "$priv_file_parent" ] && ! [ -k "$priv_file_parent" ]; then
    echo "You can replace entries in the $priv_file_type file's parent directory: $priv_file_parent" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  fi
  if [ "$priv_file_owner" ] && [ "$priv_file_owner" != "root" ]; then
    echo "$priv_file_type file is owned by non-root user $priv_file_owner: $priv_file_name" | sed -${E} "s,.*,${SED_RED},"
  fi
  if [ "$priv_file_unusual" ] && find "$priv_file_name" -mtime -7 -print 2>/dev/null | grep -q .; then
    echo "$priv_file_type file in an unusual location was modified in the last 7 days: $priv_file_name" | sed -${E} "s,.*,${SED_RED},"
  fi
}
