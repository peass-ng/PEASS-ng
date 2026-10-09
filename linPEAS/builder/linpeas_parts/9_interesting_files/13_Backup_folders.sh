# Title: Interesting Files - Backup folders
# ID: IF_Backup_folders
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: Backup folders and readable root-owned archives in their immediate entries
# License: GNU GPL
# Version: 1.1
# Mitre: T1552.001
# Functions Used: print_2title
# Global Variables: $DEBUG, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $entry, $backup_uid, $backup_groups, $archive_path, $archive_metadata, $archive_mode, $archive_owner, $archive_group, $archive_size, $archive_access
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ "$PSTORAGE_BACKUPS" ] || [ "$DEBUG" ]; then
    backup_uid=$(id -u 2>/dev/null)
    backup_groups=$(id -G 2>/dev/null)
    report_readable_root_archive() (
      archive_path=$1
      [ -n "$backup_uid" ] && [ "$backup_uid" != 0 ] || exit
      [ -f "$archive_path" ] && [ ! -L "$archive_path" ] && [ -r "$archive_path" ] || exit
      case "$archive_path" in
        *.tar|*.tar.gz|*.tgz|*.tar.bz2|*.tbz2|*.tar.xz|*.txz|*.tar.zst|*.zip|*.7z) ;;
        *) exit ;;
      esac
      archive_metadata=$(LC_ALL=C ls -ldn "$archive_path" 2>/dev/null | awk '{print $1, $3, $4, $5}')
      set -- $archive_metadata
      [ "$#" -eq 4 ] && [ "$2" = 0 ] || exit
      archive_mode=$1 archive_owner=$2 archive_group=$3 archive_size=$4
      case "$archive_mode" in
        ???????r*) archive_access=world-readable ;;
        ????r*)
          case " $backup_groups " in
            *" $archive_group "*) archive_access=group-readable ;;
            *) exit ;;
          esac ;;
        *) exit ;;
      esac
      printf '  Possible exposure: readable root-owned backup archive: %s (owner=%s group=%s mode=%s size=%s access=%s)\n' \
        "$archive_path" "$archive_owner" "$archive_group" "$archive_mode" "$archive_size" "$archive_access"
    )
    print_2title "Backup folders" "T1552.001"
    printf "%s\n" "$PSTORAGE_BACKUPS" | while IFS= read -r b; do
      [ -d "$b" ] && [ -r "$b" ] && [ -x "$b" ] || continue
      ls -ld "$b" 2>/dev/null | sed -${E} "s,backups|backup,${SED_RED},g"
      # Prune each child: list immediate entries only and cap metadata calls.
      find "$b" ! -path "$b" -prune -print 2>/dev/null | head -n 30 |
        while IFS= read -r entry; do
          ls -ld "$entry" 2>/dev/null
          report_readable_root_archive "$entry"
        done
      echo ""
    done
    echo ""
  fi
fi
