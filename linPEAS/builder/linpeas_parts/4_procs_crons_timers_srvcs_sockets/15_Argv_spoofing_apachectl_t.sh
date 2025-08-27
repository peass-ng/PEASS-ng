# Title: Processes & Cron & Services & Timers - Argv spoofing + apachectl -t risks
# ID: PR_Argv_spoofing_apachectl_t
# Author: HT Bot
# Last Update: 2025-08-27
# Description: Detect potentially dangerous root scripts/units that (1) parse process argv via pgrep -f/ps and then execute a constructed command variable (argv spoofing primitive), and/or (2) invoke apache2ctl/apachectl -t which can execute piped loggers from attacker-controlled configs.
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, print_info
# Global Variables: $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $scan_paths, $p, $regex_procgrab, $regex_var_exec, $regex_apache_test, $f, $hit, $svc
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Potential argv-spoofing privescs and apachectl -t abuse"
  print_info "Abusing roots crons/services that build commands from 'pgrep -f' or 'ps' output can allow argv spoofing. Running apache2ctl -t on attacker-controlled configs may execute piped loggers. See https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#scheduledcron-jobs and https://gtfobins.github.io/gtfobins/apache2ctl/."

  # Candidate paths to scan (keep fast; avoid whole FS)
  scan_paths="/usr/local/bin /usr/local/sbin /root/bin /etc/cron.d /etc/cron.daily /etc/cron.hourly /etc/cron.weekly /etc/cron.monthly /etc/cron /etc/anacrontab"

  # Regexes
  regex_procgrab='pgrep[[:space:]]+-[^ ]*f|pgrep[[:space:]]+-lfa|ps[[:space:]].*(-eo[[:space:]]+args|auxw|auxww)'
  regex_var_exec='\$[A-Za-z_][A-Za-z0-9_]*([[:space:]]|$)'  # naive exec of unquoted shell variable
  regex_apache_test='(apache2ctl|apachectl)[[:space:]].*-t(\>|[[:space:]]|$)'

  print_3title "Scanning cron/root script locations for argv parsing + var execution"
  for p in $scan_paths; do
    [ -e "$p" ] || continue
    # Limit to reasonable file sizes (<200KB) and regular files
    find "$p" -type f -size -200k 2>/dev/null | while IFS= read -r f; do
      # Fast prefilter: only text-like files
      hit=$(head -c 2048 "$f" 2>/dev/null | tr -d '\0' | grep -E "$regex_procgrab|$regex_apache_test" -n 2>/dev/null)
      [ -n "$hit" ] || continue

      # If file references pgrep/ps argv scanning and executes a var, flag
      if grep -E "$regex_procgrab" -n "$f" 2>/dev/null | head -n 1 >/dev/null; then
        if grep -E "(^|[^A-Za-z0-9_])\$[A-Za-z_][A-Za-z0-9_]*[[:space:]]*(2>|1>|>|>>|$)" -n "$f" 2>/dev/null | grep -v '\$\(' | head -n 1 >/dev/null; then
          echo "[!] Potential argv-spoof privesc in: $f"
          # Show a small snippet around interesting lines
          grep -nE "$regex_procgrab|while[[:space:]]+read|cmd=" "$f" 2>/dev/null | head -n 6 | sed 's/^/  └─ /'
        fi
      fi

      # If file invokes apachectl -t, flag and try to show -f/-d usage
      if grep -E "$regex_apache_test" -n "$f" 2>/dev/null | head -n 1 >/dev/null; then
        echo "[!] apachectl/apache2ctl -t usage in: $f"
        grep -nE "$regex_apache_test|-f[[:space:]]+| -d[[:space:]]+|ErrorLog[[:space:]]+\"\|" "$f" 2>/dev/null | head -n 6 | sed 's/^/  └─ /'
        # Quick hint about writable temp dirs
        for d in /dev/shm /tmp /var/tmp; do
          [ -w "$d" ] && echo "    Note: $d is writable; attacker could drop malicious Apache config and abuse piped logs during -t"
        done
      fi
    done
  done

  # Also scan systemd unit files for apachectl -t
  print_3title "Scanning systemd unit files for apachectl -t"
  for svc in /etc/systemd/system/*.service /lib/systemd/system/*.service; do
    [ -f "$svc" ] || continue
    if grep -E "Exec(Start|StartPre|StartPost).* (apache2ctl|apachectl).* -t(\>|[[:space:]]|$)" -n "$svc" 2>/dev/null >/dev/null; then
      echo "[!] apachectl/apache2ctl -t in unit: $svc"
      grep -nE "^User=|Exec(Start|StartPre|StartPost).* (apache2ctl|apachectl).* -t" "$svc" 2>/dev/null | sed 's/^/  └─ /'
    fi
  done
  echo ""
else
  print_2title "Argv spoofing + apachectl -t risks"
  for p in /etc /usr/local/bin /usr/local/sbin; do
    [ -e "$p" ] && echo "$p" && ls -lR "$p" 2>/dev/null | head -n 50
  done
  echo ""
fi

