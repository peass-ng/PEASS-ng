# Title: Software Information - Control Web Panel user API
# ID: SI_Control_Web_Panel
# Author: HT Bot
# Last Update: 26-11-2026
# Description: Passively identify pre-1.29 CWP installations and the privileged local user API on TCP 2302.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: lp_version_lt, print_3title, print_info
# Global Variables: $MACPEAS, $SEARCH_IN_FOLDER, $E, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $cwp_version, $cwp_listener, $cwp_pid, $cwp_identity, $cwp_routes, $cwp_source_cues
# Fat linpeas: 0
# Small linpeas: 1

# CWP's root-owned user API is a local privilege boundary. Never send it a request.
if [ ! "$MACPEAS" ] && [ ! "$SEARCH_IN_FOLDER" ] && [ -d /usr/local/cwpsrv ]; then
  print_3title 'Control Web Panel user API (local root escalation)' 'T1068'
  print_info 'https://www.vulncheck.com/blog/control-web-panel-pre-auth-module-dispatch-rce'

  cwp_version=
  if command -v rpm >/dev/null 2>&1; then
    if command -v timeout >/dev/null 2>&1; then
      cwp_version=$(timeout 2 rpm -q --qf '%{VERSION}\n' webpanel 2>/dev/null | head -n 1)
    else
      cwp_version=$(rpm -q --qf '%{VERSION}\n' webpanel 2>/dev/null | head -n 1)
    fi
  fi
  if printf '%s\n' "$cwp_version" | LC_ALL=C grep -Eq '^[0-9]+\.[0-9]+(\.[0-9]+)?$'; then
    printf 'CWP webpanel package version: %s\n' "$cwp_version"
    if lp_version_lt "$cwp_version" '1.29'; then
      echo 'CWP before 1.29: user API root command injection and SQL/authentication flaws may allow a hosting account to become root (CVE-2026-77877 through CVE-2026-77881, CVE-2026-104080); verify the installed build.' | sed -${E} "s,.*,${SED_RED_YELLOW},g"
    else
      echo 'CWP version is at least 1.29; verify the installed build and vendor patches.'
    fi
  else
    echo 'CWP webpanel package version unavailable; verify the installed build (1.29 or later fixes the reported user API chain).'
  fi

  cwp_listener=
  if command -v ss >/dev/null 2>&1; then
    cwp_listener=$(ss -H -ltnp 2>/dev/null | LC_ALL=C awk 'NR > 4096 { exit } $1 == "LISTEN" && $4 ~ /:2302$/ { print; exit }')
  elif command -v netstat >/dev/null 2>&1; then
    cwp_listener=$(netstat -ltnp 2>/dev/null | LC_ALL=C awk 'NR > 4096 { exit } $6 == "LISTEN" && $4 ~ /[:.]2302$/ { print; exit }')
  fi
  if [ -n "$cwp_listener" ]; then
    printf 'TCP 2302 listener: %s\n' "$(printf '%s' "$cwp_listener" | cut -c1-256)"
    cwp_pid=$(printf '%s\n' "$cwp_listener" | sed -n 's/.*pid=\([0-9][0-9]*\).*/\1/p')
    if [ -z "$cwp_pid" ]; then
      cwp_pid=$(printf '%s\n' "$cwp_listener" | awk '$7 ~ /^[0-9]+\// { split($7, p, "/"); print p[1] }')
    fi
    if [ -n "$cwp_pid" ]; then
      cwp_identity=$(ps -p "$cwp_pid" -o user=,comm= 2>/dev/null | awk 'NR == 1 { print $1, $2 }')
      printf 'Listener process PID %s: %s\n' "$cwp_pid" "${cwp_identity:-identity unavailable}"
      case "$cwp_identity" in
        root\ *) echo 'TCP 2302 listener process runs as root; verify it belongs to CWP and that the user API is patched.' ;;
      esac
    else
      echo 'Listener PID unavailable to this user; ownership cannot be confirmed.'
    fi
  else
    echo 'TCP 2302 listener not observed (or socket inventory unavailable); service may be stopped or hidden.'
  fi

  # The shipped API is commonly ionCube-encoded. Only report bounded readable
  # route and sink markers as review leads; their presence is not proof of a flaw.
  if [ -r /usr/local/cwpsrv/var/services/user_api/v1/app/routes/api.php ]; then
    cwp_routes=$(LC_ALL=C head -c 65536 /usr/local/cwpsrv/var/services/user_api/v1/app/routes/api.php 2>/dev/null |
      LC_ALL=C grep -aEo 'restoredatabase|newpass|valid_token|user_panel_dashboard|access_token' | sort -u | tr '\n' ' ')
    [ -n "$cwp_routes" ] && printf 'Readable API route markers (review authentication and input handling): %s\n' "$cwp_routes"
    cwp_source_cues=$(LC_ALL=C head -c 65536 /usr/local/cwpsrv/var/services/user_api/v1/app/routes/api.php 2>/dev/null |
      LC_ALL=C grep -aEo 'shell_exec|authorization_code|client_id|passwd|mysql' | sort -u | tr '\n' ' ')
    [ -n "$cwp_source_cues" ] && printf 'Readable API shell/SQL markers (trace request parameters to sinks): %s\n' "$cwp_source_cues"
  fi
  echo 'Loopback binding alone does not protect a root API from a compromised hosting account; no API requests were made.'
fi
