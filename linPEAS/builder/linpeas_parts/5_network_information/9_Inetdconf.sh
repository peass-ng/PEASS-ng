# Title: Network Information - Inetd/Xinetd Services Analysis
# ID: NT_Inetdconf
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Analyze inetd and xinetd services and configurations
# License: GNU GPL
# Version: 1.0
# Mitre: T1049
# Functions Used: print_2title, print_3title, warn_exec, echo_not_found
# Global Variables: $EXTRA_CHECKS, $E, $SED_RED, $SED_GREEN, $SED_YELLOW
# Initial Functions:
# Generated Global Variables: $inetd_service, $log_file, $cmd, $service_name, $conf_file, $service_dir, $service_file, $file, $inetd_command, $inetd_telnet_probe_count, $service, $socket, $protocol, $user, $server, $checked_binary, $version_output, $version, $major, $minor, $patch, $rest
# Fat linpeas: 0
# Small linpeas: 0

# Function to check if a command exists and is executable
check_command() {
    local cmd=$1
    if command -v "$cmd" >/dev/null 2>&1; then
        if [ -x "$(command -v "$cmd")" ]; then
            return 0
        fi
    fi
    return 1
}

inetd_telnet_upstream_affected() {
    local major=$1 minor=$2 patch=$3
    if [ "$major" -eq 1 ]; then
        [ "$minor" -gt 9 ] || { [ "$minor" -eq 9 ] && [ "$patch" -ge 3 ]; }
    elif [ "$major" -eq 2 ]; then
        [ "$minor" -lt 7 ] || { [ "$minor" -eq 7 ] && [ "$patch" -eq 0 ]; }
    else
        return 1
    fi
}

# Inspect only configured root telnet stanzas. A version is an upstream hint,
# not proof that a downstream package is unpatched or that a listener is active.
inetd_telnet_cve_hint() {
    local conf_file=$1 service socket protocol wait_mode user server arguments
    local checked_binary='' version_output='' version='' major minor patch rest

    [ -f "$conf_file" ] || return 0
    while read -r service socket protocol wait_mode user server arguments || [ -n "$service" ]; do
        [ "$service" = telnet ] && [ "$socket" = stream ] || continue
        case "$protocol" in tcp*) ;; *) continue ;; esac
        case "$user" in root|root:*|root.*|0|0:*|0.*) ;; *) continue ;; esac
        [ -n "$server" ] || continue

        if [ "$server" != "$checked_binary" ]; then
            checked_binary=$server
            version_output=''
            # A non-GNU daemon may not support --version or honor SIGTERM.
            # Bound both the process lifetime and captured output.
            case "$server" in
                /*) if [ -x "$server" ] && [ "${inetd_telnet_probe_count:-0}" -lt 4 ] && command -v timeout >/dev/null 2>&1; then
                        inetd_telnet_probe_count=$((inetd_telnet_probe_count + 1))
                        version_output=$(timeout -k 1 1 "$server" --version 2>&1 | head -c 4096)
                    fi ;;
            esac
        fi

        if printf '%s\n' "$version_output" | grep -qi 'GNU inetutils'; then
            version=$(printf '%s\n' "$version_output" | sed -n 's/.*[Ii]netutils[^0-9]*\([0-9][0-9.]*\).*/\1/p' | head -n 1)
            case "$version" in *.*) ;; *) version='' ;; esac
            case "$version" in *[!0-9.]*) version='' ;; esac
            if [ -n "$version" ]; then
                major=${version%%.*}
                rest=${version#*.}
                minor=${rest%%.*}
                patch=0
                [ "$rest" = "$minor" ] || patch=${rest#*.}
                case "$major:$minor:$patch" in *[!0-9:]*|*::*|:*|*:) version='' ;; esac
            fi
            if [ -n "$version" ] && inetd_telnet_upstream_affected "$major" "$minor" "$patch"; then
                echo "CVE-2026-24061: GNU Inetutils telnetd $version in upstream affected range (1.9.3-2.7), root inetd stanza in $conf_file; confirm package patches and active listener."
            elif [ -n "$version" ]; then
                echo "CVE-2026-24061: GNU Inetutils telnetd $version outside upstream affected range, root inetd stanza in $conf_file."
            else
                echo "CVE-2026-24061: GNU Inetutils telnetd version unknown, root inetd stanza in $conf_file; inspect package and listener."
            fi
        elif printf '%s\n' "$version_output" | grep -qiE 'BusyBox|NetKit|OpenBSD|FreeBSD|BSD telnetd'; then
            echo "CVE-2026-24061: configured telnetd appears non-GNU, root inetd stanza in $conf_file."
        else
            echo "CVE-2026-24061: telnetd implementation/version unknown, root inetd stanza in $conf_file; inspect package and listener."
        fi
    done < "$conf_file"
}

