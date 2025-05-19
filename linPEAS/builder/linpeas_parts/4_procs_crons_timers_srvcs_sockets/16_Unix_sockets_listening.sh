# Title: Processes & Cron & Services & Timers - Unix Sockets Listening
# ID: PR_Unix_sockets_listening
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Unix Sockets Listening
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $EXTRA_CHECKS, $groupsB, $groupsVB, $IAMROOT, $idB, $knw_grps, $knw_usrs, $nosh_usrs, $SEARCH_IN_FOLDER, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables: $unix_scks_list, $unix_scks_list2, $unix_scks_list3, $perms, $socketcurl, $owner, $CANNOT_CONNECT_TO_SOCKET
# Fat linpeas: 0
# Small linpeas: 0


#TODO: .socket files in MACOS are folders
if ! [ "$IAMROOT" ]; then
  if ! [ "$SEARCH_IN_FOLDER" ]; then
    print_2title "Unix Sockets Listening"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#sockets"
    # Search sockets using netstat and ss
    unix_scks_list=$(ss -xlp -H state listening 2>/dev/null | grep -Eo "/.* " | cut -d " " -f1)
    if ! [ "$unix_scks_list" ];then
      unix_scks_list=$(ss -l -p -A 'unix' 2>/dev/null | grep -Ei "listen|Proc" | grep -Eo "/[a-zA-Z0-9\._/\-]+")
    fi
    if ! [ "$unix_scks_list" ];then
      unix_scks_list=$(netstat -a -p --unix 2>/dev/null | grep -Ei "listen|PID" | grep -Eo "/[a-zA-Z0-9\._/\-]+" | tail -n +2)
    fi
      unix_scks_list3=$(lsof -U 2>/dev/null | awk '{print $9}' | grep "/") 
  fi
  
  if ! [ "$SEARCH_IN_FOLDER" ]; then
    # But also search socket files
    unix_scks_list2=$(find / -type s 2>/dev/null)
  else
    unix_scks_list2=$(find "SEARCH_IN_FOLDER" -type s 2>/dev/null)
  fi

  # Detele repeated dockets and check permissions
  (printf "%s\n" "$unix_scks_list" && printf "%s\n" "$unix_scks_list2" && printf "%s\n" "$unix_scks_list3") | sort | uniq | while read l; do
    perms=""
    if [ -r "$l" ]; then
      perms="Read "
    fi
    if [ -w "$l" ];then
      perms="${perms}Write"
    fi
    
    if [ "$EXTRA_CHECKS" ] && [ "$(command -v curl || echo -n '')" ]; then
      CANNOT_CONNECT_TO_SOCKET="$(curl -v --unix-socket "$l" --max-time 1 http:/linpeas 2>&1 | grep -i 'Permission denied')"
      if ! [ "$CANNOT_CONNECT_TO_SOCKET" ]; then
        perms="${perms} - Can Connect"
      else
        perms="${perms} - Cannot Connect"
      fi
    fi
    
    if ! [ "$perms" ]; then echo "$l" | sed -${E} "s,$l,${SED_GREEN},g";
    else 
      echo "$l" | sed -${E} "s,$l,${SED_RED},g"
      echo "  └─(${RED}${perms}${NC})" | sed -${E} "s,Cannot Connect,${SED_GREEN},g"
      # Try to contact the socket
      socketcurl=$(curl --max-time 2 --unix-socket "$s" http:/index 2>/dev/null)
      if [ $? -eq 0 ]; then
        owner=$(ls -l "$s" | cut -d ' ' -f 3)
        echo "Socket $s owned by $owner uses HTTP. Response to /index: (limt 30)" | sed -${E} "s,$groupsB,${SED_RED},g" | sed -${E} "s,$groupsVB,${SED_RED},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,root,${SED_RED}," | sed -${E} "s,$knw_grps,${SED_GREEN},g" | sed -${E} "s,$idB,${SED_RED},g"
        echo "$socketcurl" | head -n 30
      fi
    fi
  done
  echo ""
fi