# Title: Interesting Files - Suspicious sed persistence commands in history
# ID: IF_Suspicious_sed_history
# Author: HT Bot
# Last Update: 26-11-2025
# Description: Flags sed history entries that write/read sensitive startup files, indicating possible prompt-injection persistence (e.g., CVE-2025-64755 style attacks).
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables: $DEBUG, $HOME, $PSTORAGE_HISTORY
# Initial Functions:
# Generated Global Variables: $sed_history_sensitive, $sed_history_pattern, $history_candidates, $matches
# Fat linpeas: 0
# Small linpeas: 1

sed_history_sensitive='\\.zsh(env|rc|profile|login|logout)|\\.zprofile|\\.zlogin|\\.zlogout|\\.bash(rc|_profile|_login|_logout)?|\\.profile|\\.kshrc|\\.cshrc|\\.login|\\.aws/credentials|\\.ssh/(authorized_keys|config)|\\.kube/config'
sed_history_pattern="sed[^|;&]*[wWrR][[:space:]]*(~|/|\\.)[^|;&]*(${sed_history_sensitive})"

history_candidates=""

if [ "$PSTORAGE_HISTORY" ]; then
  history_candidates="$PSTORAGE_HISTORY"
fi

if [ -z "$history_candidates" ]; then
  if [ "$HOME" ]; then
    for hf in "$HOME/.bash_history" "$HOME/.zsh_history" "$HOME/.zhistory" "$HOME/.history" "$HOME/.sh_history" "$HOME/.ksh_history" "$HOME/.config/fish/fish_history"; do
      if [ -r "$hf" ]; then
        if [ "$history_candidates" ]; then
          history_candidates="$history_candidates"$'\n'"$hf"
        else
          history_candidates="$hf"
        fi
      fi
    done
  fi
  for hf in "/root/.bash_history" "/root/.zsh_history" "/var/root/.zsh_history" "/var/root/.bash_history"; do
    if [ -r "$hf" ]; then
      if [ "$history_candidates" ]; then
        history_candidates="$history_candidates"$'\n'"$hf"
      else
        history_candidates="$hf"
      fi
    fi
  done
fi

if [ -z "$history_candidates" ] && [ -d "$HOME" ]; then
  history_candidates=$(find "$HOME" -maxdepth 2 -type f \( -name "*_history" -o -name ".*history" -o -name "history" \) 2>/dev/null | head -n 40)
fi

history_candidates=$(printf "%s\n" "$history_candidates" | awk 'NF && !seen[$0]++')

if [ "$history_candidates" ] || [ "$DEBUG" ]; then
  print_2title "Suspicious sed commands writing sensitive files (history)"
  printf "%s\n" "$history_candidates" | while IFS= read -r f; do
    [ -n "$f" ] || continue
    [ -r "$f" ] || continue
    matches=$(grep -Ein --color=never -E "$sed_history_pattern" "$f" 2>/dev/null | head -n 20)
    if [ "$matches" ]; then
      printf "%s\n" "$matches" | sed -${E} "s,${sed_history_sensitive},${SED_RED},g"
    fi
  done
  echo ""
fi
