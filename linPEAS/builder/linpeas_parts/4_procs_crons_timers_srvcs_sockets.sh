
####################################################
#-----) Processes & Cron & Services & Timers (-----#
####################################################

if ! [ "$SEARCH_IN_FOLDER" ]; then
  #-- PCS) Cleaned proccesses
  print_2title "Cleaned processes"
  if [ "$NOUSEPS" ]; then
    printf ${BLUE}"[i]$GREEN Looks like ps is not finding processes, going to read from /proc/ and not going to monitor 1min of processes\n"$NC
  fi
  print_info "Check weird & unexpected proceses run by root: https://book.hacktricks.xyz/linux-hardening/privilege-escalation#processes"

  if [ "$NOUSEPS" ]; then
    print_ps | sed -${E} "s,$Wfolders,${SED_RED},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$rootcommon,${SED_GREEN}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED}," | sed -${E} "s,$processesVB,${SED_RED_YELLOW},g" | sed "s,$processesB,${SED_RED}," | sed -${E} "s,$processesDump,${SED_RED},"
    pslist=$(print_ps)
  else
    (ps fauxwww || ps auxwww | sort ) 2>/dev/null | grep -v "\[" | grep -v "%CPU" | while read psline; do
      echo "$psline"  | sed -${E} "s,$Wfolders,${SED_RED},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$rootcommon,${SED_GREEN}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED}," | sed -${E} "s,$processesVB,${SED_RED_YELLOW},g" | sed "s,$processesB,${SED_RED}," | sed -${E} "s,$processesDump,${SED_RED},"
      if [ "$(command -v capsh)" ] && ! echo "$psline" | grep -q root; then
        cpid=$(echo "$psline" | awk '{print $2}')
        caphex=0x"$(cat /proc/$cpid/status 2> /dev/null | grep CapEff | awk '{print $2}')"
        if [ "$caphex" ] && [ "$caphex" != "0x" ] && echo "$caphex" | grep -qv '0x0000000000000000'; then
          printf "  └─(${DG}Caps${NC}) "; capsh --decode=$caphex 2>/dev/null | grep -v "WARNING:" | sed -${E} "s,$capsB,${SED_RED},g"
        fi
      fi
    done
    pslist=$(ps auxwww)
    echo ""

    #-- PCS) Binary processes permissions
    print_2title "Binary processes permissions (non 'root root' and not belonging to current user)"
    print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#processes"
    binW="IniTialiZZinnggg"
    ps auxwww 2>/dev/null | awk '{print $11}' | while read bpath; do
      if [ -w "$bpath" ]; then
        binW="$binW|$bpath"
      fi
    done
    ps auxwww 2>/dev/null | awk '{print $11}' | xargs ls -la 2>/dev/null |awk '!x[$0]++' 2>/dev/null | grep -v " root root " | grep -v " $USER " | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g" | sed -${E} "s,$binW,${SED_RED_YELLOW},g" | sed -${E} "s,$sh_usrs,${SED_RED}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED}," | sed "s,root,${SED_GREEN},"
  fi
  echo ""
fi

if ! [ "$SEARCH_IN_FOLDER" ]; then
  #-- PCS) Files opened by processes belonging to other users
  if ! [ "$IAMROOT" ]; then
    print_2title "Files opened by processes belonging to other users"
    print_info "This is usually empty because of the lack of privileges to read other user processes information"
    lsof 2>/dev/null | grep -v "$USER" | grep -iv "permission denied" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed "s,root,${SED_RED},"
    echo ""
  fi
fi

