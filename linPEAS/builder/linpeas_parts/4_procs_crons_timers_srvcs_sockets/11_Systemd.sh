# Title: System Information - Systemd
# ID: SY_Systemd
# Author: Carlos Polop
# Last Update: 2026-10-09
# Description: Check systemd version, service misconfigurations, and readable service environment files:
#   - Systemd release (distribution package and patch status require separate verification)
#   - Services running as root that could be exploited
#   - Services with dangerous capabilities that could be abused
#   - Services with writable paths that could be used to inject malicious code
#   - Exploitation methods:
#     * Version review: Compare the installed distribution package with vendor fixes
#     * Root services: Abuse services running as root to execute commands
#     * Capabilities: Abuse services with dangerous capabilities (CAP_SYS_ADMIN, etc.)
#     * Writable paths: Replace executables in writable paths to get code execution
# License: GNU GPL
# Version: 1.2
# Mitre: T1543.002,T1552.001
# Functions Used: print_2title, print_list, echo_not_found
# Global Variables: $SEARCH_IN_FOLDER, $IAMROOT, $Wfolders, $SED_RED, $SED_RED_YELLOW, $NC
# Initial Functions:
# Generated Global Variables: $WRITABLESYSTEMDPATH, $line, $service, $file, $version, $user, $caps, $path, $path_line, $service_file, $exec_line, $exec_value, $cmd, $cmd_path, $svc_path_entry, $svc_writable_path, $running_services, $env_file_findings, $env_file_path, $env_file_size, $env_key_names, $env_unit_size
# Fat linpeas: 0
# Small linpeas: 1

