# Title: Users Information - Sudo -l
# ID: UG_Sudo_l
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Checking 'sudo -l', /etc/sudoers, and /etc/sudoers.d
# License: GNU GPL
# Version: 1.0
# Mitre: T1548.003
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables:$IAMROOT, $PASSWORD, $TIMEOUT, $sudoB, $sudoG, $sudoVB1, $sudoVB2
# Initial Functions:
# Generated Global Variables: $sudo_l_output, $sudo_l_password_output, $sudo_l_cached_output, $secure_path_line
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Checking 'sudo -l', /etc/sudoers, and /etc/sudoers.d" "T1548.003"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#sudo-and-suid"

sudo_l_colorize() {
  sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g"
}

sudo_l_colorize_output() {
  printf "%s\n" "$1" | sudo_l_colorize | sed "s,\!root,${SED_RED},"
}

sudo_l_colorize_file() {
  grep -Iv "^$" "$1" | grep -v "#" | sudo_l_colorize | sed "s,pwfeedback,${SED_RED},g"
}

if [ "$(command -v sudo 2>/dev/null || echo -n '')" ]; then
  if [ "$TIMEOUT" ]; then
    sudo_l_output=$(printf '\n' | "$TIMEOUT" 15 sudo -S -l 2>/dev/null)
  else
    sudo_l_output=$(sudo -n -l 2>/dev/null)
  fi
  sudo_l_colorize_output "$sudo_l_output"

  if [ "$PASSWORD" ]; then
    if [ "$TIMEOUT" ]; then
      sudo_l_password_output=$(printf "%s\n" "$PASSWORD" | "$TIMEOUT" 15 sudo -S -l 2>/dev/null)
    else
      sudo_l_password_output=$(printf "%s\n" "$PASSWORD" | sudo -S -l 2>/dev/null)
    fi
    printf "%s\n" "$sudo_l_password_output" | sudo_l_colorize
  fi

  sudo_l_cached_output=$(sudo -n -l 2>/dev/null)
  if [ "$sudo_l_cached_output" ]; then
    sudo_l_colorize_output "$sudo_l_cached_output"
  else
    echo "No cached sudo token (sudo -n -l)"
  fi
else
  echo_not_found "sudo"
fi

secure_path_line=$(printf "%s\n%s\n%s\n" "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" | grep -o "secure_path=[^,]*" | head -n 1 | cut -d= -f2)
if [ "$secure_path_line" ]; then
  for p in $(echo "$secure_path_line" | tr ':' ' '); do
    if [ -w "$p" ]; then
      echo "Writable secure_path entry: $p" | sed -${E} "s,.*,${SED_RED},g"
    fi
  done
fi
(sudo_l_colorize_file /etc/sudoers) 2>/dev/null || echo_not_found "/etc/sudoers"
if ! [ "$IAMROOT" ] && [ -w '/etc/sudoers.d/' ]; then
  echo "You can create a file in /etc/sudoers.d/ and escalate privileges" | sed -${E} "s,.*,${SED_RED_YELLOW},"
fi
for f in /etc/sudoers.d/*; do
  if [ -w "$f" ]; then
    echo "Sudoers file: $f is writable and may allow privilege escalation" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
  fi
  if [ -r "$f" ]; then
    echo "Sudoers file: $f is readable" | sed -${E} "s,.*,${SED_RED},g"
    sudo_l_colorize_file "$f"
  fi
done
echo ""
