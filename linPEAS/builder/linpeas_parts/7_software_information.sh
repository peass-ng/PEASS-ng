###########################################
#--------) Software Information (---------#
###########################################

NGINX_KNOWN_MODULES="ngx_http_geoip_module.so|ngx_http_xslt_filter_module.so|ngx_stream_geoip_module.so|ngx_http_image_filter_module.so|ngx_mail_module.so|ngx_stream_module.so"

#-- SI) Useful software
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Useful software"
  for tool in $USEFUL_SOFTWARE; do command -v "$tool"; done
  echo ""
fi

#-- SI) Search for compilers
if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Installed Compilers"
  (dpkg --list 2>/dev/null | grep "compiler" | grep -v "decompiler\|lib" 2>/dev/null || yum list installed 'gcc*' 2>/dev/null | grep gcc 2>/dev/null; command -v gcc g++ 2>/dev/null || locate -r "/gcc[0-9\.-]\+$" 2>/dev/null | grep -v "/doc/");
  echo ""

  if [ "$(command -v pkg 2>/dev/null)" ]; then
      print_2title "Vulnerable Packages"
      pkg audit -F | sed -${E} "s,vulnerable,${SED_RED},g"
      echo ""
  fi

  if [ "$(command -v brew 2>/dev/null)" ]; then
      print_2title "Brew Installed Packages"
      brew list
      echo ""
  fi
fi

if [ "$MACPEAS" ]; then
    print_2title "Writable Installed Applications"
    system_profiler SPApplicationsDataType | grep "Location:" | cut -d ":" -f 2 | cut -c2- | while read f; do
        if [ -w "$f" ]; then
            echo "$f is writable" | sed -${E} "s,.*,${SED_RED},g"
        fi
    done

    system_profiler SPFrameworksDataType | grep "Location:" | cut -d ":" -f 2 | cut -c2- | while read f; do
        if [ -w "$f" ]; then
            echo "$f is writable" | sed -${E} "s,.*,${SED_RED},g"
        fi
    done
fi

#-- SI) Mysql version
if [ "$(command -v mysql)" ] || [ "$(command -v mysqladmin)" ] || [ "$DEBUG" ]; then
  print_2title "MySQL version"
  mysql --version 2>/dev/null || echo_not_found "mysql"
  mysqluser=$(systemctl status mysql 2>/dev/null | grep -o ".\{0,0\}user.\{0,50\}" | cut -d '=' -f2 | cut -d ' ' -f1)
  if [ "$mysqluser" ]; then
    echo "MySQL user: $mysqluser" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
  fi
  echo ""
  echo ""

  #-- SI) Mysql connection root/root
  print_list "MySQL connection using default root/root ........... "
  mysqlconnect=$(mysqladmin -uroot -proot version 2>/dev/null)
  if [ "$mysqlconnect" ]; then
    echo "Yes" | sed -${E} "s,.*,${SED_RED},"
    mysql -u root --password=root -e "SELECT User,Host,authentication_string FROM mysql.user;" 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  #-- SI) Mysql connection root/toor
  print_list "MySQL connection using root/toor ................... "
  mysqlconnect=$(mysqladmin -uroot -ptoor version 2>/dev/null)
  if [ "$mysqlconnect" ]; then
    echo "Yes" | sed -${E} "s,.*,${SED_RED},"
    mysql -u root --password=toor -e "SELECT User,Host,authentication_string FROM mysql.user;" 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  #-- SI) Mysql connection root/NOPASS
  mysqlconnectnopass=$(mysqladmin -uroot version 2>/dev/null)
  print_list "MySQL connection using root/NOPASS ................. "
  if [ "$mysqlconnectnopass" ]; then
    echo "Yes" | sed -${E} "s,.*,${SED_RED},"
    mysql -u root -e "SELECT User,Host,authentication_string FROM mysql.user;" 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi
  echo ""
fi