# Function to analyze inetd services
analyze_inetd() {
    local inetd_command
    inetd_telnet_probe_count=0
    echo ""
    print_3title "Inetd Services" "T1049"
    # Check if inetd is installed
    if check_command inetd; then
        inetd_command=inetd
    elif check_command inetutils-inetd; then
        inetd_command=inetutils-inetd
    else
        echo_not_found "inetd"
        return
    fi
    
    # Check if inetd is running
    if ! pgrep -x inetd >/dev/null 2>&1 && ! pgrep -x inetutils-inetd >/dev/null 2>&1; then
        echo "inetd is not running" | sed -${E} "s,.*,${SED_YELLOW},g"
    fi
    
    # Get inetd version
    warn_exec "$inetd_command" -v 2>/dev/null
    
    # Check main configuration file
    if [ -f "/etc/inetd.conf" ]; then
        echo -e "\nInetd Configuration (/etc/inetd.conf):"
        warn_exec cat /etc/inetd.conf | grep -v "^$" | grep -Ev "\W+\#|^#" 2>/dev/null
        
        # Check for potentially dangerous services
        echo -e "\nPotentially Dangerous Services:"
        warn_exec cat /etc/inetd.conf | grep -v "^$" | grep -Ev "\W+\#|^#" | grep -iE "shell|login|exec|rsh|rlogin|rexec|finger|telnet|ftp|tftp" 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
    else
        echo_not_found "/etc/inetd.conf"
    fi
    
    # Check for additional configuration files
    echo -e "\nAdditional Inetd Configuration Files:"
    for conf_file in /etc/inetd.d/* /etc/inet/*.conf; do
        if [ -f "$conf_file" ]; then
            echo "Found configuration in $conf_file:"
            warn_exec cat "$conf_file" | grep -v "^$" | grep -Ev "\W+\#|^#" 2>/dev/null
        fi
    done

    # Keep the existing configuration output and add a local, passive CVE hint.
    for conf_file in /etc/inetd.conf /etc/inetd.d/* /etc/inet/*.conf; do
        inetd_telnet_cve_hint "$conf_file"
    done
}

# Function to analyze xinetd services
analyze_xinetd() {
    echo ""
    print_3title "Xinetd Services" "T1049"
    # Check if xinetd is installed
    if ! check_command xinetd; then
        echo_not_found "xinetd"
        return
    fi
    
    # Check if xinetd is running
    if ! pgrep -x xinetd >/dev/null 2>&1; then
        echo "xinetd is not running" | sed -${E} "s,.*,${SED_YELLOW},g"
    fi
    
    # Get xinetd version
    warn_exec xinetd -version 2>/dev/null
    
    # Check main configuration file
    if [ -f "/etc/xinetd.conf" ]; then
        echo -e "\nXinetd Configuration (/etc/xinetd.conf):"
        warn_exec cat /etc/xinetd.conf | grep -v "^$" | grep -Ev "\W+\#|^#" 2>/dev/null
        
        # Check for included configurations
        echo -e "\nIncluded Configurations:"
        warn_exec grep -r "includedir" /etc/xinetd.conf 2>/dev/null
    else
        echo_not_found "/etc/xinetd.conf"
    fi
    
    # Check for service-specific configurations
    echo -e "\nService Configurations:"
    for service_dir in /etc/xinetd.d/ /etc/xinetd/; do
        if [ -d "$service_dir" ]; then
            echo "Services in $service_dir:"
            for service_file in "$service_dir"/*; do
                if [ -f "$service_file" ]; then
                    service_name=$(basename "$service_file")
                    echo -e "\nService: $service_name"
                    # Check if service is enabled
                    if grep -q "disable.*=.*no" "$service_file" 2>/dev/null; then
                        echo "Status: Enabled" | sed -${E} "s,.*,${SED_RED},g"
                    else
                        echo "Status: Disabled"
                    fi
                    # Show service configuration
                    warn_exec cat "$service_file" | grep -v "^$" | grep -Ev "\W+\#|^#" 2>/dev/null
                    
                    # Check for potentially dangerous configurations
                    if grep -qiE "server.*=.*/bin/|server.*=.*/sbin/|server.*=.*/usr/bin/|server.*=.*/usr/sbin/" "$service_file" 2>/dev/null; then
                        echo "Warning: Service uses system binaries" | sed -${E} "s,.*,${SED_RED},g"
                    fi
                    if grep -qiE "user.*=.*root|user.*=.*0" "$service_file" 2>/dev/null; then
                        echo "Warning: Service runs as root" | sed -${E} "s,.*,${SED_RED},g"
                    fi
                fi
            done
        fi
    done
}

