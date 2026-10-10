# Title: Interesting Files - Passwords inside logs
# ID: IF_Passwords_in_logs
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Passwords inside logs, including long credential-bearing GET requests
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.001
# Functions Used: print_2title
# Global Variables: $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $log_find_exec_end, $lp_log_started, $lp_log_now, $lp_log_seen, $lp_log_file, $lp_log_hit, $lp_log_found, $lp_log_partial
# Fat linpeas: 0
# Small linpeas: 0


lp_http_log_credential_cues() (
  lp_log_started=$(date +%s 2>/dev/null || printf 0)
  lp_log_seen=0
  lp_log_found=0
  lp_log_partial=0
  for lp_log_file do
    [ "$lp_log_seen" -lt 8 ] || { lp_log_partial=1; break; }
    lp_log_now=$(date +%s 2>/dev/null || printf 0)
    if [ "$lp_log_started" -gt 0 ] && [ "$lp_log_now" -ge "$((lp_log_started + 5))" ]; then
      lp_log_partial=1
      break
    fi
    [ -f "$lp_log_file" ] && [ -r "$lp_log_file" ] && [ ! -L "$lp_log_file" ] || continue
    lp_log_seen=$((lp_log_seen + 1))
    # The final 64 KiB is enough to inspect recent requests without reading a
    # whole access log. Never pass raw request lines or parameter values onward.
    lp_log_hit=$(tail -c 65536 "$lp_log_file" 2>/dev/null | awk '
      length($0) > 4096 { next }
      {
        quote = index($0, "\"")
        if (!quote) next
        request = substr($0, quote + 1)
        if (request !~ /^GET[[:space:]]/) next
        split(request, fields, /[[:space:]]+/)
        uri = fields[2]
        mark = index(uri, "?")
        if (!mark) next
        query = substr(uri, mark + 1)
        sub(/#.*/, "", query)
        count = split(query, params, "&")
        for (i = 1; i <= count; i++) {
          equals = index(params[i], "=")
          if (equals < 2 || equals == length(params[i])) continue
          key = tolower(substr(params[i], 1, equals - 1))
          if (key == "pass" || key == "pwd" || key ~ /(^|[_-])pwd$/ ||
              key ~ /(password|passwd|passcode)$/) {
            print "found"
            exit
          }
        }
      }
    ' 2>/dev/null)
    [ "$lp_log_hit" = found ] || continue
    if [ "$lp_log_found" -eq 0 ]; then
      printf '%s\n' 'HTTP access-log GET credential query parameters (values redacted):'
      lp_log_found=1
    fi
    printf '  %s: credential-bearing GET request present\n' "$lp_log_file"
  done
  [ "$lp_log_partial" -eq 0 ] || printf '%s\n' 'HTTP access-log credential inspection was partial (file or time cap reached).'
)

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Searching passwords inside logs (limit 70)" "T1552.001"
  # Probe batching once; use the per-file terminator on older find implementations.
  log_find_exec_end='+'
  find /dev/null -exec true '{}' + >/dev/null 2>&1 || log_find_exec_end=';'
  # shellcheck disable=SC2067
  (find /var/log/ /var/logs/ /private/var/log -type f -exec grep -H -i "pwd\|passw" "{}" "$log_find_exec_end") 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | grep -v "File does not exist:\|modules-config/config-set-passwords\|config-set-passwords already ran\|script not found or unable to stat:\|\"GET /.*\" 404" | head -n 70 | sed -${E} "s,pwd|passw,${SED_RED},"
  lp_http_log_credential_cues \
    /var/log/apache2/access.log /var/log/apache2/access.log.1 \
    /var/log/httpd/access_log /var/log/httpd/access_log.1 \
    /var/log/nginx/access.log /var/log/nginx/access.log.1 \
    /var/www/logs/access_log /var/www/logs/access_log.1
  echo ""
fi
