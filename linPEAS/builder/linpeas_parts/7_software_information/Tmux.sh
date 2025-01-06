# Title: Software Information - Tmux
# ID: SI_Tmux
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Enumerate Tmux
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $SEARCH_IN_FOLDER, $wgroups
# Initial Functions:
# Generated Global Variables: $tmuxdefsess, $tmuxnondefsess, $tmuxsess2
# Fat linpeas: 0
# Small linpeas: 1


tmuxdefsess=$(tmux ls 2>/dev/null)
tmuxnondefsess=$(ps auxwww | grep "tmux " | grep -v grep)
tmuxsess2=$(find /tmp -type d -path "/tmp/tmux-*" 2>/dev/null)
if ([ "$tmuxdefsess" ] || [ "$tmuxnondefsess" ] || [ "$tmuxsess2" ] || [ "$DEBUG" ]) && ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Searching tmux sessions"$N
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#open-shell-sessions"
  tmux -V
  printf "$tmuxdefsess\n$tmuxnondefsess\n$tmuxsess2" | sed -${E} "s,.*,${SED_RED}," | sed -${E} "s,no server running on.*,${C}[32m&${C}[0m,"

  find /tmp -type s -path "/tmp/tmux*" -not -user $USER '(' '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null | while read f; do
    echo "Other user tmux socket is writable: $f" | sed "s,$f,${SED_RED_YELLOW},"
  done
  echo ""
fi