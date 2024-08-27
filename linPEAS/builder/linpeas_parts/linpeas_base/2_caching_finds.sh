# Title: Interesting Files - Check if Network jobs
# ID: BS_caching_finds
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Cache interesting files discoevred in the file system
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $CHECKS, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $CONT_THREADS, $backup_folders_row
# Fat linpeas: 0
# Small linpeas: 1


if [ "$SEARCH_IN_FOLDER" ]; then
  printf $GREEN"Caching directories "$NC

  CONT_THREADS=0
  # FIND ALL KNOWN INTERESTING SOFTWARE FILES
  peass{FINDS_CUSTOM}

  wait # Always wait at the end
  CONT_THREADS=0 #Reset the threads counter

elif echo $CHECKS | grep -q procs_crons_timers_srvcs_sockets || echo $CHECKS | grep -q software_information || echo $CHECKS | grep -q interesting_files; then

  printf $GREEN"Caching directories "$NC

  CONT_THREADS=0
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
