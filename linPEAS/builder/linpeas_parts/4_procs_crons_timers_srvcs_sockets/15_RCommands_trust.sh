# Title: Processes & Cron & Services & Timers - r-commands trust (rsh/rlogin/rexec)
# ID: PR_RCommands_trust
# Author: HT Bot
# Last Update: 25-08-2025
# Description: Detect hostname-based trust for Berkeley r-commands and active listeners; warn about DNS-assisted abuse.
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, print_list, print_info, echo_no
# Global Variables: $SEARCH_IN_FOLDER, $E, $SED_RED, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $rhosts_found, $rsvc_listeners, $homes, $h, $f, $found
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Berkeley r-commands trust (rsh/rlogin/rexec)"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#r-commands-rlogin-rsh-rexec"

  rhosts_found=""

  # 1) Trust files: /etc/hosts.equiv and per-user ~/.rhosts
  print_list "Trust files (.rhosts / hosts.equiv)? ... "
  (
    # /etc/hosts.equiv
    if [ -r "/etc/hosts.equiv" ]; then
      printf "\n/etc/hosts.equiv (perm: %s)\n" "$(stat -c %a /etc/hosts.equiv 2>/dev/null || stat -f %p /etc/hosts.equiv 2>/dev/null)"
      # highlight risky entries: '+' or hosts granting any user
      sed -n "1,200p" /etc/hosts.equiv 2>/dev/null | sed -${E} "s,^\s*\+.*$,${SED_RED},; s,\s+\s*$,${SED_RED},"
      rhosts_found=1
    fi

    # Per-user .rhosts from passwd
    # Use getent if available, else parse /etc/passwd
    homes=$( (getent passwd 2>/dev/null || cat /etc/passwd 2>/dev/null) | awk -F: '{print $6}' | sort -u )
    for h in $homes; do
      f="$h/.rhosts"
      if [ -r "$f" ]; then
        printf "\n%s (perm: %s)\n" "$f" "$(stat -c %a "$f" 2>/dev/null || stat -f %p "$f" 2>/dev/null)"
        sed -n "1,200p" "$f" 2>/dev/null | sed -${E} "s,^\s*\+.*$,${SED_RED},; s,\s+\s*$,${SED_RED},"
        rhosts_found=1
      fi
    done

    # Common root path fallback
    if [ -r "/root/.rhosts" ] && ! echo "$homes" | grep -q "^/root$"; then
      printf "\n/root/.rhosts (perm: %s)\n" "$(stat -c %a /root/.rhosts 2>/dev/null || stat -f %p /root/.rhosts 2>/dev/null)"
      sed -n "1,200p" /root/.rhosts 2>/dev/null | sed -${E} "s,^\s*\+.*$,${SED_RED},; s,\s+\s*$,${SED_RED},"
      rhosts_found=1
    fi

    [ "$rhosts_found" ] || echo_no
  ) 2>/dev/null

  # 2) r-commands listeners (512 exec/rexec, 513 rlogin, 514 rsh)
  print_list "Are r-commands listening? ............ "
  rsvc_listeners=""
  if command -v ss >/dev/null 2>&1; then
    ss -tlpn 2>/dev/null | awk 'NR==1 || $4 ~ /:(512|513|514)$/ {print}' | sed -n '2,200p' | sed -${E} "s,.*,${SED_RED_YELLOW}," && rsvc_listeners=1
  elif command -v netstat >/dev/null 2>&1; then
    netstat -tlpn 2>/dev/null | awk 'NR==1 || $4 ~ /:(512|513|514)$/ {print}' | sed -n '2,200p' | sed -${E} "s,.*,${SED_RED_YELLOW}," && rsvc_listeners=1
  fi
  [ "$rsvc_listeners" ] || echo_no

  # 3) inetd/xinetd/systemd configuration hints
  print_list "rsh/rlogin/rexec enabled in inetd/xinetd? "
  (
    found=""
    [ -r /etc/inetd.conf ] && grep -E "(^|\s)(rsh|rlogin|rexec)(\s|$)" /etc/inetd.conf 2>/dev/null && found=1
    if ls /etc/xinetd.d/* >/dev/null 2>&1; then
      grep -E "(rsh|rlogin|rexec)" /etc/xinetd.d/* 2>/dev/null && found=1
    fi
    [ "$found" ] || echo_no
  )

  print_list "rsh/rlogin/rexec sockets in systemd? .. "
  (
    found=""
    if command -v systemctl >/dev/null 2>&1; then
      systemctl list-unit-files --type=socket --no-pager 2>/dev/null | grep -E "(rlogin|rsh|rexec)" && found=1
      systemctl list-sockets --no-pager 2>/dev/null | grep -E "(rlogin|rsh|rexec)" && found=1
    fi
    [ "$found" ] || echo_no
  )

  # 4) PAM rhosts trust
  print_list "PAM rhosts trust enabled? ............ "
  (
    found=""
    for p in /etc/pam.d/rlogin /etc/pam.d/rsh /etc/pam.d/rexec; do
      [ -r "$p" ] && grep -E "pam_rhosts|pam_rhosts_auth" "$p" 2>/dev/null && found=1
    done
    [ "$found" ] || echo_no
  )

  # 5) Container-to-host hint
  if [ -f "/.dockerenv" ]; then
    print_info "Running inside a container. If host runs r-commands and root/.rhosts trusts hostnames, aligning A+PTR DNS may allow passwordless rlogin/rsh to the host."
  fi

  # 6) Actionable guidance
  print_3title "Why risky and how to abuse"
  echo "- If a trusted entry is a hostname (not an IP) and r-services are listening, an attacker controlling DNS can set matching forward (A) and reverse (PTR) records so their IP resolves to the trusted name and reverse to the same name, passing hostname checks for passwordless access (even root if in /root/.rhosts or hosts.equiv)." | sed -${E} "s,passwordless access,${SED_RED_YELLOW},"
fi
