#!/bin/sh

file="/tmp/linPE"
RED='\033[0;31m'
Y='\033[0;33m'
B='\033[0;34m'
NC='\033[0m'
C=$(printf '\033')

suidG="/bin/fusermount\|\
/bin/mount\|\
/bin/ntfs-3g\|\
/bin/ping\|\
/bin/ping6\|\
/bin/rcp\|\
/bin/su\|\
/bin/systemctl\|\
/bin/umount\|\
/sbin/mksnap_ffs\|\
/sbin/mount.cifs\|\
/sbin/ping\|\
/sbin/ping6\|\
/sbin/poweroff\|\
/sbin/shutdown\|\
/usr/bin/at\|\
/usr/bin/atq\|\
/usr/bin/atrm\|\
/usr/bin/batch\|\
/usr/bin/bwrap\|\
/usr/bin/chage\|\
/usr/bin/chfn\|\
/usr/bin/chpass\|\
/usr/bin/chsh\|\
/usr/bin/crontab\|\
/usr/bin/doas\|\
/usr/bin/fusermount\|\
/usr/bin/gpasswd\|\
/usr/bin/gpio\|\
/usr/bin/kismet_capture\|\
/usr/bin/lock\|\
/usr/bin/login\|\
/usr/bin/lpq\|\
/usr/bin/lpr\|\
/usr/bin/lprm\|\
/usr/bin/mount\|\
/usr/bin/newgidmap\|\
/usr/bin/newgrp\|\
/usr/bin/newuidmap\|\
/usr/bin/ntfs-3g\|\
/usr/bin/opieinfo\|\
/usr/bin/opiepasswd\|\
/usr/bin/passwd\|\
/usr/bin/pkexec\|\
/usr/bin/quota\|\
/usr/bin/rlogin\|\
/usr/bin/rsh\|\
/usr/bin/staprun\|\
/usr/bin/su\|\
/usr/bin/sudo\|\
/usr/bin/traceroute6.iputils\|\
/usr/bin/umount\|\
/usr/bin/vmware-user-suid-wrapper\|\
/usr/bin/vncserver-x11\|\
/usr/bin/Xvnc\|\
/usr/lib/chromium/chrome-sandbox\|\
/usr/lib/dbus-1.0/dbus-daemon-launch-helper\|\
/usr/lib/eject/dmcrypt-get-device\|\
/usr/libexec/abrt-action-install-debuginfo-to-abrt-cache\|\
/usr/libexec/auth/login_chpass\|\
/usr/libexec/auth/login_lchpass\|\
/usr/libexec/auth/login_passwd\|\
/usr/libexec/dbus-1/dbus-daemon-launch-helper\|\
/usr/libexec/dma-mbox-create\|\
/usr/libexec/lockspool\|\
/usr/libexec/ssh-keysign\|\
/usr/libexec/ulog-helper\|\
/usr/lib/chromium-browser/chrome-sandbox\|\
/usr/lib/i386-linux-gnu/lxc/lxc-user-nic\|\
/usr/lib/openssh/ssh-keysign\|\
/usr/lib/policykit-1/polkit-agent-helper-1\|\
/usr/lib/polkit-1/polkit-agent-helper-1\|\
/usr/lib/snapd/snap-confine\|\
/usr/lib/xorg/Xorg.wrap\|\
/usr/local/bin/Xorg\|\
/usr/local/libexec/dbus-daemon-launch-helper\|\
/usr/sbin/authpf\|\
/usr/sbin/authpf-noip\|\
/usr/sbin/exim4\|\
/usr/sbin/mount.nfs\|\
/usr/sbin/pam_timestamp_check\|\
/usr/sbin/ppp\|\
/usr/sbin/pppd\|\
/usr/sbin/timedc\|\
/usr/sbin/traceroute\|\
/usr/sbin/traceroute6\|\
/usr/sbin/unix_chkpwd\|\
/usr/sbin/userhelper\|\
/usr/sbin/usernetctl\|\
/usr/X11R6/bin/Xorg\|\
/usr/kerberos/bin/ksu\|\
/usr/libexec/openssh/ssh-keysign\|\
/usr/lib/squid/ncsa_auth\|\
/usr/lib/squid/pam_auth\|\
/usr/lib/vmware-tools/bin32/vmware-user-suid-wrapper\|\
/usr/lib/vmware-tools/bin64/vmware-user-suid-wrapper\|\
/usr/lib/news/bin/startinnfeed\|\
/usr/lib/news/bin/rnews\|\
/usr/lib/news/bin/inndstart\|\
/usr/bin/rsh\|\
/usr/bin/chsh\|\
/media/.hal-mtab-lock\|\
/sbin/mount.nfs4\|\
/sbin/pam_timestamp_check\|\
/sbin/unix_chkpwd\|\
/sbin/umount.nfs4\|\
/sbin/mount.nfs\|\
/sbin/umount.nfs"

