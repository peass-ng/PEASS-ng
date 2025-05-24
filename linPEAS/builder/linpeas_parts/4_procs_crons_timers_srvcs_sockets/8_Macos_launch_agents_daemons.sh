# Title: Processes & Cron & Services & Timers - Third party LaunchAgents & LaunchDemons
# ID: PR_Macos_launch_agents_daemons
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: Third party LaunchAgents & LaunchDemons and privilege escalation vectors
# License: GNU GPL
# Version: 1.1
# Functions Used: print_2title, print_info
# Global Variables: $MACPEAS, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $program, $plist_content, $binary_path, $periodic_dir, $workdir, $startup_dir, $line, $emond_script, $startup_item, $finding, $location, $findings, $login_item, $plist, $periodic_script, $plist_dir
# Fat linpeas: 0
# Small linpeas: 0

if ! [ "$SEARCH_IN_FOLDER" ]; then
  if [ "$MACPEAS" ]; then
    print_2title "Third party LaunchAgents & LaunchDemons"
    print_info "https://book.hacktricks.wiki/en/macos-hardening/macos-auto-start-locations.html#launchd"
    print_info "Checking for privilege escalation vectors in LaunchAgents & LaunchDaemons:"
    print_info "1. Writable plist files"
    print_info "2. Writable program binaries"
    print_info "3. Environment variables with sensitive data"
    print_info "4. Unsafe program arguments"
    print_info "5. RunAtLoad with elevated privileges"
    print_info "6. KeepAlive with elevated privileges"

    # Function to check plist content for privilege escalation vectors
    check_plist_content() {
      local plist="$1"
      local findings=""
      
      # Check for environment variables
      if defaults read "$plist" EnvironmentVariables 2>/dev/null | grep -qE '(PASS|SECRET|KEY|TOKEN|CRED)'; then
        findings="${findings}ENV_VARS: Contains sensitive environment variables\n"
      fi

      # Check for RunAtLoad with elevated privileges
      if defaults read "$plist" RunAtLoad 2>/dev/null | grep -q "true"; then
        if [ -w "$plist" ]; then
          findings="${findings}RUN_AT_LOAD: Runs at load and plist is writable\n"
        fi
      fi

      # Check for KeepAlive with elevated privileges
      if defaults read "$plist" KeepAlive 2>/dev/null | grep -q "true"; then
        if [ -w "$plist" ]; then
          findings="${findings}KEEP_ALIVE: Keeps running and plist is writable\n"
        fi
      fi

      # Check for unsafe program arguments
      if defaults read "$plist" ProgramArguments 2>/dev/null | grep -qE '(sudo|su|chmod|chown|chroot|mount)'; then
        findings="${findings}UNSAFE_ARGS: Uses potentially dangerous program arguments\n"
      fi

      # Check for writable working directory
      if defaults read "$plist" WorkingDirectory 2>/dev/null | grep -qE '^/'; then
        local workdir=$(defaults read "$plist" WorkingDirectory 2>/dev/null)
        if [ -w "$workdir" ]; then
          findings="${findings}WRITABLE_WORKDIR: Working directory is writable\n"
        fi
      fi

      # If any findings, print them
      if [ -n "$findings" ]; then
        echo "Potential privilege escalation in: $plist"
        echo "$findings" | while read -r finding; do
          [ -n "$finding" ] && echo "  └─ $finding"
        done
      fi
    }

    # Check system and user LaunchAgents & LaunchDaemons
    for plist_dir in /Library/LaunchAgents/ /Library/LaunchDaemons/ ~/Library/LaunchAgents/ ~/Library/LaunchDaemons/ /System/Library/LaunchAgents/ /System/Library/LaunchDaemons/; do
      [ ! -d "$plist_dir" ] && continue
      
      echo "Checking $plist_dir..."
      find "$plist_dir" -name "*.plist" 2>/dev/null | while read -r plist; do
        # Check if plist is writable
        if [ -w "$plist" ]; then
          echo "Writable plist: $plist" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        fi

        # Get program path
        program=""
        program=$(defaults read "$plist" Program 2>/dev/null)
        if ! [ "$program" ]; then
          program=$(defaults read "$plist" ProgramArguments 2>/dev/null | grep -Ev "^\(|^\)" | cut -d '"' -f 2)
        fi

        # Check if program is writable
        if [ -n "$program" ] && [ -w "$program" ]; then
          echo "Writable program: $program" | sed -${E} "s,.*,${SED_RED_YELLOW},"
          ls -l "$program" 2>/dev/null
        fi

        # Check plist content for privilege escalation vectors
        check_plist_content "$plist"
      done
    done
    echo ""

    print_2title "StartupItems"
    print_info "https://book.hacktricks.wiki/en/macos-hardening/macos-auto-start-locations.html#startup-items"
    for startup_dir in /Library/StartupItems/ /System/Library/StartupItems/; do
      [ ! -d "$startup_dir" ] && continue
      echo "Checking $startup_dir..."
      find "$startup_dir" -type f -executable 2>/dev/null | while read -r startup_item; do
        if [ -w "$startup_item" ]; then
          echo "Writable startup item: $startup_item" | sed -${E} "s,.*,${SED_RED_YELLOW},"
          ls -l "$startup_item" 2>/dev/null
        fi
      done
    done
    echo ""

    print_2title "Login Items"
    print_info "https://book.hacktricks.wiki/en/macos-hardening/macos-auto-start-locations.html#startup-items"
    osascript -e 'tell application "System Events" to get the name of every login item' 2>/dev/null | tr ", " "\n" | while read -r login_item; do
      if [ -n "$login_item" ]; then
        # Try to find the actual binary
        binary_path=$(mdfind "kMDItemDisplayName == '$login_item'" 2>/dev/null | head -n 1)
        if [ -n "$binary_path" ] && [ -w "$binary_path" ]; then
          echo "Writable login item binary: $binary_path" | sed -${E} "s,.*,${SED_RED_YELLOW},"
          ls -l "$binary_path" 2>/dev/null
        fi
      fi
    done
    echo ""

    print_2title "SPStartupItemDataType"
    system_profiler SPStartupItemDataType 2>/dev/null | while read -r line; do
      if echo "$line" | grep -q "Location:"; then
        location=$(echo "$line" | cut -d: -f2- | xargs)
        if [ -w "$location" ]; then
          echo "Writable startup item location: $location" | sed -${E} "s,.*,${SED_RED_YELLOW},"
          ls -l "$location" 2>/dev/null
        fi
      fi
    done
    echo ""

    print_2title "Emond scripts"
    print_info "https://book.hacktricks.wiki/en/macos-hardening/macos-auto-start-locations.html#emond"
    if [ -d "/private/var/db/emondClients" ]; then
      find "/private/var/db/emondClients" -type f 2>/dev/null | while read -r emond_script; do
        if [ -w "$emond_script" ]; then
          echo "Writable emond script: $emond_script" | sed -${E} "s,.*,${SED_RED_YELLOW},"
          ls -l "$emond_script" 2>/dev/null
        fi
      done
    fi
    echo ""

    print_2title "Periodic tasks"
    print_info "Checking periodic tasks for privilege escalation vectors"
    for periodic_dir in /etc/periodic/daily /etc/periodic/weekly /etc/periodic/monthly; do
      [ ! -d "$periodic_dir" ] && continue
      echo "Checking $periodic_dir..."
      find "$periodic_dir" -type f -executable 2>/dev/null | while read -r periodic_script; do
        if [ -w "$periodic_script" ]; then
          echo "Writable periodic script: $periodic_script" | sed -${E} "s,.*,${SED_RED_YELLOW},"
          ls -l "$periodic_script" 2>/dev/null
        fi
      done
    done
    echo ""
  fi
fi