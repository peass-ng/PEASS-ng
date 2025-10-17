# Title: Processes & Cron & Services & Timers - Crontab UI (root) Misconfiguration
# ID: PR_Crontab_UI_misconfig
# Author: HT Bot
# Last Update: 2025-09-13
# Description: Detect Crontab UI service and risky configurations that can lead to privesc:
#   - Root-run Crontab UI exposed on localhost
#   - Basic-Auth credentials in systemd Environment= (BASIC_AUTH_USER/PWD)
#   - Cron DB path (CRON_DB_PATH) and weak permissions / embedded secrets in jobs
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info, print_list, echo_not_found
# Global Variables: $SEARCH_IN_FOLDER, $SED_RED, $SED_RED_YELLOW, $NC
# Initial Functions:
# Generated Global Variables: $svc, $state, $user, $envvals, $port, $dbpath, $dbfile, $candidates, $procs, $perms, $basic_user, $basic_pwd, $uprint, $pprint, $dir, $found
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Crontab UI (root) misconfiguration checks"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#scheduledcron-jobs"

  # Collect candidate services referencing crontab-ui
  candidates=""
  if command -v systemctl >/dev/null 2>&1; then
    candidates=$(systemctl list-units --type=service --all 2>/dev/null | awk '{print $1}' | grep -Ei '^crontab-ui\.service$' 2>/dev/null)
  fi

  # Fallback: grep service files for ExecStart containing crontab-ui
  if [ -z "$candidates" ]; then
    for dir in /etc/systemd/system /lib/systemd/system; do
      [ -d "$dir" ] || continue
      found=$(grep -RIl "^Exec(Start|StartPre|StartPost)=.*crontab-ui" "$dir" 2>/dev/null | xargs -r -I{} basename {} 2>/dev/null)
      if [ -n "$found" ]; then
        candidates=$(printf "%s\n%s" "$candidates" "$found" | sort -u)
      fi
    done
  fi

  # Also flag if the binary exists or a process seems to be running
  if command -v crontab-ui >/dev/null 2>&1; then
    print_list "crontab-ui binary found at: $(command -v crontab-ui)"$NC
  else
    echo_not_found "crontab-ui"
  fi

  procs=$(ps aux 2>/dev/null | grep -E "(crontab-ui|node .*crontab-ui)" | grep -v grep)
  if [ -n "$procs" ]; then
    print_list "Processes matching crontab-ui? ..................... "$NC
    printf "%s\n" "$procs"
    echo ""
  fi

  # If no candidates detected, exit quietly
  if [ "$candidates" ]; then

    # Iterate candidates and extract interesting data
    printf "%s\n" "$candidates" | while read -r svc; do
      [ -n "$svc" ] || continue
      # Ensure suffix .service if missing
      case "$svc" in
        *.service) : ;;
        *) svc="$svc.service" ;;
      esac

      state=""
      user=""
      if command -v systemctl >/dev/null 2>&1; then
        state=$(systemctl is-active "$svc" 2>/dev/null)
        user=$(systemctl show "$svc" -p User 2>/dev/null | cut -d= -f2)
      fi

      [ -z "$state" ] && state="unknown"
      [ -z "$user" ] && user="unknown"

      echo "Service: $svc (state: $state, User: $user)" | sed -${E} "s,root,${SED_RED},g"

      # Read Environment from systemd (works even if file unreadable in many setups)
      envvals=$(systemctl show "$svc" -p Environment 2>/dev/null | cut -d= -f2-)
      if [ -n "$envvals" ]; then
        basic_user=$(printf "%s\n" "$envvals" | tr ' ' '\n' | grep -E '^BASIC_AUTH_USER=' | head -n1 | cut -d= -f2-)
        basic_pwd=$(printf "%s\n" "$envvals" | tr ' ' '\n' | grep -E '^BASIC_AUTH_PWD=' | head -n1 | cut -d= -f2-)
        dbpath=$(printf "%s\n" "$envvals" | tr ' ' '\n' | grep -E '^CRON_DB_PATH=' | head -n1 | cut -d= -f2-)
        port=$(printf "%s\n" "$envvals" | tr ' ' '\n' | grep -E '^PORT=' | head -n1 | cut -d= -f2-)

        if [ -n "$basic_user" ] || [ -n "$basic_pwd" ]; then
          uprint="$basic_user"
          pprint="$basic_pwd"
          [ -n "$basic_pwd" ] && pprint="$basic_pwd"
          echo "  └─ Basic-Auth credentials in Environment: user='${uprint}' pwd='${pprint}'" | sed -${E} "s,pwd='[^']*',${SED_RED_YELLOW},g"
        fi

        if [ -n "$dbpath" ]; then
          echo "  └─ CRON_DB_PATH: $dbpath"
        fi

        # Check listener bound to localhost
        [ -z "$port" ] && port=8000
        if command -v ss >/dev/null 2>&1; then
          if ss -ltn 2>/dev/null | grep -qE "127\.0\.0\.1:${port}[[:space:]]"; then
            echo "  └─ Listener detected on 127.0.0.1:${port} (likely Crontab UI)."
          fi
        else
          if netstat -tnl 2>/dev/null | grep -qE "127\.0\.0\.1:${port}[[:space:]]"; then
            echo "  └─ Listener detected on 127.0.0.1:${port} (likely Crontab UI)."
          fi
        fi

        # If we know DB path, try to read crontab.db for obvious secrets and check perms
        if [ -n "$dbpath" ] && [ -d "$dbpath" ] && [ -r "$dbpath" ]; then
          dbfile="$dbpath/crontab.db"
          if [ -f "$dbfile" ]; then
            perms=$(ls -ld "$dbpath" 2>/dev/null | awk '{print $1, $3, $4}')
            echo "  └─ DB dir perms: $perms"
            if [ -w "$dbpath" ] || [ -w "$dbfile" ]; then
              echo "     └─ Writable by current user -> potential job injection!" | sed -${E} "s,.*,${SED_RED},g"
            fi
            echo "  └─ Inspecting $dbfile for embedded secrets in commands (zip -P / --password / pass/token/secret)..."
            grep -E "-P[[:space:]]+\S+|--password[[:space:]]+\S+|[Pp]ass(word)?|[Tt]oken|[Ss]ecret" "$dbfile" 2>/dev/null | head -n 20 | sed -${E} "s,(${SED_RED_YELLOW}),\1,g"
          fi
        fi
      fi
      echo ""
    done
  fi
fi