suidB='nmap$\|perl$\|awk$\|find$\|bash$\|sh$\|man$\|more$\|less$\|vi$\|emacs$\|vim$\|nc$\|netcat$\|python$\|ruby$\|lua$\|irb$\|tar$\|zip$\|gdb$\|pico$\|scp$\|git$\|rvim$\|script$\|ash$\|csh$\|curl$\|dash$\|ed$\|env$\|expect$\|ftp$\|sftp$\|node$\|php$\|rpm$\|rpmquery$\|socat$\|strace$\|taskset$\|tclsh$\|telnet$\|tftp$\|wget$\|wish$\|zsh$\|ssh$\|ip$\|arp$\|mtr$'

sgid="/sbin/pam_extrausers_chkpwd\|\
/sbin/unix_chkpwd\|\
/usr/bin/at\|\
/usr/bin/atq\|\
/usr/bin/atrm\|\
/usr/bin/batch\|\
/usr/bin/bsd-write\|\
/usr/bin/btsockstat\|\
/usr/bin/chage\|\
/usr/bin/crontab\|\
/usr/bin/dotlockfile\|\
/usr/bin/dotlock.mailutils\|\
/usr/bin/expiry\|\
/usr/bin/lock\|\
/usr/bin/lpq\|\
/usr/bin/lpr\|\
/usr/bin/lprm\|\
/usr/bin/mlocate\|\
/usr/bin/mutt_dotlock\|\
/usr/bin/netstat\|\
/usr/bin/screen\|\
/usr/bin/skeyaudit\|\
/usr/bin/skeyinfo\|\
/usr/bin/skeyinit\|\
/usr/bin/ssh-agent\|\
/usr/bin/wall\|\
/usr/bin/write\|\
/usr/lib/emacs/24.5/i686-linux-gnu/movemail\|\
/usr/lib/evolution/camel-lock-helper-1.2\|\
/usr/libexec/auth/login_activ\|\
/usr/libexec/auth/login_crypto\|\
/usr/libexec/auth/login_radius\|\
/usr/libexec/auth/login_skey\|\
/usr/libexec/auth/login_snk\|\
/usr/libexec/auth/login_token\|\
/usr/libexec/auth/login_yubikey\|\
/usr/libexec/dma\|\
/usr/libexec/sendmail/sendmail\|\
/usr/lib/i386-linux-gnu/utempter/utempter\|\
/usr/lib/libvte9/gnome-pty-helper\|\
/usr/lib/mc/cons.saver\|\
/usr/lib/snapd/snap-confine\|\
/usr/lib/x86_64-linux-gnu/utempter/utempter\|\
/usr/lib/xemacs-21.4.22/i686-linux-gnu/movemail\|\
/usr/lib/xorg/Xorg.wrap\|\
/usr/sbin/authpf\|\
/usr/sbin/authpf-noip\|\
/usr/sbin/lpc\|\
/usr/sbin/lpd\|\
/usr/sbin/smtpctl\|\
/usr/sbin/trpt\|\
/usr/sbin/unix_chkpwd\|\
/usr/X11R6/bin/xlock\|\
/usr/X11R6/bin/xterm"

