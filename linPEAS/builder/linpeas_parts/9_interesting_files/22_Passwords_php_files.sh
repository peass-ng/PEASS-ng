# Title: Interesting Files - passwords in PHP files
# ID: IF_Passwords_php_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Searching passwords in PHP config and webroot files
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.001
# Functions Used: print_2title
# Global Variables: $DEBUG, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $PHP_AUTH_FILES, $php_mysqli_timeout, $php_mysqli_files, $php_mysqli_started
# Fat linpeas: 0
# Small linpeas: 1


PHP_AUTH_FILES=""
if ! [ "$SEARCH_IN_FOLDER" ]; then
  PHP_AUTH_FILES=$(find /var/www -maxdepth 5 -type f -size -1048576c \( -name login.php -o -name auth.php \) -print 2>/dev/null | head -n 40)
fi

# A positional database password can appear in an ordinary PHP page without a
# password-named key. Keep this pass separate so its output never includes source.
if ! [ "$SEARCH_IN_FOLDER" ] && [ -d /var/www ]; then
  php_mysqli_timeout=$(command -v timeout 2>/dev/null || command -v gtimeout 2>/dev/null)
  if [ -z "$php_mysqli_timeout" ]; then
    echo "PHP mysqli positional credentials: unknown (timeout unavailable)."
  else
    php_mysqli_files=$("$php_mysqli_timeout" 5 find /var/www -maxdepth 5 -type f -name '*.php' -size -1048576c -print 2>/dev/null | head -n 40)
    if [ -n "$php_mysqli_files" ]; then
      print_2title "PHP mysqli positional credential candidates" "T1552.001"
      php_mysqli_started=$(date +%s)
      printf '%s\n' "$php_mysqli_files" | while IFS= read -r c; do
        if [ "$(($(date +%s) - php_mysqli_started))" -ge 5 ]; then
          echo "PHP mysqli positional credentials: partial (time limit reached)."
          break
        fi
        [ -f "$c" ] && [ -r "$c" ] || continue
        # shellcheck disable=SC2016 # The awk program intentionally reads PHP variables.
        "$php_mysqli_timeout" 1 awk '
          function trim(s) { sub(/^[[:space:]]+/, "", s); sub(/[[:space:]]+$/, "", s); return s }
          {
            if (length($0) > 4096) next
            line = $0
            low = tolower(line)
            if (low ~ /^[[:space:]]*(\/\/|#|\*)/) next
            if (!match(low, /(^|[^[:alnum:]_])new[[:space:]]+mysqli[[:space:]]*\(/)) next
            prefix = substr(line, 1, RSTART)
            prefix_quote = ""; prefix_escaped = 0; commented = 0
            for (j = 1; j <= length(prefix); j++) {
              ch = substr(prefix, j, 1)
              if (prefix_quote != "") {
                if (prefix_escaped) prefix_escaped = 0
                else if (ch == "\\") prefix_escaped = 1
                else if (ch == prefix_quote) prefix_quote = ""
              } else if (ch == "\"" || ch == sprintf("%c", 39)) prefix_quote = ch
              else if (ch == "#" || (ch == "/" && substr(prefix, j + 1, 1) == "/")) {
                commented = 1; break
              }
            }
            if (commented || prefix_quote != "") next
            start = RSTART + RLENGTH
            quote = ""; escaped = 0; depth = 0; argc = 1; arg = ""; third = ""; closed = 0
            for (i = start; i <= length(line); i++) {
              ch = substr(line, i, 1)
              if (quote != "") {
                arg = arg ch
                if (escaped) escaped = 0
                else if (ch == "\\") escaped = 1
                else if (ch == quote) quote = ""
              } else if (ch == "\"" || ch == sprintf("%c", 39)) {
                quote = ch; arg = arg ch
              } else if (ch == "(" || ch == "[" || ch == "{") {
                depth++; arg = arg ch
              } else if (ch == ")" && depth == 0) {
                if (argc == 3) third = trim(arg)
                closed = 1; break
              } else if (ch == ")" || ch == "]" || ch == "}") {
                depth--; arg = arg ch
              } else if (ch == "," && depth == 0) {
                if (argc == 3) third = trim(arg)
                argc++; arg = ""
              } else arg = arg ch
            }
            if (!closed || argc < 3 || third == "" || tolower(third) == "null" || third == "\"\"" || third == sprintf("%c%c", 39, 39)) next
            kind = "expression"
            if (third ~ /^\$[[:alpha:]_][[:alnum:]_]*$/) kind = "variable"
            else if (third ~ /^\047.*\047$/ || third ~ /^".*"$/) kind = "literal"
            printf "%s:%d: mysqli positional credentials candidate (third argument: %s)\n", FILENAME, FNR, kind
          }
        ' "$c" 2>/dev/null
      done
      echo ""
    fi
  fi
fi
if [ "$PSTORAGE_PHP_FILES" ] || [ "$PHP_AUTH_FILES" ] || [ "$DEBUG" ]; then
  print_2title "Searching passwords in PHP files" "T1552.001"
  printf "%s\n%s\n" "$PSTORAGE_PHP_FILES" "$PHP_AUTH_FILES" | sed '/^$/d' | sort -u | while IFS= read -r c; do grep -EiIH "(pwd|passwd|password|PASSWD|PASSWORD|dbuser|dbpass).*[=:].+|define ?\('(\w*passw|\w*user|\w*datab)" "$c" 2>/dev/null | grep -Ev "function|password.*= ?\"\"|password.*= ?''" | sed '/^.\{150\}./d' | sort | uniq | sed -"${E}" "s,[pP][aA][sS][sS][wW]|[dD][bB]_[pP][aA][sS][sS],${SED_RED},g"; done
  echo ""
fi
