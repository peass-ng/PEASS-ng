# Title: System Information - Systemd
# ID: SY_Systemd
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: Check for systemd vulnerabilities and misconfigurations that could lead to privilege escalation:
#   - Systemd version vulnerabilities (CVE-2021-4034, CVE-2021-33910, etc.)
#   - Services running as root that could be exploited
#   - Services with dangerous capabilities that could be abused
#   - Services with writable paths that could be used to inject malicious code
#   - Exploitation methods:
#     * Version exploits: Use known exploits for vulnerable systemd versions
#     * Root services: Abuse services running as root to execute commands
#     * Capabilities: Abuse services with dangerous capabilities (CAP_SYS_ADMIN, etc.)
#     * Writable paths: Replace executables in writable paths to get code execution
# License: GNU GPL
# Version: 1.1
# Functions Used: print_2title, print_list, echo_not_found
# Global Variables: $SEARCH_IN_FOLDER, $Wfolders, $SED_RED, $SED_RED_YELLOW, $NC
# Initial Functions:
# Generated Global Variables: $WRITABLESYSTEMDPATH, $line, $service, $file, $version, $user, $caps, $path, $path_line, $service_file, $exec_line, $cmd
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
    print_2title "Systemd Information"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#systemd-path---relative-paths"

    # Function to check if systemctl is available
    check_systemctl() {
        if ! command -v systemctl >/dev/null 2>&1; then
            echo_not_found "systemctl"
            return 1
        fi
        return 0
    }

    # Function to get service file path
    get_service_file() {
        local service="$1"
        local file=""
        for path in "/etc/systemd/system/$service" "/lib/systemd/system/$service"; do
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

    # Check systemd version and known vulnerabilities
    print_list "Systemd version and vulnerabilities? .............. "$NC
    if check_systemctl; then
        version=$(systemctl --version | head -n 1 | grep -oE '([0-9]+(\.[0-9]+)+)')
        if [ -n "$version" ]; then
            echo "$version" | sed -${E} "s,([0-9]+(\.[0-9]+)+),${SED_RED},g"
            # Check for known vulnerable versions
            case "$version" in
                "2.3"[0-4]|"2.3"[0-4]"."*)
                    echo "  └─ Vulnerable to CVE-2021-4034 (Polkit)" | sed -${E} "s,.*,${SED_RED},g"
                    ;;
                "2.4"[0-9]|"2.4"[0-9]"."*)
                    echo "  └─ Vulnerable to CVE-2021-33910 (systemd-tmpfiles)" | sed -${E} "s,.*,${SED_RED},g"
                    ;;
            esac
        fi
    fi

    # Check for systemd services running as root
    print_list "Services running as root? ..... "$NC
    if check_systemctl; then
        systemctl list-units --type=service --state=running 2>/dev/null | 
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
        systemctl list-units --type=service --state=running 2>/dev/null | 
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
        systemctl list-units --type=service --state=running 2>/dev/null | 
        grep -E "\.service" | 
        while read -r line; do
            service=$(echo "$line" | awk '{print $1}')
            service_file=$(get_service_file "$service")
            if [ -n "$service_file" ]; then
                # Check ExecStart paths
                grep -E "ExecStart|ExecStartPre|ExecStartPost" "$service_file" 2>/dev/null | 
                while read -r exec_line; do
                    # Extract the first word after ExecStart* as the command
                    cmd=$(echo "$exec_line" | awk '{print $2}' | tr -d '"')
                    # Extract the rest as arguments
                    args=$(echo "$exec_line" | awk '{$1=$2=""; print $0}' | tr -d '"')
                    
                    # Only check the command path, not arguments
                    if [ -n "$cmd" ] && [ -w "$cmd" ]; then
                        echo "$service: $cmd (from $exec_line)" | sed -${E} "s,.*,${SED_RED},g"
                    fi
                    # Check for relative paths only in the command, not arguments
                    if [ -n "$cmd" ] && [ "${cmd#/}" = "$cmd" ] && ! echo "$cmd" | grep -qE '^-|^--'; then
                        echo "$service: Uses relative path '$cmd' (from $exec_line)" | sed -${E} "s,.*,${SED_RED},g"
                    fi
                done
            fi
        done
    else
        echo ""
    fi

    echo ""

    print_2title "Systemd PATH"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#systemd-path---relative-paths"
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