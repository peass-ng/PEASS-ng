# Title: Processes & Cron & Services & Timers - Legacy r-commands and host-based trust
# ID: PR_Rcommands_trust
# Author: HT Bot
# Last Update: 27-08-2025
# Description: Detect legacy r-services (rsh/rlogin/rexec) exposure and dangerous host-based trust (.rhosts/hosts.equiv),
#              which can allow passwordless root via hostname/DNS manipulation.
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, echo_not_found
# Global Variables:
# Initial Functions:
# Generated Global Variables: $rfile, $perms, $owner, $g, $o, $any_rhosts, $shown, $f, $p
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Legacy r-commands (rsh/rlogin/rexec) and host-based trust"

  echo ""
  print_3title "Listening r-services (TCP 512-514)"
  if command -v ss >/dev/null 2>&1; then
    ss -ltnp 2>/dev/null | awk '$1 ~ /^LISTEN$/ && $4 ~ /:(512|513|514)$/ {print}' || echo_not_found "ss"
  elif command -v netstat >/dev/null 2>&1; then
    netstat -ltnp 2>/dev/null | awk '$6 ~ /LISTEN/ && $4 ~ /:(512|513|514)$/ {print}' || echo_not_found "netstat"
  else
    echo_not_found "ss|netstat"
  fi

  echo ""
  print_3title "systemd units exposing r-services"
  if command -v systemctl >/dev/null 2>&1; then
    systemctl list-unit-files 2>/dev/null | grep -E '^(rlogin|rsh|rexec)\.(socket|service)\b' || echo_not_found "rlogin|rsh|rexec units"
    systemctl list-sockets 2>/dev/null | grep -E '\b(rlogin|rsh|rexec)\.socket\b' || true
  else
    echo_not_found "systemctl"
  fi

  echo ""
  print_3title "inetd/xinetd configuration for r-services"
  if [ -f /etc/inetd.conf ]; then
    grep -vE '^\s*#|^\s*$' /etc/inetd.conf 2>/dev/null | grep -Ei '\b(shell|login|exec|rsh|rlogin|rexec)\b' 2>/dev/null || echo "  No r-services found in /etc/inetd.conf"
  else
    echo_not_found "/etc/inetd.conf"
  fi
  if [ -d /etc/xinetd.d ]; then
    # Print enabled r-services in xinetd
    for f in /etc/xinetd.d/*; do
      [ -f "$f" ] || continue
      if grep -qiE '\b(service|disable)\b' "$f" 2>/dev/null; then
        if grep -qiE 'service\s+(rsh|rlogin|rexec|shell|login|exec)\b' "$f" 2>/dev/null; then
          # Only warn if not disabled
          if ! grep -qiE '^\s*disable\s*=\s*yes\b' "$f" 2>/dev/null; then
            echo "  $(basename "$f") may enable r-services:"; grep -iE '^(\s*service|\s*disable)' "$f" 2>/dev/null | sed 's/^/    /'
          fi
        fi
      fi
    done
  else
    echo_not_found "/etc/xinetd.d"
  fi

  echo ""
  print_3title "Installed r-service server packages"
  if command -v dpkg >/dev/null 2>&1; then
    dpkg -l 2>/dev/null | grep -E '\b(rsh-server|rsh-redone-server|krb5-rsh-server|inetutils-inetd|openbsd-inetd|xinetd|netkit-rsh)\b' || echo "  No related packages found via dpkg"
  elif command -v rpm >/dev/null 2>&1; then
    rpm -qa 2>/dev/null | grep -Ei '\b(rsh|rlogin|rexec|xinetd)\b' || echo "  No related packages found via rpm"
  else
    echo_not_found "dpkg|rpm"
  fi

  echo ""
  print_3title "/etc/hosts.equiv and /etc/shosts.equiv"
  for f in /etc/hosts.equiv /etc/shosts.equiv; do
    if [ -f "$f" ]; then
      perms=$(stat -c %a "$f" 2>/dev/null)
      owner=$(stat -c %U "$f" 2>/dev/null)
      echo "  $f (perm $perms, owner $owner)"
      # Print non-comment lines
      awk 'NF && $0 !~ /^\s*#/ {print "    " $0}' "$f" 2>/dev/null
      if grep -qEv '^\s*#|^\s*$' "$f" 2>/dev/null; then
        if grep -qE '(^|\s)\+' "$f" 2>/dev/null; then
          echo "    [!] Wildcard '+' trust found"
        fi
      fi
    fi
  done

  echo ""
  print_3title "Per-user .rhosts files"
  any_rhosts=false
  for rfile in /root/.rhosts /home/*/.rhosts; do
    if [ -f "$rfile" ]; then
      any_rhosts=true
      perms=$(stat -c %a "$rfile" 2>/dev/null)
      owner=$(stat -c %U "$rfile" 2>/dev/null)
      echo "  $rfile (perm $perms, owner $owner)"
      awk 'NF && $0 !~ /^\s*#/ {print "    " $0}' "$rfile" 2>/dev/null
      # Warn on insecure perms (group/other write)
      g=$(printf "%s" "$perms" | cut -c2)
      o=$(printf "%s" "$perms" | cut -c3)
      if [ "${g:-0}" -ge 2 ] || [ "${o:-0}" -ge 2 ]; then
        echo "    [!] Insecure permissions (group/other write)"
      fi
    fi
  done
  if ! $any_rhosts; then echo_not_found ".rhosts"; fi

  echo ""
  print_3title "PAM rhosts authentication"
  shown=false
  for p in /etc/pam.d/rlogin /etc/pam.d/rsh; do
    if [ -f "$p" ]; then
      shown=true
      echo "  $p:"
      (grep -nEi 'pam_rhosts|pam_rhosts_auth' "$p" 2>/dev/null || echo "    no pam_rhosts* lines") | sed 's/^/    /'
    fi
  done
  if ! $shown; then echo_not_found "/etc/pam.d/rlogin|rsh"; fi

  echo ""
  print_3title "SSH HostbasedAuthentication"
  if [ -f /etc/ssh/sshd_config ]; then
    if grep -qiE '^[^#]*HostbasedAuthentication\s+yes' /etc/ssh/sshd_config 2>/dev/null; then
      echo "  HostbasedAuthentication yes (check /etc/shosts.equiv or ~/.shosts)"
    else
      echo "  HostbasedAuthentication no or not set"
    fi
  else
    echo_not_found "/etc/ssh/sshd_config"
  fi

  echo ""
  print_3title "Potential DNS control indicators (local)"
  (ps -eo comm,args 2>/dev/null | grep -Ei '(^|/)(pdns|pdns_server|pdns_recursor|powerdns-admin)( |$)' | grep -Ev 'grep|bash' || echo "  Not detected")

  echo ""
fi

