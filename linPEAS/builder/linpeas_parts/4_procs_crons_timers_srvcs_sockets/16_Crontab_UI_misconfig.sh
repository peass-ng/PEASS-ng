# Title: Processes & Cron & Services & Timers - Crontab UI (root) Misconfiguration
# ID: PR_Crontab_UI_misconfig
# Author: HT Bot
# Last Update: 2026-10-09
# Description: Correlate Crontab UI service/process evidence with a bounded, redacted job-store check.
# License: GNU GPL
# Version: 1.1
# Mitre: T1053.003
# Functions Used: print_2title, print_info, print_list, echo_not_found
# Global Variables: $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $candidates, $dir, $found, $unit_file, $svc, $state, $loadstate, $user, $envvals, $dbpath, $dbfile, $dbsize, $dbsummary, $jobcount, $credcount, $processes, $has_service, $has_process, $has_binary
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Crontab UI scheduler checks" "T1053.003"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/processes-crontab-systemd-dbus/cron-and-systemd-timers.html#enumerate-schedules"

  candidates=""
  has_service=""
  has_process=""
  has_binary=""
  if command -v systemctl >/dev/null 2>&1; then
    candidates=$(systemctl list-units --type=service --all --no-legend 2>/dev/null | awk '$1 ~ /[Cc]rontab[-_][Uu][Ii].*\.service$/ { print $1 }')
    found=$(systemctl list-unit-files --type=service --no-legend 2>/dev/null | awk '$1 ~ /[Cc]rontab[-_][Uu][Ii].*\.service$/ { print $1 }')
    candidates=$(printf '%s\n%s\n' "$candidates" "$found" | sed '/^$/d' | sort -u)
  fi

  # Search only service units; grep -E is required for the alternation below.
  for dir in /etc/systemd/system /lib/systemd/system /usr/lib/systemd/system; do
    [ -d "$dir" ] || continue
    found=$(find "$dir" -type f -name '*.service' -exec grep -Eil -e '^[[:space:]]*ExecStart(Pre|Post)?[[:space:]]*=[^#]*crontab-ui' {} + 2>/dev/null | while IFS= read -r unit_file; do basename "$unit_file"; done)
    candidates=$(printf '%s\n%s\n' "$candidates" "$found" | sed '/^$/d' | sort -u)
  done
  [ -n "$candidates" ] && has_service=1

  if command -v crontab-ui >/dev/null 2>&1; then
    has_binary=1
    print_list "crontab-ui binary found"
  else
    echo_not_found "crontab-ui"
  fi

  # Use the executable name, not arbitrary arguments containing 'crontab-ui'.
  # Never print full process arguments: they may contain credentials.
  processes=$(ps -eo user=,comm=,args= 2>/dev/null | awk '
    {
      user=$1; comm=$2; exe=$3; argpath=$4; arg=$4
      sub(/^.*\//, "", comm)
      sub(/^.*\//, "", exe)
      sub(/^.*\//, "", arg)
      if (comm == "crontab-ui" || (comm == "node" && exe == "node" && (arg == "crontab-ui" || argpath ~ /(^|\/)crontab-ui(\/|$)/)))
        print user " crontab-ui"
    }' | sort -u)
  if [ -n "$processes" ]; then
    has_process=1
    printf 'Process evidence (owner and executable only): %s\n' "$processes"
  fi

  if [ -n "$candidates" ]; then
    printf '%s\n' "$candidates" | head -n 20 | while IFS= read -r svc; do
      [ -n "$svc" ] || continue
      state=unknown
      loadstate=unknown
      user=unknown
      envvals=""
      if command -v systemctl >/dev/null 2>&1; then
        state=$(systemctl is-active "$svc" 2>/dev/null) || state=unknown
        loadstate=$(systemctl show "$svc" -p LoadState 2>/dev/null | cut -d= -f2-)
        user=$(systemctl show "$svc" -p User 2>/dev/null | cut -d= -f2-)
        envvals=$(systemctl show "$svc" -p Environment 2>/dev/null | cut -d= -f2-)
      fi
      [ -n "$state" ] || state=unknown
      if [ "$loadstate" = loaded ] && [ -z "$user" ]; then
        user='root (systemd default; configuration evidence)'
      elif [ -z "$user" ]; then
        user=unknown
      fi
      printf 'Service evidence: %s (state: %s, User: %s)\n' "$svc" "$state" "$user"
      if printf '%s\n' "$envvals" | grep -Eq '(^|[[:space:]])BASIC_AUTH_USER='; then
        printf '  BASIC_AUTH_USER is set (value redacted)\n'
      fi
      if printf '%s\n' "$envvals" | grep -Eq '(^|[[:space:]])BASIC_AUTH_PWD='; then
        printf '  BASIC_AUTH_PWD is set (value redacted)\n'
      fi
      printf '\n'
    done
  fi

  # systemctl Environment may be hidden; the exact conventional store is still
  # useful when independent service, binary, or process evidence exists.
  if [ -n "$has_service$has_process$has_binary" ]; then
    dbpath=""
    if [ -n "$candidates" ] && command -v systemctl >/dev/null 2>&1; then
      svc=$(printf '%s\n' "$candidates" | head -n 1)
      envvals=$(systemctl show "$svc" -p Environment 2>/dev/null | cut -d= -f2-)
      dbpath=$(printf '%s\n' "$envvals" | tr ' ' '\n' | sed -n 's/^CRON_DB_PATH=//p' | head -n 1)
    fi
    if [ -n "$dbpath" ]; then
      case "$dbpath" in
        */crontab.db) dbfile=$dbpath ;;
        *) dbfile=${dbpath%/}/crontab.db ;;
      esac
    else
      dbfile=/opt/crontabs/crontab.db
    fi

    if [ -f "$dbfile" ]; then
      if [ ! -r "$dbfile" ]; then
        printf 'DB candidate: %s (unreadable; contents unknown)\n' "$dbfile"
      else
        dbsize=$(stat -L -c %s "$dbfile" 2>/dev/null) || dbsize=$(stat -L -f %z "$dbfile" 2>/dev/null)
        case "$dbsize" in
          ''|*[!0-9]*) printf 'DB candidate: %s (size unknown; contents not inspected)\n' "$dbfile" ;;
          *)
            if [ "$dbsize" -gt 1048576 ]; then
              printf 'DB candidate: %s (readable, oversized: %s bytes; contents not inspected)\n' "$dbfile" "$dbsize"
            else
              printf 'DB candidate: %s (readable, %s bytes; writable by current user: ' "$dbfile" "$dbsize"
              if [ -w "$dbfile" ]; then printf 'yes)\n'; else printf 'no)\n'; fi
              dbsummary=$(LC_ALL=C awk '
                /^[[:space:]]*\{/ && /"command"[[:space:]]*:[[:space:]]*"/ {
                  jobs++
                  command=$0
                  sub(/^.*"command"[[:space:]]*:[[:space:]]*"/, "", command)
                  sub(/".*$/, "", command)
                  if (command ~ /(^|[[:space:]])zip[[:space:]]+[^"]*[[:space:]]-P[[:space:]]+[^[:space:]"\\]+/)
                    credentials++
                }
                END { printf "%d %d", jobs, credentials }
              ' "$dbfile" 2>/dev/null)
              if [ -n "$dbsummary" ]; then
                jobcount=${dbsummary%% *}
                credcount=${dbsummary#* }
                if [ "$jobcount" -eq 0 ]; then
                  printf '  Job store shape unrecognized; contents unknown\n'
                else
                  printf '  Jobs with command fields: %s; inline zip -P credential candidates: %s (values redacted)\n' "$jobcount" "$credcount"
                fi
              else
                printf '  Job store inspection failed; contents unknown\n'
              fi
            fi
            ;;
        esac
      fi
    elif [ -e "$dbfile" ]; then
      printf 'DB candidate: %s (not a regular file; contents unknown)\n' "$dbfile"
    else
      printf 'DB candidate: %s (not found; contents unknown)\n' "$dbfile"
    fi
  fi
  if [ -n "$has_service$has_process" ]; then
    printf 'Review existing Open ports output for a loopback listener; no service binding or credential reuse was verified.\n'
  fi
fi
