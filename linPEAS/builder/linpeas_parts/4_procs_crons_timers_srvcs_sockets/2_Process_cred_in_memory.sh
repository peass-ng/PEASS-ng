# Title: Processes & Cron & Services & Timers - Processes with credentials inside memory
# ID: PR_Process_cred_in_memory
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Processes with credentials inside memory
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $pslist, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Processes with credentials in memory (root req)"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#credentials-from-process-memory"
  if echo "$pslist" | grep -q "gdm-password"; then echo "gdm-password process found (dump creds from memory as root)" | sed "s,gdm-password process,${SED_RED},"; else echo_not_found "gdm-password"; fi
  if echo "$pslist" | grep -q "gnome-keyring-daemon"; then echo "gnome-keyring-daemon process found (dump creds from memory as root)" | sed "s,gnome-keyring-daemon,${SED_RED},"; else echo_not_found "gnome-keyring-daemon"; fi
  if echo "$pslist" | grep -q "lightdm"; then echo "lightdm process found (dump creds from memory as root)" | sed "s,lightdm,${SED_RED},"; else echo_not_found "lightdm"; fi
  if echo "$pslist" | grep -q "vsftpd"; then echo "vsftpd process found (dump creds from memory as root)" | sed "s,vsftpd,${SED_RED},"; else echo_not_found "vsftpd"; fi
  if echo "$pslist" | grep -q "apache2"; then echo "apache2 process found (dump creds from memory as root)" | sed "s,apache2,${SED_RED},"; else echo_not_found "apache2"; fi
  if echo "$pslist" | grep -q "sshd:"; then echo "sshd: process found (dump creds from memory as root)" | sed "s,sshd:,${SED_RED},"; else echo_not_found "sshd"; fi
  echo ""
fi