# Title: Software Information - PostgreSQL Event Triggers
# ID: SI_Postgresql_Event_Triggers
# Author: HT Bot
# Last Update: 19-11-2025
# Description: Detect unsafe PostgreSQL event triggers and postgres_fdw custom scripts that grant temporary SUPERUSER
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $DEBUG, $E, $SED_GREEN, $SED_RED, $SED_YELLOW, $TIMEOUT
# Initial Functions:
# Generated Global Variables: $psql_bin, $psql_evt_output, $psql_evt_status, $psql_evt_err_line, $postgres_fdw_dirs, $postgres_fdw_hits, $old_ifs, $evtname, $enabled, $owner, $owner_is_super, $func, $func_owner, $func_owner_is_super, $IFS
# Fat linpeas: 0
# Small linpeas: 1


if [ "$DEBUG" ] || { [ "$TIMEOUT" ] && [ "$(command -v psql 2>/dev/null || echo -n '')" ]; }; then
  print_2title "PostgreSQL event trigger ownership & postgres_fdw hooks"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#postgresql-event-triggers"

  psql_bin="$(command -v psql 2>/dev/null || echo -n '')"
  if [ "$TIMEOUT" ] && [ "$psql_bin" ]; then
    psql_evt_output="$($TIMEOUT 5 "$psql_bin" -w -X -q -A -t -d postgres -c "WITH evt AS ( SELECT e.evtname, e.evtenabled, pg_get_userbyid(e.evtowner) AS trig_owner, tr.rolsuper AS trig_owner_super, n.nspname || '.' || p.proname AS function_name, pg_get_userbyid(p.proowner) AS func_owner, fr.rolsuper AS func_owner_super FROM pg_event_trigger e JOIN pg_proc p ON e.evtfoid = p.oid JOIN pg_namespace n ON p.pronamespace = n.oid LEFT JOIN pg_roles tr ON tr.oid = e.evtowner LEFT JOIN pg_roles fr ON fr.oid = p.proowner ) SELECT evtname || '|' || evtenabled || '|' || COALESCE(trig_owner,'?') || '|' || COALESCE(CASE WHEN trig_owner_super THEN 'yes' ELSE 'no' END,'unknown') || '|' || function_name || '|' || COALESCE(func_owner,'?') || '|' || COALESCE(CASE WHEN func_owner_super THEN 'yes' ELSE 'no' END,'unknown') FROM evt WHERE COALESCE(trig_owner_super,false) = false OR COALESCE(func_owner_super,false) = false;" 2>&1)"
    psql_evt_status=$?
    if [ $psql_evt_status -eq 0 ]; then
      if [ "$psql_evt_output" ]; then
        echo "Non-superuser-owned event triggers were found (trigger|enabled?|owner|owner_is_super|function|function_owner|fn_owner_is_super):" | sed -${E} "s,.*,${SED_RED},"
        printf "%s\n" "$psql_evt_output" | while IFS='|' read evtname enabled owner owner_is_super func func_owner func_owner_is_super; do
          case "$enabled" in
            O) enabled="enabled" ;;
            D) enabled="disabled" ;;
            *) enabled="status_$enabled" ;;
          esac
          echo "  - $evtname ($enabled) uses $func owned by $func_owner (superuser:$func_owner_is_super); trigger owner: $owner (superuser:$owner_is_super)" | sed -${E} "s,superuser:no,${SED_RED},g"
        done
      else
        echo "No event triggers owned by non-superusers were returned." | sed -${E} "s,.*,${SED_GREEN},"
      fi
    else
      psql_evt_err_line=$(printf '%s\n' "$psql_evt_output" | head -n1)
      echo "Could not query pg_event_trigger (psql exit $psql_evt_status): $psql_evt_err_line" | sed -${E} "s,.*,${SED_YELLOW},"
    fi
  else
    if ! [ "$TIMEOUT" ]; then
      echo_not_found "timeout"
    fi
    if ! [ "$psql_bin" ]; then
      echo_not_found "psql"
    fi
  fi

  postgres_fdw_dirs="/etc/postgresql /var/lib/postgresql /var/lib/postgres /usr/lib/postgresql /usr/local/lib/postgresql /opt/supabase /opt/postgres /srv/postgres"
  postgres_fdw_hits=""
  for d in $postgres_fdw_dirs; do
    if [ -d "$d" ]; then
      old_ifs="$IFS"
      IFS="\n"
      for f in $(find "$d" -maxdepth 5 -type f \( -name '*postgres_fdw*.sql' -o -name '*postgres_fdw*.psql' -o -name 'after-create.sql' \) 2>/dev/null); do
        if [ -f "$f" ] && grep -qiE "alter[[:space:]]+role[[:space:]]+postgres[[:space:]]+superuser" "$f" 2>/dev/null; then
          postgres_fdw_hits="$postgres_fdw_hits\n$f"
        fi
      done
      IFS="$old_ifs"
    fi
  done

  if [ "$postgres_fdw_hits" ]; then
    echo "Detected postgres_fdw custom scripts granting postgres SUPERUSER (check for SupaPwn-style window):" | sed -${E} "s,.*,${SED_RED},"
    printf "%s\n" "$postgres_fdw_hits" | sed "s,^,  - ,"
  fi
fi

echo ""
