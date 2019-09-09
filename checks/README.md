# LinPEAS - Linux Privilege Escalation Awsome Script (with colors!!)

Also valid for **Unix systems**

## What does linpeas look for
- **System Information**
  - [x] SO & kernel version 
  - [x] Sudo version
  - [x] PATH
  - [x] Date
  - [x] System stats
  - [x] Environment vars
  - [x] SElinux
  - [x] Printers
  - [x] Dmesg (signature verifications)
  - [x] Container?

- **Devices**
  - [x] sd* in /dev
  - [x] Unmounted filesystems

- **Available Software**
  - [x] Useful software
  - [x] Installed compilers

- **Processes & Cron & Services**
  - [x] Cleaned processes
  - [x] Binary processes permissions
  - [x] Different processes executed during 1 min
  - [x] Cron jobs
  - [x] Services

- **Network Information**
  - [x] Hostname, hosts & dns
  - [x] Content of /etc/inetd.conf
  - [x] Networks and neighbours
  - [x] Iptables rules
  - [x] Active ports
  - [x] Sniff permissions (tcpdump)

- **Users Information**
  - [x] Info about current user
  - [x] PGP keys
  - [x] `sudo -l` without password
  - [x] doas config file
  - [x] Pkexec policy
  - [x] Try to login using `su` as other users (using null pass and the username)
  - [x] List of superusers
  - [x] List of users with console
  - [x] Login info
  - [x] List of all users

- **Software Information**
  - [x] MySQl (Version, user being configured, loging as "root:root","root:toor","root:", user hashes extraction via DB and file, possible backup user configured)
  - [x] PostgreSQL (Version, try login in "template0" and "template1" as: "postgres:", "psql:")
  - [x] Apache (Version)
  - [x] PHP cookies
  - [x] Wordpress (Database credentials)
  - [x] Tomcat (Credentials)
  - [x] Mongo (Version)
  - [x] Supervisor (Credentials)
  - [x] Cesi (Credentials)
  - [x] Rsyncd (Credentials)
  - [x] Hostapd (Credentials)
  - [x] Wifi (Credentials)
  - [x] Anaconda-ks (Credentials)
  - [x] VNC (Credentials)
  - [x] LDAP database (Credentials)
  - [x] Open VPN files (Credentials)
  - [x] SSH (private keys, known_hosts, authorized_hosts, authorized_keys, main config parameters in sshd_config, certificates)
  - [X] PAM-SSH (Unexpected "auth" values)
  - [x] AWS (Files with AWS keys)
  - [x] NFS (privilege escalation misconfiguration)
  - [x] Kerberos (configuration & tickets in /tmp)
  - [x] Kibana (credentials)
  - [x] Logstash (Username and possible code execution)
  - [x] Elasticseach (Config info and Version via port 9200)
  - [x] Vault-ssh (Config values, secrets list and .vault-token files)


- **Generic Interesting Files**
  - [x] SUID & SGID files
  - [x] Capabilities
  - [x] .sh scripts in PATH
  - [x] Hashes (passwd, shadow & master.passwd)
  - [x] Try to read root dir
  - [x] Files owned by root inside /home
  - [x] List of readable files belonging to root and not world readable
  - [x] Root files inside a folder owned by the current user
  - [x] Reduced list of files inside my home and /home
  - [x] Mails
  - [x] Backup files
  - [x] DB files
  - [x] Web files
  - [x] Files that can contain passwords (and search for passwords inside *_history files)
  - [x] List of all hidden files
  - [x] List ALL writable files for current user (global, user and groups)
  - [x] Inside /tmp, /var/tmp and /var/backups
  - [x] Password ins config PHP files
  - [x] Get IPs, passwords and emails from logs
  - [x] "pwd" and "passw" inside files (and get most probable lines)