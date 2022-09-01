###########################################
#----------) Interesting files (----------#
###########################################

check_critial_root_path(){
  folder_path="$1"
  if [ -w "$folder_path" ]; then echo "You have write privileges over $folder_path" | sed -${E} "s,.*,${SED_RED_YELLOW},"; fi
  if [ "$(find $folder_path -type f '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null)" ]; then echo "You have write privileges over $(find $folder_path -type f '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')')" | sed -${E} "s,.*,${SED_RED_YELLOW},"; fi
  if [ "$(find $folder_path -type f -not -user root 2>/dev/null)" ]; then echo "The following files aren't owned by root: $(find $folder_path -type f -not -user root 2>/dev/null)"; fi
}




##-- IF) SUID
print_2title "SUID - Check easy privesc, exploits and write perms"
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#sudo-and-suid"
if ! [ "$STRINGS" ]; then
  echo_not_found "strings"
fi
if ! [ "$STRACE" ]; then
  echo_not_found "strace"
fi
suids_files=$(find $ROOT_FOLDER -perm -4000 -type f ! -path "/dev/*" 2>/dev/null)
for s in $suids_files; do
  s=$(ls -lahtr "$s")
  #If starts like "total 332K" then no SUID bin was found and xargs just executed "ls" in the current folder
  if echo "$s" | grep -qE "^total"; then break; fi

  sname="$(echo $s | awk '{print $9}')"
  if [ "$sname" = "."  ] || [ "$sname" = ".."  ]; then
    true #Don't do nothing
  elif ! [ "$IAMROOT" ] && [ -O "$sname" ]; then
    echo "You own the SUID file: $sname" | sed -${E} "s,.*,${SED_RED},"
  elif ! [ "$IAMROOT" ] && [ -w "$sname" ]; then #If write permision, win found (no check exploits)
    echo "You can write SUID file: $sname" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  else
    c="a"
    for b in $sidB; do
      if echo $s | grep -q $(echo $b | cut -d % -f 1); then
        echo "$s" | sed -${E} "s,$(echo $b | cut -d % -f 1),${C}[1;31m&  --->  $(echo $b | cut -d % -f 2)${C}[0m,"
        c=""
        break;
      fi
    done;
    if [ "$c" ]; then
      if echo "$s" | grep -qE "$sidG1" || echo "$s" | grep -qE "$sidG2" || echo "$s" | grep -qE "$sidG3" || echo "$s" | grep -qE "$sidG4" || echo "$s" | grep -qE "$sidVB" || echo "$s" | grep -qE "$sidVB2"; then
        echo "$s" | sed -${E} "s,$sidG1,${SED_GREEN}," | sed -${E} "s,$sidG2,${SED_GREEN}," | sed -${E} "s,$sidG3,${SED_GREEN}," | sed -${E} "s,$sidG4,${SED_GREEN}," | sed -${E} "s,$sidVB,${SED_RED_YELLOW}," | sed -${E} "s,$sidVB2,${SED_RED_YELLOW},"
      else
        echo "$s (Unknown SUID binary!)" | sed -${E} "s,/.*,${SED_RED},"
        printf $ITALIC
        if ! [ "$FAST" ] && [ "$STRINGS" ]; then
          $STRINGS "$sname" 2>/dev/null | sort | uniq | while read sline; do
            sline_first="$(echo "$sline" | cut -d ' ' -f1)"
            if echo "$sline_first" | grep -qEv "$cfuncs"; then
              if echo "$sline_first" | grep -q "/" && [ -f "$sline_first" ]; then #If a path
                if [ -O "$sline_first" ] || [ -w "$sline_first" ]; then #And modifiable
                  printf "$ITALIC  --- It looks like $RED$sname$NC$ITALIC is using $RED$sline_first$NC$ITALIC and you can modify it (strings line: $sline) (https://tinyurl.com/suidpath)\n"
                fi
              else #If not a path
                if [ ${#sline_first} -gt 2 ] && command -v "$sline_first" 2>/dev/null | grep -q '/' && echo "$sline_first" | grep -Eqv "\.\."; then #Check if existing binary
                  printf "$ITALIC  --- It looks like $RED$sname$NC$ITALIC is executing $RED$sline_first$NC$ITALIC and you can impersonate it (strings line: $sline) (https://tinyurl.com/suidpath)\n"
                fi
              fi
            fi
          done
          if ! [ "$FAST" ] && [ "$TIMEOUT" ] && [ "$STRACE" ] && ! [ "$NOTEXPORT" ] && [ -x "$sname" ]; then
            printf $ITALIC
            echo "----------------------------------------------------------------------------------------"
            echo "  --- Trying to execute $sname with strace in order to look for hijackable libraries..."
            OLD_LD_LIBRARY_PATH=$LD_LIBRARY_PATH
            export LD_LIBRARY_PATH=""
            timeout 2 "$STRACE" "$sname" 2>&1 | grep -i -E "open|access|no such file" | sed -${E} "s,open|access|No such file,${SED_RED}$ITALIC,g"
            printf $NC
            export LD_LIBRARY_PATH=$OLD_LD_LIBRARY_PATH
            echo "----------------------------------------------------------------------------------------"
            echo ""
          fi
        fi
      fi
    fi
  fi
done;
echo ""


##-- IF) SGID
print_2title "SGID"
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#sudo-and-suid"
sgids_files=$(find $ROOT_FOLDER -perm -2000 -type f ! -path "/dev/*" 2>/dev/null)
for s in $sgids_files; do
  s=$(ls -lahtr "$s")
  #If starts like "total 332K" then no SUID bin was found and xargs just executed "ls" in the current folder
  if echo "$s" | grep -qE "^total";then break; fi

  sname="$(echo $s | awk '{print $9}')"
  if [ "$sname" = "."  ] || [ "$sname" = ".."  ]; then
    true #Don't do nothing
  elif ! [ "$IAMROOT" ] && [ -O "$sname" ]; then
    echo "You own the SGID file: $sname" | sed -${E} "s,.*,${SED_RED},"
  elif ! [ "$IAMROOT" ] && [ -w "$sname" ]; then #If write permision, win found (no check exploits)
    echo "You can write SGID file: $sname" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  else
    c="a"
    for b in $sidB; do
      if echo "$s" | grep -q $(echo $b | cut -d % -f 1); then
        echo "$s" | sed -${E} "s,$(echo $b | cut -d % -f 1),${C}[1;31m&  --->  $(echo $b | cut -d % -f 2)${C}[0m,"
        c=""
        break;
      fi
    done;
    if [ "$c" ]; then
      if echo "$s" | grep -qE "$sidG1" || echo "$s" | grep -qE "$sidG2" || echo "$s" | grep -qE "$sidG3" || echo "$s" | grep -qE "$sidG4" || echo "$s" | grep -qE "$sidVB" || echo "$s" | grep -qE "$sidVB2"; then
        echo "$s" | sed -${E} "s,$sidG1,${SED_GREEN}," | sed -${E} "s,$sidG2,${SED_GREEN}," | sed -${E} "s,$sidG3,${SED_GREEN}," | sed -${E} "s,$sidG4,${SED_GREEN}," | sed -${E} "s,$sidVB,${SED_RED_YELLOW}," | sed -${E} "s,$sidVB2,${SED_RED_YELLOW},"
      else
        echo "$s (Unknown SGID binary)" | sed -${E} "s,/.*,${SED_RED},"
        printf $ITALIC
        if ! [ "$FAST" ] && [ "$STRINGS" ]; then
          $STRINGS "$sname" | sort | uniq | while read sline; do
            sline_first="$(echo $sline | cut -d ' ' -f1)"
            if echo "$sline_first" | grep -qEv "$cfuncs"; then
              if echo "$sline_first" | grep -q "/" && [ -f "$sline_first" ]; then #If a path
                if [ -O "$sline_first" ] || [ -w "$sline_first" ]; then #And modifiable
                  printf "$ITALIC  --- It looks like $RED$sname$NC$ITALIC is using $RED$sline_first$NC$ITALIC and you can modify it (strings line: $sline)\n"
                fi
              else #If not a path
                if [ ${#sline_first} -gt 2 ] && command -v "$sline_first" 2>/dev/null | grep -q '/'; then #Check if existing binary
                  printf "$ITALIC  --- It looks like $RED$sname$NC$ITALIC is executing $RED$sline_first$NC$ITALIC and you can impersonate it (strings line: $sline)\n"
                fi
              fi
            fi
          done
          if ! [ "$FAST" ] && [ "$TIMEOUT" ] && [ "$STRACE" ] && [ ! "$SUPERFAST" ]; then
            printf "$ITALIC"
            echo "  --- Trying to execute $sname with strace in order to look for hijackable libraries..."
            timeout 2 "$STRACE" "$sname" 2>&1 | grep -i -E "open|access|no such file" | sed -${E} "s,open|access|No such file,${SED_RED}$ITALIC,g"
            printf "$NC"
            echo ""
          fi
        fi
      fi
    fi
  fi
done;
echo ""

##-- IF) Misconfigured ld.so
if ! [ "$SEARCH_IN_FOLDER" ] && ! [ "$IAMROOT" ]; then
  print_2title "Checking misconfigurations of ld.so"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#ld-so"
  printf $ITALIC"/etc/ld.so.conf\n"$NC;
  cat /etc/ld.so.conf 2>/dev/null | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
  cat /etc/ld.so.conf 2>/dev/null | while read l; do
    if echo "$l" | grep -q include; then
      ini_path=$(echo "$l" | cut -d " " -f 2)
      fpath=$(dirname "$ini_path")
      if [ "$(find $fpath -type f '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null)" ]; then echo "You have write privileges over $(find $fpath -type f '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null)" | sed -${E} "s,.*,${SED_RED_YELLOW},"; fi
      printf $ITALIC"$fpath\n"$NC | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
      for f in $fpath/*; do
        printf $ITALIC"  $f\n"$NC | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
        cat "$f" | grep -v "^#" | sed -${E} "s,$ldsoconfdG,${SED_GREEN}," | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
      done
    fi
  done
  echo ""
fi

##-- IF) Capabilities
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Capabilities"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#capabilities"
  if [ "$(command -v capsh)" ]; then
    echo "Current env capabilities:"
    (capsh --print 2>/dev/null | grep "Current:" | sed -${E} "s,$capsB,${SED_RED_YELLOW}," ) || echo_not_found "capsh"
    echo "Current proc capabilities:"
    (cat "/proc/$$/status" | grep Cap | sed -${E} "s,.*0000000000000000|CapBnd:	0000003fffffffff,${SED_GREEN},") 2>/dev/null || echo_not_found "/proc/$$/status"
    echo ""
    echo "Parent Shell capabilities:"
    (capsh --decode=0x"$(cat /proc/$PPID/status 2>/dev/null | grep CapEff | awk '{print $2}')" 2>/dev/null) || echo_not_found "capsh"
  else
    echo "Current capabilities:"
    cat /proc/self/status | grep Cap | sed -${E} "s, .*,${SED_RED},g" | sed -${E} "s,0000000000000000|0000003fffffffff,${SED_GREEN},g"
    echo ""
    echo "Shell capabilities:"
    cat /proc/$PPID/status | grep Cap | sed -${E} "s, .*,${SED_RED},g" | sed -${E} "s,0000000000000000|0000003fffffffff,${SED_GREEN},g"
  fi
  echo ""
  echo "Files with capabilities (limited to 50):"
  getcap -r / 2>/dev/null | head -n 50 | while read cb; do
    capsVB_vuln=""
    
    for capVB in $capsVB; do
      capname="$(echo $capVB | cut -d ':' -f 1)"
      capbins="$(echo $capVB | cut -d ':' -f 2)"
      if [ "$(echo $cb | grep -Ei $capname)" ] && [ "$(echo $cb | grep -E $capbins)" ]; then
        echo "$cb" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        capsVB_vuln="1"
        break
      fi
    done
    
    if ! [ "$capsVB_vuln" ]; then
      echo "$cb" | sed -${E} "s,$capsB,${SED_RED},"
    fi

    if ! [ "$IAMROOT" ] && [ -w "$(echo $cb | cut -d" " -f1)" ]; then
      echo "$cb is writable" | sed -${E} "s,.*,${SED_RED},"
    fi
  done
  echo ""
fi

##-- IF) Users with capabilities
if [ -f "/etc/security/capability.conf" ] || [ "$DEBUG" ]; then
  print_2title "Users with capabilities"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#capabilities"
  if [ -f "/etc/security/capability.conf" ]; then
    grep -v '^#\|none\|^$' /etc/security/capability.conf 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED},"
  else echo_not_found "/etc/security/capability.conf"
  fi
  echo ""
fi

##-- IF) AppArmor profiles to prevent suid/capabilities abuse
if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ -d "/etc/apparmor.d/" ] && [ -r "/etc/apparmor.d/" ]; then
    print_2title "AppArmor binary profiles"
    ls -l /etc/apparmor.d/ 2>/dev/null | grep -E "^-" | grep "\."
    echo ""
  fi
fi

##-- IF) Files with ACLs
print_2title "Files with ACLs (limited to 50)"
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#acls"
if ! [ "$SEARCH_IN_FOLDER" ]; then
  ( (getfacl -t -s -R -p /bin /etc $HOMESEARCH /opt /sbin /usr /tmp /root 2>/dev/null) || echo_not_found "files with acls in searched folders" ) | head -n 70 | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED},"
else
  ( (getfacl -t -s -R -p $SEARCH_IN_FOLDER 2>/dev/null) || echo_not_found "files with acls in searched folders" ) | head -n 70 | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED},"
fi

if [ "$MACPEAS" ] && ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && ! [ "$(command -v getfacl)" ]; then  #Find ACL files in macos (veeeery slow)
  ls -RAle / 2>/dev/null | grep -v "group:everyone deny delete" | grep -E -B1 "\d: " | head -n 70 | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED},"
fi
echo ""

##-- IF) Files with ResourceFork
#if [ "$MACPEAS" ] && ! [ "$FAST" ] && ! [ "$SUPERFAST" ]; then # TOO SLOW, CHECK IT LATER
#  print_2title "Files with ResourceFork"
#  print_info "https://book.hacktricks.xyz/macos/macos-security-and-privilege-escalation#resource-forks-or-macos-ads"
#  find $HOMESEARCH -type f -exec ls -ld {} \; 2>/dev/null | grep -E ' [x\-]@ ' | awk '{printf $9; printf "\n"}' | xargs -I {} xattr -lv {} | grep "com.apple.ResourceFork"
#fi
#echo ""

##-- IF) .sh files in PATH
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title ".sh files in path"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#script-binaries-in-path"
  echo $PATH | tr ":" "\n" | while read d; do
    for f in $(find "$d" -name "*.sh" 2>/dev/null); do
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
  print_2title "FIles datetimes inside the firmware (limit 50)"
  find "$SEARCH_IN_FOLDER" -type f -printf "%T+\n" 2>/dev/null | sort | uniq -c | sort | head -n 50
  echo "To find a file with an specific date execute: find \"$SEARCH_IN_FOLDER\" -type f -printf \"%T+ %p\n\" 2>/dev/null | grep \"<date>\""
  echo ""
fi

##-- IF) Executable files added by user
print_2title "Executable files potentially added by user (limit 70)"
if ! [ "$SEARCH_IN_FOLDER" ]; then
  find / -type f -executable -printf "%T+ %p\n" 2>/dev/null | grep -Ev "000|/site-packages|/python|/node_modules|\.sample|/gems" | sort -r | head -n 70
else
  find "$SEARCH_IN_FOLDER" -type f -executable -printf "%T+ %p\n" 2>/dev/null | grep -Ev "/site-packages|/python|/node_modules|\.sample|/gems" | sort -r | head -n 70
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

##-- IF) Files (scripts) in /etc/profile.d/
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Files (scripts) in /etc/profile.d/"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#profiles-files"
  if [ ! "$MACPEAS" ] && ! [ "$IAMROOT" ]; then #Those folders don´t exist on a MacOS
    (ls -la /etc/profile.d/ 2>/dev/null | sed -${E} "s,$profiledG,${SED_GREEN},") || echo_not_found "/etc/profile.d/"
    check_critial_root_path "/etc/profile"
    check_critial_root_path "/etc/profile.d/"
  fi
  echo ""
fi

  ##-- IF) Files (scripts) in /etc/init.d/
  if ! [ "$SEARCH_IN_FOLDER" ]; then
print_2title "Permissions in init, init.d, systemd, and rc.d"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#init-init-d-systemd-and-rc-d"
  if [ ! "$MACPEAS" ] && ! [ "$IAMROOT" ]; then #Those folders don´t exist on a MacOS
    check_critial_root_path "/etc/init/"
    check_critial_root_path "/etc/init.d/"
    check_critial_root_path "/etc/rc.d/init.d"
    check_critial_root_path "/usr/local/etc/rc.d"
    check_critial_root_path "/etc/rc.d"
    check_critial_root_path "/etc/systemd/"
    check_critial_root_path "/lib/systemd/"
  fi

  echo ""
fi

##-- IF) Hashes in passwd file
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_list "Hashes inside passwd file? ........... "
  if grep -qv '^[^:]*:[x\*\!]\|^#\|^$' /etc/passwd /etc/master.passwd /etc/group 2>/dev/null; then grep -v '^[^:]*:[x\*]\|^#\|^$' /etc/passwd /etc/pwd.db /etc/master.passwd /etc/group 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  ##-- IF) Writable in passwd file
  print_list "Writable passwd file? ................ "
  if [ -w "/etc/passwd" ]; then echo "/etc/passwd is writable" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  elif [ -w "/etc/pwd.db" ]; then echo "/etc/pwd.db is writable" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  elif [ -w "/etc/master.passwd" ]; then echo "/etc/master.passwd is writable" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  else echo_no
  fi

  ##-- IF) Credentials in fstab
  print_list "Credentials in fstab/mtab? ........... "
  if grep -qE "(user|username|login|pass|password|pw|credentials)[=:]" /etc/fstab /etc/mtab 2>/dev/null; then grep -E "(user|username|login|pass|password|pw|credentials)[=:]" /etc/fstab /etc/mtab 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  ##-- IF) Read shadow files
  print_list "Can I read shadow files? ............. "
  if [ "$(cat /etc/shadow /etc/shadow- /etc/shadow~ /etc/gshadow /etc/gshadow- /etc/master.passwd /etc/spwd.db 2>/dev/null)" ]; then cat /etc/shadow /etc/shadow- /etc/shadow~ /etc/gshadow /etc/gshadow- /etc/master.passwd /etc/spwd.db 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  print_list "Can I read shadow plists? ............ "
  possible_check=""
  (for l in /var/db/dslocal/nodes/Default/users/*; do if [ -r "$l" ];then echo "$l"; defaults read "$l"; possible_check="1"; fi; done; if ! [ "$possible_check" ]; then echo_no; fi) 2>/dev/null || echo_no

  print_list "Can I write shadow plists? ........... "
  possible_check=""
  (for l in /var/db/dslocal/nodes/Default/users/*; do if [ -w "$l" ];then echo "$l"; possible_check="1"; fi; done; if ! [ "$possible_check" ]; then echo_no; fi) 2>/dev/null || echo_no

  ##-- IF) Read opasswd file
  print_list "Can I read opasswd file? ............. "
  if [ -r "/etc/security/opasswd" ]; then cat /etc/security/opasswd 2>/dev/null || echo ""
  else echo_no
  fi

  ##-- IF) network-scripts
  print_list "Can I write in network-scripts? ...... "
  if ! [ "$IAMROOT" ] && [ -w "/etc/sysconfig/network-scripts/" ]; then echo "You have write privileges on /etc/sysconfig/network-scripts/" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  elif [ "$(find /etc/sysconfig/network-scripts/ '(' -not -type l -and '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' ')' 2>/dev/null)" ]; then echo "You have write privileges on $(find /etc/sysconfig/network-scripts/ '(' -not -type l -and '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' ')' 2>/dev/null)" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  else echo_no
  fi

  ##-- IF) Read root dir
  print_list "Can I read root folder? .............. "
  (ls -al /root/ 2>/dev/null | grep -vi "total 0") || echo_no
  echo ""
fi

##-- IF) Root files in home dirs
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Searching root files in home dirs (limit 30)"
  (find $HOMESEARCH -user root 2>/dev/null | head -n 30 | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_RED},") || echo_not_found
  echo ""
fi

##-- IF) Others files in my dirs
if ! [ "$IAMROOT" ]; then
  print_2title "Searching folders owned by me containing others files on it (limit 100)"
  (find $ROOT_FOLDER -type d -user "$USER" ! -path "/proc/*" 2>/dev/null | head -n 100 | while read d; do find "$d" -maxdepth 1 ! -user "$USER" \( -type f -or -type d \) -exec dirname {} \; 2>/dev/null; done) | sort | uniq | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed "s,root,${C}[1;13m&${C}[0m,g"
  echo ""
fi

##-- IF) Readable files belonging to root and not world readable
if ! [ "$IAMROOT" ]; then
  print_2title "Readable files belonging to root and readable by me but not world readable"
  (find $ROOT_FOLDER -type f -user root ! -perm -o=r ! -path "/proc/*" 2>/dev/null | grep -v "\.journal" | while read f; do if [ -r "$f" ]; then ls -l "$f" 2>/dev/null | sed -${E} "s,/.*,${SED_RED},"; fi; done) || echo_not_found
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
print_2title "All hidden files (not in /sys/ or the ones listed in the previous check) (limit 70)"
find $ROOT_FOLDER -type f -iname ".*" ! -path "/sys/*" ! -path "/System/*" ! -path "/private/var/*" -exec ls -l {} \; 2>/dev/null | grep -Ev "$INT_HIDDEN_FILES" | grep -Ev "_history$|\.gitignore|.npmignore|\.listing|\.ignore|\.uuid|\.depend|\.placeholder|\.gitkeep|\.keep|\.keepme" | head -n 70
echo ""

##-- IF) Readable files in /tmp, /var/tmp, bachups
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Readable files inside /tmp, /var/tmp, /private/tmp, /private/var/at/tmp, /private/var/tmp, and backup folders (limit 70)"
  filstmpback=$(find /tmp /var/tmp /private/tmp /private/var/at/tmp /private/var/tmp $backup_folders_row -type f 2>/dev/null | head -n 70)
  printf "%s\n" "$filstmpback" | while read f; do if [ -r "$f" ]; then ls -l "$f" 2>/dev/null; fi; done
  echo ""
fi

##-- IF) Interesting writable files by ownership or all
if ! [ "$IAMROOT" ]; then
  print_2title "Interesting writable files owned by me or writable by everyone (not in Home) (max 500)"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#writable-files"
  #In the next file, you need to specify type "d" and "f" to avoid fake link files apparently writable by all
  obmowbe=$(find $ROOT_FOLDER '(' -type f -or -type d ')' '(' '(' -user $USER ')' -or '(' -perm -o=w ')' ')' ! -path "/proc/*" ! -path "/sys/*" ! -path "$HOME/*" 2>/dev/null | grep -Ev "$notExtensions" | sort | uniq | awk -F/ '{line_init=$0; if (!cont){ cont=0 }; $NF=""; act=$0; if (act == pre){(cont += 1)} else {cont=0}; if (cont < 5){ print line_init; } if (cont == "5"){print "#)You_can_write_even_more_files_inside_last_directory\n"}; pre=act }' | head -n500)
  printf "%s\n" "$obmowbe" | while read entry; do
    if echo "$entry" | grep -q "You_can_write_even_more_files_inside_last_directory"; then printf $ITALIC"$entry\n"$NC;
    elif echo "$entry" | grep -qE "$writeVB"; then
      echo "$entry" | sed -${E} "s,$writeVB,${SED_RED_YELLOW},"
    else
      echo "$entry" | sed -${E} "s,$writeB,${SED_RED},"
    fi
  done
  echo ""
fi

##-- IF) Interesting writable files by group
if ! [ "$IAMROOT" ]; then
  print_2title "Interesting GROUP writable files (not in Home) (max 500)"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#writable-files"
  for g in $(groups); do
    iwfbg=$(find $ROOT_FOLDER '(' -type f -or -type d ')' -group $g -perm -g=w ! -path "/proc/*" ! -path "/sys/*" ! -path "$HOME/*" 2>/dev/null | grep -Ev "$notExtensions" | awk -F/ '{line_init=$0; if (!cont){ cont=0 }; $NF=""; act=$0; if (act == pre){(cont += 1)} else {cont=0}; if (cont < 5){ print line_init; } if (cont == "5"){print "#)You_can_write_even_more_files_inside_last_directory\n"}; pre=act }' | head -n500)
    if [ "$iwfbg" ] || [ "$DEBUG" ]; then
      printf "  Group $GREEN$g:\n$NC";
      printf "%s\n" "$iwfbg" | while read entry; do
        if echo "$entry" | grep -q "You_can_write_even_more_files_inside_last_directory"; then printf $ITALIC"$entry\n"$NC;
        elif echo "$entry" | grep -Eq "$writeVB"; then
          echo "$entry" | sed -${E} "s,$writeVB,${SED_RED_YELLOW},"
        else
          echo "$entry" | sed -${E} "s,$writeB,${SED_RED},"
        fi
      done
    fi
  done
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
if [ "$DEBUG" ]; then
  print_2title "Searching IPs inside logs (limit 70)"
  (find /var/log/ /private/var/log -type f -exec grep -R -a -E -o "(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)" "{}" \;) 2>/dev/null | grep -v "\.0\.\|:0\|\.0$" | sort | uniq -c | sort -r -n | head -n 70
  echo ""
fi

##-- IF) Passwords inside logs
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Searching passwords inside logs (limit 70)"
  (find /var/log/ /private/var/log -type f -exec grep -R -i "pwd\|passw" "{}" \;) 2>/dev/null | sed '/^.\{150\}./d' | sort | uniq | grep -v "File does not exist:\|script not found or unable to stat:\|\"GET /.*\" 404" | head -n 70 | sed -${E} "s,pwd|passw,${SED_RED},"
  echo ""
fi

if [ "$DEBUG" ]; then
  ##-- IF) Emails inside logs
  print_2title "Searching emails inside logs (limit 70)"
  (find /var/log/ /private/var/log -type f -exec grep -I -R -E -o "\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,6}\b" "{}" \;) 2>/dev/null | sort | uniq -c | sort -r -n | head -n 70 | sed -${E} "s,$knw_emails,${SED_GREEN},g"
  echo ""
fi




if ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && [ "$TIMEOUT" ]; then
  ##-- IF) Find possible files with passwords
  print_2title "Searching passwords inside key folders (limit 70) - only PHP files"
  if ! [ "$SEARCH_IN_FOLDER" ]; then
    intpwdfiles=$(timeout 150 find $HOMESEARCH /var/www/ /usr/local/www/ $backup_folders_row /tmp /etc /mnt /private -type f -exec grep -RiIE "(pwd|passwd|password|PASSWD|PASSWORD|dbuser|dbpass).*[=:].+|define ?\('(\w*passw|\w*user|\w*datab)" '{}' \; 2>/dev/null)
  else
    intpwdfiles=$(timeout 150 find $SEARCH_IN_FOLDER -type f -exec grep -RiIE "(pwd|passwd|password|PASSWD|PASSWORD|dbuser|dbpass).*[=:].+|define ?\('(\w*passw|\w*user|\w*datab)" '{}' \; 2>/dev/null)
  fi
  printf "%s\n" "$intpwdfiles" | grep -I ".php:" | sed '/^.\{150\}./d' | sort | uniq | grep -iIv "linpeas" | head -n 70 | sed -${E} "s,[pP][wW][dD]|[pP][aA][sS][sS][wW]|[dD][eE][fF][iI][nN][eE],${SED_RED},g"
  echo ""

  print_2title "Searching passwords inside key folders (limit 70) - no PHP files"
  printf "%s\n" "$intpwdfiles" | grep -vI ".php:" | grep -E "^/" | grep ":" | sed '/^.\{150\}./d' | sort | uniq | grep -iIv "linpeas" | head -n 70 | sed -${E} "s,[pP][wW][dD]|[pP][aA][sS][sS][wW]|[dD][eE][fF][iI][nN][eE],${SED_RED},g"
  echo ""

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