#-- SI) Mysql credentials
if [ "$PSTORAGE_MYSQL" ] || [ "$DEBUG" ]; then
  print_2title "Searching mysql credentials and exec"
  printf "%s\n" "$PSTORAGE_MYSQL" | while read d; do
    if [ -f "$d" ] && ! [ "$(basename $d)" = "mysql" ]; then # Only interested in "mysql" that are folders (filesaren't the ones with creds)
      STRINGS="`command -v strings`"
      echo "Potential file containing credentials:"
      ls -l "$d"
      if [ "$STRINGS" ]; then
        strings "$d"
      else
        echo "Strings not found, cat the file and check it to get the creds"
      fi

    else
      for f in $(find $d -name debian.cnf 2>/dev/null); do
        if [ -r "$f" ]; then
          echo "We can read the mysql debian.cnf. You can use this username/password to log in MySQL" | sed -${E} "s,.*,${SED_RED},"
          cat "$f"
        fi
      done
      
      for f in $(find $d -name user.MYD 2>/dev/null); do
        if [ -r "$f" ]; then
          echo "We can read the Mysql Hashes from $f" | sed -${E} "s,.*,${SED_RED},"
          grep -oaE "[-_\.\*a-Z0-9]{3,}" "$f" | grep -v "mysql_native_password"
        fi
      done
      
      for f in $(grep -lr "user\s*=" $d 2>/dev/null | grep -v "debian.cnf"); do
        if [ -r "$f" ]; then
          u=$(cat "$f" | grep -v "#" | grep "user" | grep "=" 2>/dev/null)
          echo "From '$f' Mysql user: $u" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED},"
        fi
      done
      
      for f in $(find $d -name my.cnf 2>/dev/null); do
        if [ -r "$f" ]; then
          echo "Found readable $f"
          grep -v "^#" "$f" | grep -Ev "\W+\#|^#" 2>/dev/null | grep -Iv "^$" | sed "s,password.*,${SED_RED},"
        fi
      done
    fi
    
    mysqlexec=$(whereis lib_mysqludf_sys.so 2>/dev/null | grep "lib_mysqludf_sys\.so")
    if [ "$mysqlexec" ]; then
      echo "Found $mysqlexec"
      echo "If you can login in MySQL you can execute commands doing: SELECT sys_eval('id');" | sed -${E} "s,.*,${SED_RED},"
    fi
  done
fi
echo ""

peass{MariaDB}

peass{PostgreSQL}

#-- SI) PostgreSQL brute
if [ "$TIMEOUT" ] && [ "$(command -v psql)" ] || [ "$DEBUG" ]; then  # In some OS (like OpenBSD) it will expect the password from console and will pause the script. Also, this OS doesn't have the "timeout" command so lets only use this checks in OS that has it.
#checks to see if any postgres password exists and connects to DB 'template0' - following commands are a variant on this
  print_list "PostgreSQL connection to template0 using postgres/NOPASS ........ "
  if [ "$(timeout 1 psql -U postgres -d template0 -c 'select version()' 2>/dev/null)" ]; then echo "Yes" | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  print_list "PostgreSQL connection to template1 using postgres/NOPASS ........ "
  if [ "$(timeout 1 psql -U postgres -d template1 -c 'select version()' 2>/dev/null)" ]; then echo "Yes" | sed "s,.*,${SED_RED},"
  else echo_no
  fi

  print_list "PostgreSQL connection to template0 using pgsql/NOPASS ........... "
  if [ "$(timeout 1 psql -U pgsql -d template0 -c 'select version()' 2>/dev/null)" ]; then echo "Yes" | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi

  print_list "PostgreSQL connection to template1 using pgsql/NOPASS ........... "
  if [ "$(timeout 1 psql -U pgsql -d template1 -c 'select version()' 2> /dev/null)" ]; then echo "Yes" | sed -${E} "s,.*,${SED_RED},"
  else echo_no
  fi
  echo ""
fi

peass{Mongo}

peass{Apache-Nginx}

peass{Tomcat}

peass{FastCGI}

peass{Http_conf}

peass{Htpasswd}

peass{PHP Sessions}

peass{Wordpress}

peass{Drupal}

peass{Moodle}

peass{Supervisord}

peass{Cesi}

peass{Rsync}

peass{Hostapd}

peass{Wifi Connections}

peass{Anaconda ks}

peass{VNC}

peass{OpenVPN}

peass{Ldap}

