###########################################
#----------) Interesting files (----------#
###########################################


##-- IF) .sh files in PATH
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title ".sh files in path"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#script-binaries-in-path"
  echo $PATH | tr ":" "\n" | while read d; do
    for f in $(find "$d" -name "*.sh" -o -name "*.sh.*" 2>/dev/null); do
      if ! [ "$IAMROOT" ] && [ -O "$f" ]; then
        echo "You own the script: $f" | sed -${E} "s,.*,${SED_RED},"
      elif ! [ "$IAMROOT" ] && [ -w "$f" ]; then #If write permision, win found (no check exploits)
        echo "You can write script: $f" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      else
        echo $f | sed -${E} "s,$shscripsG,${SED_GREEN}," | sed -${E} "s,$Wfolders,${SED_RED},";
      fi
    done
  done
  echo ""

  broken_links=$(find "$d" -type l 2>/dev/null | xargs file 2>/dev/null | grep broken)
  if [ "$broken_links" ] || [ "$DEBUG" ]; then 
    print_2title "Broken links in path"
    echo $PATH | tr ":" "\n" | while read d; do
      find "$d" -type l 2>/dev/null | xargs file 2>/dev/null | grep broken | sed -${E} "s,broken,${SED_RED},";
    done
    echo ""
  fi
fi

##-- IF) Date times inside firmware
if [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Files datetimes inside the firmware (limit 50)"
  find "$SEARCH_IN_FOLDER" -type f -printf "%T+\n" 2>/dev/null | sort | uniq -c | sort | head -n 50
  echo "To find a file with an specific date execute: find \"$SEARCH_IN_FOLDER\" -type f -printf \"%T+ %p\n\" 2>/dev/null | grep \"<date>\""
  echo ""
fi

##-- IF) Executable files added by user
print_2title "Executable files potentially added by user (limit 70)"
if ! [ "$SEARCH_IN_FOLDER" ]; then
  find / -type f -executable -printf "%T+ %p\n" 2>/dev/null | grep -Ev "000|/site-packages|/python|/node_modules|\.sample|/gems|/cgroup/" | sort -r | head -n 70
else
  find "$SEARCH_IN_FOLDER" -type f -executable -printf "%T+ %p\n" 2>/dev/null | grep -Ev "/site-packages|/python|/node_modules|\.sample|/gems|/cgroup/" | sort -r | head -n 70
fi
echo ""



if [ "$MACPEAS" ]; then
  print_2title "Unsigned Applications"
  macosNotSigned /System/Applications
fi

##-- IF) Unexpected in /opt
if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ "$(ls /opt 2>/dev/null)" ]; then
    print_2title "Unexpected in /opt (usually empty)"
    ls -la /opt
    echo ""
  fi
fi

##-- IF) Unexpected folders in /
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Unexpected in root"
  if [ "$MACPEAS" ]; then
    (find $ROOT_FOLDER -maxdepth 1 | grep -Ev "$commonrootdirsMacG" | sed -${E} "s,.*,${SED_RED},") || echo_not_found
  else
    (find $ROOT_FOLDER -maxdepth 1 | grep -Ev "$commonrootdirsG" | sed -${E} "s,.*,${SED_RED},") || echo_not_found
  fi
  echo ""
fi

##-- IF) Modified interesting files into specific folders in the last 5mins
print_2title "Modified interesting files in the last 5mins (limit 100)"
find $ROOT_FOLDER -type f -mmin -5 ! -path "/proc/*" ! -path "/sys/*" ! -path "/run/*" ! -path "/dev/*" ! -path "/var/lib/*" ! -path "/private/var/*" 2>/dev/null | grep -v "/linpeas" | head -n 100 | sed -${E} "s,$Wfolders,${SED_RED},"
echo ""