# Inspect only literal EnvironmentFile paths in a service unit. This is an
# indicator of readable credential-like assignments, not evidence that a key
# is used for authentication or provides privilege escalation.
systemd_envfile_candidates() {
    [ -f "$1" ] && [ -r "$1" ] || return
    local env_unit_size
    env_unit_size=$(stat -c %s "$1" 2>/dev/null) ||
        env_unit_size=$(stat -f %z "$1" 2>/dev/null) || return
    case "$env_unit_size" in ''|*[!0-9]*) return ;; esac
    [ "$env_unit_size" -le 65536 ] || return
    awk '
        function add_path(value, path, quoted, optional) {
            sub(/^[[:space:]]*/, "", value)
            sub(/[[:space:]]*$/, "", value)
            if (value == "") { count = 0; return }
            optional = 0
            if (substr(value, 1, 1) == "-") { value = substr(value, 2); optional = 1 }
            quoted = 0
            if ((substr(value, 1, 1) == "\"" && substr(value, length(value), 1) == "\"") ||
                (substr(value, 1, 1) == sprintf("%c", 39) && substr(value, length(value), 1) == sprintf("%c", 39))) {
                quoted = 1
                value = substr(value, 2, length(value) - 2)
            }
            if (!optional && substr(value, 1, 1) == "-") value = substr(value, 2)
            # Skip globs, escapes, unquoted whitespace, and relative paths.
            if (!quoted && value ~ /[[:space:]]/) return
            if (value !~ /^\// || value ~ /[*?\[\]{}\\%$"\047]/ || value ~ /[\r\n]/) return
            for (path = 1; path <= count; path++) if (paths[path] == value) return
            paths[++count] = value
        }
        /^[[:space:]]*\[/ {
            section = $0
            sub(/^[[:space:]]*/, "", section)
            sub(/[[:space:]]*$/, "", section)
            next
        }
        section == "[Service]" && /^[[:space:]]*EnvironmentFile[[:space:]]*=/ {
            value = $0
            sub(/^[[:space:]]*EnvironmentFile[[:space:]]*=/, "", value)
            add_path(value)
        }
        END { for (i = 1; i <= count && i <= 4; i++) print paths[i] }
    ' "$1" 2>/dev/null
}

systemd_envfile_key_names() {
    awk '
        /^[[:space:]]*[#;]/ { next }
        /^[[:space:]]*[A-Za-z_][A-Za-z0-9_]*[[:space:]]*=/ {
            line = $0
            sub(/=.*/, "", line)
            gsub(/[[:space:]]/, "", line)
            value = $0
            sub(/^[^=]*=[[:space:]]*/, "", value)
            if (value == "" || value == "\"\"" || value == sprintf("%c%c", 39, 39)) next
            upper = toupper(line)
            if (upper !~ /(^|_)(SECRET|TOKEN|PASSWORD|PASSWD|APIKEY|KEY)($|_)/) next
            if (seen[line]++) next
            if (found >= 20) exit
            if (found++) printf ","
            printf "%s", line
        }
        END { if (found) printf "\n" }
    ' "$1" 2>/dev/null
}

systemd_envfile_active_units() {
    awk '
        {
            unit = $1
            if (unit == "●") unit = $2
            if (unit !~ /^[A-Za-z0-9_.@-]+[.]service$/ || unit ~ /[.][.]/) next
            print unit
            if (++count >= 200) exit
        }
    '
}

systemd_envfile_findings_for_unit() {
    systemd_envfile_candidates "$1" | while IFS= read -r env_file_path; do
        [ -f "$env_file_path" ] && [ -r "$env_file_path" ] || continue
        env_file_size=$(stat -c %s "$env_file_path" 2>/dev/null) ||
            env_file_size=$(stat -f %z "$env_file_path" 2>/dev/null) || continue
        case "$env_file_size" in ''|*[!0-9]*) continue ;; esac
        [ "$env_file_size" -le 65536 ] || continue
        env_key_names=$(systemd_envfile_key_names "$env_file_path")
        [ "$env_key_names" ] && printf '%s: %s\n' "$env_file_path" "$env_key_names"
    done
}

if ! [ "$SEARCH_IN_FOLDER" ]; then
    print_2title "Systemd Information" "T1543.002"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/linux-basics/linux-privilege-escalation/index.html#systemd-path---relative-paths"

    # Function to check if systemctl is available
    check_systemctl() {
        if ! command -v systemctl >/dev/null 2>&1; then
            echo_not_found "systemctl"
            return 1
        fi
        return 0
    }

    # Function to list running systemd services
    list_running_services() {
        systemctl list-units --type=service --state=running 2>/dev/null
    }

    # Function to get service file path
    get_service_file() {
        local service="$1"
        local file=""
        for path in "/etc/systemd/system/$service" "/run/systemd/system/$service" "/usr/lib/systemd/system/$service" "/lib/systemd/system/$service"; do
            if [ -f "$path" ]; then
                file="$path"
                break
            fi
        done
        echo "$file"
    }

    # Function to check dangerous capabilities
    check_dangerous_caps() {
        local caps="$1"
        echo "$caps" | grep -qE '(CAP_SYS_ADMIN|CAP_DAC_OVERRIDE|CAP_DAC_READ_SEARCH|CAP_SETUID|CAP_SETGID|CAP_NET_ADMIN)'
        return $?
    }

    # Reuse one active-unit listing without changing the scope of older checks.
    running_services=""
    if check_systemctl; then
        running_services=$(list_running_services)
    fi

    # The upstream release alone does not identify distribution backports or
    # mitigations. PwnKit is a polkit/pkexec issue, not a systemd version issue.
    print_list "Systemd release (verify package fixes)? .......... "$NC
    if check_systemctl; then
        version=$(systemctl --version 2>/dev/null | awk 'NR == 1 && $1 == "systemd" && $2 ~ /^[0-9]+$/ { print $2 }')
        if [ -n "$version" ]; then
            printf '%s\n' "$version"
        fi
    fi

    # Check for systemd services running as root
    print_list "Services running as root? ..... "$NC
    if check_systemctl; then
        printf '%s\n' "$running_services" |
        grep -E "root|0:0" | 
        while read -r line; do
            service=$(echo "$line" | awk '{print $1}')
            user=$(systemctl show "$service" -p User 2>/dev/null | cut -d= -f2)
            echo "$service (User: $user)" | sed -${E} "s,root|0:0,${SED_RED},g"
        done
        echo ""
    else
        echo ""
    fi

    # Check for systemd services with dangerous capabilities
    print_list "Running services with dangerous capabilities? ... "$NC
    if check_systemctl; then
        printf '%s\n' "$running_services" |
        grep -E "\.service" | 
        while read -r line; do
            service=$(echo "$line" | awk '{print $1}')
            caps=$(systemctl show "$service" -p CapabilityBoundingSet 2>/dev/null | cut -d= -f2)
            if [ -n "$caps" ] && check_dangerous_caps "$caps"; then
                echo "$service: $caps" | sed -${E} "s,.*,${SED_RED},g"
            fi
        done
        echo ""
    else
        echo ""
    fi

    # Check for systemd services with writable paths
    print_list "Services with writable paths? . "$NC
    if check_systemctl; then
        printf '%s\n' "$running_services" |
        grep -E "\.service" | 
        while read -r line; do
            service=$(echo "$line" | awk '{print $1}')
            service_file=$(get_service_file "$service")
            if [ -n "$service_file" ]; then
                # Check service-specific PATH entries (Environment=PATH=...)
                svc_writable_path=$(grep -E '^Environment=.*PATH=' "$service_file" 2>/dev/null | sed -E 's/^Environment=//; s/^"//; s/"$//; s/^PATH=//' | tr ':' '\n' | while read -r svc_path_entry; do
                    [ -z "$svc_path_entry" ] && continue
                    if [ -d "$svc_path_entry" ] && [ -w "$svc_path_entry" ]; then
                        echo "$svc_path_entry"
                    fi
                done)
                if [ "$svc_writable_path" ]; then
                    for svc_path_entry in $svc_writable_path; do
                        echo "$service: Writable service PATH entry '$svc_path_entry'" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
                    done
                fi

                # Check ExecStart paths
                grep -E "ExecStart|ExecStartPre|ExecStartPost" "$service_file" 2>/dev/null | 
                while read -r exec_line; do
                    # Extract command from the right side of Exec*=, not from argv
                    exec_value="${exec_line#*=}"
                    exec_value=$(echo "$exec_value" | sed 's/^[[:space:]]*//')
                    cmd=$(echo "$exec_value" | awk '{print $1}' | tr -d '"')
                    # Strip systemd command prefixes (-, @, :, +, !) before path checks
                    cmd_path=$(echo "$cmd" | sed -E 's/^[-@:+!]+//')
                    
                    # Only check the command path, not arguments
                    if [ -n "$cmd_path" ] && [ -w "$cmd_path" ]; then
                        echo "$service: $cmd_path (from $exec_line)" | sed -${E} "s,.*,${SED_RED},g"
                    fi
                    # Check for relative paths only in the command, not arguments
                    if [ -n "$cmd_path" ] && [ "${cmd_path#/}" = "$cmd_path" ] && [ "${cmd_path#\$}" = "$cmd_path" ]; then
                        echo "$service: Uses relative path '$cmd_path' (from $exec_line)" | sed -${E} "s,.*,${SED_RED},g"
                        if [ "$svc_writable_path" ]; then
                            echo "$service: Relative Exec path + writable service PATH can allow path hijacking" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
                        fi
                    fi
                done
            fi
        done
    else
        echo ""
    fi

    echo ""

    # Unit files may point at files outside the usual .env naming convention.
    # Keep this passive and print key names only, never environment values.
    if ! [ "$IAMROOT" ]; then
        env_file_findings=$(
            printf '%s\n' "$running_services" | systemd_envfile_active_units | while IFS= read -r service; do
                service_file=$(get_service_file "$service")
                [ "$service_file" ] || continue
                systemd_envfile_findings_for_unit "$service_file"
            done | sort -u
        )
        if [ "$env_file_findings" ]; then
            print_2title "Readable systemd EnvironmentFile credential-like keys (possible exposure)" "T1552.001"
            printf '%s\n' "$env_file_findings"
            echo ""
        fi
    fi

    print_2title "Systemd PATH" "T1543.002"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/linux-basics/linux-privilege-escalation/index.html#systemd-path---relative-paths"
    if check_systemctl; then
        systemctl show-environment 2>/dev/null | 
        grep "PATH" | 
        while read -r path_line; do
            echo "$path_line" | sed -${E} "s,$Wfolders\|\./\|\.:\|:\.,${SED_RED_YELLOW},g"
            # Store writable paths for later use
            if echo "$path_line" | grep -qE "$Wfolders"; then
                WRITABLESYSTEMDPATH="$path_line"
            fi
        done
    fi

    echo ""
fi
