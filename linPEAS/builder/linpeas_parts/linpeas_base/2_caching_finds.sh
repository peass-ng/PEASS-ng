# Title: Interesting Files - Check if Network jobs
# ID: BS_caching_finds
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Cache interesting files discoevred in the file system
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $CHECKS, $SEARCH_IN_FOLDER, $TMPDIR
# Initial Functions:
# Generated Global Variables: $CONT_THREADS, $FIND_CACHE_DIR, $cache_find_name, $backup_folders_row
# Fat linpeas: 0
# Small linpeas: 1

cache_find() {
  cache_find_name=$1
  shift
  (find "$@" 2>/dev/null | sort > "$FIND_CACHE_DIR/$cache_find_name"; printf '%s' "$YELLOW. $NC" >&2) &
  CONT_THREADS=$((CONT_THREADS + 1))
  if [ $((CONT_THREADS % THREADS)) -eq 0 ]; then wait; fi
}

if [ "$SEARCH_IN_FOLDER" ]; then
  printf $GREEN"Caching directories "$NC

  CONT_THREADS=0
  FIND_CACHE_DIR=$(mktemp -d "${TMPDIR:-/tmp}/linpeas-finds.XXXXXX") || exit 1
  trap 'rm -rf "$FIND_CACHE_DIR"' 0
  # FIND ALL KNOWN INTERESTING SOFTWARE FILES
  peass{FINDS_CUSTOM}

  wait # Always wait at the end
  CONT_THREADS=0 #Reset the threads counter

elif echo $CHECKS | grep -q procs_crons_timers_srvcs_sockets || echo $CHECKS | grep -q software_information || echo $CHECKS | grep -q interesting_files; then

  printf $GREEN"Caching directories "$NC

  CONT_THREADS=0
  FIND_CACHE_DIR=$(mktemp -d "${TMPDIR:-/tmp}/linpeas-finds.XXXXXX") || exit 1
  trap 'rm -rf "$FIND_CACHE_DIR"' 0
  # FIND ALL KNOWN INTERESTING SOFTWARE FILES
  peass{FINDS_HERE}

  wait # Always wait at the end
  CONT_THREADS=0 #Reset the threads counter
fi

if [ "$SEARCH_IN_FOLDER" ] || echo $CHECKS | grep -q procs_crons_timers_srvcs_sockets || echo $CHECKS | grep -q software_information || echo $CHECKS | grep -q interesting_files; then
  #GENERATE THE STORAGES OF THE FOUND FILES
  peass{STORAGES_HERE}

  ##### POST SERACH VARIABLES #####
  backup_folders_row="$(echo $PSTORAGE_BACKUPS | tr '\n' ' ')"
  printf ${YELLOW}"DONE\n"$NC
  echo ""
fi