##-- IF) Writable log files
if command -v logrotate >/dev/null && logrotate --version | head -n 1 | grep -Eq "[012]\.[0-9]+\.|3\.[0-9]\.|3\.1[0-7]\.|3\.18\.0"; then #3.18.0 and below
print_2title "Writable log files (logrotten) (limit 50)"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#logrotate-exploitation"
  logrotate --version 2>/dev/null || echo_not_found "logrotate"
  lastWlogFolder="ImPOsSiBleeElastWlogFolder"
  logfind=$(find $ROOT_FOLDER -type f -name "*.log" -o -name "*.log.*" 2>/dev/null | awk -F/ '{line_init=$0; if (!cont){ cont=0 }; $NF=""; act=$0; if (act == pre){(cont += 1)} else {cont=0}; if (cont < 3){ print line_init; }; if (cont == "3"){print "#)You_can_write_more_log_files_inside_last_directory"}; pre=act}' | head -n 50)
  printf "%s\n" "$logfind" | while read log; do
    if ! [ "$IAMROOT" ] && [ "$log" ] && [ -w "$log" ] || ! [ "$IAMROOT" ] && echo "$log" | grep -qE "$Wfolders"; then #Only print info if something interesting found
      if echo "$log" | grep -q "You_can_write_more_log_files_inside_last_directory"; then printf $ITALIC"$log\n"$NC;
      elif ! [ "$IAMROOT" ] && [ -w "$log" ] && [ "$(command -v logrotate 2>/dev/null)" ] && logrotate --version 2>&1 | grep -qE ' 1| 2| 3.1'; then printf "Writable:$RED $log\n"$NC; #Check vuln version of logrotate is used and print red in that case
      elif ! [ "$IAMROOT" ] && [ -w "$log" ]; then echo "Writable: $log";
      elif ! [ "$IAMROOT" ] && echo "$log" | grep -qE "$Wfolders" && [ "$log" ] && [ ! "$lastWlogFolder" == "$log" ]; then lastWlogFolder="$log"; echo "Writable folder: $log" | sed -${E} "s,$Wfolders,${SED_RED},g";
      fi
    fi
  done
fi

echo ""