intfol="/etc/\|/root/\|/home/\|/var/log\|/mnt/\|/usr/local/sbin\|/usr/sbin\|/sbin\|/usr/local/bin\|/usr/bin\|/bin\|/usr/local/games\|/usr/games\|/usr/lib"


if [ "$(/usr/bin/id -u)" -eq "0" ]; then printf $B"[*] "$RED"YOU ARE ALREADY ROOT!!! (nothing is going to be executed)\n"$NC; exit; fi

rm -rf $file
echo "File: $file"

echo "[+]Gathering system information..."
printf $B"[*] "$RED"BASIC SYSTEM INFO\n"$NC >> $file 
echo "" >> $file
printf $Y"[+] "$RED"Operative system\n"$NC >> $file
(cat /proc/version || uname -a ) 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"PATH\n"$NC >> $file
echo $PATH 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Date\n"$NC >> $file
date 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Sudo version\n"$NC >> $file
sudo -V 2>/dev/null| grep "Sudo ver" >> $file
echo "" >> $file

printf $Y"[+] "$RED"selinux enabled?\n"$NC >> $file
sestatus 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Useful software?\n"$NC >> $file
which nc ncat netcat wget curl ping gcc make gdb base64 socat python python2 python3 python2.7 python2.6 python3.6 python3.7 perl php ruby xterm doas sudo 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Capabilities\n"$NC >> $file
getcap -r / 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Environment\n"$NC >> $file
(set || env) 2>/dev/null | grep -v "suidG\|suidB\|sgid\|intfol" >> $file
echo "" >> $file

printf $Y"[+] "$RED"Cleaned proccesses\n"$NC >> $file
ps aux 2>/dev/null | grep -v "\[" | sed "s,root,${C}[31m&${C}[0m," >> $file
echo "" >> $file

printf $Y"[+] "$RED"Binary processes permissions\n"$NC >> $file
ps aux 2>/dev/null | awk '{print $11}'|xargs -r ls -la 2>/dev/null |awk '!x[$0]++' 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Services\n"$NC >> $file
(/usr/sbin/service --status-all || /sbin/chkconfig --list || /bin/rc-status) 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Different processes executed during 1 min (HTB)\n"$NC >> $file
if [ "`ps -e --format cmd`" ]; then for i in {1..121}; do ps -e --format cmd >> $file.tmp1; sleep 0.5; done; sort $file.tmp1 | uniq | grep -v "\[" | sed '/^.\{500\}./d' >> $file; rm $file.tmp1; fi
echo "" >> $file

printf $Y"[+] "$RED"Proccesses binary permissions\n"$NC >> $file
ps aux 2>/dev/null | awk '{print $11}'|xargs -r ls -la 2>/dev/null |awk '!x[$0]++' 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Scheduled tasks\n"$NC >> $file
crontab -l 2>/dev/null >> $file
ls -al /etc/cron* 2>/dev/null >> $file
cat /etc/cron* /etc/at* /etc/anacrontab /var/spool/cron/crontabs/root /var/spool/anacron 2>/dev/null | grep -v "^#" >> $file
echo "" >> $file

printf $Y"[+] "$RED"Any sd* disk in /dev?\n"$NC >> $file
ls /dev 2>/dev/null | grep -i "sd" >> $file
echo "" >> $file

printf $Y"[+] "$RED"Storage information\n"$NC >> $file
df -h 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Unmounted file-system?\n"$NC >> $file
cat /etc/fstab 2>/dev/null | grep -v "^#" >> $file
echo "" >> $file

