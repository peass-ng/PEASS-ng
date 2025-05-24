# Title: Processes & Cron & Services & Timers - Services and Service Files
# ID: PR_Services
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: Services and service files analysis with privilege escalation vectors
# License: GNU GPL
# Version: 1.2
# Functions Used: echo_not_found, print_2title, print_info, print_3title
# Global Variables: $EXTRA_CHECKS, $SEARCH_IN_FOLDER, $IAMROOT, $WRITABLESYSTEMDPATH
# Initial Functions:
# Generated Global Variables: $service_unit, $service_path, $service_content, $finding, $findings, $service_file, $exec_path, $exec_paths, $service, $line, $target_file, $target_exec, $relpath1, $relpath2
# Fat linpeas: 0
# Small linpeas: 0

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Services and Service Files"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#services"

  # Function to check service content for privilege escalation vectors
  check_service_content() {
    local service="$1"
    local findings=""
    
    # Check if service runs with elevated privileges
    if systemctl show "$service" -p User 2>/dev/null | grep -q "root"; then
      findings="${findings}RUNS_AS_ROOT: Service runs as root\n"
    fi

    # Get the executable path and check it
    local exec_path=$(systemctl show "$service" -p ExecStart 2>/dev/null | cut -d= -f2 | cut -d' ' -f1)
    if [ -n "$exec_path" ]; then
      if [ -w "$exec_path" ]; then
        findings="${findings}WRITABLE_EXEC: Executable is writable: $exec_path\n"
      fi
      # Check for relative paths
      #case "$exec_path" in
      #  /*) : ;; # Absolute path, do nothing
      #  *) findings="${findings}RELATIVE_PATH: Uses relative path: $exec_path\n" ;;
      #esac
      # Check for weak permissions
      if [ -e "$exec_path" ] && [ "$(stat -c %a "$exec_path" 2>/dev/null)" = "777" ]; then
        findings="${findings}WEAK_PERMS: Executable has 777 permissions\n"
      fi
    fi

    # Check for unsafe configurations
    if systemctl show "$service" -p ExecStart 2>/dev/null | grep -qE '(chmod|chown|mount|sudo|su)'; then
      findings="${findings}UNSAFE_CMD: Uses potentially dangerous commands\n"
    fi

    # Check for environment variables with sensitive data
    if systemctl show "$service" -p Environment 2>/dev/null | grep -qE '(PASS|SECRET|KEY|TOKEN|CRED)'; then
      findings="${findings}SENSITIVE_ENV: Contains sensitive environment variables\n"
    fi

    # Check for capabilities
    if systemctl show "$service" -p CapabilityBoundingSet 2>/dev/null | grep -qE '(CAP_SYS_ADMIN|CAP_DAC_OVERRIDE|CAP_DAC_READ_SEARCH)'; then
      findings="${findings}DANGEROUS_CAPS: Has dangerous capabilities\n"
    fi

    # If any findings, print them
    if [ -n "$findings" ]; then
      echo "  Potential issue in service: $service"
      echo "$findings" | while read -r finding; do
        [ -n "$finding" ] && echo "  └─ $finding"
      done
    fi
  }

  # Function to check service file for privilege escalation vectors
  check_service_file() {
    local service_file="$1"
    local findings=""

    # Check if service file is writable (following symlinks)
    if [ -L "$service_file" ]; then
      # If it's a symlink, check the target file
      local target_file=$(readlink -f "$service_file")
      if ! [ "$IAMROOT" ] && [ -w "$target_file" ] && [ -f "$target_file" ] && ! [ "$SEARCH_IN_FOLDER" ]; then
        findings="${findings}WRITABLE_FILE: Service target file is writable: $target_file\n"
      fi
    elif ! [ "$IAMROOT" ] && [ -w "$service_file" ] && [ -f "$service_file" ] && ! [ "$SEARCH_IN_FOLDER" ]; then
      findings="${findings}WRITABLE_FILE: Service file is writable\n"
    fi

    # Check for weak permissions (following symlinks)
    if [ "$(stat -L -c %a "$service_file" 2>/dev/null)" = "777" ]; then
      findings="${findings}WEAK_PERMS: Service file has 777 permissions\n"
    fi

    # Check for relative paths in Exec directives - Original logic
    local relpath1=$(grep -E '^Exec.*=(?:[^/]|-[^/]|\+[^/]|![^/]|!![^/]|)[^/@\+!-].*' "$service_file" 2>/dev/null | grep -Iv "=/")
    local relpath2=$(grep -E '^Exec.*=.*/bin/[a-zA-Z0-9_]*sh ' "$service_file" 2>/dev/null)
    if [ "$relpath1" ] || [ "$relpath2" ]; then
      if [ "$WRITABLESYSTEMDPATH" ]; then
        findings="${findings}RELATIVE_PATH: Could be executing some relative path (systemd path is writable)\n"
      else
        findings="${findings}RELATIVE_PATH: Could be executing some relative path\n"
      fi
    fi

    # Check for writable executables (following symlinks)
    local exec_paths=$(grep -Eo '^Exec.*?=[!@+-]*[a-zA-Z0-9_/\-]+' "$service_file" 2>/dev/null | cut -d '=' -f2 | sed 's,^[@\+!-]*,,')
    printf "%s\n" "$exec_paths" | while read -r exec_path; do
      if [ -n "$exec_path" ]; then
        if [ -L "$exec_path" ]; then
          local target_exec=$(readlink -f "$exec_path")
          if [ -w "$target_exec" ]; then
            findings="${findings}WRITABLE_EXEC: Executable target is writable: $target_exec\n"
          fi
        elif [ -w "$exec_path" ]; then
          findings="${findings}WRITABLE_EXEC: Executable is writable: $exec_path\n"
        fi
      fi
    done

    # If any findings, print them
    if [ -n "$findings" ]; then
      echo "  Potential issue in service file: $service_file"
      echo "$findings" | while read -r finding; do
        [ -n "$finding" ] && echo "  └─ $finding"
      done
    fi
  }

  # List all services and check for privilege escalation vectors
  echo ""
  print_3title "Active services:"
  systemctl list-units --type=service --state=active 2>/dev/null | grep -v "UNIT" | while read -r line; do
    service_unit=$(echo "$line" | awk '{print $1}')
    if [ -n "$service_unit" ]; then
      # Print the service line with highlighting
      echo "$line" | sed -${E} "s,$service_unit,${SED_GREEN},"

      # Get service file path
      service_path=$(systemctl show "$service_unit" -p FragmentPath 2>/dev/null | cut -d= -f2)
      if [ -n "$service_path" ]; then
        check_service_file "$service_path"
      fi

      # Check service content for privilege escalation vectors
      check_service_content "$service_unit"
    fi
  done || echo_not_found

  # Check for disabled but available services
  echo ""
  print_3title "Disabled services:"
  systemctl list-unit-files --type=service --state=disabled 2>/dev/null | grep -v "UNIT FILE" | while read -r line; do
    service_unit=$(echo "$line" | awk '{print $1}')
    if [ -n "$service_unit" ]; then
      # Print the service line with highlighting
      echo "$line" | sed -${E} "s,$service_unit,${SED_GREEN},"

      # Get service file path
      service_path=$(systemctl show "$service_unit" -p FragmentPath 2>/dev/null | cut -d= -f2)
      if [ -n "$service_path" ]; then
        check_service_file "$service_path"
      fi

      # Check service content for privilege escalation vectors
      check_service_content "$service_unit"
    fi
  done || echo_not_found

  # Check service files from PSTORAGE_SYSTEMD
  if [ -n "$PSTORAGE_SYSTEMD" ]; then
    echo ""
    print_3title "Additional service files:"
    printf "%s\n" "$PSTORAGE_SYSTEMD" | while read -r service_file; do
      if [ -n "$service_file" ] && [ -e "$service_file" ]; then
        check_service_file "$service_file"
      fi
    done
  fi

  # Check for outdated services if EXTRA_CHECKS is enabled
  if [ "$EXTRA_CHECKS" ]; then
    echo ""
    print_3title "Service versions and status:"
    (service --status-all || service -e || chkconfig --list || rc-status || launchctl list) 2>/dev/null || echo_not_found "service|chkconfig|rc-status|launchctl"
  fi

  # Check systemd path writability
  if [ ! "$WRITABLESYSTEMDPATH" ]; then 
    echo "You can't write on systemd PATH" | sed -${E} "s,.*,${SED_GREEN},"
  else
    echo "You can write on systemd PATH" | sed -${E} "s,.*,${SED_RED},"
    echo "If a relative path is used, it's possible to abuse it."
  fi

  echo ""
fi