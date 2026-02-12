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
# Generated Global Variables: $secure_path_line
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Checking 'sudo -l', /etc/sudoers, and /etc/sudoers.d"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#sudo-and-suid"
(echo '' | timeout 1 sudo -S -l | sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g" | sed "s,\!root,${SED_RED},") 2>/dev/null || echo_not_found "sudo"
if [ "$PASSWORD" ]; then
  (echo "$PASSWORD" | timeout 1 sudo -S -l | sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g") 2>/dev/null  || echo_not_found "sudo"
fi
(sudo -n -l 2>/dev/null | sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g" | sed "s,\!root,${SED_RED},") 2>/dev/null || echo "No cached sudo token (sudo -n -l)"

secure_path_line=$(sudo -l 2>/dev/null | grep -o "secure_path=[^,]*" | head -n 1 | cut -d= -f2)
if [ "$secure_path_line" ]; then
  for p in $(echo "$secure_path_line" | tr ':' ' '); do
    if [ -w "$p" ]; then
      echo "Writable secure_path entry: $p" | sed -${E} "s,.*,${SED_RED},g"
    fi
  done
fi
( grep -Iv "^$" cat /etc/sudoers | grep -v "#" | sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g" | sed "s,pwfeedback,${SED_RED},g" ) 2>/dev/null  || echo_not_found "/etc/sudoers"
if ! [ "$IAMROOT" ] && [ -w '/etc/sudoers.d/' ]; then
  echo "You can create a file in /etc/sudoers.d/ and escalate privileges" | sed -${E} "s,.*,${SED_RED_YELLOW},"
fi
for f in /etc/sudoers.d/*; do
  if [ -w "$f" ]; then
    echo "Sudoers file: $f is writable and may allow privilege escalation" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
  fi
  if [ -r "$f" ]; then
    echo "Sudoers file: $f is readable" | sed -${E} "s,.*,${SED_RED},g"
    grep -Iv "^$" "$f" | grep -v "#" | sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g" | sed "s,pwfeedback,${SED_RED},g"
  fi
done
echo ""
