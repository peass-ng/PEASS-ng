# Title: Processes & Cron & Services & Timers - Cron jobs and Wildcards
# ID: PR_Cron_jobs
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: Enumerate system cron jobs and check for privilege escalation vectors
# License: GNU GPL
# Version: 1.2
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $cronjobsG, $nosh_usrs, $SEARCH_IN_FOLDER, $sh_usrs, $USER, $Wfolders, $cronjobsB, $PATH
# Initial Functions:
# Generated Global Variables: $cmd, $VAR, $file, $path, $user_crontab, $username, $job_id, $cron_dir, $crontab, $findings, $line, $finding, $bin
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Check for vulnerable cron jobs"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#scheduledcron-jobs"

  print_3title "Cron jobs list"
  command -v crontab 2>/dev/null || echo_not_found "crontab"
  crontab -l 2>/dev/null | tr -d "\r" | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed "s,root,${SED_RED},"
  command -v incrontab 2>/dev/null || echo_not_found "incrontab"
  incrontab -l 2>/dev/null
  ls -alR /etc/cron* /var/spool/cron/crontabs /var/spool/anacron 2>/dev/null | sed -${E} "s,$cronjobsG,${SED_GREEN},g" | sed "s,$cronjobsB,${SED_RED},g"
  cat /etc/cron* /etc/at* /etc/anacrontab /var/spool/cron/crontabs/* /etc/incron.d/* /var/spool/incron/* 2>/dev/null | tr -d "\r" | grep -v "^#" | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed -${E} "s,$nosh_usrs,${SED_BLUE},"  | sed "s,root,${SED_RED},"
  crontab -l -u "$USER" 2>/dev/null | tr -d "\r"
  ls -lR /usr/lib/cron/tabs/ /private/var/at/jobs /var/at/tabs/ /etc/periodic/ 2>/dev/null | sed -${E} "s,$cronjobsG,${SED_GREEN},g" | sed "s,$cronjobsB,${SED_RED},g" #MacOS paths
  atq 2>/dev/null
  echo ""

  print_3title "Checking for specific cron jobs vulnerabilities"



  # Function to check if a binary is writable and executable
  check_binary_perms() {
    local bin="$1"
    [ -z "$bin" ] && return
    
    # Skip if binary doesn't exist
    [ ! -e "$bin" ] && return
    
    # Check if it's a regular file
    [ ! -f "$bin" ] && return
    
    # Check if it's writable and executable
    if [ -w "$bin" ]; then
      echo "Writable binary: $bin"
      ls -l "$bin" 2>/dev/null
    fi
  }

  # Function to extract binary path from command
  get_binary_path() {
    local cmd="$1"
    local bin=""
    
    # Try to get the first word of the command
    bin=$(echo "$cmd" | awk '{print $1}')
    [ -z "$bin" ] && return
    
    # If it's an absolute path, use it directly
    if [ "$(echo "$bin" | cut -c1)" = "/" ]; then
      echo "$bin"
      return
    fi
    
    # If it's a relative path, try to resolve it
    if [ -e "$bin" ]; then
      echo "$(pwd)/$bin"
      return
    fi
    
    # Try to find it in PATH
    for path in $(echo "$PATH" | tr ':' ' '); do
      if [ -x "$path/$bin" ]; then
        echo "$path/$bin"
        return
      fi
    done
  }

  # Function to check for privilege escalation vectors in a command
  check_privesc_vectors() {
    local cmd="$1"
    local file="$2"
    local findings=""
    local bin=""

    # Skip common false positives (mail commands, shell conditionals, variable assignments)
    if echo "$cmd" | grep -qE '^(mail|echo|then|else|fi|if|for|while|do|done|case|esac|exit|return|break|continue|:|\[|test|\[\[|\]\]|true|false|source|\.|cd|pwd|export|unset|readonly|local|declare|typeset|alias|unalias|set|unset|shift|wait|trap|umask|ulimit|exec|eval|command|builtin|let|read|printf|^[[:space:]]*[A-Za-z0-9_]+[[:space:]]*[=:])'; then
      return
    fi

    # Get the binary path
    bin=$(get_binary_path "$cmd")
    if [ -n "$bin" ]; then
      check_binary_perms "$bin"
    fi

    # Check for wildcard injection vectors
    # Attack: Using wildcards in tar/chmod/chown to execute arbitrary commands
    # Example: tar cf archive.tar * (where * expands to --checkpoint=1 --checkpoint-action=exec=sh)
    if echo "$cmd" | grep -qE '\*'; then
      findings="${findings}POTENTIAL_WILDCARD_INJECTION: Command uses wildcards with potentially exploitable command\n"
    fi

    # Check for path hijacking vectors
    # Attack: Using relative paths or commands without full path that can be hijacked
    # Example: script.sh instead of /usr/bin/script.sh
    if echo "$cmd" | grep -qE '^[[:space:]]*[^/][^[:space:]]*[[:space:]]'; then
      # Skip common false positives like shell builtins, control structures, and variable assignments
      # Also skip test commands ([ ]), logical operators (&& ||), and complex shell constructs
      if ! echo "$cmd" | grep -qE '^[[:space:]]*(cd|\.|source|\./|if|then|else|fi|for|while|do|done|case|esac|exit|return|break|continue|:|\[[[:space:]]|test|\[\[|\]\]|true|false|export|unset|readonly|local|declare|typeset|alias|unalias|set|unset|shift|wait|trap|umask|ulimit|exec|eval|command|builtin|let|read|printf|[A-Za-z0-9_]+[[:space:]]*[=:]|&&|\|\||;|\(|\)|\{|\})'; then
        findings="${findings}PATH_HIJACKING: Command uses relative path\n"
      fi
    fi

    # Check for command injection vectors
    # Attack: Using unquoted variables or command substitution that can be injected
    # Example: echo $VAR or echo $(command)
    if echo "$cmd" | grep -qE '\$\{?[A-Za-z0-9_]|\$\(|`'; then
      findings="${findings}COMMAND_INJECTION: Command uses unquoted variables or command substitution\n"
    fi

    # Check for overly permissive commands
    # Attack: Commands that can be used to escalate privileges
    # Example: chmod 777, chown root, etc.
    if echo "$cmd" | grep -qE '\b(chmod\s+[0-7]{3,4}|chown\s+root|chgrp\s+root|sudo|su |pkexec)\b'; then
      findings="${findings}PERMISSIVE_COMMAND: Command modifies permissions or uses privilege escalation tools\n"
    fi

    # If any findings, print them
    if [ -n "$findings" ]; then
      echo "Potential privilege escalation in cron job:"
      echo "  └─ File: $file"
      echo "  └─ Command: $cmd"
      if [ -n "$bin" ]; then
        echo "  └─ Binary: $bin"
      fi
      echo "  └─ Findings:"
      echo "$findings" | while read -r finding; do
        [ -n "$finding" ] && echo "     * $finding"
      done
    fi
  }

  # Check system crontabs
  #echo "Checking system crontabs..."
  #for crontab in /etc/cron.d/* /etc/cron.daily/* /etc/cron.hourly/* /etc/cron.monthly/* /etc/cron.weekly/* /var/spool/cron/crontabs/* /etc/at* /etc/anacrontab /etc/incron.d/* /var/spool/incron/*; do
  #  [ ! -f "$crontab" ] && continue
  #  [ ! -r "$crontab" ] && continue

  #  # Check if the file is writable
  #  if [ -w "$crontab" ]; then
  #    echo "Writable cron file: $crontab"
  #  fi

  #  # Check each line for privilege escalation vectors
  #  while IFS= read -r line || [ -n "$line" ]; do
  #    # Skip comments and empty lines
  #    case "$line" in
  #      \#*|"") continue ;;
  #    esac

  #    # Extract the command part (everything after the time specification)
  #    cmd=$(echo "$line" | sed -E 's/^[^ ]+ [^ ]+ [^ ]+ [^ ]+ [^ ]+ //')
  #    [ -z "$cmd" ] && continue

  #    check_privesc_vectors "$cmd" "$crontab"
  #  done < "$crontab"
  #done

  # Check user crontabs
  #echo "Checking user crontabs..."
  #if command -v crontab >/dev/null 2>&1; then
  #  # Check current user's crontab
  #  crontab -l 2>/dev/null | while IFS= read -r line || [ -n "$line" ]; do
  #    case "$line" in
  #      \#*|"") continue ;;
  #    esac
  #    cmd=$(echo "$line" | sed -E 's/^[^ ]+ [^ ]+ [^ ]+ [^ ]+ [^ ]+ //')
  #    [ -z "$cmd" ] && continue
  #    check_privesc_vectors "$cmd" "current user crontab"
  #  done

  #  # Check other users' crontabs if accessible
  #  for user_crontab in /var/spool/cron/crontabs/*; do
  #    [ ! -f "$user_crontab" ] && continue
  #    [ ! -r "$user_crontab" ] && continue
  #    username=$(basename "$user_crontab")
  #    [ "$username" = "$USER" ] && continue
      
  #    echo "Found crontab for user: $username"
  #    while IFS= read -r line || [ -n "$line" ]; do
  #      case "$line" in
  #        \#*|"") continue ;;
  #      esac
  #      cmd=$(echo "$line" | sed -E 's/^[^ ]+ [^ ]+ [^ ]+ [^ ]+ [^ ]+ //')
  #      [ -z "$cmd" ] && continue
  #      check_privesc_vectors "$cmd" "$user_crontab"
  #    done < "$user_crontab"
  #  done
  #else
  #  echo_not_found "crontab"
  #fi

  # Check for writable cron directories
  echo "Checking cron directories..."
  for cron_dir in /etc/cron.d /etc/cron.daily /etc/cron.hourly /etc/cron.monthly /etc/cron.weekly /var/spool/cron/crontabs /usr/lib/cron/tabs /private/var/at/jobs /var/at/tabs /etc/periodic; do
    [ ! -d "$cron_dir" ] && continue
    if [ -w "$cron_dir" ]; then
      echo "Writable cron directory: $cron_dir"
    fi
  done

  # Check for at jobs
  #if command -v atq >/dev/null 2>&1; then
  #  echo "Checking at jobs..."
  #  atq 2>/dev/null | while IFS= read -r line || [ -n "$line" ]; do
  #    [ -z "$line" ] && continue
  #    job_id=$(echo "$line" | awk '{print $1}')
  #    [ -z "$job_id" ] && continue
  #    at -c "$job_id" 2>/dev/null | while IFS= read -r cmd || [ -n "$cmd" ]; do
  #      case "$cmd" in
  #        \#*|"") continue ;;
  #      esac
  #      check_privesc_vectors "$cmd" "at job $job_id"
  #    done
  #  done
  #fi

  # Check for incron jobs
  #if command -v incrontab >/dev/null 2>&1; then
  #  echo "Checking incron jobs..."
  #  incrontab -l 2>/dev/null | while IFS= read -r line || [ -n "$line" ]; do
  #    case "$line" in
  #      \#*|"") continue ;;
  #    esac
  #    cmd=$(echo "$line" | awk '{print $3}')
  #    [ -z "$cmd" ] && continue
  #    check_privesc_vectors "$cmd" "incron job"
  #  done
  #fi
else
  print_2title "Cron jobs"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#scheduledcron-jobs"
  find "$SEARCH_IN_FOLDER" '(' -type d -or -type f ')' '(' -name "cron*" -or -name "anacron" -or -name "anacrontab" -or -name "incron.d" -or -name "incron" -or -name "at" -or -name "periodic" ')' -exec echo {} \; -exec ls -lR {} \;
fi
echo ""