# Title: Processes & Cron & Services & Timers - Socket Files Analysis
# ID: PR_Socket_files
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: Analyze .socket files for privilege escalation vectors:
#   - Writable socket files
#   - Socket files executing writable binaries
#   - Socket files with writable listeners
#   - Socket files with relative paths
#   - Socket files with unsafe configurations
# License: GNU GPL
# Version: 1.2
# Functions Used: print_2title, print_info, print_list
# Global Variables: $IAMROOT, $SEARCH_IN_FOLDER, $SED_RED, $SED_RED_YELLOW, $NC
# Initial Functions:
# Generated Global Variables: $exec_path, $listen_path, $path, $exec_paths, $finding, $listen_paths, $socket_file, $findings, $target_file, $target_listen, $target_exec, $lpath
# Fat linpeas: 0
# Small linpeas: 0

if ! [ "$IAMROOT" ]; then
    print_2title "Analyzing .socket files"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#sockets"

    # Function to check if path is relative
    is_relative_path() {
        local lpath="$1"
        case "$lpath" in
            /*) return 1 ;; # Absolute path
            *) return 0 ;;  # Relative path
        esac
    }

    # Function to check socket file content
    check_socket_file() {
        local socket_file="$1"
        local findings=""

        # Check if socket file is writable (following symlinks)
        if [ -L "$socket_file" ]; then
            # If it's a symlink, check the target file
            local target_file=$(readlink -f "$socket_file")
            if ! [ "$IAMROOT" ] && [ -w "$target_file" ] && [ -f "$target_file" ] && ! [ "$SEARCH_IN_FOLDER" ]; then
                findings="${findings}WRITABLE_FILE: Socket target file is writable: $target_file\n"
            fi
        elif ! [ "$IAMROOT" ] && [ -w "$socket_file" ] && [ -f "$socket_file" ] && ! [ "$SEARCH_IN_FOLDER" ]; then
            findings="${findings}WRITABLE_FILE: Socket file is writable\n"
        fi

        # Check for weak permissions (following symlinks)
        if [ "$(stat -L -c %a "$socket_file" 2>/dev/null)" = "777" ]; then
            findings="${findings}WEAK_PERMS: Socket file has 777 permissions\n"
        fi

        # Check for executables (following symlinks)
        local exec_paths=$(grep -Eo '^(Exec).*?=[!@+-]*/[a-zA-Z0-9_/\-]+' "$socket_file" 2>/dev/null | cut -d '=' -f2 | sed 's,^[@\+!-]*,,')
        printf "%s\n" "$exec_paths" | while read -r exec_path; do
            if [ -n "$exec_path" ]; then
                # Check if executable is writable (following symlinks)
                if [ -L "$exec_path" ]; then
                    local target_exec=$(readlink -f "$exec_path")
                    if [ -w "$target_exec" ]; then
                        findings="${findings}WRITABLE_EXEC: Executable target is writable: $target_exec\n"
                    fi
                    # Check for weak permissions on target
                    if [ -e "$target_exec" ] && [ "$(stat -L -c %a "$target_exec" 2>/dev/null)" = "777" ]; then
                        findings="${findings}WEAK_EXEC_PERMS: Executable target has 777 permissions: $target_exec\n"
                    fi
                else
                    if [ -w "$exec_path" ]; then
                        findings="${findings}WRITABLE_EXEC: Executable is writable: $exec_path\n"
                    fi
                    # Check for weak permissions
                    if [ -e "$exec_path" ] && [ "$(stat -L -c %a "$exec_path" 2>/dev/null)" = "777" ]; then
                        findings="${findings}WEAK_EXEC_PERMS: Executable has 777 permissions: $exec_path\n"
                    fi
                fi
                # Check for relative paths
                if is_relative_path "$exec_path"; then
                    findings="${findings}RELATIVE_PATH: Uses relative path: $exec_path\n"
                fi
            fi
        done

        # Check for listeners (following symlinks)
        local listen_paths=$(grep -Eo '^(Listen).*?=[!@+-]*/[a-zA-Z0-9_/\-]+' "$socket_file" 2>/dev/null | cut -d '=' -f2 | sed 's,^[@\+!-]*,,')
        printf "%s\n" "$listen_paths" | while read -r listen_path; do
            if [ -n "$listen_path" ]; then
                # Check if listener path is writable (following symlinks)
                if [ -L "$listen_path" ]; then
                    local target_listen=$(readlink -f "$listen_path")
                    if [ -w "$target_listen" ]; then
                        findings="${findings}WRITABLE_LISTENER: Listener target path is writable: $target_listen\n"
                    fi
                    # Check for weak permissions on target
                    if [ -e "$target_listen" ] && [ "$(stat -L -c %a "$target_listen" 2>/dev/null)" = "777" ]; then
                        findings="${findings}WEAK_LISTENER_PERMS: Listener target path has 777 permissions: $target_listen\n"
                    fi
                else
                    if [ -w "$listen_path" ]; then
                        findings="${findings}WRITABLE_LISTENER: Listener path is writable: $listen_path\n"
                    fi
                    # Check for weak permissions
                    if [ -e "$listen_path" ] && [ "$(stat -L -c %a "$listen_path" 2>/dev/null)" = "777" ]; then
                        findings="${findings}WEAK_LISTENER_PERMS: Listener path has 777 permissions: $listen_path\n"
                    fi
                fi
                # Check for relative paths
                if is_relative_path "$listen_path"; then
                    findings="${findings}RELATIVE_LISTENER: Uses relative path: $listen_path\n"
                fi
            fi
        done

        # Check for unsafe configurations
        if grep -qE '^(User|Group)=root' "$socket_file" 2>/dev/null; then
            findings="${findings}ROOT_USER: Socket runs as root\n"
        fi
        if grep -qE '^(CapabilityBoundingSet).*CAP_SYS_ADMIN' "$socket_file" 2>/dev/null; then
            findings="${findings}DANGEROUS_CAPS: Has dangerous capabilities\n"
        fi
        if grep -qE '^(BindIP|BindIPv6Only)=yes' "$socket_file" 2>/dev/null; then
            findings="${findings}NETWORK_BIND: Can bind to network interfaces\n"
        fi

        # If any findings, print them
        if [ -n "$findings" ]; then
            echo "Potential privilege escalation in socket file: $socket_file"
            echo "$findings" | while read -r finding; do
                [ -n "$finding" ] && echo "  └─ $finding" | sed -${E} "s,WRITABLE.*,${SED_RED},g" | sed -${E} "s,RELATIVE.*,${SED_RED_YELLOW},g"
            done
        fi
    }

    # Process each socket file
    if [ -n "$PSTORAGE_SOCKET" ]; then
        printf "%s\n" "$PSTORAGE_SOCKET" | while read -r socket_file; do
            if [ -n "$socket_file" ] && [ -e "$socket_file" ]; then
                check_socket_file "$socket_file"
            fi
        done
    else
        print_list "No socket files found" "$NC"
    fi

    echo ""
fi