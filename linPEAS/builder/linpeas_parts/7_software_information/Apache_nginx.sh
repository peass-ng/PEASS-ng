# Title: Software Information - Apache-Nginx
# ID: SI_Apache_nginx
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Apache-Nginx
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.001
# Functions Used: print_3title, warn_exec
# Global Variables: $NGINX_KNOWN_MODULES, $TIMEOUT, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $lp_apache_checked, $lp_apache_deadline, $lp_apache_dir, $lp_apache_file, $lp_apache_found, $lp_apache_lines, $lp_apache_name, $lp_remco_checked, $lp_remco_config, $lp_remco_deadline, $lp_remco_raw, $lp_remco_root, $lp_remco_size, $lp_remco_source, $lp_remco_sources, $lp_remco_template_size, $lp_remco_templates
# Fat linpeas: 0
# Small linpeas: 1

# The ordinary Apache file inventory below already displays virtual-host
# configuration. This extra view names piped-log directives without echoing
# their commands (which may contain credentials or attacker-controlled text).
lp_apache_piped_log_check() (
  if [ "$#" -eq 0 ]; then
    if [ "$SEARCH_IN_FOLDER" ]; then
      set -- "$SEARCH_IN_FOLDER" "$SEARCH_IN_FOLDER/sites-enabled"
    else
      set -- /etc/apache2/sites-enabled /etc/apache2/sites-available /etc/httpd/conf.d /usr/local/etc/apache24/Includes
    fi
  fi
  if [ -z "$TIMEOUT" ] || [ ! -x "$TIMEOUT" ]; then
    for lp_apache_dir do
      if [ -d "$lp_apache_dir" ]; then
        printf '%s\n' 'Apache piped-log inspection unavailable (timeout missing); configuration output withholds piped-log commands.'
        break
      fi
    done
    return 0
  fi
  lp_apache_deadline=$(($(date +%s 2>/dev/null || printf 0) + 4))
  lp_apache_checked=0
  lp_apache_found=0
  for lp_apache_dir do
    [ -d "$lp_apache_dir" ] && [ -r "$lp_apache_dir" ] || continue
    for lp_apache_file in "$lp_apache_dir"/*; do
      [ "$lp_apache_checked" -lt 64 ] || break 2
      [ "$(date +%s 2>/dev/null || printf 0)" -lt "$lp_apache_deadline" ] || break 2
      [ -f "$lp_apache_file" ] && [ -r "$lp_apache_file" ] || continue
      lp_apache_checked=$((lp_apache_checked + 1))
      lp_apache_lines=$("$TIMEOUT" 1 dd if="$lp_apache_file" bs=4096 count=16 2>/dev/null | awk '
        {
          line = $0
          sub(/^[[:space:]]*/, "", line)
          if (line ~ /^#/ || line !~ /^[[:alpha:]]+[[:space:]]+/) next
          directive = line
          sub(/[[:space:]].*$/, "", directive)
          if (tolower(directive) != "customlog" && tolower(directive) != "errorlog" &&
              tolower(directive) != "transferlog") next
          sub(/^[^[:space:]]+[[:space:]]+/, "", line)
          if (substr(line, 1, 1) == "\"" || substr(line, 1, 1) == "\047") line = substr(line, 2)
          if (substr(line, 1, 2) == "|$") mode = "shell"
          else if (substr(line, 1, 1) == "|") mode = "direct"
          else next
          if (shown++ < 2) print directive ": " mode " pipeline (command redacted)"
        }
      ' 2>/dev/null)
      [ -n "$lp_apache_lines" ] || continue
      if [ "$lp_apache_found" -eq 0 ]; then
        printf '%s\n' 'Apache piped-log directives (bounded, read-only):'
        lp_apache_found=1
      fi
      # Limit the displayed basename; never print the directive's arguments.
      lp_apache_name=$(basename "$lp_apache_file" | tr -cd '[:print:]' | cut -c 1-80)
      printf '  %s: %s\n' "$lp_apache_name" "$lp_apache_lines"
    done
  done
  [ "$lp_apache_found" -eq 0 ] || printf '%s\n' 'A piped logger is a review cue; its command and privilege depend on the Apache parent.'
  if [ "$lp_apache_checked" -ge 64 ] || [ "$(date +%s 2>/dev/null || printf 0)" -ge "$lp_apache_deadline" ]; then
    printf '%s\n' 'Apache piped-log inspection was partial (file or time cap reached).'
  fi
)

