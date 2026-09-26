# Title: Processes & Cron & Services & Timers - macOS privileged updater TOCTOU candidates
# ID: PR_Macos_updater_toctou
# Author: HT Bot
# Last Update: 26-09-2026
# Description: Identify root LaunchDaemon configurations and running updater processes that reference an attacker-writable staging, cache, download, or temporary path. This is a passive heuristic for pathname-based updater TOCTOU risks, not confirmation that a race is exploitable.
# License: GNU GPL
# Version: 1.1
# Mitre: T1574
# Functions Used: print_3title, print_info
# Global Variables: $E, $IAMROOT, $MACPEAS, $ROOT_FOLDER, $SEARCH_IN_FOLDER, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $mupt_candidate, $mupt_details, $mupt_dir, $mupt_dir_mode, $mupt_job, $mupt_launchdaemons_dir, $mupt_lower_path, $mupt_path, $mupt_path_owner, $mupt_pid, $mupt_plist, $mupt_plist_data, $mupt_plist_owner, $mupt_records, $mupt_source, $mupt_user, $mupt_user_id
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ] && ! [ "$SEARCH_IN_FOLDER" ] && ! [ "$IAMROOT" ]; then
  # Return absolute paths found in a defaults/plist dump. Quoted paths retain
  # spaces. The remaining expressions cover unquoted defaults output.
  mupt_paths_from_plist() {
    printf '%s\n' "$1" |
      sed -n \
        -e 's/^[[:space:]]*"\(file:\/\/\/[^"?]*\)"[,;]*[[:space:]]*$/\1/p' \
        -e 's/.*"\(file:\/\/\/[^"?]*\)".*/\1/p' \
        -e 's/^[[:space:]]*"\(\/[^"?]*\)"[,;]*[[:space:]]*$/\1/p' \
        -e 's/.*"\(\/[^"?]*\)".*/\1/p' \
        -e 's/.*=>[[:space:]]*\(\/[^,;)]*\).*/\1/p' \
        -e 's/.*=[[:space:]]*\(\/[^,;)]*\).*/\1/p' \
        -e 's/^[[:space:]]*\(\/[^,;)]*\)[,;]*[[:space:]]*$/\1/p' |
      sort -u
  }

  # Print one record when the configured update path, its payload, or one of
  # its existing parent components is controlled by the current user. Sticky
  # directories are not enough on their own to replace another user's entry,
  # but a directly writable/user-owned payload remains attacker-controlled.
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

    mupt_user_id="$(id -u 2>/dev/null)"
    if [ -e "$mupt_path" ]; then
      mupt_path_owner="$(stat -f '%u' "$mupt_path" 2>/dev/null)"
      if [ -w "$mupt_path" ] || { [ -n "$mupt_user_id" ] && [ "$mupt_path_owner" = "$mupt_user_id" ]; }; then
        printf '%s|%s|%s|%s\n' "$mupt_job" "$mupt_source" "$mupt_path" "$mupt_path"
        return 0
      fi
    fi

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
      if [ -w "$mupt_dir" ] && [ -x "$mupt_dir" ]; then
        mupt_dir_mode="$(stat -f '%Sp' "$mupt_dir" 2>/dev/null)"
        case "$mupt_dir_mode" in
          *t|*T) ;;
          *)
            printf '%s|%s|%s|%s\n' "$mupt_job" "$mupt_source" "$mupt_path" "$mupt_dir"
            return 0
            ;;
        esac
      fi
      [ "$mupt_dir" = "/" ] && break
      mupt_dir="${mupt_dir%/*}"
      [ -n "$mupt_dir" ] || mupt_dir="/"
    done
  }

  mupt_launchdaemons_dir="${ROOT_FOLDER:-}/Library/LaunchDaemons"
  # Keep the collector body outside command-substitution syntax. macOS still
  # ships Bash 3.2, whose parser can miscount parentheses inside quoted EREs
  # when they appear directly in a multiline $(...) body.
  mupt_collect_records() {
    # System LaunchDaemons run as root unless UserName says otherwise. Ignore
    # user-writable plists because existing launchd checks already report that
    # stronger, direct privilege-escalation condition.
    for mupt_plist in "$mupt_launchdaemons_dir"/*.plist; do
      [ -f "$mupt_plist" ] || continue
      [ -w "$mupt_plist" ] && continue
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
    # do not scan every writable temporary directory on the host and never eval
    # process arguments.
    # pgrep cannot emit the user, PID, and full command together portably.
    # shellcheck disable=SC2009
    ps -axo user=,pid=,command= 2>/dev/null |
      grep -Ei '^[[:space:]]*root[[:space:]]+[0-9]+[[:space:]].*(electron|install|patch|shipit|squirrel|updat(e|er)|upgrade)' |
      while IFS= read -r mupt_candidate; do
        mupt_pid="$(printf '%s\n' "$mupt_candidate" | awk '{print $2}')"
        printf '%s\n' "$mupt_candidate" |
          grep -Eo '/[^()[:space:]",;]+' 2>/dev/null |
          sort -u |
          while IFS= read -r mupt_path; do
            mupt_check_path "PID $mupt_pid" "root updater command line" "$mupt_path"
          done
      done
  }

  mupt_records="$(mupt_collect_records | sort -u)"

  if [ -n "$mupt_records" ]; then
    print_3title "Potential privileged updater TOCTOU paths" "T1574"
    print_info "https://blog.doyensec.com/2026/02/16/electron-safe-updater.html"
    print_info "Root updater candidates reference paths controlled by the current user. Manually verify whether an update is checked by pathname and later reopened for privileged installation instead of using the same descriptor or another immutable handle."
    printf '%s\n' "$mupt_records" | while IFS='|' read -r mupt_job mupt_source mupt_path mupt_dir; do
      mupt_details="$mupt_job: $mupt_path ($mupt_source; writable path or component: $mupt_dir)"
      printf '%s\n' "$mupt_details" | sed -"${E}" "s,.*,${SED_RED_YELLOW},"
      ls -ld "$mupt_path" "$mupt_dir" 2>/dev/null
    done
    echo ""
  fi
fi
