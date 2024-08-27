# Title: Users Information - Clipboard and highlighted text
# ID: UG_Clipboard_highlighted_text
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Clipboard and highlighted text
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title
# Global Variables: $DEBUG, $pwd_inside_history
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if [ "$(command -v xclip 2>/dev/null || echo -n '')" ] || [ "$(command -v xsel 2>/dev/null || echo -n '')" ] || [ "$(command -v pbpaste 2>/dev/null || echo -n '')" ] || [ "$DEBUG" ]; then
  print_2title "Clipboard or highlighted text?"
  if [ "$(command -v xclip 2>/dev/null || echo -n '')" ]; then
    echo "Clipboard: "$(xclip -o -selection clipboard 2>/dev/null) | sed -${E} "s,$pwd_inside_history,${SED_RED},"
    echo "Highlighted text: "$(xclip -o 2>/dev/null) | sed -${E} "s,$pwd_inside_history,${SED_RED},"
  elif [ "$(command -v xsel 2>/dev/null || echo -n '')" ]; then
    echo "Clipboard: "$(xsel -ob 2>/dev/null) | sed -${E} "s,$pwd_inside_history,${SED_RED},"
    echo "Highlighted text: "$(xsel -o 2>/dev/null) | sed -${E} "s,$pwd_inside_history,${SED_RED},"
  elif [ "$(command -v pbpaste 2>/dev/null || echo -n '')" ]; then
    echo "Clipboard: "$(pbpaste) | sed -${E} "s,$pwd_inside_history,${SED_RED},"
  else echo_not_found "xsel and xclip"
  fi
  echo ""
fi
