# Title: Processes & Cron & Services & Timers - Unsafe pgrep/ps → command execution
# ID: PR_Unsafe_processlist_cmd_exec
# Author: HT Bot
# Last Update: 2025-08-27
# Description: Detect privileged cron/systemd scripts that read process command lines (pgrep/ps),
#              transform them into a command string and execute it (e.g., cmd var / eval). This
#              catches patterns like the apache2ctl -t injection from pgrep -lfa found in HTB Zero.
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $candfile, $line, $file, $svc, $user, $exec, $pgrep_lines, $ps_lines, $exec_lines, $hint, $pattern_dollar
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Unsafe process-list command execution in privileged jobs"
  print_info "Looks for root cron/systemd scripts that build commands from pgrep/ps output and execute them (e.g., pgrep -lfa → var 'cmd' → exec/eval). If found, try forging a matching process argv to get code run as root."

  candfile=$(mktemp 2>/dev/null || echo "/tmp/linpeas.candidates.$$")

  # 1) Collect candidates from cron
  for f in /etc/crontab /etc/anacrontab /var/spool/cron/crontabs/root; do
    [ -r "$f" ] && cat "$f" 2>/dev/null | grep -vE '^[[:space:]]*#' | awk '{print $0}' || true
  done |
  while IFS= read -r line || [ -n "$line" ]; do
    case "$line" in
      ""|\#*) continue ;;
    esac
    # Extract absolute paths from the line
    echo "$line" | grep -oE '/[A-Za-z0-9_./-]+' 2>/dev/null | while IFS= read -r file; do
      [ -e "$file" ] && printf '%s\n' "$file" >>"$candfile"
      # Expand run-parts directories
      if echo "$line" | grep -qE '\brun-parts\b' && [ -d "$file" ]; then
        ls -1 "$file" 2>/dev/null | while IFS= read -r x; do
          [ -f "$file/$x" ] && printf '%s\n' "$file/$x" >>"$candfile"
        done
      fi
    done
  done

  # Include files inside cron directories
  for d in /etc/cron.d /etc/cron.daily /etc/cron.hourly /etc/cron.weekly /etc/cron.monthly; do
    [ -d "$d" ] || continue
    find "$d" -maxdepth 1 -type f -readable 2>/dev/null >>"$candfile"
  done

  # 2) Collect candidates from systemd (services running as root)
  if command -v systemctl >/dev/null 2>&1; then
    systemctl list-units --type=service --all 2>/dev/null | awk '{print $1}' | grep -E '\.service$' | while IFS= read -r svc; do
      user=$(systemctl show "$svc" -p User 2>/dev/null | cut -d= -f2)
      # Default user is root when empty; consider both empty and 'root'
      if [ -z "$user" ] || [ "$user" = "root" ]; then
        exec=$(systemctl show "$svc" -p ExecStart 2>/dev/null | cut -d= -f2-)
        # Extract absolute paths referenced in ExecStart
        printf '%s' "$exec" | grep -oE '/[A-Za-z0-9_./-]+' 2>/dev/null | while IFS= read -r file; do
          [ -e "$file" ] && printf '%s\n' "$file" >>"$candfile"
        done
      fi
    done
  fi

  # De-duplicate and trim to a reasonable number
  if [ -s "$candfile" ]; then
    sort -u "$candfile" | head -n 150 | while IFS= read -r file; do
      # Only plain text, readable files <= 500KB
      [ -r "$file" ] || continue
      [ -f "$file" ] || continue
      # Skip obvious binaries
      case "$(head -c 4 "$file" 2>/dev/null | od -An -t x1 | tr -d ' \n')" in
        7f454c46|cafebabe|feedface|feedfacf) continue;;
      esac
      [ $(wc -c <"$file" 2>/dev/null) -le 512000 ] || continue

      # Look for pgrep/ps grabbing command lines
      pgrep_lines=$(grep -nE '\bpgrep\b[^\n]*\-(f|\-\-full)' "$file" 2>/dev/null)
      ps_lines=$(grep -nE '\bps\b[^\n]*\-o[[:space:]]*(pid,(cmd|args)|command)|\bps\b[^\n]*\-ef' "$file" 2>/dev/null)

      [ -z "$pgrep_lines$ps_lines" ] && continue

      # Look for building a command string and executing it
      exec_lines=""
      # variable execution via a variable named "cmd"
      pattern_dollar="\$"
      if grep -nE '^[[:space:]]*cmd *=.*' "$file" 2>/dev/null | grep -q '.'; then
        exec_lines=$(grep -nE "(^|[;&|][[:space:]]*)${pattern_dollar}cmd(\b|[[:space:]])" "$file" 2>/dev/null)
      fi
      # eval/bash -c patterns
      exec_lines=${exec_lines}
      exec_lines=$(printf '%s\n' "$exec_lines"; grep -nE '\beval\b[[:space:]]+"?\$?[A-Za-z_][A-Za-z0-9_]*' "$file" 2>/dev/null)
      exec_lines=$(printf '%s\n' "$exec_lines"; grep -nE '\b(bash|sh)\b[[:space:]]+\-c[[:space:]]+"?\$?[A-Za-z_][A-Za-z0-9_]*' "$file" 2>/dev/null)

      [ -z "$exec_lines" ] && continue

      echo "[!] Potentially vulnerable privileged script: $file"
      [ -n "$pgrep_lines" ] && echo "$pgrep_lines" | sed 's/^/    pgrep: /'
      [ -n "$ps_lines" ] && echo "$ps_lines"       | sed 's/^/    ps:    /'
      echo "$exec_lines" | sed 's/^/    exec:  /'

      # Extra hint if apache2ctl -t appears
      if grep -qE 'apache2ctl' "$file" 2>/dev/null && grep -qE '\-t(\b|[[:space:]])' "$file" 2>/dev/null; then
        hint="    hint: script references 'apache2ctl -t' — try forging argv via a fake process name to load attacker-controlled config (e.g., -d/-f)."
        echo "$hint"
      fi

      echo
    done
  fi

  rm -f "$candfile" 2>/dev/null
else
  # Folder mode: best-effort regex scan within the provided directory (bounded)
  print_2title "Unsafe process-list command execution (folder scan)"
  print_info "Searching for scripts that read pgrep/ps output and execute a constructed command."
  find "${SEARCH_IN_FOLDER}" -type f -maxdepth 5 -size -512k 2>/dev/null \
    | xargs -I{} sh -c 'grep -qE "\\bpgrep\\b.*-(f|--full)|\\bps\\b.*-o[[:space:]]*(pid,(cmd|args)|command)|\\bps\\b.*-ef" "{}" 2>/dev/null && \
                        grep -qE "(^|[;&|][[:space:]]*)\(\$[cC][mM][dD])(\\b|[[:space:]])|\\beval\\b|\\b(bash|sh)\\b[[:space:]]+-c" "{}" 2>/dev/null && echo "[!] Potential match: {}"' \
    | head -n 50
  echo
fi
