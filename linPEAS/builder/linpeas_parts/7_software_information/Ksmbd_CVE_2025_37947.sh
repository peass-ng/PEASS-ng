# Title: Software Information - KSMBD streams_xattr exposure (CVE-2025-37947)
# ID: SI_Ksmbd_CVE_2025_37947
# Author: HT Bot
# Last Update: 16-10-2025
# Description: Detect ksmbd kernel server exposure to CVE-2025-37947 when a writable share enables streams_xattr.
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables: $kernel_rel, $findings, $soft_hits, $any_streams, $confs, $section, $conf
# Fat linpeas: 0
# Small linpeas: 1

# This check aims to be lightweight and avoid false positives:
#  - It requires ksmbd to be present/active (kernel module or service)
#  - It parses common Samba/ksmbd config files and flags shares that both enable
#    streams_xattr and are writable (writeable=yes|writable=yes|read only=no)

ksmbd_is_active() {
  # ksmbd can be detected as a kernel module or via its userspace helper
  if [ -d /sys/module/ksmbd ] || [ -e /proc/fs/ksmbd ]; then
    return 0
  fi
  if command -v lsmod >/dev/null 2>&1 && lsmod 2>/dev/null | grep -qw "^ksmbd"; then
    return 0
  fi
  if command -v systemctl >/dev/null 2>&1 && systemctl is-active ksmbd >/dev/null 2>&1; then
    return 0
  fi
  # As a best-effort fallback, check if something is listening on 445 without smbd
  if command -v ss >/dev/null 2>&1; then
    if ss -H -ltpn 2>/dev/null | grep -qE "\bLISTEN\b.*:445\b"; then
      # If smbd is not the listener PID (ksmbd is in-kernel, no PID shown)
      if ! pgrep -x smbd >/dev/null 2>&1; then
        return 0
      fi
    fi
  fi
  return 1
}

find_streams_writable_shares() {
  # Parse typical config locations for Samba/ksmbd
  # Note: ksmbd-tools often uses /etc/ksmbd/ksmbd.conf with Samba-like syntax
  local confs
  confs="/etc/ksmbd/ksmbd.conf /etc/ksmbd/smb.conf /etc/samba/smb.conf /usr/local/etc/smb.conf"
  for f in $confs; do
    [ -r "$f" ] || continue
    # Use awk to find sections that enable streams_xattr and are writable
    awk -v IGNORECASE=1 -v file="$f" '
      function flush(){
        if (section!="" && has_streams && writable) {
          printf("%s|%s\n", file, section);
        }
      }
      BEGIN{ section=""; has_streams=0; writable=0; }
      {
        line=$0;
        # strip trailing comments
        sub(/[;#].*$/, "", line);
        if ($0 ~ /^\s*\[/) {
          flush();
          section=$0; gsub(/^\s*\[/, "", section); gsub(/\]\s*$/, "", section);
          has_streams=0; writable=0;
        }
        if (line ~ /vfs[[:space:]]+objects[[:space:]]*=.*streams_xattr/)
          has_streams=1;
        if (line ~ /streams[ _]?xattr[[:space:]]*=[[:space:]]*(yes|true|on)/)
          has_streams=1;
        if (line ~ /writ(e)?able[[:space:]]*=[[:space:]]*(yes|true|on)/)
          writable=1;
        if (line ~ /read[[:space:]]*only[[:space:]]*=[[:space:]]*(no|false|off)/)
          writable=1;
      }
      END{ flush(); }
    ' "$f"
  done
}

print_ksmbd_header() {
  print_2title "KSMBD streams_xattr exposure (CVE-2025-37947)"
  print_info "Checks if ksmbd is active and a writable share enables streams_xattr (precondition for CVE-2025-37947)."
  print_info "Reference: CVE-2025-37947 - ksmbd_vfs_stream_write out-of-bounds write in streams_xattr path"
}

if ksmbd_is_active || [ "$DEBUG" ]; then
  print_ksmbd_header
  kernel_rel=$(uname -r 2>/dev/null)
  echo "Kernel: $kernel_rel" 2>/dev/null

  findings=$(find_streams_writable_shares)
  if [ -n "$findings" ]; then
    echo "$findings" | while IFS='|' read -r conf section; do
      printf "Potentially vulnerable share: [%s] in %s\n" "$section" "$conf" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    done
    echo "  └─ ksmbd detected and streams_xattr enabled on writable share(s). Host likely exploitable for local LPE (CVE-2025-37947)." | sed -${E} "s,.*,${SED_RED},"
  else
    # If streams_xattr is enabled but not writable, provide a softer note
    soft_hits=$(find_streams_writable_shares | wc -l 2>/dev/null)
    if [ "$soft_hits" -eq 0 ]; then
      # Search for any streams_xattr mention to help manual triage
      any_streams=$(grep -Rils --include='*.conf' 'streams_xattr' /etc/ksmbd /etc/samba /usr/local/etc 2>/dev/null | head -n 3)
      if [ -n "$any_streams" ]; then
        echo "streams_xattr referenced in configuration but no writable share detected:" | sed -${E} "s,.*,${SED_YELLOW},"
        printf "%s\n" "$any_streams" | sed -${E} "s,.*,${SED_YELLOW},"
      else
        echo "ksmbd detected, but no writable share with streams_xattr found." | sed -${E} "s,.*,${SED_GREEN},"
      fi
    fi
  fi
  echo ""
fi
