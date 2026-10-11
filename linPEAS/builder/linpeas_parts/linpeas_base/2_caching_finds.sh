# Title: LinPeasBase - Caching finds
# ID: BS_caching_finds
# Author: Carlos Polop, HT Bot
# Last Update: 11-10-2026
# Description: Cache interesting files discovered in the file system
# License: GNU GPL
# Version: 1.1
# Functions Used:
# Global Variables: $CHECKS, $GREEN, $NC, $SEARCH_IN_FOLDER, $THREADS, $TMPDIR, $YELLOW
# Initial Functions:
# Generated Global Variables: $CONT_THREADS, $FIND_CACHE_DIR, $cache_find_name, $backup_folders_row, $find_cache_required
# Fat linpeas: 0
# Small linpeas: 1

cache_find() {
  cache_find_name=$1
  shift
  (find "$@" 2>/dev/null | sort > "$FIND_CACHE_DIR/$cache_find_name"; printf '%s' "$YELLOW. $NC" >&2) &
  CONT_THREADS=$((CONT_THREADS + 1))
  if [ $((CONT_THREADS % THREADS)) -eq 0 ]; then wait; fi
}

find_cache_required=
if [ "$SEARCH_IN_FOLDER" ]; then
  find_cache_required=1
  printf $GREEN"Caching directories "$NC

  CONT_THREADS=0
  FIND_CACHE_DIR=$(mktemp -d "${TMPDIR:-/tmp}/linpeas-finds.XXXXXX") || exit 1
  trap 'rm -rf "$FIND_CACHE_DIR"' 0
  # FIND ALL KNOWN INTERESTING SOFTWARE FILES
  peass{FINDS_CUSTOM}

  wait # Always wait at the end
  CONT_THREADS=0 #Reset the threads counter

else
  case "$CHECKS" in
    *procs_crons_timers_srvcs_sockets*|*software_information*|*interesting_files*) find_cache_required=1 ;;
  esac
fi

if [ "$find_cache_required" ] && ! [ "$SEARCH_IN_FOLDER" ]; then

  printf $GREEN"Caching directories "$NC

  CONT_THREADS=0
  FIND_CACHE_DIR=$(mktemp -d "${TMPDIR:-/tmp}/linpeas-finds.XXXXXX") || exit 1
  trap 'rm -rf "$FIND_CACHE_DIR"' 0
  # FIND ALL KNOWN INTERESTING SOFTWARE FILES
  peass{FINDS_HERE}

  wait # Always wait at the end
  CONT_THREADS=0 #Reset the threads counter
fi

if [ "$find_cache_required" ]; then
  #GENERATE THE STORAGES OF THE FOUND FILES
  peass{STORAGES_HERE}

  ##### POST SERACH VARIABLES #####
  backup_folders_row="$(echo $PSTORAGE_BACKUPS | tr '\n' ' ')"
  printf ${YELLOW}"DONE\n"$NC
  echo ""
fi
