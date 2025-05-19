# Title: Processes & Cron & Services & Timers - D-Bus Service Objects list
# ID: PR_DBus_service_objects_list
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Enumerate D-Bus Service Objects list
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables:  $dbuslistG, $knw_usrs, $nosh_usrs, $rootcommon, $SEARCH_IN_FOLDER, $USER
# Initial Functions:
# Generated Global Variables: $dbuslist, $srvc_object, $srvc_object_info
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "D-Bus Service Objects list"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#d-bus"
  dbuslist=$(busctl list 2>/dev/null)
  if [ "$dbuslist" ]; then
    busctl list | while read l; do
      echo "$l" | sed -${E} "s,$dbuslistG,${SED_GREEN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$rootcommon,${SED_GREEN}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},";
      if ! echo "$l" | grep -qE "$dbuslistG"; then
        srvc_object=$(echo $l | cut -d " " -f1)
        srvc_object_info=$(busctl status "$srvc_object" 2>/dev/null | grep -E "^UID|^EUID|^OwnerUID" | tr '\n' ' ')
        if [ "$srvc_object_info" ]; then
          echo " -- $srvc_object_info" | sed "s,UID=0,${SED_RED},"
        fi
      fi
    done
  else echo_not_found "busctl"
  fi
fi