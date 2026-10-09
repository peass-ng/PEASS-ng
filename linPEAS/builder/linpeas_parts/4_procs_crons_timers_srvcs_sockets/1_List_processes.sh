# Title: Processes & Cron & Services & Timers - List processes
# ID: PR_List_processes
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: List running processes and check for unusual configurations
# License: GNU GPL
# Version: 1.4
# Mitre: T1057
# Functions Used: print_2title, print_info, print_ps, checkRootWritableProcessPaths
# Global Variables: $capsB, $knw_usrs, $nosh_usrs, $NOUSEPS, $processesB, $processesDump, $processesVB, $rootcommon, $SEARCH_IN_FOLDER, $sh_usrs, $USER, $Wfolders
# Initial Functions:
# Generated Global Variables: $pslist, $cpid, $caphex, $psline, $pid, $selinux_ctx, $current_env_vars, $env_findings, $apparmor_profile, $mount, $mount_findings, $fd_findings, $proc_cmd, $proc_user, $mount_point, $current_mounts, $fd_target, $var, $findings, $sec_findings, $proc_env_vars, $fd_count, $proc_mounts, $office_uno_sockets, $spice_console_endpoints, $spice_exec_field, $$escaped_var
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Running processes (cleaned)" "T1057"
  if [ "$NOUSEPS" ]; then
    printf ${BLUE}"[i]$GREEN Looks like ps is not finding processes, going to read from /proc/ and not going to monitor 1min of processes\n"$NC
  fi
  print_info "Check weird & unexpected processes run by root: https://book.hacktricks.wiki/en/linux-hardening/processes-crontab-systemd-dbus/process-enumeration-and-service-paths.html#map-processes-and-ownership"

  if [ -f "/etc/fstab" ] && cat /etc/fstab | grep -q "hidepid=2"; then
    echo "Looks like /etc/fstab has hidepid=2, so ps will not show processes of other users"
  fi

  # Get current process environment variables
  if [ -r "/proc/self/environ" ]; then
    current_env_vars=$(cat /proc/self/environ 2>/dev/null | tr '\0' '\n' | sort)
  else
    current_env_vars=$(env 2>/dev/null | sort)
  fi
  
  # Get current process mounts
  if [ -r "/proc/self/mountinfo" ]; then
    current_mounts=$(cat /proc/self/mountinfo 2>/dev/null | sort)
  else
    current_mounts=$(mount 2>/dev/null | sort)
  fi

  # Function to check for unusual environment variables
  check_env_vars() {
    local pid="$1"
    local proc_user="$2"
    local proc_cmd="$3"
    local findings=""
    
    # Skip if we can't read the environment
    [ ! -r "/proc/$pid/environ" ] && return
    
    # Get process environment variables
    proc_env_vars=$(cat "/proc/$pid/environ" 2>/dev/null | tr '\0' '\n' | sort)
    [ -z "$proc_env_vars" ] && return

    # Find environment variables that the target process has but we don't
    if [ -n "$current_env_vars" ]; then
      echo "$proc_env_vars" | while read -r var; do
        if [ -n "$var" ]; then
          # Escape special regex characters in var
          escaped_var=$(echo "$var" | sed 's/[][^$.*+?(){}|]/\\&/g')
          if ! echo "$current_env_vars" | grep -q "^$escaped_var$"; then
            if [ -z "$findings" ]; then
              findings="Has additional environment variables:"
            fi
            findings="$findings\n  └─ $var"
          fi
        fi
      done
    else
      # If we can't get current env vars, just show all process env vars
      findings="Has environment variables:"
      echo "$proc_env_vars" | while read -r var; do
        if [ -n "$var" ]; then
          findings="$findings\n  └─ $var"
        fi
      done
    fi

    # Return findings if any
    if [ -n "$findings" ]; then
      echo "$findings"
    fi
  }

  # Function to check for unusual security contexts
  check_security_context() {
    local pid="$1"
    local proc_user="$2"
    local proc_cmd="$3"
    local findings=""
    
    # Check SELinux context
    if [ -r "/proc/$pid/attr/current" ]; then
      selinux_ctx=$(cat "/proc/$pid/attr/current" 2>/dev/null)
      if [ -n "$selinux_ctx" ] && [ "$selinux_ctx" != "unconfined" ]; then
        findings="SELinux context: $selinux_ctx"
      fi
    fi
    
    # Check AppArmor profile
    if [ -r "/proc/$pid/attr/apparmor/current" ]; then
      apparmor_profile=$(cat "/proc/$pid/attr/apparmor/current" 2>/dev/null)
      if [ -n "$apparmor_profile" ] && [ "$apparmor_profile" != "unconfined" ]; then
        if [ -n "$findings" ]; then
          findings="$findings\n  └─ AppArmor profile: $apparmor_profile"
        else
          findings="AppArmor profile: $apparmor_profile"
        fi
      fi
    fi

    # Return findings if any
    if [ -n "$findings" ]; then
      echo "$findings"
    fi
  }

  # Function to check for unusual mount namespaces
  check_mount_namespace() {
    local pid="$1"
    local proc_user="$2"
    local proc_cmd="$3"
    local findings=""
    
    # Skip if we can't read the mountinfo
    [ ! -r "/proc/$pid/mountinfo" ] && return
    
    # Get process mounts
    proc_mounts=$(cat "/proc/$pid/mountinfo" 2>/dev/null | sort)
    [ -z "$proc_mounts" ] && return

    # Find mounts that the target process has but we don't
    if [ -n "$current_mounts" ]; then
      echo "$proc_mounts" | while read -r mount; do
        if [ -n "$mount" ] && ! echo "$current_mounts" | grep -q "^$mount$"; then
          mount_point=$(echo "$mount" | sed "s,.* - \(.*\),\1,")
          if [ -z "$findings" ]; then
            findings="Has additional mounts:"
          fi
          findings="$findings\n  └─ $mount_point"
        fi
      done
    else
      # If we can't get current mounts, just show all process mounts
      findings="Has mounts:"
      echo "$proc_mounts" | while read -r mount; do
        if [ -n "$mount" ]; then
          mount_point=$(echo "$mount" | sed "s,.* - \(.*\),\1,")
          findings="$findings\n  └─ $mount_point"
        fi
      done
    fi

    # Return findings if any
    if [ -n "$findings" ]; then
      echo "$findings"
    fi
  }

  # Function to check for unusual file descriptors
  check_file_descriptors() {
    local pid="$1"
    local proc_user="$2"
    local proc_cmd="$3"
    local findings=""
    
    # Skip if we can't read the file descriptors
    [ ! -r "/proc/$pid/fd" ] && return

    # Check for interesting file descriptors
    for fd in /proc/$pid/fd/*; do
      # Skip if fd doesn't exist or we can't access it
      [ ! -e "$fd" ] && continue
      
      # Get fd target
      fd_target=$(readlink "$fd" 2>/dev/null)
      [ -z "$fd_target" ] && continue

      # Skip if target doesn't exist
      [ ! -e "$fd_target" ] && continue

      # Check if we can access the FD but not the target file
      if [ -r "$fd" ] && [ ! -r "$fd_target" ]; then
        if [ -z "$findings" ]; then
          findings="Readable FD to unreadable file: $fd -> $fd_target"
        else
          findings="$findings\n  └─ Readable FD to unreadable file: $fd -> $fd_target"
        fi
      fi
      if [ -w "$fd" ] && [ ! -w "$fd_target" ]; then
        if [ -z "$findings" ]; then
          findings="Writable FD to unwritable file: $fd -> $fd_target"
        else
          findings="$findings\n  └─ Writable FD to unwritable file: $fd -> $fd_target"
        fi
      fi
    done

    # Check for unusual number of file descriptors
    fd_count=$(ls -1 "/proc/$pid/fd" 2>/dev/null | wc -l)
    [ -z "$fd_count" ] && return

    # If process has more than 100 file descriptors, it might be interesting
    if [ "$fd_count" -gt 100 ]; then
      if [ -z "$findings" ]; then
        findings="Unusual number of FDs: $fd_count"
      else
        findings="$findings\n  └─ Unusual number of FDs: $fd_count"
      fi
    fi

    # Return findings if any
    if [ -n "$findings" ]; then
      echo "$findings"
    fi
  }

  # Inspect the already collected process list; do not connect to UNO or probe ports.
  check_privileged_office_uno_sockets() {
    [ -n "$pslist" ] || return
    printf '%s\n' "$pslist" | awk '
      NR > 5000 { exit }
      length($0) > 4096 { next }
      $1 == "root" &&
      /(^|[[:space:]\/])(soffice(\.bin)?|libreoffice|openoffice)([[:space:]]|$)/ &&
      /--accept=/ {
        if (!match($0, /--accept=[^[:space:]]+/)) next
        accept = substr($0, RSTART, RLENGTH)
        sub(/^--accept=/, "", accept)
        sub(/^"/, "", accept)
        sub(/"$/, "", accept)
        if (accept !~ /^socket,host=/ || accept !~ /;urp(;|$)/) next
        if (!match(accept, /host=[^,;[:space:]]+/)) next
        host = substr(accept, RSTART + 5, RLENGTH - 5)
        if (!match(accept, /port=[0-9]+/)) next
        port = substr(accept, RSTART + 5, RLENGTH - 5)
        if (port + 0 < 1 || port + 0 > 65535) next
        endpoint = host ":" port
        if (!seen[endpoint]++) {
          print endpoint
          if (++count >= 5) exit
        }
      }'
  }

  # Reuse the process snapshot; a loopback SPICE option is a guest-console
  # lead, not evidence of a live listener or privilege on the host.
  check_unauthenticated_spice_console() {
    [ -n "$pslist" ] || return
    # The two cached snapshots have different executable columns. Select the
    # actual producer format so argument text cannot impersonate an executable.
    if [ -n "$NOUSEPS" ]; then spice_exec_field=3; else spice_exec_field=11; fi
    printf '%s\n' "$pslist" | awk -v exec_field="$spice_exec_field" '
      NR > 5000 { exit }
      length($0) > 8192 || NF <= exec_field { next }
      {
        if ($(exec_field) !~ /(^|\/)qemu-system-[A-Za-z0-9_]+$/) next
        first_arg = exec_field + 1
        for (i = first_arg; i < NF && i < first_arg + 116; i++) {
          if ($i != "-spice") continue
          spec = $(i + 1)
          gsub(/^"|"$/, "", spec)
          if (length(spec) > 512) continue
          n = split(spec, fields, ",")
          if (n > 32) continue
          addr = port = ""
          noauth = conflicting_auth = 0
          for (j = 1; j <= n; j++) {
            if (fields[j] == "disable-ticketing" || fields[j] == "disable-ticketing=on") noauth = 1
            else if (fields[j] ~ /^disable-ticketing=/ || fields[j] == "sasl=on" ||
                     fields[j] ~ /^password-secret=/ || fields[j] ~ /^x509-/) conflicting_auth = 1
            else if (fields[j] ~ /^addr=/) addr = substr(fields[j], 6)
            else if (fields[j] ~ /^port=/) port = substr(fields[j], 6)
          }
          if (!noauth || conflicting_auth || (addr != "127.0.0.1" && addr != "::1")) continue
          if (port !~ /^[0-9]+$/ || port + 0 < 1 || port + 0 > 65535) continue
          endpoint = (addr == "::1" ? "[::1]" : addr) ":" (port + 0)
          if (!seen[endpoint]++) {
            print endpoint
            if (++count >= 5) exit
          }
        }
      }'
  }

  if [ "$NOUSEPS" ]; then
    print_ps | grep -v 'sed-Es' | sed -${E} "s,$Wfolders,${SED_RED},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$rootcommon,${SED_GREEN}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED}," | sed -${E} "s,$processesVB,${SED_RED_YELLOW},g" | sed "s,$processesB,${SED_RED}," | sed -${E} "s,$processesDump,${SED_RED},"
    pslist=$(print_ps)
  else
    (ps fauxwww || ps auxwww | sort ) 2>/dev/null | grep -v "\[" | grep -v "%CPU" | while read psline; do
      echo "$psline"  | sed -${E} "s,$Wfolders,${SED_RED},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$rootcommon,${SED_GREEN}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed "s,root,${SED_RED}," | sed -${E} "s,$processesVB,${SED_RED_YELLOW},g" | sed "s,$processesB,${SED_RED}," | sed -${E} "s,$processesDump,${SED_RED},"
      if [ "$(command -v capsh || echo -n '')" ] && ! echo "$psline" | grep -q "root"; then
        cpid=$(echo "$psline" | awk '{print $2}')
        caphex=0x"$(cat /proc/$cpid/status 2> /dev/null | grep CapEff | awk '{print $2}')"
        if [ "$caphex" ] && [ "$caphex" != "0x" ] && echo "$caphex" | grep -qv '0x0000000000000000'; then
          printf "  └─(${DG}Caps${NC}) "; capsh --decode=$caphex 2>/dev/null | grep -v "WARNING:" | sed -${E} "s,$capsB,${SED_RED},g"
        fi
      fi
    done
    pslist=$(ps auxwww)
    echo ""
  fi

  office_uno_sockets=$(check_privileged_office_uno_sockets)
  if [ -n "$office_uno_sockets" ]; then
    print_2title "Privileged office automation sockets" "T1068"
    printf 'Root-owned process command advertises a LibreOffice/OpenOffice UNO acceptor at:\n%s\n' "$office_uno_sockets"
    print_info "Confirm the listener and local access; a reachable UNO API can act with the office process identity: https://book.hacktricks.wiki/en/linux-hardening/processes-crontab-systemd-dbus/process-enumeration-and-service-paths.html#privileged-office-automation-sockets"
  fi

  spice_console_endpoints=$(check_unauthenticated_spice_console)
  if [ -n "$spice_console_endpoints" ]; then
    print_2title "Loopback guest-console review candidates" "T1057"
    printf 'QEMU command line advertises SPICE without ticket authentication at:\n%s\n' "$spice_console_endpoints"
    print_info "Verify the live listener, local access, and guest login/boot policy. Guest-console control is not host-root access: https://book.hacktricks.wiki/en/linux-hardening/processes-crontab-systemd-dbus/process-enumeration-and-service-paths.html"
  fi

  checkRootWritableProcessPaths

  # Additional checks for each process
  print_2title "Processes with unusual configurations" "T1057"
  for pid in $(find /proc -maxdepth 1 -regex '/proc/[0-9]+' -printf "%f\n" 2>/dev/null); do
    # Skip if process doesn't exist or we can't access it
    [ ! -d "/proc/$pid" ] && continue
    
    # Get process user and command
    proc_user=$(stat -c '%U' "/proc/$pid" 2>/dev/null)
    proc_cmd=$(cat "/proc/$pid/cmdline" 2>/dev/null | tr '\0' ' ' | head -c 100)
    [ -z "$proc_user" ] || [ -z "$proc_cmd" ] && continue
    
    # Run all checks and collect findings
    sec_findings=$(check_security_context "$pid" "$proc_user" "$proc_cmd")
    mount_findings=$(check_mount_namespace "$pid" "$proc_user" "$proc_cmd")
    fd_findings=$(check_file_descriptors "$pid" "$proc_user" "$proc_cmd")
    env_findings=$(check_env_vars "$pid" "$proc_user" "$proc_cmd")

    # If any findings exist, print process info and findings
    if [ -n "$env_findings" ] || [ -n "$sec_findings" ] || [ -n "$mount_findings" ] || [ -n "$fd_findings" ]; then
      echo "Process $pid ($proc_user) - $proc_cmd"
      [ -n "$env_findings" ] && echo "$env_findings"
      [ -n "$sec_findings" ] && echo "$sec_findings"
      [ -n "$mount_findings" ] && echo "$mount_findings"
      [ -n "$fd_findings" ] && echo "$fd_findings"
      echo ""
    fi
  done

  echo ""
fi
