# Title: Processes & Cron & Services & Timers - System V Shared Memory
# ID: PR_Sysv_shared_memory
# Author: Carlos Polop
# Last Update: 2026-10-09
# Description: Report root-owned, other-writable System V shared-memory segments for passive review
# License: GNU GPL
# Version: 1.0
# Mitre: T1559
# Functions Used: print_2title, print_info
# Global Variables: $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $sysv_shm_report
# Fat linpeas: 0
# Small linpeas: 0

# This is a single snapshot. Short-lived segments can disappear before it runs;
# absence is not evidence that a privileged IPC consumer is safe. Never attach
# to a segment or launch a privileged helper to make one appear.
if [ ! "$SEARCH_IN_FOLDER" ] && command -v ipcs >/dev/null 2>&1; then
  sysv_shm_report=$(LC_ALL=C ipcs -m 2>/dev/null | awk '
    NR > 512 { limited = 1; exit }
    {
      if (tolower($1) == "key" && tolower($2) == "shmid" &&
          tolower($3) == "owner" && tolower($4) == "perms") {
        format = "linux"
        next
      }
      if (toupper($1) == "T" && toupper($2) == "ID" &&
          toupper($3) == "KEY" && toupper($4) == "MODE" &&
          toupper($5) == "OWNER") {
        format = "bsd"
        next
      }
      if (format == "linux" && NF >= 5) {
        id = $2; key = $1; owner = $3; perms = $4; size = $5
      } else if (format == "bsd" && NF >= 6 && $1 == "m") {
        id = $2; key = $3; perms = $4; owner = $5; size = "unknown"
      } else next
      if (owner != "root" && owner != "0") next
      other_writable = 0
      if (perms ~ /^[0-7]+$/ && length(perms) >= 3) {
        last = substr(perms, length(perms), 1)
        if (last ~ /[2367]/) other_writable = 1
      } else if (perms ~ /^[-a-zA-Z][-a-zA-Z][r-][w-][xstST-][r-][w-][xstST-][r-][w-][xstST-]$/) {
        if (substr(perms, length(perms) - 1, 1) == "w") other_writable = 1
      }
      if (!other_writable) next
      if (shown < 20) {
        printf "Root-owned System V shared memory writable by others: id=%s key=%s mode=%s size=%s\n", id, key, perms, size
        shown++
      } else limited = 1
    }
    END { if (limited) print "System V shared-memory review incomplete (512-line/20-candidate limit)" }
  ')
  if [ "$sysv_shm_report" ]; then
    print_2title "Writable privileged System V shared memory (review candidates)" "T1559"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/processes-crontab-systemd-dbus/process-enumeration-and-service-paths.html#system-v-shared-memory-consumed-by-privileged-processes"
    printf '%s\n' "$sysv_shm_report"
    printf '%s\n' "A writable segment needs a privileged consumer that trusts it to be exploitable; this snapshot can miss transient segments."
  fi
fi
