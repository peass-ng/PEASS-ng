###########################################
#-) Files with Interesting Permissions  (-#
###########################################

check_critial_root_path(){
  folder_path="$1"
  if [ -w "$folder_path" ]; then echo "You have write privileges over $folder_path" | sed -${E} "s,.*,${SED_RED_YELLOW},"; fi
  if [ "$(find $folder_path -type f '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null)" ]; then echo "You have write privileges over $(find $folder_path -type f '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')')" | sed -${E} "s,.*,${SED_RED_YELLOW},"; fi
  if [ "$(find $folder_path -type f -not -user root 2>/dev/null)" ]; then echo "The following files aren't owned by root: $(find $folder_path -type f -not -user root 2>/dev/null)"; fi
}




##-- IPF) SUID
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
        if ! [ "$FAST" ]; then
          
          if [ "$STRINGS" ]; then
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
          fi

          if [ "$LDD" ] || [ "$READELF" ]; then
            echo "$ITALIC  --- Checking for writable dependencies of $sname...$NC"
          fi
          if [ "$LDD" ]; then
            "$LDD" "$sname" | grep -E "$Wfolders" | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
          fi
          if [ "$READELF" ]; then
            "$READELF" -d "$sname" | grep PATH | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
          fi
          
          if [ "$TIMEOUT" ] && [ "$STRACE" ] && ! [ "$NOTEXPORT" ] && [ -x "$sname" ]; then
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


##-- IPF) SGID
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
        if ! [ "$FAST" ]; then
        
          if [ "$STRINGS" ]; then
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
          fi

          if [ "$LDD" ] || [ "$READELF" ]; then
            echo "$ITALIC  --- Checking for writable dependencies of $sname...$NC"
          fi
          if [ "$LDD" ]; then
            "$LDD" "$sname" | grep -E "$Wfolders" | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
          fi
          if [ "$READELF" ]; then
            "$READELF" -d "$sname" | grep PATH | grep -E "$Wfolders" | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
          fi
            
          if [ "$TIMEOUT" ] && [ "$STRACE" ] && [ ! "$SUPERFAST" ]; then
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

