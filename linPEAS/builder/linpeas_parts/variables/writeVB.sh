# Title: Variables - writeVB
# ID: writeVB
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Very dangerous files and folders if writable
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables: $PATH
# Initial Functions:
# Generated Global Variables: $writeVB
# Fat linpeas: 0
# Small linpeas: 1


writeVB="/etc/anacrontab|/etc/apt/apt.conf.d|/etc/bash.bashrc|/etc/bash_completion|/etc/bash_completion.d/|/etc/cron|/etc/environment|/etc/environment.d/|/etc/group|/etc/incron.d/|/etc/init|/etc/ld.so.conf.d/|/etc/master.passwd|/etc/passwd|/etc/profile.d/|/etc/profile|/etc/rc.d|/etc/shadow|/etc/skey/|/etc/sudoers|/etc/sudoers.d/|/etc/supervisor/conf.d/|/etc/supervisor/supervisord.conf|/etc/systemd|/etc/sys|/lib/systemd|/etc/update-motd.d/|/root/.ssh/|/run/systemd|/usr/lib/cron/tabs/|/usr/lib/systemd|/systemd/system|/var/db/yubikey/|/var/spool/anacron|/var/spool/cron/crontabs|"$(echo $PATH 2>/dev/null | sed 's/:\.:/:/g' | sed 's/:\.$//g' | sed 's/^\.://g' | sed 's/:/$|^/g') #Add Path but remove simple dot in PATH