printf $Y"[+] "$RED"Printer?\n"$NC >> $file
lpstat -a 2>/dev/null >> $file
echo "" >> $file

echo "" >> $file
echo "[+]Gathering network information..."
printf $B"[*] "$RED"NETWORK INFO\n"$NC >> $file 
echo "" >> $file
printf $Y"[+] "$RED"Hostname, hosts and DNS\n"$NC >> $file
cat /etc/hostname /etc/hosts /etc/resolv.conf 2>/dev/null | grep -v "^#" >> $file
dnsdomainname 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Networks and neightbours\n"$NC >> $file
cat /etc/networks 2>/dev/null >> $file
(ifconfig || ip a) 2>/dev/null >> $file
iptables -L 2>/dev/null >> $file
ip n 2>/dev/null >> $file
route -n 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Ports\n"$NC >> $file
(netstat -punta || ss -t; ss -u) 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Can I sniff with tcpdump?\n"$NC >> $file
timeout 1 tcpdump >> $file 2>&1
echo "" >> $file

echo "" >> $file
echo "[+]Gathering users information..."
printf $B"[*] "$RED"USERS INFO\n"$NC >> $file 
echo "" >> $file
printf $Y"[+] "$RED"Me\n"$NC >> $file
(id || (whoami && groups)) 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Sudo -l without password & /etc/sudoers\n"$NC >> $file
echo '' | sudo -S -l -k 2>/dev/null >> $file
cat /etc/sudoers 2>/dev/null >> $file #Add to one-liner  
echo "" >> $file

printf $Y"[+] "$RED"Do I have PGP keys?\n"$NC >> $file
gpg --list-keys 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Superusers\n"$NC >> $file
awk -F: '($3 == "0") {print}' /etc/passwd 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Login\n"$NC >> $file
w 2>/dev/null >> $file
last 2>/dev/null | tail >> $file
echo "" >> $file

printf $Y"[+] "$RED"Users with console\n"$NC >> $file
cat /etc/passwd 2>/dev/null | grep "sh$" >> $file
echo "" >> $file

printf $Y"[+] "$RED"All users\n"$NC >> $file
cat /etc/passwd 2>/dev/null | cut -d: -f1 >> $file
echo "" >> $file

echo "" >> $file
echo "[+]Gathering files information..."
printf $B"[*] "$RED"INTERESTING FILES\n"$NC >> $file 
echo "" >> $file
printf $Y"[+] "$RED"SUID\n"$NC >> $file
find / -perm -4000 2>/dev/null | sed "s,$suidG,${C}[32m&${C}[0m," | sed "s,$suidB,${C}[31m&${C}[0m," >> $file
echo "" >> $file

printf $Y"[+] "$RED"SGID\n"$NC >> $file
find / -perm -g=s -type f 2>/dev/null | sed "s,$sgid,${C}[32m&${C}[0m," >> $file
echo "" >> $file

printf $Y"[+] "$RED"Files inside \$HOME (limit 20)\n"$NC >> $file
ls -la $HOME 2>/dev/null | head -n 20 >> $file
echo "" >> $file

printf $Y"[+] "$RED"20 First files of /home\n"$NC >> $file
find /home -type f 2>/dev/null | column -t | grep -v -i "/"$USER | head -n 20 >> $file
echo "" >> $file

printf $Y"[+] "$RED"Files inside .ssh directory?\n"$NC >> $file
find  /home /root -name .ssh 2>/dev/null -exec ls -laR {} \; >> $file
echo "" >> $file

printf $Y"[+] "$RED"*sa_key* files\n"$NC >> $file
find / -type f -name "*sa_key*" -ls 2>/dev/null -exec ls -l {} \; >> $file
echo "" >> $file

