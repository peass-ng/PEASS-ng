# Title: Users Information - Sudo tokens
# ID: UG_Sudo_tokens
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Checking Sudo tokens
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $HOME, $CURRENT_USER_PIVOT_PID
# Initial Functions: get_current_user_privot_pid
# Generated Global Variables: $ptrace_scope
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Checking sudo tokens"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#reusing-sudo-tokens"
ptrace_scope="$(cat /proc/sys/kernel/yama/ptrace_scope 2>/dev/null)"
if [ "$ptrace_scope" ] && [ "$ptrace_scope" -eq 0 ]; then
  echo "ptrace protection is disabled (0), so sudo tokens could be abused" | sed "s,is disabled,${SED_RED},g";

  if [ "$(command -v gdb 2>/dev/null || echo -n '')" ]; then
    echo "gdb was found in PATH" | sed -${E} "s,.*,${SED_RED},g";
  fi

  if [ "$CURRENT_USER_PIVOT_PID" ]; then
    echo "The current user proc $CURRENT_USER_PIVOT_PID is the parent of a different user proccess" | sed -${E} "s,.*,${SED_RED},g";
  fi

  if [ -f "$HOME/.sudo_as_admin_successful" ]; then
    echo "Current user has .sudo_as_admin_successful file, so he can execute with sudo" | sed -${E} "s,.*,${SED_RED},";
  fi

  if ps -eo pid,command -u "$(id -u)" | grep -v "$PPID" | grep -v " " | grep -qE '(ash|ksh|csh|dash|bash|zsh|tcsh|sh)$'; then
    echo "Current user has other interactive shells running: " | sed -${E} "s,.*,${SED_RED},g";
    ps -eo pid,command -u "$(id -u)" | grep -v "$PPID" | grep -v " " | grep -E '(ash|ksh|csh|dash|bash|zsh|tcsh|sh)$'
  fi

else
  echo "ptrace protection is enabled ($ptrace_scope)" | sed "s,is enabled,${SED_GREEN},g";

fi
echo ""
