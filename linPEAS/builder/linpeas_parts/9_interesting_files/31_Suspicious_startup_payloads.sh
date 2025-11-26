# Title: Interesting Files - Suspicious payloads in shell startup files
# ID: IF_Suspicious_startup_payloads
# Author: HT Bot
# Last Update: 26-11-2025
# Description: Scans shell startup files for reverse-shell style commands likely dropped via sed-based persistence.
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG, $HOME
# Initial Functions:
# Generated Global Variables: $startup_indicator_pattern, $startup_files, $matches
# Fat linpeas: 0
# Small linpeas: 1

startup_indicator_pattern='curl[[:space:]].*\|[[:space:]]*(bash|sh)|wget[[:space:]].*\|[[:space:]]*(bash|sh)|bash[[:space:]]+-i[[:space:]]+>&|/dev/tcp|nc[[:space:]].*(-e|/bin/sh)|ncat[[:space:]].*(-e|/bin/sh)|socat[[:space:]]+TCP|python[[:space:]]+-c[[:space:]].*[Ss]ocket|perl[[:space:]]+-e[[:space:]].*[Ss]ocket|ruby[[:space:]]+-rsocket|php[[:space:]]+-r[[:space:]].*fsockopen'

startup_files=""

if [ "$HOME" ]; then
  for f in "$HOME/.zshenv" "$HOME/.zprofile" "$HOME/.zlogin" "$HOME/.zlogout" "$HOME/.zshrc" \
    "$HOME/.bashrc" "$HOME/.bash_profile" "$HOME/.bash_login" "$HOME/.bash_logout" "$HOME/.profile" \
    "$HOME/.kshrc" "$HOME/.cshrc" "$HOME/.shrc" "$HOME/.config/fish/config.fish"; do
    if [ -r "$f" ]; then
      if [ "$startup_files" ]; then
        startup_files="$startup_files"$'\n'"$f"
      else
        startup_files="$f"
      fi
    fi
  done
fi

for f in "/etc/zshenv" "/etc/zprofile" "/etc/zlogin" "/etc/zlogout" "/etc/zsh/zshrc" "/etc/zshrc" \
  "/etc/profile" "/etc/bash.bashrc" "/etc/bashrc" "/usr/local/etc/zshenv" "/usr/local/etc/zprofile" \
  "/usr/local/etc/zlogin" "/usr/local/etc/zlogout" "/usr/local/etc/zshrc"; do
  if [ -r "$f" ]; then
    if [ "$startup_files" ]; then
      startup_files="$startup_files"$'\n'"$f"
    else
      startup_files="$f"
    fi
  fi
done

startup_files=$(printf "%s\n" "$startup_files" | awk 'NF && !seen[$0]++')

if [ "$startup_files" ] || [ "$DEBUG" ]; then
  print_2title "Suspicious commands sourced by shell startup files"
  printf "%s\n" "$startup_files" | while IFS= read -r f; do
    [ -n "$f" ] || continue
    [ -r "$f" ] || continue
    matches=$(grep -Ein --color=never -E "$startup_indicator_pattern" "$f" 2>/dev/null | head -n 20)
    if [ "$matches" ]; then
      printf "%s\n" "$matches" | sed -${E} "s,${startup_indicator_pattern},${SED_RED},g"
    fi
  done
  echo ""
fi
