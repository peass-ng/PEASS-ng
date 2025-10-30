# Title: Processes & Cron & Services & Timers - Debuggable runtimes exposure
# ID: PR_Debuggable_runtimes_exposure
# Author: HT Bot
# Last Update: 30-10-2025
# Description: Detect locally-exposed debuggers that can lead to code execution:
#   - Node.js V8 Inspector (node --inspect/--inspect-brk)
#   - Chrome/Chromium DevTools ( --remote-debugging-port ) and ChromeDriver (9515)
#   Correlates process cmdlines and listening sockets to highlight potential LPE vectors.
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $E, $SED_RED, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $printed, $found, $bind, $pid, $port, $exe, $user, $cmd, $out, $val
# Fat linpeas: 0
# Small linpeas: 1

# Helper: read a proc cmdline (space separated)
_lp_read_cmdline(){
  tr '\0' ' ' 2>/dev/null <"/proc/$1/cmdline"
}

# Helper: best-effort bind address discovery for pid:port using ss/lsof
_lp_find_bind_for_pid_port(){
  local pid="$1"; local port="$2"; local out=""
  if command -v ss >/dev/null 2>&1; then
    out=$(ss -H -ltnp 2>/dev/null | awk -v p=":${port}" -v pid="$pid" '$1 ~ /LISTEN/ && $4 ~ p && $0 ~ ("pid=" pid) {print $4; exit}')
  fi
  if [ -z "$out" ] && command -v lsof >/dev/null 2>&1; then
    # Print first matching nADDR for this pid/port
    out=$(lsof -nP -iTCP:"$port" -sTCP:LISTEN -Fpcn 2>/dev/null | awk -v pid="$pid" 'BEGIN{p=0} /^p/{p=($0=="p"pid)} p && /^n/{gsub("^n","",$0); print $0; exit}')
  fi
  echo "$out"
}

# Helper: extract inspector port from node cmdline (defaults to 9229)
_lp_node_inspect_port(){
  local cmd="$1"; local port=""
  # --inspect-port=NNN
  port=$(printf "%s" "$cmd" | sed -n -E 's/.*--inspect-port=([0-9]{2,5}).*/\1/p' | head -n1)
  if [ -n "$port" ]; then echo "$port"; return; fi
  # --inspect=HOST:PORT or --inspect=PORT
  local val
  val=$(printf "%s" "$cmd" | sed -n -E 's/.*--inspect=([^ ]+).*/\1/p' | head -n1)
  if [ -n "$val" ]; then
    case "$val" in
      *:*) port=${val##*:} ;;
      *) port="$val" ;;
    esac
    # ensure numeric
    if printf "%s" "$port" | grep -qE '^[0-9]{2,5}$'; then echo "$port"; return; fi
  fi
  # bare --inspect or --inspect-brk
  echo 9229
}

# Node.js Inspector discovery
_lp_check_node_inspector(){
  local found=0
  # Iterate over /proc (works without ps binaries)
  for d in /proc/[0-9]*; do
    [ -d "$d" ] || continue
    local pid=${d##*/}
    local exe user cmd
    exe=$(readlink "$d/exe" 2>/dev/null)
    [ -n "$exe" ] || continue
    case "$exe" in
      *node|*nodejs) : ;;  # candidate
      *) continue ;;
    esac
    cmd=$(_lp_read_cmdline "$pid")
    # Look for inspector flags
    if printf "%s" "$cmd" | grep -aE -- '--inspect(=|$)|--inspect-brk|--inspect-port=' >/dev/null 2>&1; then
      user=$(stat -c %U "$d" 2>/dev/null)
      local port bind
      port=$(_lp_node_inspect_port "$cmd")
      bind=$(_lp_find_bind_for_pid_port "$pid" "$port")
      [ -z "$bind" ] && bind="127.0.0.1:$port?"  # best-effort default
      if [ $found -eq 0 ]; then
        print_2title "Debuggable runtimes exposure (Node inspector)"
        print_info "Attaching to node --inspect allows arbitrary JS execution in target process. Consider SSH local port forwarding if you only have restricted access."
        found=1
      fi
      printf "  %-8s %-7s %-22s %s\n" "$user" "$pid" "$bind" "$(printf "%s" "$cmd" | sed 's/\s\+/ /g' | cut -c1-140)" | sed -${E} "s,root,${SED_RED}," | sed -${E} "s,(127\.0\.0\.1|::1):[0-9]+,${SED_RED_YELLOW},g"
    fi
  done
  [ $found -eq 1 ] && echo ""
}

# Chrome/Chromium DevTools and ChromeDriver discovery
_lp_check_chrome_debug(){
  local printed=0
  for d in /proc/[0-9]*; do
    [ -d "$d" ] || continue
    local pid=${d##*/}
    local exe user cmd
    exe=$(readlink "$d/exe" 2>/dev/null)
    [ -n "$exe" ] || continue
    cmd=$(_lp_read_cmdline "$pid")

    # ChromeDriver exposes a WebDriver HTTP service (default 9515)
    if printf "%s" "$exe" | grep -qE '/chromedriver$'; then
      user=$(stat -c %U "$d" 2>/dev/null)
      local port=9515 bind
      bind=$(_lp_find_bind_for_pid_port "$pid" "$port")
      [ -z "$bind" ] && bind=":$port?"
      if [ $printed -eq 0 ]; then
        print_2title "Debuggable runtimes exposure (Chrome DevTools/ChromeDriver)"
        print_info "Chrome/Chromium debug endpoints can be abused to run arbitrary commands when misconfigured (for example via goog:chromeOptions.binary)."
        printed=1
      fi
      printf "  %-8s %-7s %-22s %s\n" "$user" "$pid" "$bind" "chromedriver" | sed -${E} "s,root,${SED_RED}," | sed -${E} "s,(127\.0\.0\.1|::1):[0-9]+,${SED_RED_YELLOW},g"
      continue
    fi

    # Chrome/Chromium with --remote-debugging-port
    if printf "%s" "$exe" | grep -qE '/(chrome|chromium|google-chrome|headless_shell)$' || printf "%s" "$cmd" | grep -aqi 'remote-debugging-port'; then
      if printf "%s" "$cmd" | grep -aqi -- '--remote-debugging-port'; then
        user=$(stat -c %U "$d" 2>/dev/null)
        local port bind
        port=$(printf "%s" "$cmd" | sed -n -E 's/.*--remote-debugging-port=([0-9]{2,5}).*/\1/p' | head -n1)
        [ -z "$port" ] && port=9222
        bind=$(_lp_find_bind_for_pid_port "$pid" "$port")
        [ -z "$bind" ] && bind=":$port?"
        if [ $printed -eq 0 ]; then
          print_2title "Debuggable runtimes exposure (Chrome DevTools/ChromeDriver)"
          print_info "Attaching to Chrome DevTools as a privileged user may allow code execution. Prefer to restrict or disable in production."
          printed=1
        fi
        printf "  %-8s %-7s %-22s %s\n" "$user" "$pid" "$bind" "$(printf "%s" "$cmd" | sed 's/\s\+/ /g' | cut -c1-140)" | sed -${E} "s,root,${SED_RED}," | sed -${E} "s,(127\.0\.0\.1|::1):[0-9]+,${SED_RED_YELLOW},g"
      fi
    fi
  done
  [ $printed -eq 1 ] && echo ""
}

_lp_check_node_inspector
_lp_check_chrome_debug
