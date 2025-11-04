# Title: Users Information - Sudo -l
# ID: UG_Sudo_l
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Checking 'sudo -l', /etc/sudoers, and /etc/sudoers.d
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables:$IAMROOT, $PASSWORD, $sudoB, $sudoG, $sudoVB1, $sudoVB2 
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Checking 'sudo -l', /etc/sudoers, and /etc/sudoers.d"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#sudo-and-suid"
(echo '' | timeout 1 sudo -S -l | sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g" | sed "s,\!root,${SED_RED},") 2>/dev/null || echo_not_found "sudo"
if [ "$PASSWORD" ]; then
  (echo "$PASSWORD" | timeout 1 sudo -S -l | sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g") 2>/dev/null  || echo_not_found "sudo"
fi
( grep -Iv "^$" cat /etc/sudoers | grep -v "#" | sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g" | sed "s,pwfeedback,${SED_RED},g" ) 2>/dev/null  || echo_not_found "/etc/sudoers"
if ! [ "$IAMROOT" ] && [ -w '/etc/sudoers.d/' ]; then
  echo "You can create a file in /etc/sudoers.d/ and escalate privileges" | sed -${E} "s,.*,${SED_RED_YELLOW},"
fi
for f in /etc/sudoers.d/*; do
  if [ -r "$f" ]; then
    echo "Sudoers file: $f is readable" | sed -${E} "s,.*,${SED_RED},g"
    grep -Iv "^$" "$f" | grep -v "#" | sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g" | sed "s,pwfeedback,${SED_RED},g"
  fi
done
# Extra: Detect tcpdump sudoers patterns that can be abused (wildcards/-w/-Z/-r/-V)
_tcpdump_sudol="Matching Defaults entries for runner on runnervmf2e7y:
    env_reset, mail_badpass, secure_path=/usr/local/sbin\:/usr/local/bin\:/usr/sbin\:/usr/bin\:/sbin\:/bin\:/snap/bin, use_pty

User runner may run the following commands on runnervmf2e7y:
    (ALL) NOPASSWD: ALL"
if [ -z "" ] && [ "" ]; then
  _tcpdump_sudol="Matching Defaults entries for runner on runnervmf2e7y:
    env_reset, mail_badpass, secure_path=/usr/local/sbin\:/usr/local/bin\:/usr/sbin\:/usr/bin\:/sbin\:/bin\:/snap/bin, use_pty

User runner may run the following commands on runnervmf2e7y:
    (ALL) NOPASSWD: ALL"
fi
if echo "" | grep -q "/tcpdump"; then
  echo "Potentially dangerous sudo tcpdump rule(s) found:" | sed - "s,.*,,g"
  printf "%s\n" "" | grep tcpdump | sed - "s,.*,,g"
  print_info "tcpdump via sudo is commonly exploitable if arguments are not fully pinned (e.g., globbed -w path). Consider trying:"
  echo "  sudo tcpdump -c10 -w <allowed_path> -w /dev/shm/out.pcap -F <allowed_filter>" | sed - "s,.*,,g"
  echo "  sudo tcpdump -c10 -w <allowed_path> -Z root -w /dev/shm/root-owned -F <allowed_filter>" | sed - "s,.*,,g"
  echo "  sudo tcpdump -c10 -w <allowed_path> -Z root -r crafted.pcap -w /etc/sudoers.d/linpeas -F <allowed_filter>" | sed - "s,.*,,g"
  echo "  sudo tcpdump -c10 -w <allowed_path> -V /root/secret -w /tmp/dummy -F <allowed_filter>" | sed - "s,.*,,g"
  echo "    (use with caution; ensure correct perms, e.g., 440 for sudoers.d)" | sed - "s,.*,,g"
  echo ""
fi
