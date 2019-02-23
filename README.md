# LinPE - Linux Privilege Escalation (one-linner)

The goal of this script is to enumerate fast a linux machine.

This script was designed so you only have to copy and paste the code inside the **sh** or **bash** console and all the output will be redirected to a file (by default /tmp/fels, but feel free to change it).

There is only one exception, the last command executed is *sudo -l* so this commands needs you to write the password of your user (if you know it).


## Checks
- **System Information**
- [x] SO, kernel version & sudo version
- [x] Date, time, selinux & env
- [x] Useful software installed
- [x] Capabilities
- [x] Processes (top, current, executed within a minute, binary permissions)
- [x] Scheduled tasks
- [x] sd* disk in /dev, storage info, ummounted file-sys, printers


- **Network Information**
- [x] Hostname, hosts & dns 
- [x] Intefaces, networks and neightbours
- [x] Active ports and files used
- [x] Sniff permissions


- **Users Information**
- [x] Info about me (whoami, groups, sudo, PGPkeys)
- [x] List of superusers
- [x] Login info
- [x] Available users with console
- [x] List of all users


- **Interesting Files**
- [x] SUID & SGID files
- [x] Reduced list of files inside home
- [x] Files inside any .ssh directory
- [x] Mails
- [x] NFS exports
- [x] Hashes (passwd, shadow & master.passwd)
- [x] Try to read root dir
- [x] Check if Docker or LXC container
- [x] List ALL writable file for current users (global, user and groups)
- [x] *_history, profile, bashrc files
- [x] List of all hidden files
- [x] Inside /tmp, /var/tmp and /var/backups
- [x] Web files
- [x] Possible backup files
- [x] IPs inside logs
- [x] "password" and "passw" inside files


- [x] Sudo -l (so you can introduce your password if known)

## One liner

Here you have the script in one line, **just copy and paste it**;)

The defult file where all the data is recorded is: */tmp/linPE* (you can change it at the beginning of teh script)

