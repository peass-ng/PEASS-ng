#!/bin/sh

file="/tmp/linPE"
RED='\033[0;31m'
Y='\033[0;33m'
B='\033[0;34m'
NC='\033[0m'
C=$(printf '\033')

sidG="/accton$\|/allocate$\|/arping$\|/at$\|/atq$\|/atrm$\|/authpf$\|/authpf-noip$\|/batch$\|/bsd-write$\|/btsockstat$\|/bwrap$\|/cacaocsc$\|/camel-lock-helper-1.2$\|/ccreds_validate$\|/cdrw$\|/chage$\|/chfn$\|/chkey$\|/chkperm$\|/chpass$\|/chrome-sandbox$\|/chsh$\|/cons.saver$\|/crontab$\|/ct$\|/cu$\|/dbus-daemon-launch-helper$\|/deallocate$\|/dma$\|/dmcrypt-get-device$\|/doas$\|/dotlockfile$\|/dotlock.mailutils$\|/dtaction$\|/dtappgather$\|/dtfile$\|/dtprintinfo$\|/dtsession$\|/eject$\|/execabrt-action-install-debuginfo-to-abrt-cache$\|/execdbus-daemon-launch-helper$\|/execdma-mbox-create$\|/execlockspool$\|/execlogin_chpass$\|/execlogin_lchpass$\|/execlogin_passwd$\|/execssh-keysign$\|/execulog-helper$\|/exim4$\|/expiry$\|/fdformat$\|/fusermount$\|/gnome-pty-helper$\|/gnome-suspend$\|/gpasswd$\|/gpg$\|/gpio$\|/inndstart$\|/ksu$\|/list_devices$\|/lock$\|/lockdev$\|/lockfile$\|/login$\|/login_activ$\|/login_crypto$\|/login_radius$\|/login_skey$\|/login_snk$\|/login_token$\|/login_yubikey$\|/lpc$\|/lpd$\|/lpd-port$\|/lppasswd$\|/lpq$\|/lpr$\|/lprm$\|/lpset$\|/lxc-user-nic$\|/mahjongg$\|/mail-lock$\|/mailq$\|/mail-touchlock$\|/mail-unlock$\|/mksnap_ffs$\|/mlocate$\|/mount$\|/mount.cifs$\|/mount.nfs$\|/mount.nfs4$\|/movemail$\|/mtr$\|/mutt_dotlock$\|/ncsa_auth$\|/netpr$\|/netreport$\|/netstat$\|/newgidmap$\|/newgrp$\|/newtask$\|/newuidmap$\|/ntfs-3g$\|/opieinfo$\|/opiepasswd$\|/pam_auth$\|/pam_extrausers_chkpwd$\|/pam_timestamp_check$\|/pamverifier$\|/passwd$\|/pfexec$\|/ping$\|/ping6$\|/pkexec$\|/pmconfig$\|/polkit-agent-helper-1$\|/postdrop$\|/postqueue$\|/poweroff$\|/ppp$\|/pppd$\|/procmail$\|/pt_chmod$\|/pt_chown$\|/quota$\|/rcp$\|/rdist$\|/remote.unknown$\|/rlogin$\|/rmformat$\|/rnews$\|/rsh$\|/sacadm$\|/screen$\|/sdtcm_convert$\|/sendmail$\|/sendmail.sendmail$\|/shutdown$\|/skeyaudit$\|/skeyinfo$\|/skeyinit$\|/slocate$\|/smpatch$\|/smtpctl$\|/snap-confine$\|/sperl5.8.8$\|/ssh-agent$\|/ssh-keysign$\|/staprun$\|/startinnfeed$\|/stclient$\|/su$\|/sudo$\|/sudoedit$\|/suexec$\|/sys-suspend$\|/systemctl$\|/timedc$\|/tip$\|/traceroute$\|/traceroute6$\|/traceroute6.iputils$\|/trpt$\|/tsoldtlabel$\|/tsoljdslabel$\|/tsolxagent$\|/ufsdump$\|/ufsrestore$\|/umount$\|/umount.nfs$\|/umount.nfs4$\|/unix_chkpwd$\|/uptime$\|/userhelper$\|/usernetctl$\|/utempter$\|/utmp_update$\|/uucico$\|/uucp$\|/uuglist$\|/uuidd$\|/uuname$\|/uusched$\|/uustat$\|/uux$\|/uuxqt$\|/vmware-user-suid-wrapper$\|/vncserver-x11$\|/volrmmount$\|/w$\|/wall$\|/whodo$\|/write$\|/xlock$\|/Xorg$\|/Xorg.wrap$\|/xscreensaver$\|/Xsun$\|/xterm$\|/Xvnc$"
sidB='aria2c$\|arp$\|ash$\|awk$\|base64$\|bash$\|busybox$\|cat$\|chmod$\|chown$\|cp$\|csh$\|curl$\|cut$\|dash$\|date$\|dd$\|diff$\|dmsetup$\|docker$\|ed$\|emacs$\|env$\|expand$\|expect$\|file$\|find$\|flock$\|fmt$\|fold$\|gdb$\|gimp$\|git$\|grep$\|head$\|ionice$\|ip$\|jjs$\|jq$\|jrunscript$\|ksh$\|ld.so$\|less$\|logsave$\|lua$\|make$\|more$\|mv$\|mysql$\|nano$\|nc$\|nice$\|nl$\|nmap$\|node$\|od$\|openssl$\|perl$\|pg$\|php$\|pic$\|pico$\|python$\|readelf$\|rlwrap$\|rpm$\|rpmquery$\|rsync$\|rvim$\|scp$\|sed$\|setarch$\|shuf$\|socat$\|sort$\|sqlite3$\|stdbuf$\|strace$\|systemctl$\|tail$\|tar$\|taskset$\|tclsh$\|tee$\|telnet$\|tftp$\|time$\|timeout$\|ul$\|unexpand$\|uniq$\|unshare$\|vim$\|watch$\|wget$\|xargs$\|xxd$\|zip$\|zsh$'
sudoB="$(whoami)\|\*\|env_keep+=LD_PRELOAD\|apt-get$\|apt$\|aria2c$\|arp$\|ash$\|awk$\|base64$\|bash$\|busybox$\|cat$\|chmod$\|chown$\|cp$\|cpan$\|cpulimit$\|crontab$\|csh$\|curl$\|cut$\|dash$\|date$\|dd$\|diff$\|dmesg$\|dmsetup$\|dnf$\|docker$\|dpkg$\|easy_install$\|ed$\|emacs$\|env$\|expand$\|expect$\|facter$\|file$\|find$\|flock$\|fmt$\|fold$\|ftp$\|gdb$\|gimp$\|git$\|grep$\|head$\|ionice$\|ip$\|irb$\|jjs$\|journalctl$\|jq$\|jrunscript$\|ksh$\|ld.so$\|less$\|logsave$\|ltrace$\|lua$\|mail$\|make$\|man$\|more$\|mount$\|mtr$\|mv$\|mysql$\|nano$\|nc$\|nice$\|nl$\|nmap$\|node$\|od$\|openssl$\|perl$\|pg$\|php$\|pic$\|pico$\|pip$\|puppet$\|python$\|readelf$\|red$\|rlwrap$\|rpm$\|rpmquery$\|rsync$\|ruby$\|run-mailcap$\|run-parts$\|rvim$\|scp$\|screen$\|script$\|sed$\|service$\|setarch$\|sftp$\|smbclient$\|socat$\|sort$\|sqlite3$\|ssh$\|start-stop-daemon$\|stdbuf$\|strace$\|systemctl$\|tail$\|tar$\|taskset$\|tclsh$\|tcpdump$\|tee$\|telnet$\|tftp$\|time$\|timeout$\|tmux$\|ul$\|unexpand$\|uniq$\|unshare$\|vi$\|vim$\|watch$\|wget$\|wish$\|xargs$\|xxd$\|yum$\|zip$\|zsh$\|zypper$"
sudoMore="ALL:ALL\|ALL : ALL\|ALL\|NOPASSWD"
sudocapsB="/apt-get\|/apt\|/aria2c\|/arp\|/ash\|/awk\|/base64\|/bash\|/busybox\|/cat\|/chmod\|/chown\|/cp\|/cpan\|/cpulimit\|/crontab\|/csh\|/curl\|/cut\|/dash\|/date\|/dd\|/diff\|/dmesg\|/dmsetup\|/dnf\|/docker\|/dpkg\|/easy_install\|/ed\|/emacs\|/env\|/expand\|/expect\|/facter\|/file\|/find\|/flock\|/fmt\|/fold\|/ftp\|/gdb\|/gimp\|/git\|/grep\|/head\|/ionice\|/ip\|/irb\|/jjs\|/journalctl\|/jq\|/jrunscript\|/ksh\|/ld.so\|/less\|/logsave\|/ltrace\|/lua\|/mail\|/make\|/man\|/more\|/mount\|/mtr\|/mv\|/mysql\|/nano\|/nc\|/nice\|/nl\|/nmap\|/node\|/od\|/openssl\|/perl\|/pg\|/php\|/pic\|/pico\|/pip\|/puppet\|/python\|/readelf\|/red\|/rlwrap\|/rpm\|/rpmquery\|/rsync\|/ruby\|/run-mailcap\|/run-parts\|/rvim\|/scp\|/screen\|/script\|/sed\|/service\|/setarch\|/sftp\|/smbclient\|/socat\|/sort\|/sqlite3\|/ssh\|/start-stop-daemon\|/stdbuf\|/strace\|/systemctl\|/tail\|/tar\|/taskset\|/tclsh\|/tcpdump\|/tee\|/telnet\|/tftp\|/time\|/timeout\|/tmux\|/ul\|/unexpand\|/uniq\|/unshare\|/vi\|/vim\|/watch\|/wget\|/wish\|/xargs\|/xxd\|/yum\|/zip\|/zsh\|/zypper"
capsB="=ep\|cap_dac_read_search\|cap_dac_override"
intfol="\./\|/etc/\|/sys/\|/lib\|/root/\|/home/\|/var/log/\|/mnt/\|/usr/local/sbin/\|/usr/sbin/\|/sbin/\|/usr/local/bin/\|/usr/bin/\|/bin/\|/usr/local/games/\|/usr/games/\|/usr/lib/"`echo $PATH 2>/dev/null| sed 's/:/\\\|/g'`

