# Title: Variables - History files inventory
# ID: PSTORAGE_HISTORY
# Author: HT Bot
# Last Update: 26-11-2025
# Description: Collects readable shell history files to be reused by other modules.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $HOME
# Initial Functions:
# Generated Global Variables: $PSTORAGE_HISTORY, $history_inventory_candidates
# Fat linpeas: 0
# Small linpeas: 1

history_inventory_candidates=""

add_history_path() {
  [ -n "$1" ] || return 0
  [ -r "$1" ] || return 0
  if [ "$history_inventory_candidates" ]; then
    history_inventory_candidates="${history_inventory_candidates}"$'
'"$1"
  else
    history_inventory_candidates="$1"
  fi
}

if [ "$HOME" ]; then
  for hf in     "$HOME/.bash_history"     "$HOME/.bash_logout"     "$HOME/.bash_login"     "$HOME/.bash_profile"     "$HOME/.profile"     "$HOME/.zsh_history"     "$HOME/.zhistory"     "$HOME/.zshrc"     "$HOME/.zlogin"     "$HOME/.zlogout"     "$HOME/.zshenv"     "$HOME/.ksh_history"     "$HOME/.kshrc"     "$HOME/.cshrc"     "$HOME/.history"     "$HOME/.sh_history"     "$HOME/.config/fish/fish_history"; do
    add_history_path "$hf"
  done
fi

for hf in   "/root/.bash_history"   "/root/.zsh_history"   "/var/root/.bash_history"   "/var/root/.zsh_history"   "/etc/profile"   "/etc/zprofile"   "/etc/zlogin"   "/etc/zlogout"   "/etc/zsh/zshrc"   "/etc/zshenv"   "/etc/zshrc"   "/etc/bash.bashrc"   "/etc/bashrc"; do
  add_history_path "$hf"
done

if [ -z "$history_inventory_candidates" ] && [ -n "$HOME" ] && [ -d "$HOME" ]; then
  history_inventory_candidates=$(find "$HOME" -maxdepth 2 -type f     \( -name "*_history" -o -name ".*history" -o -name "history" \) 2>/dev/null | head -n 60)
fi

PSTORAGE_HISTORY=$(printf "%s
" "$history_inventory_candidates" | awk 'NF && !seen[$0]++')
