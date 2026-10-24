# Title: Software Information - X.Org Server XInput modifier validation
# ID: SI_Xorg_server
# Author: HT Bot
# Last Update: 24-10-2026
# Description: Passively inventory Xorg installations and running privilege context for CVE-2026-93523; patch status requires vendor verification.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_2title, print_info
# Global Variables: $SEARCH_IN_FOLDER, $MACPEAS, $HOME
# Initial Functions:
# Generated Global Variables: $xorg_bin, $xorg_candidate, $xorg_packages, $xorg_pkg, $xorg_record, $xorg_pids, $xorg_pid, $xorg_uid, $xorg_exe, $xorg_display, $xorg_socket, $xorg_auth, $xorg_root
# Fat linpeas: 0
# Small linpeas: 1

if [ -z "$SEARCH_IN_FOLDER" ] && [ -z "$MACPEAS" ] && [ "$(uname -s 2>/dev/null)" = Linux ]; then
  xorg_bin=""
  for xorg_candidate in /usr/lib/xorg/Xorg /usr/lib64/xorg/Xorg /usr/bin/Xorg /usr/libexec/Xorg /usr/X11R6/bin/Xorg; do
    if [ -x "$xorg_candidate" ]; then
      xorg_bin="$xorg_candidate"
      break
    fi
  done
  if [ -z "$xorg_bin" ]; then
    xorg_candidate=$(command -v Xorg 2>/dev/null)
    [ -x "$xorg_candidate" ] && xorg_bin="$xorg_candidate"
  fi

  # Query exact server package names only; do not scan the package database.
  xorg_packages=""
  if command -v dpkg-query >/dev/null 2>&1; then
    for xorg_pkg in xserver-xorg-core xorg-server; do
      xorg_record=$(dpkg-query -W -f='$''{Status} $''{Version}\n' "$xorg_pkg" 2>/dev/null)
      case "$xorg_record" in
        'install ok installed '*) xorg_packages="${xorg_packages}${xorg_pkg}: ${xorg_record#install ok installed }
" ;;
      esac
    done
  elif command -v rpm >/dev/null 2>&1; then
    for xorg_pkg in xorg-x11-server-Xorg xorg-x11-server xorg-server; do
      xorg_record=$(rpm -q --qf '%{NAME} %{VERSION}-%{RELEASE}\n' "$xorg_pkg" 2>/dev/null) || continue
      xorg_packages="${xorg_packages}${xorg_record}
"
    done
  elif command -v pacman >/dev/null 2>&1; then
    xorg_packages=$(pacman -Q xorg-server 2>/dev/null)
  elif command -v apk >/dev/null 2>&1; then
    xorg_packages=$(apk info -v xorg-server 2>/dev/null)
  fi

  xorg_pids=""
  if [ -d /proc ]; then
    if command -v pgrep >/dev/null 2>&1; then
      xorg_pids=$(pgrep -x Xorg 2>/dev/null | head -n 8)
    elif command -v ps >/dev/null 2>&1; then
      xorg_pids=$(ps -e -o pid= -o comm= 2>/dev/null | awk '$2 == "Xorg" { print $1; if (++n == 8) exit }')
    fi
  fi

  if [ -n "$xorg_bin" ] || [ -n "$xorg_packages" ] || [ -n "$xorg_pids" ]; then
    print_2title "X.Org Server XInput review (CVE-2026-93523)" "T1068"
    print_info "https://www.zerodayinitiative.com/advisories/ZDI-26-768/"
    [ -z "$xorg_bin" ] || printf 'Installed Xorg executable: %s\n' "$xorg_bin"
    [ -z "$xorg_packages" ] || printf 'Installed Xorg package(s):\n%s\n' "$xorg_packages"

    xorg_root=0
    for xorg_pid in $xorg_pids; do
      [ -r "/proc/$xorg_pid/status" ] || continue
      xorg_uid=$(awk '$1 == "Uid:" { print $3; exit }' "/proc/$xorg_pid/status" 2>/dev/null)
      xorg_exe=$(readlink "/proc/$xorg_pid/exe" 2>/dev/null)
      [ -n "$xorg_exe" ] || xorg_exe="path unavailable"
      case "$xorg_uid" in
        0) xorg_root=1; printf 'Running Xorg PID %s: effective UID 0, executable %s\n' "$xorg_pid" "$xorg_exe" ;;
        ''|*[!0-9]*) printf 'Running Xorg PID %s: UID unavailable, executable %s\n' "$xorg_pid" "$xorg_exe" ;;
        *) printf 'Running Xorg PID %s: rootless (effective UID %s), executable %s\n' "$xorg_pid" "$xorg_uid" "$xorg_exe" ;;
      esac
    done

    if [ "$xorg_root" -eq 1 ]; then
      # This is only a session-access clue, not an authentication or exploit test.
      xorg_display=$(env | sed -n 's/^DISPLAY=//p' | head -n 1)
      case "$xorg_display" in
        :[0-9]*)
          xorg_display=${xorg_display#:}
          xorg_display=${xorg_display%%.*}
          case "$xorg_display" in
            ''|*[!0-9]*) ;;
            *)
              xorg_socket="/tmp/.X11-unix/X$xorg_display"
              xorg_auth=$(env | sed -n 's/^XAUTHORITY=//p' | head -n 1)
              [ -n "$xorg_auth" ] || xorg_auth="$HOME/.Xauthority"
              if [ -S "$xorg_socket" ]; then
                printf 'Current DISPLAY has local socket %s' "$xorg_socket"
                [ -r "$xorg_auth" ] && printf ' and readable Xauthority file'
                printf ' (server/authentication match unverified).\n'
              fi
              ;;
          esac
          ;;
      esac
    fi

    printf '%s\n' 'Potentially vulnerable - verify the vendor package/backport and running binary contain upstream fix 37a1847a60c6. No affected-version range is published; the package version alone cannot establish the running binary patch state.'
    [ "$xorg_root" -eq 0 ] || printf '%s\n' 'A root-run Xorg process increases potential local privilege-escalation impact; socket presence alone does not establish access.'
    echo ""
  fi
fi