sh_usrs=`cat /etc/passwd 2>/dev/null | grep -i "sh$" | cut -d ":" -f 1 | tr '\n' '|' | sed 's/|/\\\|/g'`"ImPoSSssSiBlEee"
nosh_usrs=`cat /etc/passwd 2>/dev/null | grep -i -v "sh$" | cut -d ":" -f 1 | tr '\n' '|' | sed 's/|/\\\|/g'`"ImPoSSssSiBlEee"
knw_usrs='daemon\|message+\|syslog\|www\|www-data\|mail\|noboby\|Debian-+\|rtkit\|systemd+'
USER=`whoami`
HOME=/home/$USER

Wfolders=`find /home /tmp /var /bin /etc /usr /lib /media /mnt /opt /root -writable -type d -maxdepth 2 2>/dev/null | tr '\n' '|' | sed 's/|/\\\|/g'`"/home\|\*"

notExtensions="\.tif$\|\.tiff$\|\.gif$\|\.jpeg$\|\.jpg\|\.jif$\|\.jfif$\|\.jp2$\|\.jpx$\|\.j2k$\|\.j2c$\|\.fpx$\|\.pcd$\|\.png$\|\.pdf$\|\.flv$\|\.mp4$\|\.mp3$\|\.gifv$\|\.avi$\|\.mov$\|\.mpeg$\|\.wav$\|\.doc$\|\.docx$\|\.xls$\|\.xlsx$"