if ! [ "$SEARCH_IN_FOLDER" ]; then
  #-- PCS) Processes with credentials inside memory
  print_2title "Processes with credentials in memory (root req)"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#credentials-from-process-memory"
  if echo "$pslist" | grep -q "gdm-password"; then echo "gdm-password process found (dump creds from memory as root)" | sed "s,gdm-password process,${SED_RED},"; else echo_not_found "gdm-password"; fi
  if echo "$pslist" | grep -q "gnome-keyring-daemon"; then echo "gnome-keyring-daemon process found (dump creds from memory as root)" | sed "s,gnome-keyring-daemon,${SED_RED},"; else echo_not_found "gnome-keyring-daemon"; fi
  if echo "$pslist" | grep -q "lightdm"; then echo "lightdm process found (dump creds from memory as root)" | sed "s,lightdm,${SED_RED},"; else echo_not_found "lightdm"; fi
  if echo "$pslist" | grep -q "vsftpd"; then echo "vsftpd process found (dump creds from memory as root)" | sed "s,vsftpd,${SED_RED},"; else echo_not_found "vsftpd"; fi
  if echo "$pslist" | grep -q "apache2"; then echo "apache2 process found (dump creds from memory as root)" | sed "s,apache2,${SED_RED},"; else echo_not_found "apache2"; fi
  if echo "$pslist" | grep -q "sshd:"; then echo "sshd: process found (dump creds from memory as root)" | sed "s,sshd:,${SED_RED},"; else echo_not_found "sshd"; fi
  echo ""
fi

if ! [ "$SEARCH_IN_FOLDER" ]; then
  #-- PCS) Different processes 1 min
  if ! [ "$FAST" ] && ! [ "$SUPERFAST" ]; then
    print_2title "Different processes executed during 1 min (interesting is low number of repetitions)"
    print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#frequent-cron-jobs"
    temp_file=$(mktemp)
    if [ "$(ps -e -o command 2>/dev/null)" ]; then for i in $(seq 1 1250); do ps -e -o command >> "$temp_file" 2>/dev/null; sleep 0.05; done; sort "$temp_file" 2>/dev/null | uniq -c | grep -v "\[" | sed '/^.\{200\}./d' | sort -r -n | grep -E -v "\s*[1-9][0-9][0-9][0-9]"; rm "$temp_file"; fi
    echo ""
  fi
fi