# remco can render key/value data into privileged service configuration. This
# checks only fixed local configuration and templates; it never contacts the
# backend or invokes the target service.
lp_remco_apache_template_check() (
  [ "$SEARCH_IN_FOLDER" ] && return 0
  lp_remco_config=${1:-/etc/remco/config}
  lp_remco_templates=${2:-/etc/remco/templates}
  [ -e "$lp_remco_config" ] || return 0
  if [ ! -f "$lp_remco_config" ] || [ ! -r "$lp_remco_config" ] || [ -L "$lp_remco_config" ]; then
    printf '%s\n' 'remco configuration exists; static linkage unknown (unreadable or non-regular file).'
    return 0
  fi
  if [ -z "$TIMEOUT" ] || [ ! -x "$TIMEOUT" ]; then
    printf '%s\n' 'remco configuration exists; bounded inspection unavailable (timeout missing).'
    return 0
  fi
  lp_remco_deadline=$(($(date +%s 2>/dev/null || printf 0) + 4))
  lp_remco_size=$("$TIMEOUT" 1 dd if="$lp_remco_config" bs=65537 count=1 2>/dev/null | wc -c | tr -d '[:space:]')
  case "$lp_remco_size" in ''|*[!0-9]*) lp_remco_size=0 ;; esac
  if [ "$lp_remco_size" -eq 0 ] || [ "$lp_remco_size" -gt 65536 ]; then
    printf '%s\n' 'remco configuration exists; static linkage unknown (empty, unreadable, or over 64 KiB).'
    return 0
  fi
  lp_remco_root=${3:-}
  if [ -z "$lp_remco_root" ]; then
    lp_remco_root=$("$TIMEOUT" 1 ps -eo user=,args= 2>/dev/null | awk '
      NR > 2048 { exit }
      $1 == "root" && $2 ~ /(^|\/)remco$/ && $0 !~ /[[:space:]]-config([=[:space:]]|$)/ { print "root"; exit }
    ')
  fi
  [ "$lp_remco_root" = root ] || lp_remco_root=unknown
  lp_remco_sources=$("$TIMEOUT" 1 dd if="$lp_remco_config" bs=65536 count=1 2>/dev/null | awk -v root="$lp_remco_templates" '
    function quoted(line, fields, n) {
      n = split(line, fields, "\"")
      return n >= 3 ? fields[2] : ""
    }
    function finish() {
      if (src != "" && index(src, root "/") == 1 && src !~ /\.\./ &&
          src ~ /^[[:alnum:]_.\/-]+$/ &&
          dst ~ /^\/etc\/(apache2|httpd)\// &&
          reload ~ /(apache2|httpd)/ && reload ~ /(restart|reload|graceful)/ &&
          backend && watch && keys && emitted++ < 8) print src
    }
    /^[[:space:]]*\[\[resource(\.template)?\]\][[:space:]]*$/ {
      finish(); src = dst = reload = ""; backend = watch = keys = 0; next
    }
    /^[[:space:]]*#/ { next }
    /^[[:space:]]*src[[:space:]]*=/ { src = quoted($0) }
    /^[[:space:]]*dst[[:space:]]*=/ { dst = quoted($0) }
    /^[[:space:]]*reload_cmd[[:space:]]*=/ { reload = tolower(quoted($0)) }
    /^[[:space:]]*\[resource.backend.etcd\]/ { backend = 1 }
    /^[[:space:]]*watch[[:space:]]*=[[:space:]]*true([[:space:]#]|$)/ { watch = 1 }
    /^[[:space:]]*keys[[:space:]]*=[[:space:]]*\[[[:space:]]*"/ { keys = 1 }
    END { finish() }
  ' 2>/dev/null)
  [ -n "$lp_remco_sources" ] || return 0
  lp_remco_checked=0
  printf '%s\n' 'remco watches a backend and reloads Apache from a template; template contents and backend write permission require review.'
  printf '%s\n' "$lp_remco_sources" | while IFS= read -r lp_remco_source; do
    [ "$lp_remco_checked" -lt 8 ] || break
    [ "$(date +%s 2>/dev/null || printf 0)" -lt "$lp_remco_deadline" ] || break
    lp_remco_checked=$((lp_remco_checked + 1))
    [ -f "$lp_remco_source" ] && [ -r "$lp_remco_source" ] && [ ! -L "$lp_remco_source" ] || continue
    lp_remco_template_size=$("$TIMEOUT" 1 dd if="$lp_remco_source" bs=32769 count=1 2>/dev/null | wc -c | tr -d '[:space:]')
    case "$lp_remco_template_size" in ''|*[!0-9]*) lp_remco_template_size=0 ;; esac
    [ "$lp_remco_template_size" -gt 0 ] && [ "$lp_remco_template_size" -le 32768 ] || continue
    lp_remco_raw=$("$TIMEOUT" 1 dd if="$lp_remco_source" bs=32768 count=1 2>/dev/null | awk '
      /^[[:space:]]*(ServerName|ServerAlias|DocumentRoot|CustomLog|ErrorLog|TransferLog|Include|LoadModule)[[:space:]]+/ &&
      /\{\{[[:space:]]*getv[[:space:]]*\([^}]*\)[[:space:]]*\}\}/ { print "yes"; exit }
    ' 2>/dev/null)
    [ "$lp_remco_raw" = yes ] || continue
    if [ "$lp_remco_root" = root ]; then
      printf '%s\n' 'Candidate: root remco process and watched backend value rendered directly into Apache configuration (backend write permission unknown).'
    else
      printf '%s\n' 'Candidate: watched backend value rendered directly into Apache configuration (remco process privilege and backend write permission unknown).'
    fi
    break
  done
)


peass{Apache-Nginx}