if [ "$(/usr/bin/id -u)" -eq "0" ]; then printf $B"[*] "$RED"YOU ARE ALREADY ROOT!!! (nothing is going to be executed)\n"$NC; exit; fi

rm -rf $file
echo "File: $file"

echo "[+]Gathering system information..."
printf $B"[*] "$RED"BASIC SYSTEM INFO\n"$NC >> $file 
echo "" >> $file
printf $Y"[+] "$RED"Operative system\n"$NC >> $file
(cat /proc/version || uname -a ) 2>/dev/null >> $file
lsb_release -a 2>/dev/null >> $file #add to one-liner
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
which nc ncat netcat wget curl ping gcc g++ make gdb base64 socat python python2 python3 python2.7 python2.6 python3.6 python3.7 perl php ruby xterm doas sudo fetch 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Capabilities\n"$NC >> $file
getcap -r / 2>/dev/null | sed "s,$sudocapsB,${C}[31m&${C}[0m," | sed "s,$capsB,${C}[31m&${C}[0m," >> $file
echo "" >> $file

printf $Y"[+] "$RED"Environment\n"$NC >> $file
(set || env) 2>/dev/null | grep -v "sidG\|sidB\|sudoB\|sudoMore\|sudocapsB\|capsB|\notExtensions\|intfol\|_usrs" >> $file
echo "" >> $file