if ! [ "$SEARCH_IN_FOLDER" ]; then
  #-- PCS) Cron
  print_2title "Cron jobs"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#scheduled-cron-jobs"
  command -v crontab 2>/dev/null || echo_not_found "crontab"
  crontab -l 2>/dev/null | tr -d "\r" | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed "s,root,${SED_RED},"
  command -v incrontab 2>/dev/null || echo_not_found "incrontab"
  incrontab -l 2>/dev/null
  ls -alR /etc/cron* /var/spool/cron/crontabs /var/spool/anacron 2>/dev/null | sed -${E} "s,$cronjobsG,${SED_GREEN},g" | sed "s,$cronjobsB,${SED_RED},g"
  cat /etc/cron* /etc/at* /etc/anacrontab /var/spool/cron/crontabs/* /etc/incron.d/* /var/spool/incron/* 2>/dev/null | tr -d "\r" | grep -v "^#" | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed -${E} "s,$nosh_usrs,${SED_BLUE},"  | sed "s,root,${SED_RED},"
  crontab -l -u "$USER" 2>/dev/null | tr -d "\r"
  ls -lR /usr/lib/cron/tabs/ /private/var/at/jobs /var/at/tabs/ /etc/periodic/ 2>/dev/null | sed -${E} "s,$cronjobsG,${SED_GREEN},g" | sed "s,$cronjobsB,${SED_RED},g" #MacOS paths
  atq 2>/dev/null
else
  print_2title "Cron jobs"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#scheduled-cron-jobs"
  find "$SEARCH_IN_FOLDER" '(' -type d -or -type f ')' '(' -name "cron*" -or -name "anacron" -or -name "anacrontab" -or -name "incron.d" -or -name "incron" -or -name "at" -or -name "periodic" ')' -exec echo {} \; -exec ls -lR {} \;
fi
echo ""


if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ "$MACPEAS" ]; then
    print_2title "Third party LaunchAgents & LaunchDemons"
    print_info "https://book.hacktricks.xyz/macos/macos-security-and-privilege-escalation#launchd"
    ls -l /Library/LaunchAgents/ /Library/LaunchDaemons/ ~/Library/LaunchAgents/ ~/Library/LaunchDaemons/ 2>/dev/null
    echo ""

    print_2title "Writable System LaunchAgents & LaunchDemons"
    find /System/Library/LaunchAgents/ /System/Library/LaunchDaemons/ /Library/LaunchAgents/ /Library/LaunchDaemons/ | grep ".plist" | while read f; do
      program=""
      program=$(defaults read "$f" Program 2>/dev/null)
      if ! [ "$program" ]; then
        program=$(defaults read /Library/LaunchDaemons/MonitorHelper.plist ProgramArguments | grep -Ev "^\(|^\)" | cut -d '"' -f 2)
      fi
      if [ -w "$program" ]; then
        echo "$program" is writable | sed -${E} "s,.*,${SED_RED_YELLOW},";
      fi
    done
    echo ""

    print_2title "StartupItems"
    print_info "https://book.hacktricks.xyz/macos/macos-security-and-privilege-escalation#startup-items"
    ls -l /Library/StartupItems/ /System/Library/StartupItems/ 2>/dev/null
    echo ""

    print_2title "Login Items"
    print_info "https://book.hacktricks.xyz/macos/macos-security-and-privilege-escalation#login-items"
    osascript -e 'tell application "System Events" to get the name of every login item' 2>/dev/null
    echo ""

    print_2title "SPStartupItemDataType"
    system_profiler SPStartupItemDataType
    echo ""

    print_2title "Emond scripts"
    print_info "https://book.hacktricks.xyz/macos/macos-security-and-privilege-escalation#emond"
    ls -l /private/var/db/emondClients
    echo ""
  fi
fi

if ! [ "$SEARCH_IN_FOLDER" ]; then
  #-- PCS) Services
  if [ "$EXTRA_CHECKS" ]; then
    print_2title "Services"
    print_info "Search for outdated versions"
    (service --status-all || service -e || chkconfig --list || rc-status || launchctl list) 2>/dev/null || echo_not_found "service|chkconfig|rc-status|launchctl"
    echo ""
  fi
fi

if ! [ "$SEARCH_IN_FOLDER" ]; then
  #-- PSC) systemd PATH
  print_2title "Systemd PATH"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#systemd-path-relative-paths"
  systemctl show-environment 2>/dev/null | grep "PATH" | sed -${E} "s,$Wfolders\|\./\|\.:\|:\.,${SED_RED_YELLOW},g"
  WRITABLESYSTEMDPATH=$(systemctl show-environment 2>/dev/null | grep "PATH" | grep -E "$Wfolders")
  echo ""
fi

#-- PSC) .service files
#TODO: .service files in MACOS are folders
print_2title "Analyzing .service files"
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#services"
printf "%s\n" "$PSTORAGE_SYSTEMD" | while read s; do
  if [ ! -O "$s" ] || [ "$SEARCH_IN_FOLDER" ]; then #Remove services that belongs to the current user or if firmware see everything
    if ! [ "$IAMROOT" ] && [ -w "$s" ] && [ -f "$s" ] && ! [ "$SEARCH_IN_FOLDER" ]; then
      echo "$s" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
    fi
    servicebinpaths=$(grep -Eo '^Exec.*?=[!@+-]*[a-zA-Z0-9_/\-]+' "$s" 2>/dev/null | cut -d '=' -f2 | sed 's,^[@\+!-]*,,') #Get invoked paths
    printf "%s\n" "$servicebinpaths" | while read sp; do
      if [ -w "$sp" ]; then
        echo "$s is calling this writable executable: $sp" | sed "s,writable.*,${SED_RED_YELLOW},g"
      fi
    done
    relpath1=$(grep -E '^Exec.*=(?:[^/]|-[^/]|\+[^/]|![^/]|!![^/]|)[^/@\+!-].*' "$s" 2>/dev/null | grep -Iv "=/")
    relpath2=$(grep -E '^Exec.*=.*/bin/[a-zA-Z0-9_]*sh ' "$s" 2>/dev/null | grep -Ev "/[a-zA-Z0-9_]+/")
    if [ "$relpath1" ] || [ "$relpath2" ]; then
      if [ "$WRITABLESYSTEMDPATH" ]; then
        echo "$s is executing some relative path" | sed -${E} "s,.*,${SED_RED},";
      else
        echo "$s is executing some relative path"
      fi
    fi
  fi
