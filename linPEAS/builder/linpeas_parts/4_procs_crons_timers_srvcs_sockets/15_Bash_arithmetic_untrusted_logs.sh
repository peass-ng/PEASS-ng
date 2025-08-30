# Title: Processes & Cron & Services & Timers - Bash arithmetic on untrusted logs
# ID: PR_Bash_arithmetic_untrusted_logs
# Author: HT Bot
# Last Update: 2025-08-30
# Description: Heuristic detection of root-run periodic scripts (cron/timers) that perform Bash arithmetic on variables sourced from logs or user-controlled files. Flags common patterns that enable command substitution inside arithmetic ((...)), let, or declare -i when parsing log lines/arguments.
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, print_info
# Global Variables: $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $cron_file, $line, $tok, $script, $args, $arg, $timer_unit, $service_unit, $exec_line, $user_field, $cand, $match_lines, $severity, $msg, $file, $sev_and_lines, $candidates_tmp
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Potential Bash arithmetic injection in root-run parsers (cron/timers)"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#scheduledcron-jobs"

  # Small helpers -----------------------------------------------------------
  uniq_print_cand() {
    # de-duplicate candidate scripts
    # usage: uniq_print_cand <path>
    cand="$1"
    [ -z "$cand" ] && return
    [ ! -f "$cand" ] && return
    # only consider text-ish files to keep it fast
    if grep -Iq . "$cand" 2>/dev/null; then
      printf '%s\n' "$cand"
    fi
  }

  is_shell_script() {
    # returns 0 if file looks like a shell script (shebang or .sh), else 1
    [ ! -f "$1" ] && return 1
    head -n1 "$1" 2>/dev/null | grep -Eq '(/bash|/sh|/dash|/zsh|/ksh)' && return 0
    printf '%s' "$1" | grep -qE '\\.sh(\\.|$)' && return 0
    return 1
  }

  find_arith_patterns() {
    # echo severity and matched lines for arithmetic patterns
    # severity: STRONG if command substitution inside arithmetic, WEAK if variable arithmetic
    file="$1"
    # Strong: command substitution inside arithmetic context
    if grep -nE '\\(\\(.*\\$\\(.*\\).*\\)\\)|\\blet\\b[^#]*\\$\\(.*\\)|declare[[:space:]]+-i[[:space:]]+[A-Za-z_][A-Za-z0-9_]*=[^#]*\\$\\(.*\\)' "$file" 2>/dev/null | head -n 3 | sed 's/^/    /' ; then
      severity="STRONG"
      match_lines=$(grep -nE '\\(\\(.*\\$\\(.*\\).*\\)\\)|\\blet\\b[^#]*\\$\\(.*\\)|declare[[:space:]]+-i[[:space:]]+[A-Za-z_][A-Za-z0-9_]*=[^#]*\\$\\(.*\\)' "$file" 2>/dev/null | head -n 3)
      printf 'STRONG\n'; printf '%s\n' "$match_lines"
      return 0
    fi
    # Weak: arithmetic with unquoted variables that could be attacker-controlled
    if grep -nE '\\(\\([^#]*\\$[A-Za-z_][A-Za-z0-9_]*[^#]*\\)\\)|\\blet\\b[^#]*\\$[A-Za-z_][A-Za-z0-9_]*|declare[[:space:]]+-i[[:space:]]+[A-Za-z_][A-Za-z0-9_]*=[^#]*\\$[A-Za-z_][A-Za-z0-9_]*' "$file" 2>/dev/null | head -n 3 | sed 's/^/    /' ; then
      severity="WEAK"
      match_lines=$(grep -nE '\\(\\([^#]*\\$[A-Za-z_][A-Za-z0-9_]*[^#]*\\)\\)|\\blet\\b[^#]*\\$[A-Za-z_][A-Za-z0-9_]*|declare[[:space:]]+-i[[:space:]]+[A-Za-z_][A-Za-z0-9_]*=[^#]*\\$[A-Za-z_][A-Za-z0-9_]*' "$file" 2>/dev/null | head -n 3)
      printf 'WEAK\n'; printf '%s\n' "$match_lines"
      return 0
    fi
    return 1
  }

  looks_like_log_parsing() {
    # heuristics: script references logs/WWW logs/tmp or uses variables LOG/FILE in pipelines/process substitution
    file="$1"
    # explicit log-like paths
    if grep -qE '/var/log|/var/www/.*/log|/tmp/' "$file" 2>/dev/null; then
      return 0
    fi
    # log-ish variables used in pipelines or redirections
    if grep -qE '(LOG_FILE|LOG|FILE)=' "$file" 2>/dev/null && \
       grep -qE '(grep|awk|sed|cut|tail|head)[^\n]*\\$[A-Za-z_][A-Za-z0-9_]*' "$file" 2>/dev/null; then
      return 0
    fi
    # process substitution with grep/cat reading from variable
    grep -qE '(<\\(|<\\s*[^<])' "$file" 2>/dev/null && \
    grep -qE 'grep[^\n]*\\$[A-Za-z_][A-Za-z0-9_]*' "$file" 2>/dev/null && return 0
    return 1
  }

  print_writable_arg_info() {
    # Given an argument path, print if current user can write the file or its parent dir
    arg="$1"
    [ -z "$arg" ] && return
    case "$arg" in
      /*)
        if [ -e "$arg" ]; then
          if [ -w "$arg" ]; then
            echo "      Writable argument file: $arg"; ls -l "$arg" 2>/dev/null | sed 's/^/        /'
          fi
        else
          d=$(dirname -- "$arg")
          if [ -d "$d" ] && [ -w "$d" ]; then
            echo "      Parent dir writable: $d (arg $arg does not exist)"
            ls -ld "$d" 2>/dev/null | sed 's/^/        /'
          fi
        fi
        ;;
    esac
  }

  # Collect candidate scripts from cron ------------------------------------------------
  candidates_tmp=$(mktemp 2>/dev/null || echo "/tmp/.lp.cand.$$")
  : > "$candidates_tmp"

  # Root crontabs and system cron files
  for cron_file in /etc/crontab /var/spool/cron/crontabs/root /etc/cron.d/*; do
    [ -r "$cron_file" ] || continue
    # Iterate non-comment lines
    while IFS= read -r line || [ -n "$line" ]; do
      case "$line" in 
        \#*|"") continue ;;
      esac
      # Extract absolute paths from the line
      # First token that is an absolute path is likely the script/binary
      script=""
      args=""
      # Get all absolute-like tokens
      for tok in $(printf '%s\n' "$line" | grep -oE '/[^[:space:]]+'); do
        if [ -z "$script" ]; then
          script="$tok"
        else
          args="$args $tok"
        fi
      done
      if [ -n "$script" ]; then
        uniq_print_cand "$script" >> "$candidates_tmp"
        # If script is run by root (likely in these files), show writable argument hints now
        if [ -n "$args" ]; then
          echo "Root cron entry: $script$args"
          for arg in $args; do
            print_writable_arg_info "$arg"
          done
        fi
      fi
    done < "$cron_file"
  done

  # run-parts style cron directories (executed as root by system crond/anacron)
  for d in /etc/cron.daily /etc/cron.hourly /etc/cron.weekly /etc/cron.monthly; do
    [ -d "$d" ] || continue
    for f in "$d"/*; do
      [ -f "$f" ] || continue
      uniq_print_cand "$f" >> "$candidates_tmp"
    done
  done

  # Collect candidate scripts from systemd timers --------------------------------------
  if command -v systemctl >/dev/null 2>&1; then
    systemctl list-timers --all 2>/dev/null | awk 'NR>1 {print $1}' | grep -E '\\.timer$' | while read -r timer_unit; do
      [ -z "$timer_unit" ] && continue
      service_unit=$(systemctl show "$timer_unit" -p Unit 2>/dev/null | cut -d= -f2)
      [ -z "$service_unit" ] && continue
      user_field=$(systemctl show "$service_unit" -p User 2>/dev/null | cut -d= -f2)
      # Default user for services without User= is root; include both empty and root
      if [ -z "$user_field" ] || [ "$user_field" = "root" ]; then
        exec_line=$(systemctl show "$service_unit" -p ExecStart 2>/dev/null | cut -d= -f2)
        # Extract first absolute path as executable/script and any absolute path args
        script=""
        args=""
        for tok in $(printf '%s\n' "$exec_line" | grep -oE '/[^[:space:]]+'); do
          if [ -z "$script" ]; then
            script="$tok"
          else
            args="$args $tok"
          fi
        done
        if [ -n "$script" ]; then
          uniq_print_cand "$script" >> "$candidates_tmp"
          if [ -n "$args" ]; then
            echo "Root timer entry: $script$args"
            for arg in $args; do
              print_writable_arg_info "$arg"
            done
          fi
        fi
      fi
    done
  fi

  # Evaluate candidates ---------------------------------------------------------------
  if [ -s "$candidates_tmp" ]; then
    print_3title "Reviewing root-run scripts for arithmetic on untrusted input"
    sort -u "$candidates_tmp" | while read -r script; do
      [ -z "$script" ] && continue
      [ -r "$script" ] || continue
      is_shell_script "$script" || continue

      sev_and_lines=$(find_arith_patterns "$script")
      if [ $? -eq 0 ]; then
        severity=$(printf '%s' "$sev_and_lines" | head -n1)
        match_lines=$(printf '%s' "$sev_and_lines" | tail -n +2)
        if looks_like_log_parsing "$script"; then
          echo "[!] $severity risk: arithmetic evaluation with variables in $script"
          printf '%s\n' "$match_lines" | sed 's/^/      /'
          # Bonus: try to spot obvious log file variables
          if grep -qE 'LOG_FILE|LOG_PATH|LOG' "$script" 2>/dev/null; then
            msg=$(grep -nE 'LOG_FILE|LOG_PATH|LOG' "$script" 2>/dev/null | head -n 2)
            printf '%s\n' "$msg" | sed 's/^/      hint: /'
          fi
        fi
      fi
    done
  fi

  rm -f "$candidates_tmp" 2>/dev/null
  echo ""
else
  # Folder analysis mode: just list potential log-parsing shell scripts under the target folder
  print_2title "Potential Bash arithmetic/log-parsing scripts in folder"
  find "$SEARCH_IN_FOLDER" -type f -name "*.sh" -maxdepth 6 2>/dev/null \
    -exec sh -c 'head -n1 "$1" 2>/dev/null | grep -Eq "(/bash|/sh|/dash|/zsh|/ksh)" || exit 1' _ {} \; \
    -exec grep -Ilq . {} \; \
    -exec grep -qE "\\(\\(|\\blet\\b|declare[[:space:]]+-i" {} \; \
    -print
  echo ""
fi