printf $Y"[+] "$RED"Cleaned proccesses\n"$NC >> $file
ps aux 2>/dev/null | grep -v "\[" | sed "s,$sh_usrs,${C}[34m&${C}[0m," | sed "s,$nosh_usrs,${C}[96m&${C}[0m," | sed "s,$knw_usrs,${C}[32m&${C}[0m," | sed "s,root,${C}[31m&${C}[0m," >> $file
echo "" >> $file

printf $Y"[+] "$RED"Binary processes permissions\n"$NC >> $file
ps aux 2>/dev/null | awk '{print $11}'|xargs -r ls -la 2>/dev/null |awk '!x[$0]++' 2>/dev/null | sed "s,$sh_usrs,${C}[34m&${C}[0m," | sed "s,$nosh_usrs,${C}[96m&${C}[0m," | sed "s,$knw_usrs,${C}[32m&${C}[0m," | sed "s,root,${C}[31m&${C}[0m," >> $file
echo "" >> $file

printf $Y"[+] "$RED"Services\n"$NC >> $file
(/usr/sbin/service --status-all || /sbin/chkconfig --list || /bin/rc-status) 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Different processes executed during 1 min (interesting is low number of repetitions)\n"$NC >> $file
if [ "`ps -e --format cmd`" ]; then for i in {1..121}; do ps -e --format cmd >> $file.tmp1; sleep 0.5; done; sort $file.tmp1 | uniq -c | grep -v "\[" | sed '/^.\{500\}./d' >> $file; rm $file.tmp1; fi
echo "" >> $file

printf $Y"[+] "$RED"Scheduled tasks\n"$NC >> $file
crontab -l 2>/dev/null | sed "s,$Wfolders,${C}[31m&${C}[0m," >> $file
ls -al /etc/cron* 2>/dev/null >> $file
cat /etc/cron* /etc/at* /etc/anacrontab /var/spool/cron/crontabs/root /var/spool/anacron 2>/dev/null | grep -v "^#" | sed "s,$Wfolders,${C}[31m&${C}[0m," >> $file
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
(netstat -punta || ss -t; ss -u) 2>/dev/null | sed "s,127.0.0.1,${C}[31m&${C}[0m," >> $file
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
echo '' | sudo -S -l -k 2>/dev/null | sed "s,$sudoB,${C}[31m&${C}[0m," | sed "s,$sudoMore,${C}[31m&${C}[0m,"  >> $file
cat /etc/sudoers 2>/dev/null | sed "s,$sudoB,${C}[31m&${C}[0m," | sed "s,$sudoMore,${C}[31m&${C}[0m," >> $file #Add to one-liner  
echo "" >> $file

