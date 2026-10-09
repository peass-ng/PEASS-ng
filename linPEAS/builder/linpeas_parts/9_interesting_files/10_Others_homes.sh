# Title: Interesting Files - Files inside /home
# ID: IF_Others_homes
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: Files inside home directories and HTTPie session paths
# License: GNU GPL
# Version: 1.1
# Mitre: T1552.001
# Functions Used: echo_not_found, print_2title
# Global Variables: $HOME, $HOMESEARCH, $SEARCH_IN_FOLDER, $USER
# Initial Functions:
# Generated Global Variables: $httpie_home, $httpie_home_count, $httpie_config, $httpie_app, $httpie_sessions, $httpie_dir, $httpie_dir_count, $httpie_file, $httpie_file_count, $httpie_session_paths
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Files inside others home (limit 20)" "T1552.001"
  (find $HOMESEARCH -type f 2>/dev/null | grep -v -i "/"$USER | head -n 20) || echo_not_found
  echo ""

  # Fixed-depth globbing under at most 65 login homes; no JSON content reads.
  httpie_list_session_paths() {
    httpie_home_count=0
    httpie_dir_count=0
    httpie_file_count=0
    while IFS= read -r httpie_home; do
      [ "$httpie_home_count" -lt 65 ] || return
      httpie_home_count=$((httpie_home_count + 1))
      case "$httpie_home" in
        /*) ;;
        *) continue ;;
      esac
      case "$httpie_home/" in
        *'/../'*|*'/./'*|*'//'*) continue ;;
      esac
      httpie_config="$httpie_home/.config"
      httpie_app="$httpie_config/httpie"
      httpie_sessions="$httpie_app/sessions"
      [ -d "$httpie_home" ] && [ -x "$httpie_home" ] && [ ! -L "$httpie_home" ] || continue
      [ -d "$httpie_config" ] && [ -x "$httpie_config" ] && [ ! -L "$httpie_config" ] || continue
      [ -d "$httpie_app" ] && [ -x "$httpie_app" ] && [ ! -L "$httpie_app" ] || continue
      [ -d "$httpie_sessions" ] && [ -x "$httpie_sessions" ] && [ ! -L "$httpie_sessions" ] || continue
      for httpie_dir in "$httpie_sessions"/*; do
        [ -d "$httpie_dir" ] && [ -x "$httpie_dir" ] && [ ! -L "$httpie_dir" ] || continue
        [ "$httpie_dir_count" -lt 32 ] || return
        httpie_dir_count=$((httpie_dir_count + 1))
        for httpie_file in "$httpie_dir"/*.json; do
          [ -f "$httpie_file" ] && [ -r "$httpie_file" ] && [ ! -L "$httpie_file" ] || continue
          [ "$httpie_file_count" -lt 24 ] || return
          printf '%s\n' "$httpie_file"
          httpie_file_count=$((httpie_file_count + 1))
        done
      done
    done
  }

  httpie_session_paths=$({ printf '%s\n' "$HOME"; awk -F: '$6 ~ /^\// && $7 !~ /(nologin|false)$/ && $6 != "/" && !seen[$6]++ { print $6; if (++n == 64) exit }' /etc/passwd 2>/dev/null; } | httpie_list_session_paths)
  if [ -n "$httpie_session_paths" ]; then
    print_2title "Readable HTTPie session files (paths only, max 24)" "T1552.001"
    printf '%s\n' "$httpie_session_paths"
    echo "Session files may contain auth or cookies; path discovery does not prove usable credentials."
    echo ""
  fi
fi
