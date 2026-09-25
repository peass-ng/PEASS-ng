# Title: Container - LXD group root-equivalent access
# ID: CT_LXD_group
# Author: HT Bot
# Last Update: 25-09-2026
# Description: Detect LXD group membership and access to root-running LXD or the Ubuntu on-demand LXD installer
# License: GNU GPL
# Version: 1.0
# Mitre: T1069.001,T1613
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $E, $IAMROOT, $MACPEAS, $SEARCH_IN_FOLDER, $SED_RED, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $lxd_group_member, $lxd_installer_config, $lxd_installer_package, $lxd_installer_service_root, $lxd_service_unit, $lxd_socket, $lxd_socket_found, $lxd_unit
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$MACPEAS" ] && ! [ "$SEARCH_IN_FOLDER" ] && [ "${IAMROOT:-0}" != "1" ]; then
  lxd_group_member="No"
  if id -Gn 2>/dev/null | tr ' ' '\n' | grep -qx lxd; then
    lxd_group_member="Yes"
  fi

  lxd_socket_found="No"
  for lxd_socket in /var/snap/lxd/common/lxd/unix.socket /var/lib/lxd/unix.socket /run/lxd-installer.socket; do
    if [ -S "$lxd_socket" ]; then
      lxd_socket_found="Yes"
      break
    fi
  done

  lxd_unit=""
  for lxd_unit in /etc/systemd/system/lxd-installer.socket /run/systemd/system/lxd-installer.socket /usr/lib/systemd/system/lxd-installer.socket /lib/systemd/system/lxd-installer.socket; do
    [ -r "$lxd_unit" ] && break
    lxd_unit=""
  done

  lxd_service_unit=""
  for lxd_service_unit in /etc/systemd/system/lxd-installer@.service /run/systemd/system/lxd-installer@.service /usr/lib/systemd/system/lxd-installer@.service /lib/systemd/system/lxd-installer@.service; do
    [ -r "$lxd_service_unit" ] && break
    lxd_service_unit=""
  done

  lxd_installer_config="No"
  lxd_installer_service_root="No"
  if [ "$lxd_unit" ] && \
     grep -Eq '^[[:space:]]*ListenStream[[:space:]]*=[[:space:]]*/run/lxd-installer\.socket([[:space:]]*(#.*)?)?$' "$lxd_unit" 2>/dev/null && \
     grep -Eq '^[[:space:]]*SocketUser[[:space:]]*=[[:space:]]*root([[:space:]]*(#.*)?)?$' "$lxd_unit" 2>/dev/null && \
     grep -Eq '^[[:space:]]*SocketGroup[[:space:]]*=[[:space:]]*lxd([[:space:]]*(#.*)?)?$' "$lxd_unit" 2>/dev/null && \
     grep -Eq '^[[:space:]]*SocketMode[[:space:]]*=[[:space:]]*0?660([[:space:]]*(#.*)?)?$' "$lxd_unit" 2>/dev/null && \
     grep -Eiq '^[[:space:]]*Accept[[:space:]]*=[[:space:]]*(yes|true|1)([[:space:]]*(#.*)?)?$' "$lxd_unit" 2>/dev/null; then
    lxd_installer_config="Yes"
  fi

  if [ "$lxd_service_unit" ] && \
     grep -Eq '^[[:space:]]*ExecStart[[:space:]]*=[[:space:]]*/usr/share/lxd-installer/lxd-installer-service([[:space:]]|$)' "$lxd_service_unit" 2>/dev/null; then
    lxd_installer_service_root="Yes"
    if grep -Eq '^[[:space:]]*User[[:space:]]*=' "$lxd_service_unit" 2>/dev/null && \
       ! grep -Eq '^[[:space:]]*User[[:space:]]*=[[:space:]]*root([[:space:]]*(#.*)?)?$' "$lxd_service_unit" 2>/dev/null; then
      lxd_installer_service_root="No"
    fi
  fi

  lxd_installer_package=""
  if command -v dpkg-query >/dev/null 2>&1; then
    lxd_installer_package="$(dpkg-query -W -f='$''{db:Status-Abbrev} $''{Version}' lxd-installer 2>/dev/null)"
  fi

  if [ "$lxd_group_member" = "Yes" ] || [ "$lxd_socket_found" = "Yes" ] || \
     [ "$lxd_installer_config" = "Yes" ] || [ "$DEBUG" ]; then
    print_2title "LXD group and root-equivalent socket access" "T1069.001,T1613"
    print_info "https://starlabs.sg/blog/2026/06-old-wine-in-a-new-bottle-a-decade-old-lxd-group-root-re-armed"

    if [ "$lxd_group_member" = "Yes" ]; then
      echo "Current user belongs to the lxd group (root-equivalent when LXD or its installer is reachable)" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      echo "An LXD administrator can create a privileged container and attach the host filesystem"
    else
      echo "Current user does not belong to the lxd group"
    fi

    for lxd_socket in /var/snap/lxd/common/lxd/unix.socket /var/lib/lxd/unix.socket /run/lxd-installer.socket; do
      if [ -S "$lxd_socket" ]; then
        ls -ld "$lxd_socket" 2>/dev/null
        if [ -w "$lxd_socket" ]; then
          echo "Current user can write to $lxd_socket" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        fi
      fi
    done

    if [ "$lxd_installer_package" ]; then
      echo "lxd-installer package: $lxd_installer_package"
    fi
    if [ "$lxd_installer_config" = "Yes" ]; then
      echo "LXD installer socket is configured root:lxd mode 0660 with per-connection activation: $lxd_unit" | sed -${E} "s,.*,${SED_RED},"
    fi
    if [ "$lxd_installer_service_root" = "Yes" ]; then
      echo "LXD installer service runs as root: $lxd_service_unit" | sed -${E} "s,.*,${SED_RED},"
    fi
    if command -v lxc >/dev/null 2>&1; then
      echo "LXD client/wrapper: $(command -v lxc 2>/dev/null)"
    fi
    echo ""
  fi
fi