printf $Y"[+] "$RED"Do not forget to execute 'sudo -l' without password or with valid password (if you know it)!!\n"$NC >> $file
echo "" >> $file

printf $Y"[+] "$RED"Do I have PGP keys?\n"$NC >> $file
gpg --list-keys 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Superusers\n"$NC >> $file
awk -F: '($3 == "0") {print}' /etc/passwd 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Login\n"$NC >> $file
w 2>/dev/null | sed "s,$sh_usrs,${C}[34m&${C}[0m," | sed "s,$nosh_usrs,${C}[96m&${C}[0m," | sed "s,$knw_usrs,${C}[32m&${C}[0m," | sed "s,root,${C}[31m&${C}[0m," >> $file
last 2>/dev/null | tail | sed "s,$sh_usrs,${C}[34m&${C}[0m," | sed "s,$nosh_usrs,${C}[96m&${C}[0m," | sed "s,$knw_usrs,${C}[32m&${C}[0m," | sed "s,root,${C}[31m&${C}[0m," >> $file
echo "" >> $file

printf $Y"[+] "$RED"Users with console\n"$NC >> $file
cat /etc/passwd 2>/dev/null | grep "sh$" | sed "s,$sh_usrs,${C}[34m&${C}[0m," >> $file
echo "" >> $file

printf $Y"[+] "$RED"All users\n"$NC >> $file
cat /etc/passwd 2>/dev/null | cut -d: -f1 | sed "s,$sh_usrs,${C}[34m&${C}[0m," | sed "s,$nosh_usrs,${C}[96m&${C}[0m," | sed "s,$knw_usrs,${C}[32m&${C}[0m," | sed "s,root,${C}[31m&${C}[0m," >> $file
echo "" >> $file

echo "" >> $file
echo "[+]Gathering files information..."
printf $B"[*] "$RED"INTERESTING FILES\n"$NC >> $file 
echo "" >> $file
printf $Y"[+] "$RED"SUID\n"$NC >> $file
find / -perm -4000 2>/dev/null | sed "s,$sidG,${C}[32m&${C}[0m," | sed "s,$sidB,${C}[31m&${C}[0m," >> $file
echo "" >> $file

printf $Y"[+] "$RED"SGID\n"$NC >> $file
find / -perm -g=s -type f 2>/dev/null | sed "s,$sidG,${C}[32m&${C}[0m," | sed "s,$sidB,${C}[31m&${C}[0m," >> $file
echo "" >> $file

printf $Y"[+] "$RED"Files inside \$HOME (limit 20)\n"$NC >> $file
ls -la $HOME 2>/dev/null | head -n 23 >> $file
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

printf $Y"[+] "$RED"Hashes inside /etc/passwd? Readable /etc/shadow, /etc/master.passwd, or /root?\n"$NC >> $file
grep -v '^[^:]*:[x]' /etc/passwd 2>/dev/null >> $file
cat /etc/shadow /etc/master.passwd 2>/dev/null >> $file
ls -ahl /root/ 2>/dev/null >> $file #Modify in one-liner  
echo "" >> $file

printf $Y"[+] "$RED"Inside docker or lxc?\n"$NC >> $file
dockercontainer=`grep -i docker /proc/self/cgroup  2>/dev/null; find / -name "*dockerenv*" -exec ls -la {} \; 2>/dev/null`
lxccontainer=`grep -qa container=lxc /proc/1/environ 2>/dev/null`
if [ "$dockercontainer" ]; then echo "Looks like we're in a Docker container" | sed "s,.*,${C}[31m&${C}[0m," >> $file; fi
if [ "$lxccontainer" ]; then echo "Looks like we're in a LXC container" | sed "s,.*,${C}[31m&${C}[0m," >> $file; fi
echo "" >> $file