done
if [ ! "$WRITABLESYSTEMDPATH" ]; then echo "You can't write on systemd PATH" | sed -${E} "s,.*,${SED_GREEN},"; fi
echo ""

if ! [ "$SEARCH_IN_FOLDER" ]; then
  #-- PSC) Timers
  print_2title "System timers"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#timers"
  (systemctl list-timers --all 2>/dev/null | grep -Ev "(^$|timers listed)" | sed -${E} "s,$timersG,${SED_GREEN},") || echo_not_found
  echo ""
fi

#-- PSC) .timer files
print_2title "Analyzing .timer files"
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#timers"
printf "%s\n" "$PSTORAGE_TIMER" | while read t; do
  if ! [ "$IAMROOT" ] && [ -w "$t" ] && ! [ "$SEARCH_IN_FOLDER" ]; then
    echo "$t" | sed -${E} "s,.*,${SED_RED},g"
  fi
  timerbinpaths=$(grep -Po '^Unit=*(.*?$)' $t 2>/dev/null | cut -d '=' -f2)
  printf "%s\n" "$timerbinpaths" | while read tb; do
    if [ -w "$tb" ]; then
      echo "$t timer is calling this writable executable: $tb" | sed "s,writable.*,${SED_RED},g"
    fi
  done
  #relpath="`grep -Po '^Unit=[^/].*' \"$t\" 2>/dev/null`"
  #for rp in "$relpath"; do
  #  echo "$t is calling a relative path: $rp" | sed "s,relative.*,${SED_RED},g"
  #done
done
echo ""

#-- PSC) .socket files
#TODO: .socket files in MACOS are folders
if ! [ "$IAMROOT" ]; then
  print_2title "Analyzing .socket files"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#sockets"
  printf "%s\n" "$PSTORAGE_SOCKET" | while read s; do
    if ! [ "$IAMROOT" ] && [ -w "$s" ] && [ -f "$s" ] && ! [ "$SEARCH_IN_FOLDER" ]; then
      echo "Writable .socket file: $s" | sed "s,/.*,${SED_RED},g"
    fi
    socketsbinpaths=$(grep -Eo '^(Exec).*?=[!@+-]*/[a-zA-Z0-9_/\-]+' "$s" 2>/dev/null | cut -d '=' -f2 | sed 's,^[@\+!-]*,,')
    printf "%s\n" "$socketsbinpaths" | while read sb; do
      if [ -w "$sb" ]; then
        echo "$s is calling this writable executable: $sb" | sed "s,writable.*,${SED_RED},g"
      fi
    done
    socketslistpaths=$(grep -Eo '^(Listen).*?=[!@+-]*/[a-zA-Z0-9_/\-]+' "$s" 2>/dev/null | cut -d '=' -f2 | sed 's,^[@\+!-]*,,')
    printf "%s\n" "$socketslistpaths" | while read sl; do
      if [ -w "$sl" ]; then
        echo "$s is calling this writable listener: $sl" | sed "s,writable.*,${SED_RED},g";
      fi
    done
  done
  echo ""
  
  if ! [ "$SEARCH_IN_FOLDER" ]; then
    print_2title "Unix Sockets Listening"
    print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#sockets"
    # Search sockets using netstat and ss
    unix_scks_list=$(ss -xlp -H state listening 2>/dev/null | grep -Eo "/.* " | cut -d " " -f1)
    if ! [ "$unix_scks_list" ];then
      unix_scks_list=$(ss -l -p -A 'unix' 2>/dev/null | grep -Ei "listen|Proc" | grep -Eo "/[a-zA-Z0-9\._/\-]+")
    fi
    if ! [ "$unix_scks_list" ];then
      unix_scks_list=$(netstat -a -p --unix 2>/dev/null | grep -Ei "listen|PID" | grep -Eo "/[a-zA-Z0-9\._/\-]+" | tail -n +2)
    fi
  fi
  
  if ! [ "$SEARCH_IN_FOLDER" ]; then
    # But also search socket files
    unix_scks_list2=$(find / -type s 2>/dev/null)
  else
    unix_scks_list2=$(find "SEARCH_IN_FOLDER" -type s 2>/dev/null)
  fi

  # Detele repeated dockets and check permissions
  (printf "%s\n" "$unix_scks_list" && printf "%s\n" "$unix_scks_list2") | sort | uniq | while read l; do
    perms=""
    if [ -r "$l" ]; then
      perms="Read "
    fi
    if [ -w "$l" ];then
      perms="${perms}Write"
    fi
    
    if [ "$EXTRA_CHECKS" ] && [ "$(command -v curl)" ]; then
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