``file="/tmp/linPE";RED='\033[0;31m';Y='\033[0;33m';B='\033[0;34m';NC='\033[0m';rm -rf $file;echo "File: $file";echo "[+]Gathering system information...";printf $B"[*] "$RED"BASIC SYSTEM INFO\n"$NC >> $file ;echo "" >> $file;printf $Y"[+] "$RED"Operative system\n"$NC >> $file;(cat /proc/version || uname -a ) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"PATH\n"$NC >> $file;echo $PATH 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Date\n"$NC >> $file;date 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Sudo version\n"$NC >> $file;sudo -V 2>/dev/null| grep "Sudo ver" >> $file;echo "" >> $file;printf $Y"[+] "$RED"selinux enabled?\n"$NC >> $file;sestatus 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Useful software?\n"$NC >> $file;which nc ncat netcat wget curl ping gcc make gdb base64 socat python python2 python3 python2.7 python2.6 python3.6 python3.7 perl php ruby xterm 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Capabilities\n"$NC >> $file;getcap -r / 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Environment\n"$NC >> $file;(set || env) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Top and cleaned proccesses\n"$NC >> $file;top -n 1 2>/dev/null | head -n 13 >> $file;ps aux 2>/dev/null | grep -v "\[" >> $file;echo "" >> $file;printf $Y"[+] "$RED"Binary processes permissions\n"$NC >> $file;ps aux 2>/dev/null | awk '{print $11}'|xargs -r ls -la 2>/dev/null |awk '!x[$0]++' 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Different processes executed during 1 min (HTB)\n"$NC >> $file;if [ "`ps -e --format cmd`" ]; then for i in {1..121}; do ps -e --format cmd >> $file.tmp1; sleep 0.5; done; sort $file.tmp1 | uniq | grep -v "\[" | sed '/^.\{500\}./d' >> $file; rm $file.tmp1; fi;echo "" >> $file;printf $Y"[+] "$RED"Proccesses binary permissions\n"$NC >> $file;ps aux 2>/dev/null | awk '{print $11}'|xargs -r ls -la 2>/dev/null |awk '!x[$0]++' 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Scheduled tasks\n"$NC >> $file;crontab -l 2>/dev/null >> $file;ls -al /etc/cron* 2>/dev/null >> $file;cat /etc/cron* /etc/at* /etc/anacrontab /var/spool/cron/crontabs/root /var/spool/anacron 2>/dev/null | grep -v "^#" >> $file;echo "" >> $file;printf $Y"[+] "$RED"Any sd* disk in /dev?\n"$NC >> $file;ls /dev 2>/dev/null | grep -i "sd" >> $file;echo "" >> $file;printf $Y"[+] "$RED"Storage information\n"$NC >> $file;df -h 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Unmounted file-system?\n"$NC >> $file;cat /etc/fstab 2>/dev/null | grep -v "^#" >> $file;echo "" >> $file;printf $Y"[+] "$RED"Printer?\n"$NC >> $file;lpstat -a 2>/dev/null >> $file;echo "" >> $file;echo "" >> $file;echo "[+]Gathering network information...";printf $B"[*] "$RED"NETWORK INFO\n"$NC >> $file ;echo "" >> $file;printf $Y"[+] "$RED"Hostname, hosts and DNS\n"$NC >> $file;cat /etc/hostname /etc/hosts /etc/resolv.conf 2>/dev/null | grep -v "^#" >> $file;dnsdomainname 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Networks and neightbours\n"$NC >> $file;cat /etc/networks 2>/dev/null >> $file;(ifconfig || ip a) 2>/dev/null >> $file;iptables -L 2>/dev/null >> $file;(arp -e || arp -a || ip n) 2>/dev/null >> $file;route 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Ports\n"$NC >> $file;(netstat -punta || ss -t; ss -u) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Files in use by network services\n"$NC >> $file;lsof -i 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Can I sniff with tcpdump?\n"$NC >> $file;timeout 1 tcpdump >> $file 2>&1;echo "" >> $file;echo "" >> $file;echo "[+]Gathering users information...";printf $B"[*] "$RED"USERS INFO\n"$NC >> $file ;echo "" >> $file;printf $Y"[+] "$RED"Me\n"$NC >> $file;(id || (whoami && groups)) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Sudo -l without password\n"$NC >> $file;echo '' | sudo -S -l -k 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Do I have PGP keys?\n"$NC >> $file;gpg --list-keys 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Superusers\n"$NC >> $file;awk -F: '($3 == "0") {print}' /etc/passwd 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Login\n"$NC >> $file;w 2>/dev/null >> $file;last 2>/dev/null | tail >> $file;echo "" >> $file;printf $Y"[+] "$RED"Users with console\n"$NC >> $file;cat /etc/passwd 2>/dev/null | grep "sh$" >> $file;echo "" >> $file;printf $Y"[+] "$RED"All users\n"$NC >> $file;cat /etc/passwd 2>/dev/null | cut -d: -f1 >> $file;echo "" >> $file;echo "" >> $file;echo "[+]Gathering files information...";printf $B"[*] "$RED"INTERESTING FILES\n"$NC >> $file ;echo "" >> $file;printf $Y"[+] "$RED"SUID\n"$NC >> $file;find / -perm -4000 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"SGID\n"$NC >> $file;find / -perm -g=s -type f 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Files inside \$HOME (limit 20)\n"$NC >> $file;ls -la $HOME 2>/dev/null | head -n 20 >> $file;echo "" >> $file;printf $Y"[+] "$RED"20 First files of /home\n"$NC >> $file;find /home -type f 2>/dev/null | column -t | grep -v -i "/"$USER | head -n 20 >> $file;echo "" >> $file;printf $Y"[+] "$RED"Files inside .ssh directory?\n"$NC >> $file;find  /home /root -name .ssh 2>/dev/null -exec ls -laR {} \; >> $file;echo "" >> $file;printf $Y"[+] "$RED"*sa_key* files\n"$NC >> $file;find / -type f -name "*sa_key*" -ls 2>/dev/null -exec ls -l {} \; >> $file;echo "" >> $file;printf $Y"[+] "$RED"Mails?\n"$NC >> $file;ls -alh /var/mail/ /var/spool/mail/ 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"NFS exports?\n"$NC >> $file;cat /etc/exports 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Hashes inside /etc/passwd? Readable /etc/shadow or /etc/master.passwd?\n"$NC >> $file;grep -v '^[^:]*:[x]' /etc/passwd 2>/dev/null >> $file;cat /etc/shadow /etc/master.passwd 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Readable /root?\n"$NC >> $file;ls -ahl /root/ 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Inside docker or lxc?\n"$NC >> $file;dockercontainer=`grep -i docker /proc/self/cgroup  2>/dev/null; find / -name "*dockerenv*" -exec ls -la {} \; 2>/dev/null`;lxccontainer=`grep -qa container=lxc /proc/1/environ 2>/dev/null`;if [ "$dockercontainer" ]; then echo "Looks like we're in a Docker container" >> $file; fi;if [ "$lxccontainer" ]; then echo "Looks like we're in a LXC container" >> $file; fi;echo "" >> $file;printf $Y"[+] "$RED"*_history, profile, bashrc\n"$NC >> $file;find / -type f \( -name "*_history" -o -name "profile" -o -name "*bashrc" \) -exec ls -l {} \; 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"All hidden files (not in /sys/)\n"$NC >> $file;find / -type f -iname ".*" -ls 2>/dev/null | grep -v "/sys/" >> $file;echo "" >> $file;printf $Y"[+] "$RED"What inside /tmp, /var/tmp, /var/backups\n"$NC >> $file;ls -a /tmp /var/tmp /var/backups 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Writable Files (not in \$HOME or /proc)\n"$NC >> $file;USER=`whoami`;HOME=/home/$USER;find / '(' -type f -or -type d ')' '(' '(' -user $USER ')' -or '(' -perm -o=w ')' ')' 2>/dev/null | grep -v '/proc/' | grep -v $HOME | sort | uniq >> $file;for g in `groups`; do find \( -type f -or -type d \) -group $g -perm -g=w 2>/dev/null | grep -v '/proc/' | grep -v $HOME >> $file; done;echo "" >> $file;printf $Y"[+] "$RED"Web files?\n"$NC >> $file;ls -alhR /var/www/ 2>/dev/null | head >> $file;ls -alhR /srv/www/htdocs/ 2>/dev/null | head >> $file;ls -alhR /usr/local/www/apache22/data/ 2>/dev/null | head >> $file;ls -alhR /opt/lampp/htdocs/ 2>/dev/null | head >> $file;echo "" >> $file;printf $Y"[+] "$RED"Backup files?\n"$NC >> $file;find /var /etc /bin /sbin /home /usr/local/bin /usr/local/sbin /usr/bin /usr/games /usr/sbin /root /tmp -type f \( -name "*back*" -o -name "*bck*" \) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Find IPs inside logs\n"$NC >> $file;grep -a -R -o '[0-9]\{1,3\}\.[0-9]\{1,3\}\.[0-9]\{1,3\}\.[0-9]\{1,3\}' /var/log/ 2>/dev/null | sort | uniq >> $file;echo "" >> $file;printf $Y"[+] "$RED"Find 'password' or 'passw' string inside /home, /var/www, /var/log, /etc\n"$NC >> $file;grep -lRi "password\|passw" /home /var/www /var/log 2>/dev/null | sort | uniq >> $file;echo "" >> $file;printf $Y"[+] "$RED"Sudo -l (you need to puts the password and the result appear in console)\n"$NC >> $file;sudo -l;``