printf $Y"[+] "$RED"*_history, profile, bashrc, httpd.conf\n"$NC >> $file
find / -type f \( -name "*_history" -o -name ".profile" -o -name "*bashrc" -o -name "httpd.conf" \) -exec ls -l {} \; 2>/dev/null | sed "s,_history$,${C}[33m&${C}[0m," | sed "s,profile$,${C}[36m&${C}[0m," | sed "s,bashrc$,${C}[34m&${C}[0m," | sed "s,httpd.conf$,${C}[35m&${C}[0m,">> $file
echo "" >> $file

printf $Y"[+] "$RED"All hidden files (not in /sys/, not: .gitignore, .listing, .profile, .bashrc) (limit 100)\n"$NC >> $file
find / -type f -iname ".*" -ls 2>/dev/null | grep -v "/sys/\|\.gitignore\|\.profile\|\.bashrc\|\.listing" | head -n 100 >> $file
echo "" >> $file

printf $Y"[+] "$RED"What inside /tmp, /var/tmp, /var/backups (limited 100)\n"$NC >> $file
ls -a /tmp /var/tmp /var/backups 2>/dev/null | head 105 >> $file
echo "" >> $file

printf $Y"[+] "$RED"Interesting writable Files\n"$NC >> $file
find / '(' -type f -or -type d ')' '(' '(' -user $USER ')' -or '(' -perm -o=w ')' ')' 2>/dev/null | grep -v '/proc/' | grep -v $HOME | grep -v '/sys/fs' | grep -v $notExtensions | sort | uniq | sed "s,$intfol,${C}[31m&${C}[0m," >> $file
for g in `groups`; do find / \( -type f -or -type d \) -group $g -perm -g=w 2>/dev/null | grep -v '/proc/' | grep -v $HOME | grep -v '/sys/fs' | grep -v $notExtensions | sed "s,$intfol,${C}[31m&${C}[0m," >> $file; done
echo "" >> $file

printf $Y"[+] "$RED"Web files?(output limited)\n"$NC >> $file
ls -alhR /var/www/ 2>/dev/null | head >> $file
ls -alhR /srv/www/htdocs/ 2>/dev/null | head >> $file
ls -alhR /usr/local/www/apache22/data/ 2>/dev/null | head >> $file
ls -alhR /opt/lampp/htdocs/ 2>/dev/null | head >> $file
echo "" >> $file

printf $Y"[+] "$RED"Backup files?\n"$NC >> $file
find /var /etc /bin /sbin /home /usr/local/bin /usr/local/sbin /usr/bin /usr/games /usr/sbin /root /tmp -type f \( -name "*backup*" -o -name "*bck*" \) 2>/dev/null >> $file
echo "" >> $file

printf $Y"[+] "$RED"Finding IPs inside logs\n"$NC >> $file
grep -R -a -E -o "(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)" /var/log/ 2>/dev/null | sort | uniq -c >> $file #Add to one-liner
echo "" >> $file

printf $Y"[+] "$RED"Finding passwords inside logs (limited 100)\n"$NC >> $file
grep -a -R -i "pwd\|passw" /var/log/ 2>/dev/null | sort | uniq | head -n 100 >> $file #Add to one-liner
echo "" >> $file

printf $Y"[+] "$RED"Finding emails inside logs (limited 100)\n"$NC >> $file
grep -R -E -a -o "\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,6}\b" /var/log/ 2>/dev/null | sort | uniq -c | head -n 100 >> $file #Add to one-liner 
echo "" >> $file 

printf $Y"[+] "$RED"Find 'pwd' or 'passw' string inside /home, /var/www, /etc, /root and list possible web(/var/www) and config(/etc) passwords\n"$NC >> $file
grep -lRi "pwd\|passw" /home /var/www /root 2>/dev/null | sort | uniq -c >> $file
grep -R -i "password.* = ['\"]" /var/www | sed '/^.\{150\}./d' | grep "\.php" >> $file #Add to one-liner
grep -R -i "password" /etc 2>/dev/null | grep "conf" | grep -v "#" >> $file #Add to one-liner
echo "" >> $file