if [ "$PSTORAGE_LOG4SHELL" ] || [ "$DEBUG" ]; then
  print_2title "Searching Log4Shell vulnerable libraries"
  printf "%s\n" "$PSTORAGE_LOG4SHELL" | while read f; do
    echo "$f" | grep -E "log4j\-core\-(1\.[^0]|2\.[0-9][^0-9]|2\.1[0-6])" | sed -${E} "s,log4j\-core\-(1\.[^0]|2\.[0-9][^0-9]|2\.1[0-6]),${SED_RED},";
  done
  echo ""
fi

#-- SI) ssh files
print_2title "Searching ssl/ssh files"
if [ "$PSTORAGE_CERTSB4" ]; then certsb4_grep=$(grep -L "\"\|'\|(" $PSTORAGE_CERTSB4 2>/dev/null); fi
if ! [ "$SEARCH_IN_FOLDER" ]; then
  sshconfig="$(ls /etc/ssh/ssh_config 2>/dev/null)"
  hostsdenied="$(ls /etc/hosts.denied 2>/dev/null)"
  hostsallow="$(ls /etc/hosts.allow 2>/dev/null)"
  writable_agents=$(find /tmp /etc /home -type s -name "agent.*" -or -name "*gpg-agent*" '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null)
else
  sshconfig="$(ls ${ROOT_FOLDER}etc/ssh/ssh_config 2>/dev/null)"
  hostsdenied="$(ls ${ROOT_FOLDER}etc/hosts.denied 2>/dev/null)"
  hostsallow="$(ls ${ROOT_FOLDER}etc/hosts.allow 2>/dev/null)"
  writable_agents=$(find  ${ROOT_FOLDER} -type s -name "agent.*" -or -name "*gpg-agent*" '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null)
fi

peass{SSH}

grep "PermitRootLogin \|ChallengeResponseAuthentication \|PasswordAuthentication \|UsePAM \|Port\|PermitEmptyPasswords\|PubkeyAuthentication\|ListenAddress\|ForwardAgent\|AllowAgentForwarding\|AuthorizedKeysFiles" /etc/ssh/sshd_config 2>/dev/null | grep -v "#" | sed -${E} "s,PermitRootLogin.*es|PermitEmptyPasswords.*es|ChallengeResponseAuthentication.*es|FordwardAgent.*es,${SED_RED},"

if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ "$TIMEOUT" ]; then
    privatekeyfilesetc=$(timeout 40 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY.*\-\-\-\-\-' /etc 2>/dev/null)
    privatekeyfileshome=$(timeout 40 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY.*\-\-\-\-\-' $HOMESEARCH 2>/dev/null)
    privatekeyfilesroot=$(timeout 40 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY.*\-\-\-\-\-' /root 2>/dev/null)
    privatekeyfilesmnt=$(timeout 40 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY.*\-\-\-\-\-' /mnt 2>/dev/null)
  else
    privatekeyfilesetc=$(grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY.*\-\-\-\-\-' /etc 2>/dev/null) #If there is tons of files linpeas gets frozen here without a timeout
    privatekeyfileshome=$(grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY.*\-\-\-\-\-' $HOME/.ssh 2>/dev/null)
  fi
else
  # If $SEARCH_IN_FOLDER lets just search for private keys in the whole firmware
  privatekeyfilesetc=$(timeout 120 grep -rl '\-\-\-\-\-BEGIN .* PRIVATE KEY.*\-\-\-\-\-' "$ROOT_FOLDER" 2>/dev/null)
fi

if [ "$privatekeyfilesetc" ] || [ "$privatekeyfileshome" ] || [ "$privatekeyfilesroot" ] || [ "$privatekeyfilesmnt" ] ; then
  echo ""
  print_3title "Possible private SSH keys were found!" | sed -${E} "s,private SSH keys,${SED_RED},"
  if [ "$privatekeyfilesetc" ]; then printf "$privatekeyfilesetc\n" | sed -${E} "s,.*,${SED_RED},"; fi
  if [ "$privatekeyfileshome" ]; then printf "$privatekeyfileshome\n" | sed -${E} "s,.*,${SED_RED},"; fi
  if [ "$privatekeyfilesroot" ]; then printf "$privatekeyfilesroot\n" | sed -${E} "s,.*,${SED_RED},"; fi
  if [ "$privatekeyfilesmnt" ]; then printf "$privatekeyfilesmnt\n" | sed -${E} "s,.*,${SED_RED},"; fi
  echo ""
fi
if [ "$certsb4_grep" ] || [ "$PSTORAGE_CERTSBIN" ]; then
  print_3title "Some certificates were found (out limited):"
  printf "$certsb4_grep\n" | head -n 20
  printf "$$PSTORAGE_CERTSBIN\n" | head -n 20
    echo ""
fi
if [ "$PSTORAGE_CERTSCLIENT" ]; then
  print_3title "Some client certificates were found:"
  printf "$PSTORAGE_CERTSCLIENT\n"
  echo ""
fi
if [ "$PSTORAGE_SSH_AGENTS" ]; then
  print_3title "Some SSH Agent files were found:"
  printf "$PSTORAGE_SSH_AGENTS\n"
  echo ""
fi
if ssh-add -l 2>/dev/null | grep -qv 'no identities'; then
  print_3title "Listing SSH Agents"
  ssh-add -l
  echo ""
fi
if gpg-connect-agent "keyinfo --list" /bye 2>/dev/null | grep "D - - 1"; then
  print_3title "Listing gpg keys cached in gpg-agent"
  gpg-connect-agent "keyinfo --list" /bye
  echo ""
fi
if [ "$writable_agents" ]; then
  print_3title "Writable ssh and gpg agents"
  printf "%s\n" "$writable_agents"
fi
if [ "$PSTORAGE_SSH_CONFIG" ]; then
  print_3title "Some home ssh config file was found"
  printf "%s\n" "$PSTORAGE_SSH_CONFIG" | while read f; do ls "$f" | sed -${E} "s,$f,${SED_RED},"; cat "$f" 2>/dev/null | grep -Iv "^$" | grep -v "^#" | sed -${E} "s,User|ProxyCommand,${SED_RED},"; done
  echo ""
fi
if [ "$hostsdenied" ]; then
  print_3title "/etc/hosts.denied file found, read the rules:"
  printf "$hostsdenied\n"
  cat " ${ROOT_FOLDER}etc/hosts.denied" 2>/dev/null | grep -v "#" | grep -Iv "^$" | sed -${E} "s,.*,${SED_GREEN},"
  echo ""
fi
if [ "$hostsallow" ]; then
  print_3title "/etc/hosts.allow file found, trying to read the rules:"
  printf "$hostsallow\n"
  cat " ${ROOT_FOLDER}etc/hosts.allow" 2>/dev/null | grep -v "#" | grep -Iv "^$" | sed -${E} "s,.*,${SED_RED},"
  echo ""
fi
if [ "$sshconfig" ]; then
  echo ""
  echo "Searching inside /etc/ssh/ssh_config for interesting info"
  grep -v "^#"  ${ROOT_FOLDER}etc/ssh/ssh_config 2>/dev/null | grep -Ev "\W+\#|^#" 2>/dev/null | grep -Iv "^$" | sed -${E} "s,Host|ForwardAgent|User|ProxyCommand,${SED_RED},"
fi
echo ""

peass{PAM Auth}

#-- SI) Passwords inside pam.d
pamdpass=$(grep -Ri "passwd"  ${ROOT_FOLDER}etc/pam.d/ 2>/dev/null | grep -v ":#")
if [ "$pamdpass" ] || [ "$DEBUG" ]; then
  print_2title "Passwords inside pam.d"
  grep -Ri "passwd"  ${ROOT_FOLDER}etc/pam.d/ 2>/dev/null | grep -v ":#" | sed "s,passwd,${SED_RED},"
  echo ""
fi

peass{NFS Exports}

#-- SI) Kerberos
kadmin_exists="$(command -v kadmin)"
klist_exists="$(command -v klist)"
if [ "$kadmin_exists" ] || [ "$klist_exists" ] || [ "$PSTORAGE_KERBEROS" ] || [ "$DEBUG" ]; then
  print_2title "Searching kerberos conf files and tickets"
  print_info "http://book.hacktricks.xyz/linux-hardening/privilege-escalation/linux-active-directory"

  if [ "$kadmin_exists" ]; then echo "kadmin was found on $kadmin_exists" | sed "s,$kadmin_exists,${SED_RED},"; fi
  if [ "$klist_exists" ] && [ -x "$klist_exists" ]; then echo "klist execution"; klist; fi
  ptrace_scope="$(cat /proc/sys/kernel/yama/ptrace_scope 2>/dev/null)"
  if [ "$ptrace_scope" ] && [ "$ptrace_scope" -eq 0 ]; then echo "ptrace protection is disabled (0), you might find tickets inside processes memory" | sed "s,is disabled,${SED_RED},g";
  else echo "ptrace protection is enabled ($ptrace_scope), you need to disable it to search for tickets inside processes memory" | sed "s,is enabled,${SED_GREEN},g";
  fi

  printf "%s\n" "$PSTORAGE_KERBEROS" | while read f; do
    if [ -r "$f" ]; then
      if echo "$f" | grep -q .k5login; then
        echo ".k5login file (users with access to the user who has this file in his home)"
        cat "$f" 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
      elif echo "$f" | grep -q keytab; then
        echo ""
        echo "keytab file found, you may be able to impersonate some kerberos principals and add users or modify passwords"
        klist -k "$f" 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
        printf "$(klist -k $f 2>/dev/null)\n" | awk '{print $2}' | while read l; do
          if [ "$l" ] && echo "$l" | grep -q "@"; then
            printf "$ITALIC  --- Impersonation command: ${NC}kadmin -k -t /etc/krb5.keytab -p \"$l\"\n" | sed -${E} "s,$l,${SED_RED},g"
            #kadmin -k -t /etc/krb5.keytab -p "$l" -q getprivs 2>/dev/null #This should show the permissions of each impersoanted user, the thing is that in a test it showed that every user had the same permissions (even if they didn't). So this test isn't valid
            #We could also try to create a new user or modify a password, but I'm not user if linpeas should do that
          fi
        done
      elif echo "$f" | grep -q krb5.conf; then
        ls -l "$f"
        cat "$f" 2>/dev/null | sed -${E} "s,default_ccache_name,${SED_RED},";
      elif echo "$f" | grep -q kadm5.acl; then
        ls -l "$f" 
        cat "$f" 2>/dev/null
      elif echo "$f" | grep -q sssd.conf; then
        ls -l "$f"
        cat "$f" 2>/dev/null | sed -${E} "s,cache_credentials ?= ?[tT][rR][uU][eE],${SED_RED},";
      elif echo "$f" | grep -q secrets.ldb; then
        echo "You could use SSSDKCMExtractor to extract the tickets stored here" | sed -${E} "s,SSSDKCMExtractor,${SED_RED},";
        ls -l "$f"
      elif echo "$f" | grep -q .secrets.mkey; then
        echo "This is the secrets file to use with SSSDKCMExtractor" | sed -${E} "s,SSSDKCMExtractor,${SED_RED},";
        ls -l "$f"
      fi
    fi
  done
  ls -l "/tmp/krb5cc*" "/var/lib/sss/db/ccache_*" "/etc/opt/quest/vas/host.keytab" 2>/dev/null || echo_not_found "tickets kerberos"
  klist 2>/dev/null || echo_not_found "klist"
  echo ""

fi

peass{Knockd}

peass{Kibana}

peass{Elasticsearch}

##-- SI) Logstash
if [ "$PSTORAGE_LOGSTASH" ] || [ "$DEBUG" ]; then
  print_2title "Searching logstash files"
  printf "$PSTORAGE_LOGSTASH"
  printf "%s\n" "$PSTORAGE_LOGSTASH" | while read d; do
    if [ -r "$d/startup.options" ]; then
      echo "Logstash is running as user:"
      cat "$d/startup.options" 2>/dev/null | grep "LS_USER\|LS_GROUP" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed -${E} "s,$USER,${SED_LIGHT_MAGENTA}," | sed -${E} "s,root,${SED_RED},"
    fi
    cat "$d/conf.d/out*" | grep "exec\s*{\|command\s*=>" | sed -${E} "s,exec\W*\{|command\W*=>,${SED_RED},"
    cat "$d/conf.d/filt*" | grep "path\s*=>\|code\s*=>\|ruby\s*{" | sed -${E} "s,path\W*=>|code\W*=>|ruby\W*\{,${SED_RED},"
  done
fi
echo ""

#-- SI) Vault-ssh
if [ "$PSTORAGE_VAULT_SSH_HELPER" ] || [ "$DEBUG" ]; then
  print_2title "Searching Vault-ssh files"
  printf "$PSTORAGE_VAULT_SSH_HELPER\n"
  printf "%s\n" "$PSTORAGE_VAULT_SSH_HELPER" | while read f; do cat "$f" 2>/dev/null; vault-ssh-helper -verify-only -config "$f" 2>/dev/null; done
  echo ""
  vault secrets list 2>/dev/null
  printf "%s\n" "$PSTORAGE_VAULT_SSH_TOKEN" | sed -${E} "s,.*,${SED_RED}," 2>/dev/null
fi
echo ""

#-- SI) Cached AD Hashes
adhashes=$(ls "/var/lib/samba/private/secrets.tdb" "/var/lib/samba/passdb.tdb" "/var/opt/quest/vas/authcache/vas_auth.vdb" "/var/lib/sss/db/cache_*" 2>/dev/null)
if [ "$adhashes" ] || [ "$DEBUG" ]; then
  print_2title "Searching AD cached hashes"
  ls -l "/var/lib/samba/private/secrets.tdb" "/var/lib/samba/passdb.tdb" "/var/opt/quest/vas/authcache/vas_auth.vdb" "/var/lib/sss/db/cache_*" 2>/dev/null
  echo ""
fi

#-- SI) Screen sessions
if ([ "$screensess" ] || [ "$screensess2" ] || [ "$DEBUG" ]) && ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Searching screen sessions"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#open-shell-sessions"
  screensess=$(screen -ls 2>/dev/null)
  screensess2=$(find /run/screen -type d -path "/run/screen/S-*" 2>/dev/null)
  
  screen -v
  printf "$screensess\n$screensess2" | sed -${E} "s,.*,${SED_RED}," | sed -${E} "s,No Sockets found.*,${C}[32m&${C}[0m,"
  
  find /run/screen -type s -path "/run/screen/S-*" -not -user $USER '(' '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null | while read f; do
    echo "Other user screen socket is writable: $f" | sed "s,$f,${SED_RED_YELLOW},"
  done
  echo ""
fi

#-- SI) Tmux sessions
tmuxdefsess=$(tmux ls 2>/dev/null)
tmuxnondefsess=$(ps auxwww | grep "tmux " | grep -v grep)
tmuxsess2=$(find /tmp -type d -path "/tmp/tmux-*" 2>/dev/null)
if ([ "$tmuxdefsess" ] || [ "$tmuxnondefsess" ] || [ "$tmuxsess2" ] || [ "$DEBUG" ]) && ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Searching tmux sessions"$N
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#open-shell-sessions"
  tmux -V
  printf "$tmuxdefsess\n$tmuxnondefsess\n$tmuxsess2" | sed -${E} "s,.*,${SED_RED}," | sed -${E} "s,no server running on.*,${C}[32m&${C}[0m,"

  find /tmp -type s -path "/tmp/tmux*" -not -user $USER '(' '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null | while read f; do
    echo "Other user tmux socket is writable: $f" | sed "s,$f,${SED_RED_YELLOW},"
  done
  echo ""
fi

peass{CouchDB}

peass{Redis}

#-- SI) Dovecot
# Needs testing
dovecotpass=$(grep -r "PLAIN" /etc/dovecot 2>/dev/null)
if [ "$dovecotpass" ] || [ "$DEBUG" ]; then
  print_2title "Searching dovecot files"
  if [ -z "$dovecotpass" ]; then
    echo_not_found "dovecot credentials"
  else
    printf "%s\n" "$dovecotpass" | while read d; do
      df=$(echo $d |cut -d ':' -f1)
      dp=$(echo $d |cut -d ':' -f2-)
      echo "Found possible PLAIN text creds in $df"
      echo "$dp" | sed -${E} "s,.*,${SED_RED}," 2>/dev/null
    done
  fi
  echo ""
fi

peass{Mosquitto}

peass{Neo4j}

AWSVAULT="$(command -v aws-vault 2>/dev/null)"
if [ "$AWSVAULT" ] || [ "$DEBUG" ]; then
  print_2title "Check aws-vault"
  aws-vault list
fi

peass{Cloud Credentials}

peass{Cloud Init}

peass{CloudFlare}

peass{Erlang}

peass{GMV Auth}

peass{IPSec}

peass{IRSSI}

peass{Keyring}

peass{Filezilla}

peass{Backup Manager}

##-- SI) passwd files (splunk)
SPLUNK_BIN="$(command -v splunk 2>/dev/null)"
if [ "$PSTORAGE_SPLUNK" ] || [ "$SPLUNK_BIN" ] || [ "$DEBUG" ]; then
  print_2title "Searching uncommon passwd files (splunk)"
  if [ "$SPLUNK_BIN" ]; then echo "splunk binary was found installed on $SPLUNK_BIN" | sed "s,.*,${SED_RED},"; fi
  printf "%s\n" "$PSTORAGE_SPLUNK" | sort | uniq | while read f; do
    if [ -f "$f" ] && ! [ -x "$f" ]; then
      echo "passwd file: $f" | sed "s,$f,${SED_RED},"
      cat "$f" 2>/dev/null | grep "'pass'|'password'|'user'|'database'|'host'|\$" | sed -${E} "s,password|pass|user|database|host|\$,${SED_RED},"
    fi
  done
  echo ""
fi

if [ "$PSTORAGE_KCPASSWORD" ] || [ "$DEBUG" ]; then
  print_2title "Analyzing kcpassword files"
  print_info "https://book.hacktricks.xyz/macos/macos-security-and-privilege-escalation#kcpassword"
  printf "%s\n" "$PSTORAGE_KCPASSWORD" | while read f; do
    echo "$f" | sed -${E} "s,.*,${SED_RED},"
    base64 "$f" 2>/dev/null | sed -${E} "s,.*,${SED_RED},"
  done
  echo ""
fi

##-- SI) Gitlab
if [ "$(command -v gitlab-rails)" ] || [ "$(command -v gitlab-backup)" ] || [ "$PSTORAGE_GITLAB" ] || [ "$DEBUG" ]; then
  print_2title "Searching GitLab related files"
  #Check gitlab-rails
  if [ "$(command -v gitlab-rails)" ]; then
    echo "gitlab-rails was found. Trying to dump users..."
    gitlab-rails runner 'User.where.not(username: "peasssssssss").each { |u| pp u.attributes }' | sed -${E} "s,email|password,${SED_RED},"
    echo "If you have enough privileges, you can make an account under your control administrator by running: gitlab-rails runner 'user = User.find_by(email: \"youruser@example.com\"); user.admin = TRUE; user.save!'"
    echo "Alternatively, you could change the password of any user by running: gitlab-rails runner 'user = User.find_by(email: \"admin@example.com\"); user.password = \"pass_peass_pass\"; user.password_confirmation = \"pass_peass_pass\"; user.save!'"
    echo ""
  fi
  if [ "$(command -v gitlab-backup)" ]; then
    echo "If you have enough privileges, you can create a backup of all the repositories inside gitlab using 'gitlab-backup create'"
    echo "Then you can get the plain-text with something like 'git clone \@hashed/19/23/14348274[...]38749234.bundle'"
    echo ""
  fi
  #Check gitlab files
  printf "%s\n" "$PSTORAGE_GITLAB" | sort | uniq | while read f; do
    if echo $f | grep -q secrets.yml; then
      echo "Found $f" | sed "s,$f,${SED_RED},"
      cat "$f" 2>/dev/null | grep -Iv "^$" | grep -v "^#"
    elif echo $f | grep -q gitlab.yml; then
      echo "Found $f" | sed "s,$f,${SED_RED},"
      cat "$f" | grep -A 4 "repositories:"
    elif echo $f | grep -q gitlab.rb; then
      echo "Found $f" | sed "s,$f,${SED_RED},"
      cat "$f" | grep -Iv "^$" | grep -v "^#" | sed -${E} "s,email|user|password,${SED_RED},"
    fi
    echo ""
  done
  echo ""
fi

peass{Github}

peass{Svn}

peass{PGP-GPG}

peass{Cache Vi}

peass{Wget}

##-- SI) containerd installed
if ! [ "$SEARCH_IN_FOLDER" ]; then
  containerd=$(command -v ctr)
  if [ "$containerd" ] || [ "$DEBUG" ]; then
    print_2title "Checking if containerd(ctr) is available"
    print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation/containerd-ctr-privilege-escalation"
    if [ "$containerd" ]; then
      echo "ctr was found in $containerd, you may be able to escalate privileges with it" | sed -${E} "s,.*,${SED_RED},"
      ctr image list 2>&1
    fi
    echo ""
  fi
fi

##-- SI) runc installed
if ! [ "$SEARCH_IN_FOLDER" ]; then
  runc=$(command -v runc)
  if [ "$runc" ] || [ "$DEBUG" ]; then
    print_2title "Checking if runc is available"
    print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation/runc-privilege-escalation"
    if [ "$runc" ]; then
      echo "runc was found in $runc, you may be able to escalate privileges with it" | sed -${E} "s,.*,${SED_RED},"
    fi
    echo ""
  fi
fi

#-- SI) Docker
if [ "$PSTORAGE_DOCKER" ] || [ "$DEBUG" ]; then
  print_2title "Searching docker files (limit 70)"
  print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation/docker-breakout/docker-breakout-privilege-escalation"
  printf "%s\n" "$PSTORAGE_DOCKER" | head -n 70 | while read f; do
    ls -l "$f" 2>/dev/null
    if ! [ "$IAMROOT" ] && [ -S "$f" ] && [ -w "$f" ]; then
      echo "Docker related socket ($f) is writable" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    fi
  done
  echo ""
fi

peass{Kubernetes}

peass{Firefox}

peass{Chrome}

peass{Autologin}

#-- SI) S/Key athentication
if (grep auth= /etc/login.conf 2>/dev/null | grep -v "^#" | grep -q skey) || [ "$DEBUG" ] ; then
  print_2title "S/Key authentication"
  printf "System supports$RED S/Key$NC authentication\n"
  if ! [ -d /etc/skey/ ]; then
    echo "${GREEN}S/Key authentication enabled, but has not been initialized"
  elif ! [ "$IAMROOT" ] && [ -w /etc/skey/ ]; then
    echo "${RED}/etc/skey/ is writable by you"
    ls -ld /etc/skey/
  else
    ls -ld /etc/skey/ 2>/dev/null
  fi
fi
echo ""

#-- SI) YubiKey athentication
if (grep "auth=" /etc/login.conf 2>/dev/null | grep -v "^#" | grep -q yubikey) || [ "$DEBUG" ]; then
  print_2title "YubiKey authentication"
  printf "System supports$RED YubiKey$NC authentication\n"
  if ! [ "$IAMROOT" ] && [ -w /var/db/yubikey/ ]; then
    echo "${RED}/var/db/yubikey/ is writable by you"
    ls -ld /var/db/yubikey/
  else
    ls -ld /var/db/yubikey/ 2>/dev/null
  fi
  echo ""
fi

peass{SNMP}

peass{Pypirc}

peass{Postfix}

peass{Ldaprc}

peass{Env}

peass{Msmtprc}

peass{Keepass}

peass{FTP}

peass{EXTRA_SECTIONS}

peass{Interesting logs}

peass{Windows}

peass{Other Interesting}

if ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && [ "$TIMEOUT" ]; then
  print_2title "Checking leaks in git repositories"
  printf "%s\n" "$PSTORAGE_GITHUB" | while read f; do
    if echo "$f" | grep -Eq ".git$"; then
      git_dirname=$(dirname "$f")
      if [ "$MACPEAS" ]; then
        execBin "GitLeaks (checking $git_dirname)" "https://github.com/zricethezav/gitleaks" "$FAT_LINPEAS_GITLEAKS_MACOS" "detect -s '$git_dirname' -v | grep -E 'Description|Match|Secret|Message|Date'"
      else
        execBin "GitLeaks (checking $git_dirname)" "https://github.com/zricethezav/gitleaks" "$FAT_LINPEAS_GITLEAKS_LINUX" "detect -s '$git_dirname' -v | grep -E 'Description|Match|Secret|Message|Date'"
      fi
    fi
  done
fi
