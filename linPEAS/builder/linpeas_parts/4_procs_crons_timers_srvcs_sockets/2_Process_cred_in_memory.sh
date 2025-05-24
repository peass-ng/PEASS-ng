# Title: Processes & Cron & Services & Timers - Processes with credentials inside memory
# ID: PR_Process_cred_in_memory
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: Processes with credentials inside memory and memory-mapped files
# License: GNU GPL
# Version: 1.2
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $pslist, $SEARCH_IN_FOLDER, $processesDump, $nosh_usrs, $processesB, $knw_usrs, $rootcommon, $sh_usrs, $processesVB
# Initial Functions:
# Generated Global Variables: $line, $cred_files, $filename, $fd_target, $found_cred_files, $proc, $proc_cmd, $pid, $proc_user, $cred_processes, $seen_files
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Processes with credentials in memory (root req)"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#credentials-from-process-memory"

  # Common credential-storing processes
  cred_processes="gdm-password gnome-keyring-daemon lightdm vsftpd apache2 sshd: mysql postgres redis-server mongod memcached elasticsearch jenkins tomcat nginx php-fpm supervisord vncserver xrdp teamviewer"

  # Check for credential-storing processes
  for proc in $cred_processes; do
    if echo "$pslist" | grep -q "$proc"; then
      echo "$proc process found (dump creds from memory as root)" | sed "s,$proc,${SED_RED},"
    else
      echo_not_found "$proc"
    fi
  done

  # Check for processes with open handles to credential files
  echo ""
  print_2title "Opened Files by processes"
  for pid in $(find /proc -maxdepth 1 -regex '/proc/[0-9]+' -printf "%f\n" 2>/dev/null); do
    # Skip if process doesn't exist or we can't access it
    [ ! -d "/proc/$pid" ] && continue
    [ ! -r "/proc/$pid/fd" ] && continue

    # Get process user and command
    proc_user=$(stat -c '%U' "/proc/$pid" 2>/dev/null)
    proc_cmd=$(cat "/proc/$pid/cmdline" 2>/dev/null | tr '\0' ' ' | head -c 100)
    [ -z "$proc_user" ] || [ -z "$proc_cmd" ] && continue
    
    # Skip processes that start with "sed " or contain "linpeas.sh"
    echo "$proc_cmd" | grep -q "^sed " && continue
    echo "$proc_cmd" | grep -q "linpeas.sh" && continue

    # Variable to store unique files for this process
    seen_files=""
    found_cred_files=""
    
    # Check for open credential files
    for fd in /proc/$pid/fd/*; do
      [ ! -e "$fd" ] && continue
      fd_target=$(readlink "$fd" 2>/dev/null)
      [ -z "$fd_target" ] && continue
      [ "$fd_target" = "/dev/null" ] && continue
      echo "$fd_target" | grep -q "^socket:" && continue
      echo "$fd_target" | grep -q "^anon_inode:" && continue

      # Only add if not already seen (using case to check)
      case " $seen_files " in
        *" $fd_target "*) continue ;;
        *)
          seen_files="$seen_files $fd_target"
          if [ -z "$found_cred_files" ]; then
            echo "Process $pid ($proc_user) - $proc_cmd"
            echo "  └─ Has open files:"
            found_cred_files="yes"
          fi
          echo "    └─ $fd_target"
          ;;
      esac
    done
  done | sed -${E} "s,\.(pem|key|cred|db|sqlite|conf|cnf|ini|env|secret|token|auth|passwd|shadow)$,\1${SED_RED},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$rootcommon,${SED_GREEN}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED}," | sed -${E} "s,$processesVB,${SED_RED_YELLOW},g" | sed "s,$processesB,${SED_RED}," | sed -${E} "s,$processesDump,${SED_RED},"

  # Check for processes with memory-mapped files that might contain credentials
  echo ""
  print_2title "Processes with memory-mapped credential files"
  for pid in $(find /proc -maxdepth 1 -regex '/proc/[0-9]+' -printf "%f\n" 2>/dev/null); do
    # Skip if process doesn't exist or we can't access it
    [ ! -d "/proc/$pid" ] && continue
    [ ! -r "/proc/$pid/maps" ] && continue

    # Get process user and command
    proc_user=$(stat -c '%U' "/proc/$pid" 2>/dev/null)
    proc_cmd=$(cat "/proc/$pid/cmdline" 2>/dev/null | tr '\0' ' ' | head -c 100)
    [ -z "$proc_user" ] || [ -z "$proc_cmd" ] && continue

    # Check for memory-mapped files that might contain credentials
    cred_files=$(grep -E '\.(pem|key|cred|db|sqlite|conf|cnf|ini|env|secret|token|auth|passwd|shadow)$' "/proc/$pid/maps" 2>/dev/null)
    if [ -n "$cred_files" ]; then
      echo "Process $pid ($proc_user) - $proc_cmd"
      echo "  └─ Has memory-mapped credential files:"
      echo "$cred_files" | while read -r line; do
        filename=$(echo "$line" | sed "s,.*/\(.*\),\1,")
        echo "    └─ $filename"
      done
    fi
  done

  echo ""
fi