#-- PSC) Writable and weak policies in D-Bus config files
print_2title "D-Bus config files"
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#d-bus"
if [ "$PSTORAGE_DBUS" ]; then
  printf "%s\n" "$PSTORAGE_DBUS" | while read d; do
    for f in $d/*; do
      if ! [ "$IAMROOT" ] && [ -w "$f" ] && ! [ "$SEARCH_IN_FOLDER" ]; then
        echo "Writable $f" | sed -${E} "s,.*,${SED_RED},g"
      fi

      genpol=$(grep "<policy>" "$f" 2>/dev/null)
      if [ "$genpol" ]; then printf "Weak general policy found on $f ($genpol)\n" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_RED},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$mygroups,${SED_RED},g"; fi
      #if [ "`grep \"<policy user=\\\"$USER\\\">\" \"$f\" 2>/dev/null`" ]; then printf "Possible weak user policy found on $f () \n" | sed "s,$USER,${SED_RED},g"; fi

      userpol=$(grep "<policy user=" "$f" 2>/dev/null | grep -v "root")
      if [ "$userpol" ]; then printf "Possible weak user policy found on $f ($userpol)\n" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_RED},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$mygroups,${SED_RED},g"; fi
      #for g in `groups`; do
      #  if [ "`grep \"<policy group=\\\"$g\\\">\" \"$f\" 2>/dev/null`" ]; then printf "Possible weak group ($g) policy found on $f\n" | sed "s,$g,${SED_RED},g"; fi
      #done
      grppol=$(grep "<policy group=" "$f" 2>/dev/null | grep -v "root")
      if [ "$grppol" ]; then printf "Possible weak user policy found on $f ($grppol)\n" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_RED},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$mygroups,${SED_RED},g"; fi

      #TODO: identify allows in context="default"
    done
  done
fi
echo ""

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "D-Bus Service Objects list"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#d-bus"
  dbuslist=$(busctl list 2>/dev/null)
  if [ "$dbuslist" ]; then
    busctl list | while read line; do
      echo "$line" | sed -${E} "s,$dbuslistG,${SED_GREEN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$rootcommon,${SED_GREEN}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},";
      if ! echo "$line" | grep -qE "$dbuslistG"; then
        srvc_object=$(echo $line | cut -d " " -f1)
        srvc_object_info=$(busctl status "$srvc_object" 2>/dev/null | grep -E "^UID|^EUID|^OwnerUID" | tr '\n' ' ')
        if [ "$srvc_object_info" ]; then
          echo " -- $srvc_object_info" | sed "s,UID=0,${SED_RED},"
        fi
      fi
    done
  else echo_not_found "busctl"
  fi
fi