##-- IPF) Misconfigured ld.so
if ! [ "$SEARCH_IN_FOLDER" ] && ! [ "$IAMROOT" ]; then
  print_2title "Checking misconfigurations of ld.so"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#ld.so"
  if [ -f "/etc/ld.so.conf" ] && [ -w "/etc/ld.so.conf" ]; then 
    echo "You have write privileges over /etc/ld.so.conf" | sed -${E} "s,.*,${SED_RED_YELLOW},"; 
    printf $RED$ITALIC"/etc/ld.so.conf\n"$NC;
  else
    printf $GREEN$ITALIC"/etc/ld.so.conf\n"$NC;
  fi

  echo "Content of /etc/ld.so.conf:"
  cat /etc/ld.so.conf 2>/dev/null | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"

  # Check each configured folder
  cat /etc/ld.so.conf 2>/dev/null | while read l; do
    if echo "$l" | grep -q include; then
      ini_path=$(echo "$l" | cut -d " " -f 2)
      fpath=$(dirname "$ini_path")

      if [ -d "/etc/ld.so.conf" ] && [ -w "$fpath" ]; then 
        echo "You have write privileges over $fpath" | sed -${E} "s,.*,${SED_RED_YELLOW},"; 
        printf $RED_YELLOW$ITALIC"$fpath\n"$NC;
      else
        printf $GREEN$ITALIC"$fpath\n"$NC;
      fi

      if [ "$(find $fpath -type f '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null)" ]; then
        echo "You have write privileges over $(find $fpath -type f '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null)" | sed -${E} "s,.*,${SED_RED_YELLOW},"; 
      fi

      for f in $fpath/*; do
        if [ -w "$f" ]; then 
          echo "You have write privileges over $f" | sed -${E} "s,.*,${SED_RED_YELLOW},"; 
          printf $RED_YELLOW$ITALIC"$f\n"$NC;
        else
          printf $GREEN$ITALIC"  $f\n"$NC;
        fi

        cat "$f" | grep -v "^#" | while read l2; do
          if [ -f "$l2" ] && [ -w "$l2" ]; then 
            echo "You have write privileges over $l2" | sed -${E} "s,.*,${SED_RED_YELLOW},"; 
            printf $RED_YELLOW$ITALIC"  - $l2\n"$NC;
          else
            echo $ITALIC"  - $l2"$NC | sed -${E} "s,$l2,${SED_GREEN}," | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g";
          fi
        done
      done
    fi
  done
  echo ""


  if [ -f "/etc/ld.so.preload" ] && [ -w "/etc/ld.so.preload" ]; then 
    echo "You have write privileges over /etc/ld.so.preload" | sed -${E} "s,.*,${SED_RED_YELLOW},"; 
  else
    printf $ITALIC$GREEN"/etc/ld.so.preload\n"$NC;
  fi
  cat /etc/ld.so.preload 2>/dev/null | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
  cat /etc/ld.so.preload 2>/dev/null | while read l; do
    if [ -f "$l" ] && [ -w "$l" ]; then echo "You have write privileges over $l" | sed -${E} "s,.*,${SED_RED_YELLOW},"; fi
  done

fi

##-- IPF) Capabilities
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Capabilities"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#capabilities"
  if [ "$(command -v capsh)" ]; then

    print_3title "Current shell capabilities"
    cat "/proc/$$/status" | grep Cap | while read -r cap_line; do
      cap_name=$(echo "$cap_line" | awk '{print $1}')
      cap_value=$(echo "$cap_line" | awk '{print $2}')
      if [ "$cap_name" = "CapEff:" ]; then
        echo "$cap_name	 $(capsh --decode=0x"$cap_value" | sed -${E} "s,$capsB,${SED_RED_YELLOW},")"
      else
        echo "$cap_name  $(capsh --decode=0x"$cap_value" | sed -${E} "s,$capsB,${SED_RED},")"
      fi
    done
    echo ""

    print_3title "Parent process capabilities"
    cat "/proc/$PPID/status" | grep Cap | while read -r cap_line; do
      cap_name=$(echo "$cap_line" | awk '{print $1}')
      cap_value=$(echo "$cap_line" | awk '{print $2}')
      if [ "$cap_name" = "CapEff:" ]; then
        echo "$cap_name	 $(capsh --decode=0x"$cap_value" | sed -${E} "s,$capsB,${SED_RED_YELLOW},")"
      else
        echo "$cap_name	 $(capsh --decode=0x"$cap_value" | sed -${E} "s,$capsB,${SED_RED},")"
      fi
    done
    echo ""
  
  else
    print_3title "Current shell capabilities"
    (cat "/proc/$$/status" | grep Cap | sed -${E} "s,.*0000000000000000|CapBnd:	0000003fffffffff,${SED_GREEN},") 2>/dev/null || echo_not_found "/proc/$$/status"
    echo ""
    
    print_3title "Parent proc capabilities"
    (cat "/proc/$PPID/status" | grep Cap | sed -${E} "s,.*0000000000000000|CapBnd:	0000003fffffffff,${SED_GREEN},") 2>/dev/null || echo_not_found "/proc/$PPID/status"
    echo ""
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

##-- IPF) Users with capabilities
if [ -f "/etc/security/capability.conf" ] || [ "$DEBUG" ]; then
  print_2title "Users with capabilities"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#capabilities"
  if [ -f "/etc/security/capability.conf" ]; then
    grep -v '^#\|none\|^$' /etc/security/capability.conf 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED},"
  else echo_not_found "/etc/security/capability.conf"
  fi
  echo ""
fi

##-- IPF) AppArmor profiles to prevent suid/capabilities abuse
if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ -d "/etc/apparmor.d/" ] && [ -r "/etc/apparmor.d/" ]; then
    print_2title "AppArmor binary profiles"
    ls -l /etc/apparmor.d/ 2>/dev/null | grep -E "^-" | grep "\."
    echo ""
  fi
fi

##-- IPF) Files with ACLs
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

##-- IPF) Files with ResourceFork
#if [ "$MACPEAS" ] && ! [ "$FAST" ] && ! [ "$SUPERFAST" ]; then # TOO SLOW, CHECK IT LATER
#  print_2title "Files with ResourceFork"
#  print_info "https://book.hacktricks.xyz/macos/macos-security-and-privilege-escalation#resource-forks-or-macos-ads"
#  find $HOMESEARCH -type f -exec ls -ld {} \; 2>/dev/null | grep -E ' [x\-]@ ' | awk '{printf $9; printf "\n"}' | xargs -I {} xattr -lv {} | grep "com.apple.ResourceFork"
#fi
#echo ""

##-- IPF) Files (scripts) in /etc/profile.d/
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

  ##-- IPF) Files (scripts) in /etc/init.d/
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



##-- IPF) Hashes in passwd file
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_list "Hashes inside passwd file? ........... "
  if grep -qv '^[^:]*:[x\*\!]\|^#\|^$' /etc/passwd /etc/master.passwd /etc/group 2>/dev/null; then grep -v '^[^:]*:[x\*]\|^#\|^$' /etc/passwd /etc/pwd.db /etc/master.passwd /etc/group 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  ##-- IPF) Writable in passwd file
  print_list "Writable passwd file? ................ "
  if [ -w "/etc/passwd" ]; then echo "/etc/passwd is writable" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  elif [ -w "/etc/pwd.db" ]; then echo "/etc/pwd.db is writable" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  elif [ -w "/etc/master.passwd" ]; then echo "/etc/master.passwd is writable" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  else echo_no
  fi

  ##-- IPF) Credentials in fstab
  print_list "Credentials in fstab/mtab? ........... "
  if grep -qE "(user|username|login|pass|password|pw|credentials)[=:]" /etc/fstab /etc/mtab 2>/dev/null; then grep -E "(user|username|login|pass|password|pw|credentials)[=:]" /etc/fstab /etc/mtab 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  ##-- IPF) Read shadow files
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

  ##-- IPF) Read opasswd file
  print_list "Can I read opasswd file? ............. "
  if [ -r "/etc/security/opasswd" ]; then cat /etc/security/opasswd 2>/dev/null || echo ""
  else echo_no
  fi

  ##-- IPF) network-scripts
  print_list "Can I write in network-scripts? ...... "
  if ! [ "$IAMROOT" ] && [ -w "/etc/sysconfig/network-scripts/" ]; then echo "You have write privileges on /etc/sysconfig/network-scripts/" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  elif [ "$(find /etc/sysconfig/network-scripts/ '(' -not -type l -and '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' ')' 2>/dev/null)" ]; then echo "You have write privileges on $(find /etc/sysconfig/network-scripts/ '(' -not -type l -and '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' ')' 2>/dev/null)" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  else echo_no
  fi

  ##-- IPF) Read root dir
  print_list "Can I read root folder? .............. "
  (ls -al /root/ 2>/dev/null | grep -vi "total 0") || echo_no
  echo ""
fi

##-- IPF) Root files in home dirs
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Searching root files in home dirs (limit 30)"
  (find $HOMESEARCH -user root 2>/dev/null | head -n 30 | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_RED},g") || echo_not_found
  echo ""
fi

##-- IPF) Others files in my dirs
if ! [ "$IAMROOT" ]; then
  print_2title "Searching folders owned by me containing others files on it (limit 100)"
  (find $ROOT_FOLDER -type d -user "$USER" ! -path "/proc/*" ! -path "/sys/*" 2>/dev/null | head -n 100 | while read d; do find "$d" -maxdepth 1 ! -user "$USER" \( -type f -or -type d \) -exec ls -l {} \; 2>/dev/null; done) | sort | uniq | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed "s,root,${C}[1;13m&${C}[0m,g"
  echo ""
fi

##-- IPF) Readable files belonging to root and not world readable
if ! [ "$IAMROOT" ]; then
  print_2title "Readable files belonging to root and readable by me but not world readable"
  (find $ROOT_FOLDER -type f -user root ! -perm -o=r ! -path "/proc/*" 2>/dev/null | grep -v "\.journal" | while read f; do if [ -r "$f" ]; then ls -l "$f" 2>/dev/null | sed -${E} "s,/.*,${SED_RED},"; fi; done) || echo_not_found
  echo ""
fi

##-- IPF) Interesting writable files by ownership or all
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

##-- IPF) Interesting writable files by group
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