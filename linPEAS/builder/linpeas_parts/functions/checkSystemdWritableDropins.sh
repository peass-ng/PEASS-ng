# Title: Function - checkSystemdWritableDropins
# ID: checkSystemdWritableDropins
# Author: PEASS-ng contributors
# Last Update: 08-10-2026
# Description: Passively identify writable systemd service drop-in directories for active root services.
# License: GNU GPL
# Version: 1.0
# Mitre: T1543.002
# Functions Used: print_3title, print_info
# Global Variables: $E, $IAMROOT, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $sdwdd_system_dir, $sdwdd_dir, $sdwdd_unit, $sdwdd_checked, $sdwdd_properties, $sdwdd_property, $sdwdd_active, $sdwdd_loaded, $sdwdd_user, $sdwdd_user_seen, $sdwdd_dynamic_user, $sdwdd_root_dir, $sdwdd_root_image, $sdwdd_private_users, $sdwdd_found
# Fat linpeas: 0
# Small linpeas: 1


checkSystemdWritableDropins() {
  [ "$(uname -s 2>/dev/null)" = "Linux" ] || return 0
  [ -z "${IAMROOT:-}" ] || return 0
  [ "$(id -u 2>/dev/null)" != "0" ] || return 0
  command -v systemctl >/dev/null 2>&1 || return 0

  # The optional directory argument lets tests use a fixture without touching
  # the host's systemd tree. Normal calls inspect only the system unit path.
  sdwdd_system_dir=${1:-/etc/systemd/system}
  [ -d "$sdwdd_system_dir" ] || return 0
  sdwdd_checked=0
  sdwdd_found=""

  for sdwdd_dir in "$sdwdd_system_dir"/*.service.d; do
    [ -d "$sdwdd_dir" ] && [ ! -L "$sdwdd_dir" ] || continue
    [ -w "$sdwdd_dir" ] && [ -x "$sdwdd_dir" ] || continue

    # A writable directory can accept a new .conf even if it is empty and
    # every existing unit file is protected. Bound systemctl to 20 candidates.
    [ "$sdwdd_checked" -lt 20 ] || break
    sdwdd_checked=$((sdwdd_checked + 1))
    sdwdd_unit=${sdwdd_dir##*/}
    sdwdd_unit=${sdwdd_unit%.d}
    if command -v timeout >/dev/null 2>&1; then
      sdwdd_properties=$(timeout 2 systemctl show \
        -p LoadState -p ActiveState -p User -p DynamicUser \
        -p RootDirectory -p RootImage -p PrivateUsers \
        -- "$sdwdd_unit" 2>/dev/null) || continue
    else
      sdwdd_properties=$(systemctl show \
        -p LoadState -p ActiveState -p User -p DynamicUser \
        -p RootDirectory -p RootImage -p PrivateUsers \
        -- "$sdwdd_unit" 2>/dev/null) || continue
    fi

    sdwdd_active=""
    sdwdd_loaded=""
    sdwdd_user=""
    sdwdd_user_seen=""
    sdwdd_dynamic_user=""
    sdwdd_root_dir=""
    sdwdd_root_image=""
    sdwdd_private_users=""
    while IFS= read -r sdwdd_property; do
      case "$sdwdd_property" in
        ActiveState=*) sdwdd_active=${sdwdd_property#ActiveState=} ;;
        LoadState=*) sdwdd_loaded=${sdwdd_property#LoadState=} ;;
        User=*) sdwdd_user=${sdwdd_property#User=}; sdwdd_user_seen=1 ;;
        DynamicUser=*) sdwdd_dynamic_user=${sdwdd_property#DynamicUser=} ;;
        RootDirectory=*) sdwdd_root_dir=${sdwdd_property#RootDirectory=} ;;
        RootImage=*) sdwdd_root_image=${sdwdd_property#RootImage=} ;;
        PrivateUsers=*) sdwdd_private_users=${sdwdd_property#PrivateUsers=} ;;
      esac
    done <<EOF
$sdwdd_properties
EOF

    [ "$sdwdd_loaded" = "loaded" ] && [ "$sdwdd_active" = "active" ] || continue
    [ -n "$sdwdd_user_seen" ] || continue
    case "$sdwdd_user" in ""|root|0) ;; *) continue ;; esac
    [ "$sdwdd_dynamic_user" != "yes" ] || continue
    [ -z "$sdwdd_root_dir" ] && [ -z "$sdwdd_root_image" ] || continue
    [ "$sdwdd_private_users" != "yes" ] || continue

    if [ -z "$sdwdd_found" ]; then
      print_3title "Writable systemd drop-in directories for active root services" "T1543.002"
      print_info "https://book.hacktricks.wiki/en/linux-hardening/linux-basics/linux-privilege-escalation/index.html#services"
      sdwdd_found=1
    fi
    echo "$sdwdd_unit: $sdwdd_dir (a new .conf could run on a later reload and restart)" |
      sed -"${E}" "s,.*,${SED_RED_YELLOW},"
    ls -ld "$sdwdd_dir" 2>/dev/null
  done

  [ -z "$sdwdd_found" ] || echo ""
}
