# Title: Users Information - Clipboard and highlighted text
# ID: UG_Clipboard_highlighted_text
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check clipboard and highlighted text for sensitive information
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $DEBUG, $pwd_inside_history
# Initial Functions:
# Generated Global Variables: $content
# Fat linpeas: 0
# Small linpeas: 1


if [ "$(command -v xclip 2>/dev/null || echo -n '')" ] || [ "$(command -v xsel 2>/dev/null || echo -n '')" ] || [ "$(command -v pbpaste 2>/dev/null || echo -n '')" ] || [ "$(command -v wl-paste 2>/dev/null || echo -n '')" ] || [ "$DEBUG" ]; then
  print_2title "Clipboard and Highlighted Text"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#clipboard"

  # Function to check clipboard content
  check_clipboard() {
    local content="$1"
    if [ -n "$content" ]; then
      echo "$content" | sed -${E} "s,$pwd_inside_history,${SED_RED},g" | sed -${E} "s,(password|passwd|pwd).*=.*,${SED_RED},g" | sed -${E} "s,(token|key|secret).*=.*,${SED_RED},g"
    fi
  }

  # Check different clipboard tools
  if [ "$(command -v xclip 2>/dev/null || echo -n '')" ]; then
    echo "Using xclip:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    echo "Clipboard:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    check_clipboard "$(xclip -o -selection clipboard 2>/dev/null)"
    echo "Highlighted text:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    check_clipboard "$(xclip -o 2>/dev/null)"
  elif [ "$(command -v xsel 2>/dev/null || echo -n '')" ]; then
    echo "Using xsel:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    echo "Clipboard:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    check_clipboard "$(xsel -ob 2>/dev/null)"
    echo "Highlighted text:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    check_clipboard "$(xsel -o 2>/dev/null)"
  elif [ "$(command -v pbpaste 2>/dev/null || echo -n '')" ]; then
    echo "Using pbpaste:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    echo "Clipboard:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    check_clipboard "$(pbpaste 2>/dev/null)"
  elif [ "$(command -v wl-paste 2>/dev/null || echo -n '')" ]; then
    echo "Using wl-paste:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    echo "Clipboard:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    check_clipboard "$(wl-paste 2>/dev/null)"
  else
    echo_not_found "clipboard tools (xclip, xsel, pbpaste, wl-paste)"
  fi
  echo ""
fi
