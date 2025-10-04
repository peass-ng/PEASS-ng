# Title: Container - Writable bind mounts without nosuid (SUID risk)
# ID: CT_RW_bind_mounts_nosuid
# Author: HT Bot
# Last Update: 17-09-2025
# Description: Detect writable bind-mounted paths inside containers that are not mounted with nosuid.
#   If the container user is root and the mount is a host bind mount without nosuid, an attacker may
#   be able to drop a SUID binary on the shared path and execute it from the host to escalate to root
#   (classic container-to-host breakout via writable bind mount).
# License: GNU GPL
# Version: 1.0
# Functions Used: containerCheck, print_2title, print_list, print_info
# Global Variables: $inContainer
# Initial Functions: containerCheck
# Generated Global Variables: $CT_RW_bind_mounts_matches
# Fat linpeas: 0
# Small linpeas: 1

containerCheck

if [ "$inContainer" ]; then
  echo ""
  print_2title "Container - Writable bind mounts w/o nosuid (SUID persistence risk)"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/docker-security/docker-breakout-privilege-escalation/index.html#writable-bind-mounts"

  if [ -r /proc/self/mountinfo ]; then
    CT_RW_bind_mounts_matches=$(grep -E "(^| )bind( |$)" /proc/self/mountinfo 2>/dev/null | grep -E "(^|,)rw(,|$)" | grep -v "nosuid" || true)
  else
    CT_RW_bind_mounts_matches=$(mount -l 2>/dev/null | grep -E "bind" | grep -E "(^|,)rw(,|$)" | grep -v "nosuid" || true)
  fi

  if [ -z "$CT_RW_bind_mounts_matches" ]; then
    print_list "Writable bind mounts without nosuid ............ No"
  else
    print_list "Writable bind mounts without nosuid ............ Yes" | sed -${E} "s,Yes,${SED_RED},"
    echo "$CT_RW_bind_mounts_matches" | sed -${E} "s,/proc/self/mountinfo,${SED_GREEN},"
    echo ""
    if [ "$(id -u 2>/dev/null)" = "0" ]; then
      print_list "Note"; echo ": You are root inside a container and there are writable bind mounts without nosuid." | sed -${E} "s,.*,${SED_RED},"
      echo "  If the path is shared with the host and executable there, you may plant a SUID binary (e.g., copy /bin/bash and chmod 6777)"
      echo "  and execute it from the host to obtain root. Ensure proper authorization before testing."
    else
      print_list "Note"; echo ": Current user is not root; if you obtain container root, these mounts may enable host escalation via SUID planting." | sed -${E} "s,.*,${SED_RED},"
    fi
  fi
  echo ""
fi
