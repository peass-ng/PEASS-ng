# Title: Processes & Cron & Services & Timers - System Timers
# ID: PR_System_timers
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: System Timers and privilege escalation vectors
# License: GNU GPL
# Version: 1.2
# Functions Used: echo_not_found, print_2title, print_info, print_3title
# Global Variables: $SEARCH_IN_FOLDER, $timersG
# Initial Functions:
# Generated Global Variables: $timer_unit, $timer_path, $timer_content, $exec_path, $timer_file, $line, $findings, $unit_path, $finding, $service_unit, $timer, $target_unit, $target_file
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "System timers"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#timers"

  # Function to check timer content for privilege escalation vectors
  check_timer_content() {
    local timer="$1"
    local findings=""
    
    # Get the service unit this timer activates
    local service_unit=$(systemctl show "$timer" -p Unit 2>/dev/null | cut -d= -f2)
    if [ -n "$service_unit" ]; then
      # Check if the service runs with elevated privileges
      if systemctl show "$service_unit" -p User 2>/dev/null | grep -q "root"; then
        findings="${findings}RUNS_AS_ROOT: Service runs as root\n"
      fi

      # Get the executable path
      local exec_path=$(systemctl show "$service_unit" -p ExecStart 2>/dev/null | cut -d= -f2 | cut -d' ' -f1)
      if [ -n "$exec_path" ]; then
        if [ -w "$exec_path" ]; then
          findings="${findings}WRITABLE_EXEC: Executable is writable: $exec_path\n"
        fi
        # Check for relative paths
        case "$exec_path" in
          /*) : ;; # Absolute path, do nothing
          *) findings="${findings}RELATIVE_PATH: Uses relative path: $exec_path\n" ;;
        esac
      fi

      # Check for unsafe configurations
      if systemctl show "$service_unit" -p ExecStart 2>/dev/null | grep -qE '(chmod|chown|mount|sudo|su)'; then
        findings="${findings}UNSAFE_CMD: Uses potentially dangerous commands\n"
      fi

      # Check for weak permissions
      if [ -e "$exec_path" ] && [ "$(stat -c %a "$exec_path" 2>/dev/null)" = "777" ]; then
        findings="${findings}WEAK_PERMS: Executable has 777 permissions\n"
      fi
    fi

    # If any findings, print them
    if [ -n "$findings" ]; then
      echo "Potential privilege escalation in timer: $timer"
      echo "$findings" | while read -r finding; do
        [ -n "$finding" ] && echo "  └─ $finding"
      done
    fi
  }

  # Function to check timer file for privilege escalation vectors
  check_timer_file() {
    local timer_file="$1"
    local findings=""

    # Check if timer file is writable (following symlinks)
    if [ -L "$timer_file" ]; then
      # If it's a symlink, check the target file
      local target_file=$(readlink -f "$timer_file")
      if [ -w "$target_file" ]; then
        findings="${findings}WRITABLE_FILE: Timer target file is writable: $target_file\n"
      fi
    elif [ -w "$timer_file" ]; then
      findings="${findings}WRITABLE_FILE: Timer file is writable\n"
    fi

    # Check for weak permissions (following symlinks)
    if [ "$(stat -L -c %a "$timer_file" 2>/dev/null)" = "777" ]; then
      findings="${findings}WEAK_PERMS: Timer file has 777 permissions\n"
    fi

    # Check for relative paths in Unit directive
    if grep -q "^Unit=[^/]" "$timer_file" 2>/dev/null; then
      findings="${findings}RELATIVE_PATH: Uses relative path in Unit directive\n"
    fi

    # Check for writable executables in Unit directive (following symlinks)
    local unit_path=$(grep -Po '^Unit=*(.*?$)' "$timer_file" 2>/dev/null | cut -d '=' -f2)
    if [ -n "$unit_path" ]; then
      if [ -L "$unit_path" ]; then
        local target_unit=$(readlink -f "$unit_path")
        if [ -w "$target_unit" ]; then
          findings="${findings}WRITABLE_UNIT: Unit target file is writable: $target_unit\n"
        fi
      elif [ -w "$unit_path" ]; then
        findings="${findings}WRITABLE_UNIT: Unit file is writable: $unit_path\n"
      fi
    fi

    # If any findings, print them
    if [ -n "$findings" ]; then
      echo "Potential privilege escalation in timer file: $timer_file"
      echo "$findings" | while read -r finding; do
        [ -n "$finding" ] && echo "  └─ $finding"
      done
    fi
  }

  # List all timers and check for privilege escalation vectors
  print_3title "Active timers:"
  systemctl list-timers --all 2>/dev/null | grep -Ev "(^$|timers listed)" | while read -r line; do
    # Extract timer unit name
    timer_unit=$(echo "$line" | awk '{print $1}')
    if [ -n "$timer_unit" ]; then
      # Check if timer file is writable
      timer_path=$(systemctl show "$timer_unit" -p FragmentPath 2>/dev/null | cut -d= -f2)
      if [ -n "$timer_path" ]; then
        check_timer_file "$timer_path"
      fi

      # Check timer content for privilege escalation vectors
      check_timer_content "$timer_unit"

      # Print the timer line with highlighting
      echo "$line" | sed -${E} "s,$timersG,${SED_GREEN},"
    fi
  done || echo_not_found

  # Check for disabled but available timers
  print_3title "Disabled timers:"
  systemctl list-unit-files --type=timer --state=disabled 2>/dev/null | grep -v "UNIT FILE" | while read -r line; do
    timer_unit=$(echo "$line" | awk '{print $1}')
    if [ -n "$timer_unit" ]; then
      timer_path=$(systemctl show "$timer_unit" -p FragmentPath 2>/dev/null | cut -d= -f2)
      if [ -n "$timer_path" ]; then
        check_timer_file "$timer_path"
      fi
    fi
  done || echo_not_found

  # Check timer files from PSTORAGE_TIMER
  if [ -n "$PSTORAGE_TIMER" ]; then
    print_3title "Additional timer files:"
    printf "%s\n" "$PSTORAGE_TIMER" | while read -r timer_file; do
      if [ -n "$timer_file" ] && [ -e "$timer_file" ]; then
        check_timer_file "$timer_file"
      fi
    done
  fi

  echo ""
fi