if ! [ "$SEARCH_IN_FOLDER" ]; then
  ##-- IF) Files inside my home
  print_2title "Files inside $HOME (limit 20)"
  (ls -la $HOME 2>/dev/null | head -n 23) || echo_not_found
  echo ""

  ##-- IF) Files inside /home
  print_2title "Files inside others home (limit 20)"
  (find $HOMESEARCH -type f 2>/dev/null | grep -v -i "/"$USER | head -n 20) || echo_not_found
  echo ""

  ##-- IF) Mail applications
  print_2title "Searching installed mail applications"
  ls /bin /sbin /usr/bin /usr/sbin /usr/local/bin /usr/local/sbin /etc 2>/dev/null | grep -Ewi "$mail_apps" | sort | uniq
  echo ""

  ##-- IF) Mails
  print_2title "Mails (limit 50)"
  (find /var/mail/ /var/spool/mail/ /private/var/mail -type f -ls 2>/dev/null | head -n 50 | sed -${E} "s,$sh_usrs,${SED_RED}," | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,root,${SED_GREEN},g" | sed "s,$USER,${SED_RED},g") || echo_not_found
  echo ""

  ##-- IF) Backup folders
  if [ "$backup_folders" ] || [ "$DEBUG" ]; then
    print_2title "Backup folders"
    printf "%s\n" "$backup_folders" | while read b ; do
      ls -ld "$b" 2> /dev/null | sed -${E} "s,backups|backup,${SED_RED},g";
      ls -l "$b" 2>/dev/null && echo ""
    done
    echo ""
  fi
fi

##-- IF) Backup files
print_2title "Backup files (limited 100)"
backs=$(find $ROOT_FOLDER -type f \( -name "*backup*" -o -name "*\.bak" -o -name "*\.bak\.*" -o -name "*\.bck" -o -name "*\.bck\.*" -o -name "*\.bk" -o -name "*\.bk\.*" -o -name "*\.old" -o -name "*\.old\.*" \) -not -path "/proc/*" 2>/dev/null)
printf "%s\n" "$backs" | head -n 100 | while read b ; do
  if [ -r "$b" ]; then
    ls -l "$b" | grep -Ev "$notBackup" | grep -Ev "$notExtensions" | sed -${E} "s,backup|bck|\.bak|\.old,${SED_RED},g";
  fi;
done
echo ""

##-- IF) DB files
if [ "$MACPEAS" ]; then
  print_2title "Reading messages database"
  sqlite3 $HOME/Library/Messages/chat.db 'select * from message' 2>/dev/null
  sqlite3 $HOME/Library/Messages/chat.db 'select * from attachment' 2>/dev/null
  sqlite3 $HOME/Library/Messages/chat.db 'select * from deleted_messages' 2>/dev/null

fi


if [ "$PSTORAGE_DATABASE" ] || [ "$DEBUG" ]; then
  print_2title "Searching tables inside readable .db/.sql/.sqlite files (limit 100)"
  FILECMD="$(command -v file 2>/dev/null)"
  printf "%s\n" "$PSTORAGE_DATABASE" | while read f; do
    if [ "$FILECMD" ]; then
      echo "Found "$(file "$f") | sed -${E} "s,\.db|\.sql|\.sqlite|\.sqlite3,${SED_RED},g";
    else
      echo "Found $f" | sed -${E} "s,\.db|\.sql|\.sqlite|\.sqlite3,${SED_RED},g";
    fi
  done
  SQLITEPYTHON=""
  echo ""
  printf "%s\n" "$PSTORAGE_DATABASE" | while read f; do
    if ([ -r "$f" ] && [ "$FILECMD" ] && file "$f" | grep -qi sqlite) || ([ -r "$f" ] && [ ! "$FILECMD" ]); then #If readable and filecmd and sqlite, or readable and not filecmd
      if [ "$(command -v sqlite3 2>/dev/null)" ]; then
        tables=$(sqlite3 $f ".tables" 2>/dev/null)
        #printf "$tables\n" | sed "s,user.*\|credential.*,${SED_RED},g"
      elif [ "$(command -v python 2>/dev/null)" ] || [ "$(command -v python3 2>/dev/null)" ]; then
        SQLITEPYTHON=$(command -v python 2>/dev/null || command -v python3 2>/dev/null)
        tables=$($SQLITEPYTHON -c "print('\n'.join([t[0] for t in __import__('sqlite3').connect('$f').cursor().execute('SELECT name FROM sqlite_master WHERE type=\'table\' and tbl_name NOT like \'sqlite_%\';').fetchall()]))" 2>/dev/null)
        #printf "$tables\n" | sed "s,user.*\|credential.*,${SED_RED},g"
      else
        tables=""
      fi
      if [ "$tables" ] || [ "$DEBUG" ]; then
          printf $GREEN" -> Extracting tables from$NC $f $DG(limit 20)\n"$NC
          printf "%s\n" "$tables" | while read t; do
          columns=""
          # Search for credentials inside the table using sqlite3
          if [ -z "$SQLITEPYTHON" ]; then
            columns=$(sqlite3 $f ".schema $t" 2>/dev/null | grep "CREATE TABLE")
          # Search for credentials inside the table using python
          else
            columns=$($SQLITEPYTHON -c "print(__import__('sqlite3').connect('$f').cursor().execute('SELECT sql FROM sqlite_master WHERE type!=\'meta\' AND sql NOT NULL AND name =\'$t\';').fetchall()[0][0])" 2>/dev/null)
          fi
          #Check found columns for interesting fields
          INTCOLUMN=$(echo "$columns" | grep -i "username\|passw\|credential\|email\|hash\|salt")
          if [ "$INTCOLUMN" ]; then
            printf ${BLUE}"  --> Found interesting column names in$NC $t $DG(output limit 10)\n"$NC | sed -${E} "s,user.*|credential.*,${SED_RED},g"
            printf "$columns\n" | sed -${E} "s,username|passw|credential|email|hash|salt|$t,${SED_RED},g"
            (sqlite3 $f "select * from $t" || $SQLITEPYTHON -c "print(', '.join([str(x) for x in __import__('sqlite3').connect('$f').cursor().execute('SELECT * FROM \'$t\';').fetchall()[0]]))") 2>/dev/null | head
            echo ""
          fi
        done
      fi
    fi
  done
fi
echo ""

if [ "$MACPEAS" ]; then
  print_2title "Downloaded Files"
  sqlite3 ~/Library/Preferences/com.apple.LaunchServices.QuarantineEventsV2 'select LSQuarantineAgentName, LSQuarantineDataURLString, LSQuarantineOriginURLString, date(LSQuarantineTimeStamp + 978307200, "unixepoch") as downloadedDate from LSQuarantineEvent order by LSQuarantineTimeStamp' | sort | grep -Ev "\|\|\|"
fi

##-- IF) Web files
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Web files?(output limit)"
  ls -alhR /var/www/ 2>/dev/null | head
  ls -alhR /srv/www/htdocs/ 2>/dev/null | head
  ls -alhR /usr/local/www/apache22/data/ 2>/dev/null | head
  ls -alhR /opt/lampp/htdocs/ 2>/dev/null | head
  echo ""
fi

##-- IF) All hidden files
print_2title "All relevant hidden files (not in /sys/ or the ones listed in the previous check) (limit 70)"
find $ROOT_FOLDER -type f -iname ".*" ! -path "/sys/*" ! -path "/System/*" ! -path "/private/var/*" -exec ls -l {} \; 2>/dev/null | grep -Ev "$INT_HIDDEN_FILES" | grep -Ev "_history$|\.gitignore|.npmignore|\.listing|\.ignore|\.uuid|\.depend|\.placeholder|\.gitkeep|\.keep|\.keepme|\.travis.yml" | head -n 70
echo ""

##-- IF) Readable files in /tmp, /var/tmp, backups
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Readable files inside /tmp, /var/tmp, /private/tmp, /private/var/at/tmp, /private/var/tmp, and backup folders (limit 70)"
  filstmpback=$(find /tmp /var/tmp /private/tmp /private/var/at/tmp /private/var/tmp $backup_folders_row -type f 2>/dev/null | grep -Ev "dpkg\.statoverride\.|dpkg\.status\.|apt\.extended_states\.|dpkg\.diversions\." | head -n 70)
  printf "%s\n" "$filstmpback" | while read f; do if [ -r "$f" ]; then ls -l "$f" 2>/dev/null; fi; done
  echo ""
fi

##-- IF) Passwords in history cmd
if [ "$(history 2>/dev/null)" ] || [ "$DEBUG" ]; then
  print_2title "Searching passwords in history cmd"
  history | grep -Ei "$pwd_inside_history" "$f" 2>/dev/null | sed -${E} "s,$pwd_inside_history,${SED_RED},"
  echo ""
fi

##-- IF) Passwords in history files
if [ "$PSTORAGE_HISTORY" ] || [ "$DEBUG" ]; then
  print_2title "Searching passwords in history files"
  printf "%s\n" "$PSTORAGE_HISTORY" | while read f; do grep -Ei "$pwd_inside_history" "$f" 2>/dev/null | sed -${E} "s,$pwd_inside_history,${SED_RED},"; done
  echo ""
fi

##-- IF) Passwords in config PHP files
if [ "$PSTORAGE_PHP_FILES" ] || [ "$DEBUG" ]; then
  print_2title "Searching passwords in config PHP files"
  printf "%s\n" "$PSTORAGE_PHP_FILES" | while read c; do grep -EiI "(pwd|passwd|password|PASSWD|PASSWORD|dbuser|dbpass).*[=:].+|define ?\('(\w*passw|\w*user|\w*datab)" "$c" 2>/dev/null | grep -Ev "function|password.*= ?\"\"|password.*= ?''" | sed '/^.\{150\}./d' | sort | uniq | sed -${E} "s,[pP][aA][sS][sS][wW]|[dD][bB]_[pP][aA][sS][sS],${SED_RED},g"; done
  echo ""
fi

##-- IF) Passwords files in home
if [ "$PSTORAGE_PASSWORD_FILES" ] || [ "$DEBUG" ]; then
  print_2title "Searching *password* or *credential* files in home (limit 70)"
  (printf "%s\n" "$PSTORAGE_PASSWORD_FILES" | grep -v "/snap/" | awk -F/ '{line_init=$0; if (!cont){ cont=0 }; $NF=""; act=$0; if (cont < 3){ print line_init; } if (cont == "3"){print "  #)There are more creds/passwds files in the previous parent folder\n"}; if (act == pre){(cont += 1)} else {cont=0}; pre=act }' | head -n 70 | sed -${E} "s,password|credential,${SED_RED}," | sed "s,There are more creds/passwds files in the previous parent folder,${C}[3m&${C}[0m,") || echo_not_found
  echo ""
fi

##-- IF) TTY passwords
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Checking for TTY (sudo/su) passwords in audit logs"
  aureport --tty 2>/dev/null | grep -E "su |sudo " | sed -${E} "s,su|sudo,${SED_RED},g"
  find /var/log/ -type f -exec grep -RE 'comm="su"|comm="sudo"' '{}' \; 2>/dev/null | sed -${E} "s,\"su\"|\"sudo\",${SED_RED},g" | sed -${E} "s,data=.*,${SED_RED},g"
  echo ""
fi

##-- IF) IPs inside logs
if [ "$DEBUG" ] || ( ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && ! [ "$SEARCH_IN_FOLDER" ] ); then
  print_2title "Searching IPs inside logs (limit 70)"
  (find /var/log/ /var/logs /private/var/log -type f -exec grep -R -a -E -o "(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)" "{}" \;) 2>/dev/null | grep -v "\.0\.\|:0\|\.0$" | sort | uniq -c | sort -r -n | head -n 70
  echo ""
fi

##-- IF) Passwords inside logs
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Searching passwords inside logs (limit 70)"
  (find /var/log/ /var/logs/ /private/var/log -type f -exec grep -R -i "pwd\|passw" "{}" \;) 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | grep -v "File does not exist:\|modules-config/config-set-passwords\|config-set-passwords already ran\|script not found or unable to stat:\|\"GET /.*\" 404" | head -n 70 | sed -${E} "s,pwd|passw,${SED_RED},"
  echo ""
fi

if [ "$DEBUG" ] || ( ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && ! [ "$SEARCH_IN_FOLDER" ] ); then
  ##-- IF) Emails inside logs
  print_2title "Searching emails inside logs (limit 70)"
  (find /var/log/ /var/logs/ /private/var/log -type f -exec grep -I -R -E -o "\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,6}\b" "{}" \;) 2>/dev/null | sort | uniq -c | sort -r -n | head -n 70 | sed -${E} "s,$knw_emails,${SED_GREEN},g"
  echo ""
fi

if ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && [ "$TIMEOUT" ]; then
  ##-- IF) Find possible files with passwords
  print_2title "Searching possible password variables inside key folders (limit 140)"
  if ! [ "$SEARCH_IN_FOLDER" ]; then
    timeout 150 find $HOMESEARCH -exec grep -HnRiIE "($pwd_in_variables1|$pwd_in_variables2|$pwd_in_variables3|$pwd_in_variables4|$pwd_in_variables5|$pwd_in_variables6|$pwd_in_variables7|$pwd_in_variables8|$pwd_in_variables9|$pwd_in_variables10|$pwd_in_variables11).*[=:].+" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | grep -Ev "^#" | grep -iv "linpeas" | sort | uniq | head -n 70 | sed -${E} "s,$pwd_in_variables1,${SED_RED},g" | sed -${E} "s,$pwd_in_variables2,${SED_RED},g" | sed -${E} "s,$pwd_in_variables3,${SED_RED},g" | sed -${E} "s,$pwd_in_variables4,${SED_RED},g" | sed -${E} "s,$pwd_in_variables5,${SED_RED},g" | sed -${E} "s,$pwd_in_variables6,${SED_RED},g" | sed -${E} "s,$pwd_in_variables7,${SED_RED},g" | sed -${E} "s,$pwd_in_variables8,${SED_RED},g" | sed -${E} "s,$pwd_in_variables9,${SED_RED},g" | sed -${E} "s,$pwd_in_variables10,${SED_RED},g" | sed -${E} "s,$pwd_in_variables11,${SED_RED},g" &
    timeout 150 find /var/www $backup_folders_row /tmp /etc /mnt /private grep -HnRiIE "($pwd_in_variables1|$pwd_in_variables2|$pwd_in_variables3|$pwd_in_variables4|$pwd_in_variables5|$pwd_in_variables6|$pwd_in_variables7|$pwd_in_variables8|$pwd_in_variables9|$pwd_in_variables10|$pwd_in_variables11).*[=:].+" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | grep -Ev "^#" | grep -iv "linpeas" | sort | uniq | head -n 70 | sed -${E} "s,$pwd_in_variables1,${SED_RED},g" | sed -${E} "s,$pwd_in_variables2,${SED_RED},g" | sed -${E} "s,$pwd_in_variables3,${SED_RED},g" | sed -${E} "s,$pwd_in_variables4,${SED_RED},g" | sed -${E} "s,$pwd_in_variables5,${SED_RED},g" | sed -${E} "s,$pwd_in_variables6,${SED_RED},g" | sed -${E} "s,$pwd_in_variables7,${SED_RED},g" | sed -${E} "s,$pwd_in_variables8,${SED_RED},g" | sed -${E} "s,$pwd_in_variables9,${SED_RED},g" | sed -${E} "s,$pwd_in_variables10,${SED_RED},g" | sed -${E} "s,$pwd_in_variables11,${SED_RED},g" &
  else
    timeout 150 find $SEARCH_IN_FOLDER -exec grep -HnRiIE "($pwd_in_variables1|$pwd_in_variables2|$pwd_in_variables3|$pwd_in_variables4|$pwd_in_variables5|$pwd_in_variables6|$pwd_in_variables7|$pwd_in_variables8|$pwd_in_variables9|$pwd_in_variables10|$pwd_in_variables11).*[=:].+" '{}' \; 2>/dev/null | sed '/^.\{150\}./d' | grep -Ev "^#" | grep -iv "linpeas" | sort | uniq | head -n 70 | sed -${E} "s,$pwd_in_variables1,${SED_RED},g" | sed -${E} "s,$pwd_in_variables2,${SED_RED},g" | sed -${E} "s,$pwd_in_variables3,${SED_RED},g" | sed -${E} "s,$pwd_in_variables4,${SED_RED},g" | sed -${E} "s,$pwd_in_variables5,${SED_RED},g" | sed -${E} "s,$pwd_in_variables6,${SED_RED},g" | sed -${E} "s,$pwd_in_variables7,${SED_RED},g" | sed -${E} "s,$pwd_in_variables8,${SED_RED},g" | sed -${E} "s,$pwd_in_variables9,${SED_RED},g" | sed -${E} "s,$pwd_in_variables10,${SED_RED},g" | sed -${E} "s,$pwd_in_variables11,${SED_RED},g" &
  fi
  wait
  echo ""

  ##-- IF) Find possible conf files with passwords
  print_2title "Searching possible password in config files (if k8s secrets are found you need to read the file)"
  if ! [ "$SEARCH_IN_FOLDER" ]; then
    ppicf=$(timeout 150 find $HOMESEARCH /var/www/ /usr/local/www/ /etc /opt /tmp /private /Applications /mnt -name "*.conf" -o -name "*.cnf" -o -name "*.config" -name "*.json" -name "*.yml" -name "*.yaml" 2>/dev/null)
  else
    ppicf=$(timeout 150 find $SEARCH_IN_FOLDER -name "*.conf" -o -name "*.cnf" -o -name "*.config" -name "*.json" -name "*.yml" -name "*.yaml" 2>/dev/null)
  fi
  printf "%s\n" "$ppicf" | while read f; do
    if grep -qEiI 'passwd.*|creden.*|^kind:\W?Secret|\Wenv:|\Wsecret:|\WsecretName:|^kind:\W?EncryptionConfiguration|\-\-encriyption\-provider\-config' \"$f\" 2>/dev/null; then
      echo "$ITALIC $f$NC"
      grep -HnEiIo 'passwd.*|creden.*|^kind:\W?Secret|\Wenv:|\Wsecret:|\WsecretName:|^kind:\W?EncryptionConfiguration|\-\-encriyption\-provider\-config' "$f" 2>/dev/null | sed -${E} "s,[pP][aA][sS][sS][wW]|[cC][rR][eE][dD][eE][nN],${SED_RED},g"
    fi
  done
  echo ""
fi
