# Title: Processes & Cron & Services & Timers - MacOS privileged updater TOCTOU candidates
# ID: PR_Macos_updater_toctou
# Author: HT Bot
# Last Update: 22-09-2026
# Description: Identify root launchd jobs and running updater processes that reference an attacker-writable staging, cache, download, or temporary path. This is a passive heuristic for pathname-based updater TOCTOU risks, not confirmation that a race is exploitable.
# License: GNU GPL
# Version: 1.0
# Mitre: T1574
# Functions Used: print_3title, print_info
# Global Variables: $E, $IAMROOT, $MACPEAS, $SEARCH_IN_FOLDER, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $mupt_candidate, $mupt_details, $mupt_dir, $mupt_job, $mupt_lower_path, $mupt_path, $mupt_pid, $mupt_plist, $mupt_plist_data, $mupt_plist_owner, $mupt_records, $mupt_source, $mupt_user
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ] && ! [ "$SEARCH_IN_FOLDER" ] && ! [ "$IAMROOT" ]; then
  # Return paths found in a launchd plist dump. The first expression preserves
  # spaces in quoted paths; the second covers unquoted XML/defaults output.
  mupt_paths_from_plist() {
    printf '%s\n' "$1" |
      sed -n \
        -e 's/^[[:space:]]*"\(\/[^"?]*\)"[,;]*[[:space:]]*$/\1/p' \
        -e 's/.*"\(\/[^"?]*\)".*/\1/p' \
        -e 's/.*=>[[:space:]]*"\(\/[^"?]*\)".*/\1/p' \
        -e 's/.*=[[:space:]]*\(\/[^,;)]*\).*/\1/p' \
        -e 's/^[[:space:]]*\(\/[^,;)]*\)[,;]*[[:space:]]*$/\1/p' |
      sort -u
  }

  # Print one machine-readable record when the configured update path or one
  # of its existing parents is writable by the current user. A sticky parent
  # is not reported because it normally prevents replacing root-owned entries.
  mupt_check_path() {
    mupt_job="$1"
    mupt_source="$2"
    mupt_path="$3"

    case "$mupt_path" in
      file://*) mupt_path="${mupt_path#file://}" ;;
    esac
    case "$mupt_path" in
      /*) ;;
      *) return 0 ;;
    esac

    mupt_lower_path="$(printf '%s' "$mupt_path" | tr '[:upper:]' '[:lower:]')"
    case "$mupt_lower_path" in
      *cache*|*download*|*electron*|*shipit*|*squirrel*|*stag*|*temp*|*tmp*|*updat*) ;;
      *) return 0 ;;
    esac

    if [ -d "$mupt_path" ]; then
      mupt_dir="$mupt_path"
    else
      mupt_dir="${mupt_path%/*}"
      [ -n "$mupt_dir" ] || mupt_dir="/"
      while [ ! -d "$mupt_dir" ] && [ "$mupt_dir" != "/" ]; do
        mupt_dir="${mupt_dir%/*}"
        [ -n "$mupt_dir" ] || mupt_dir="/"
      done
    fi

    while [ -d "$mupt_dir" ]; do
      if [ -w "$mupt_dir" ] && [ -x "$mupt_dir" ] && ! [ -k "$mupt_dir" ]; then
        printf '%s|%s|%s|%s\n' "$mupt_job" "$mupt_source" "$mupt_path" "$mupt_dir"
        return 0
      fi
      [ "$mupt_dir" = "/" ] && break
      mupt_dir="${mupt_dir%/*}"
      [ -n "$mupt_dir" ] || mupt_dir="/"
    done
  }

  mupt_records="$({
    # System LaunchDaemons run as root unless UserName says otherwise. Requiring
    # a root-owned plist avoids treating an untrusted, unloaded plist as a root
    # updater. Existing launchd checks separately report writable plist files.
    for mupt_plist in /Library/LaunchDaemons/*.plist; do
      [ -f "$mupt_plist" ] || continue
      mupt_plist_owner="$(stat -f '%u' "$mupt_plist" 2>/dev/null)"
      [ "$mupt_plist_owner" = "0" ] || continue

      mupt_plist_data="$(defaults read "$mupt_plist" 2>/dev/null)"
      [ -n "$mupt_plist_data" ] || continue
      printf '%s\n%s\n' "$mupt_plist" "$mupt_plist_data" |
        grep -Eiq '(electron|install|patch|shipit|squirrel|updat(e|er)|upgrade)' || continue

      mupt_user="$(defaults read "$mupt_plist" UserName 2>/dev/null | head -n 1)"
      case "$mupt_user" in
        ""|0|root) ;;
        *) continue ;;
      esac

      mupt_paths_from_plist "$mupt_plist_data" | while IFS= read -r mupt_path; do
        [ -n "$mupt_path" ] || continue
        mupt_check_path "$mupt_plist" "root LaunchDaemon metadata" "$mupt_path"
      done
    done

    # A privileged helper or an updater inside an application bundle may only
    # expose its staging path while running. Inspect only root updater commands;
    # do not scan every writable temporary directory on the host.
    ps -axo user=,pid=,command= 2>/dev/null |
      grep -Ei '^[[:space:]]*root[[:space:]]+[0-9]+[[:space:]].*(electron|install|patch|shipit|squirrel|updat(e|er)|upgrade)' |
      while IFS= read -r mupt_candidate; do
        mupt_pid="$(printf '%s\n' "$mupt_candidate" | awk '{print $2}')"
        printf '%s\n' "$mupt_candidate" |
          grep -Eo '/[^[:space:]",;\)]+' 2>/dev/null |
          sort -u |
          while IFS= read -r mupt_path; do
            mupt_check_path "PID $mupt_pid" "root updater command line" "$mupt_path"
          done
      done
  } | sort -u)"

  if [ -n "$mupt_records" ]; then
    print_3title "Potential privileged updater TOCTOU paths" "T1574"
    print_info "https://blog.doyensec.com/2026/02/16/electron-safe-updater.html"
    print_info "Root updater candidates reference paths writable by the current user. Manually verify whether an update is checked by pathname and later reopened for privileged installation instead of using the same descriptor or another immutable handle."
    printf '%s\n' "$mupt_records" | while IFS='|' read -r mupt_job mupt_source mupt_path mupt_dir; do
      mupt_details="$mupt_job: $mupt_path ($mupt_source; writable path component: $mupt_dir)"
      printf '%s\n' "$mupt_details" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      ls -ld "$mupt_path" "$mupt_dir" 2>/dev/null
    done
    echo ""
  fi
fi