# Function to check for running inetd/xinetd services
check_running_services() {
    echo ""
    print_3title "Running Inetd/Xinetd Services" "T1049"
    # Check netstat for services
    if check_command netstat; then
        echo "Active Services (from netstat):"
        warn_exec netstat -tulpn 2>/dev/null | grep -E "inetd|xinetd" | sed -${E} "s,.*,${SED_RED},g"
    fi
    
    # Check ss for services
    if check_command ss; then
        echo -e "\nActive Services (from ss):"
        warn_exec ss -tulpn 2>/dev/null | grep -E "inetd|xinetd" | sed -${E} "s,.*,${SED_RED},g"
    fi
    
    # Check for service processes
    echo -e "\nRunning Service Processes:"
    for inetd_service in $(pgrep -l inetd 2>/dev/null; pgrep -l xinetd 2>/dev/null); do
        echo "$inetd_service" | sed -${E} "s,.*,${SED_RED},g"
    done
}

# Main function to analyze inetd/xinetd services
analyze_inetd_services() {
    print_2title "Inetd/Xinetd Services Analysis" "T1049"
    # Analyze inetd and xinetd services
    analyze_inetd
    analyze_xinetd
    
    # Check for running services
    check_running_services
    
    # Additional checks if EXTRA_CHECKS is enabled
    if [ "$EXTRA_CHECKS" ]; then
        echo ""
        print_3title "Additional Inetd/Xinetd Information" "T1049"
        # Check for inetd/xinetd logs
        echo "Checking for service logs..."
        for log_file in /var/log/inetd.log /var/log/xinetd.log /var/log/messages /var/log/syslog; do
            if [ -f "$log_file" ]; then
                echo "Found log file: $log_file" | sed -${E} "s,.*,${SED_GREEN},g"
                warn_exec tail -n 20 "$log_file" | grep -iE "inetd|xinetd" 2>/dev/null
            fi
        done
        
        # Check for inetd/xinetd related files
        echo -e "\nChecking for related files..."
        for file in /etc/init.d/inetd /etc/init.d/xinetd /etc/default/inetd /etc/default/xinetd; do
            if [ -f "$file" ]; then
                echo "Found file: $file" | sed -${E} "s,.*,${SED_GREEN},g"
                warn_exec cat "$file" | grep -v "^$" | grep -Ev "\W+\#|^#" 2>/dev/null
            fi
        done
    fi
    
    echo ""
}

# Run the main function
analyze_inetd_services
