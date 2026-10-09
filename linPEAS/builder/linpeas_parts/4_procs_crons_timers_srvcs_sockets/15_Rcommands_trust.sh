# Title: Processes & Cron & Services & Timers - Legacy r-commands and host-based trust
# ID: PR_Rcommands_trust
# Author: HT Bot
# Last Update: 27-08-2025
# Description: Detect legacy r-services (rsh/rlogin/rexec) exposure and dangerous host-based trust (.rhosts/hosts.equiv),
#              which can allow passwordless root via hostname/DNS manipulation.
# License: GNU GPL
# Version: 1.0
# Mitre: T1021.004
# Functions Used: print_2title, print_3title, echo_not_found
# Global Variables:
# Initial Functions:
# Generated Global Variables: $rfile, $perms, $owner, $size, $metadata, $metadata_rest, $any_rhosts, $rhosts_scanned, $shown, $f, $p
# Fat linpeas: 0
# Small linpeas: 1

rcommands_trust_metadata() {
  # GNU and BSD/OpenBSD stat use different format flags.
  stat -c '%a %U %s' "$1" 2>/dev/null || stat -f '%Lp %Su %z' "$1" 2>/dev/null
}

rcommands_trust_group_other_writable() {
  case "$1" in
    [0-7][0-7][0-7]|[0-7][0-7][0-7][0-7])
      [ "$((0${1} & 0022))" -ne 0 ]
      ;;
    *) return 1 ;;
  esac
}

rcommands_trust_entries() {
  # Read at most 64 KiB and stop after one extra non-comment entry.
  dd if="$1" bs=65536 count=1 2>/dev/null | LC_ALL=C awk -v size="$2" '
    /^[[:space:]]*(#|$)/ { next }
    {
      entries++
      if (entries > 64) {
        print "    [!] Trust entries truncated after 64 lines"
        exit
      }
      if (length($0) > 512) print "    " substr($0, 1, 512) " ... [line shortened]"
      else print "    " $0
      if ($1 !~ /^[-+@]/ && $1 !~ /:/ && $1 ~ /[[:alpha:]]/) hostname = 1
      if ($2 == "+") any_user = 1
      if ($1 == "+" || $2 == "+") wildcard = 1
    }
    END {
      if (size == "" || size + 0 > 65536) print "    [!] Trust-file input limited to first 64 KiB"
      if (hostname) print "    [!] Hostname-based trust candidate: review control of DNS and local name mappings"
      if (any_user) print "    [!] Remote-user + may broaden trust (service and PAM policy still apply)"
      if (wildcard) print "    [!] Wildcard '\''+'\'' trust found"
    }
  '
}

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Legacy r-commands (rsh/rlogin/rexec) and host-based trust" "T1021.004"
  echo ""
  print_3title "Listening r-services (TCP 512-514)" "T1021.004"
  if command -v ss >/dev/null 2>&1; then
    ss -ltnp 2>/dev/null | awk '$1 ~ /^LISTEN$/ && $4 ~ /:(512|513|514)$/ {print}' || echo_not_found "ss"
  elif command -v netstat >/dev/null 2>&1; then
    netstat -ltnp 2>/dev/null | awk '$6 ~ /LISTEN/ && $4 ~ /:(512|513|514)$/ {print}' || echo_not_found "netstat"
  else
    echo_not_found "ss|netstat"
  fi

  echo ""
  print_3title "systemd units exposing r-services" "T1021.004"
  if command -v systemctl >/dev/null 2>&1; then
    systemctl list-unit-files 2>/dev/null | grep -E '^(rlogin|rsh|rexec)\.(socket|service)\b' || echo_not_found "rlogin|rsh|rexec units"
    systemctl list-sockets 2>/dev/null | grep -E '\b(rlogin|rsh|rexec)\.socket\b' || true
  else
    echo_not_found "systemctl"
  fi

  echo ""
  print_3title "inetd/xinetd configuration for r-services" "T1021.004"
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
  print_3title "Installed r-service server packages" "T1021.004"
  if command -v dpkg >/dev/null 2>&1; then
    dpkg -l 2>/dev/null | grep -E '\b(rsh-server|rsh-redone-server|krb5-rsh-server|inetutils-inetd|openbsd-inetd|xinetd|netkit-rsh)\b' || echo "  No related packages found via dpkg"
  elif command -v rpm >/dev/null 2>&1; then
    rpm -qa 2>/dev/null | grep -Ei '\b(rsh|rlogin|rexec|xinetd)\b' || echo "  No related packages found via rpm"
  else
    echo_not_found "dpkg|rpm"
  fi

  echo ""
  print_3title "/etc/hosts.equiv and /etc/shosts.equiv" "T1021.004"
  for f in /etc/hosts.equiv /etc/shosts.equiv; do
    if [ -f "$f" ]; then
      metadata=$(rcommands_trust_metadata "$f")
      if [ -n "$metadata" ]; then
        perms=${metadata%% *}
        metadata_rest=${metadata#* }
        owner=${metadata_rest%% *}
        size=${metadata_rest##* }
        echo "  $f (perm $perms, owner $owner)"
      else
        size=
        echo "  $f (metadata unavailable)"
      fi
      rcommands_trust_entries "$f" "$size"
    fi
  done

  echo ""
  print_3title "Per-user .rhosts files" "T1021.004"
  any_rhosts=false
  rhosts_scanned=0
  for rfile in /root/.rhosts /home/*/.rhosts; do
    if [ -f "$rfile" ]; then
      rhosts_scanned=$((rhosts_scanned + 1))
      if [ "$rhosts_scanned" -gt 64 ]; then
        echo "  [!] .rhosts discovery limited to first 64 files; coverage is partial"
        break
      fi
      any_rhosts=true
      metadata=$(rcommands_trust_metadata "$rfile")
      if [ -n "$metadata" ]; then
        perms=${metadata%% *}
        metadata_rest=${metadata#* }
        owner=${metadata_rest%% *}
        size=${metadata_rest##* }
        echo "  $rfile (perm $perms, owner $owner)"
      else
        perms=
        size=
        echo "  $rfile (metadata unavailable)"
      fi
      rcommands_trust_entries "$rfile" "$size"
      # Warn on insecure perms (group/other write)
      if rcommands_trust_group_other_writable "$perms"; then
        echo "    [!] Insecure permissions (group/other write)"
      fi
    fi
  done
  if ! $any_rhosts; then echo_not_found ".rhosts"; fi

  echo ""
  print_3title "PAM rhosts authentication" "T1021.004"
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
  print_3title "SSH HostbasedAuthentication" "T1021.004"
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
  print_3title "Potential DNS control indicators (local)" "T1021.004"
  (ps -eo comm,args 2>/dev/null | grep -Ei '(^|/)(pdns|pdns_server|pdns_recursor|powerdns-admin)( |$)' | grep -Ev 'grep|bash' || echo "  Not detected")

  echo ""
fi