printf $Y"[+] "$RED"Mails?\n"$NC >> $file
ls -alh /var/mail/ /var/spool/mail/ 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"NFS exports?\n"$NC >> $file
cat /etc/exports 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Hashes inside /etc/passwd? Readable /etc/shadow, /etc/master.passwd?, or /root?\n"$NC >> $file
grep -v '^[^:]*:[x]' /etc/passwd 2>/dev/null >> $file
cat /etc/shadow /etc/master.passwd 2>/dev/null >> $file
ls -ahl /root/ 2>/dev/null >> $file #Modify in one-liner  
echo "" >> $file

printf $Y"[+] "$RED"Inside docker or lxc?\n"$NC >> $file
dockercontainer=`grep -i docker /proc/self/cgroup  2>/dev/null; find / -name "*dockerenv*" -exec ls -la {} \; 2>/dev/null`
lxccontainer=`grep -qa container=lxc /proc/1/environ 2>/dev/null`
if [ "$dockercontainer" ]; then echo "Looks like we're in a Docker container" >> $file; fi
if [ "$lxccontainer" ]; then echo "Looks like we're in a LXC container" >> $file; fi
echo "" >> $file

printf $Y"[+] "$RED"*_history, profile, bashrc, httpd.conf\n"$NC >> $file
find / -type f \( -name "*_history" -o -name "profile" -o -name "*bashrc" -o -name "httpd.conf" \) -exec ls -l {} \; 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"All hidden files (not in /sys/) (limit 100)\n"$NC >> $file
find / -type f -iname ".*" -ls 2>/dev/null | grep -v "/sys/" | head -n 100 >> $file
echo "" >> $file

printf $Y"[+] "$RED"What inside /tmp, /var/tmp, /var/backups\n"$NC >> $file
ls -a /tmp /var/tmp /var/backups 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Interesting writable Files\n"$NC >> $file
USER=`whoami`
HOME=/home/$USER
find / '(' -type f -or -type d ')' '(' '(' -user $USER ')' -or '(' -perm -o=w ')' ')' 2>/dev/null | grep -v '/proc/' | grep -v $HOME | grep -v '/sys/fs'| sort | uniq | sed "s,$intfol,${C}[31m&${C}[0m," >> $file
for g in `groups`; do find / \( -type f -or -type d \) -group $g -perm -g=w 2>/dev/null | grep -v '/proc/' | grep -v $HOME | grep -v '/sys/fs' | sed "s,$intfol,${C}[31m&${C}[0m,"; done >> $file
echo "" >> $file

printf $Y"[+] "$RED"Web files?(output limited)\n"$NC >> $file
ls -alhR /var/www/ 2>/dev/null | head >> $file
ls -alhR /srv/www/htdocs/ 2>/dev/null | head >> $file
ls -alhR /usr/local/www/apache22/data/ 2>/dev/null | head >> $file
ls -alhR /opt/lampp/htdocs/ 2>/dev/null | head >> $file
echo "" >> $file

printf $Y"[+] "$RED"Backup files?\n"$NC >> $file
find /var /etc /bin /sbin /home /usr/local/bin /usr/local/sbin /usr/bin /usr/games /usr/sbin /root /tmp -type f \( -name "*back*" -o -name "*bck*" \) 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Find IPs & passwords inside logs\n"$NC >> $file
grep -a -R -o '[0-9]\{1,3\}\.[0-9]\{1,3\}\.[0-9]\{1,3\}\.[0-9]\{1,3\}' /var/log/ 2>/dev/null | sort | uniq >> $file
grep -a -R -i 'password' /var/log/ 2>/dev/null | sort | uniq >> $file #Add to one-liner 
echo "" >> $file

printf $Y"[+] "$RED"Find 'password' or 'passw' string inside /home, /var/www, /var/log, /etc\n"$NC >> $file
grep -lRi "password\|passw" /home /var/www /var/log 2>/dev/null | sort | uniq >> $file
echo "" >> $file

printf $Y"[+] "$RED"Sudo -l (you need to puts the password and the result appear in console)\n"$NC >> $file
sudo -l
