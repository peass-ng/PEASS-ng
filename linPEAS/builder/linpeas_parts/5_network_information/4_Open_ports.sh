# Title: Network Information - Open ports
# ID: NT_Open_ports
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Enumerate open ports
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, print_info
# Global Variables: $E, $SED_RED
# Initial Functions:
# Generated Global Variables: $pid_dir, $tx_queue, $pid, $rem_port, $proc_file, $rem_ip, $local_ip, $rx_queue, $proto, $rem_addr, $program, $state, $header_sep, $proc_info, $inode, $header, $line, $local_addr, $local_port
# Fat linpeas: 0
# Small linpeas: 1

# Function to get process info from inode
get_process_info() {
    local inode=$1
    local pid=""
    local program=""
    
    if [ -n "$inode" ]; then
        for pid_dir in /proc/[0-9]*/fd; do
            if [ -d "$pid_dir" ]; then
                if ls -l "$pid_dir" 2>/dev/null | grep -q "$inode"; then
                    pid=$(echo "$pid_dir" | awk -F/ '{print $3}')
                    if [ -f "/proc/$pid/cmdline" ]; then
                        program=$(tr '\0' ' ' < "/proc/$pid/cmdline" | cut -d' ' -f1)
                        program=$(basename "$program")
                    fi
                    break
                fi
            fi
        done
    fi
    echo "$pid/$program"
}

# Function to parse /proc/net/tcp and /proc/net/udp files
parse_proc_net_ports() {
    local proto=$1
    local proc_file="/proc/net/$proto"
    local header="Proto  Recv-Q  Send-Q  Local Address          Foreign Address        State       PID/Program name"
    local header_sep="--------------------------------------------------------------------------------"

    if [ -f "$proc_file" ]; then
        print_3title "Active $proto Ports (from /proc/net/$proto)"
        echo "$header"
        echo "$header_sep"

        # Process each connection using a pipe
        tail -n +2 "$proc_file" 2>/dev/null | while IFS= read -r line; do
            [ -z "$line" ] && continue
            
            # Skip header
            case "$line" in
                *"sl"*) continue ;;
                *) : ;;
            esac

            # Extract fields using awk
            sl=$(echo "$line" | awk '{print $1}')
            local_addr=$(echo "$line" | awk '{print $2}')
            rem_addr=$(echo "$line" | awk '{print $3}')
            st=$(echo "$line" | awk '{print $4}')
            tx_queue=$(echo "$line" | awk '{print $5}')
            rx_queue=$(echo "$line" | awk '{print $6}')
            uid=$(echo "$line" | awk '{print $7}')
            inode=$(echo "$line" | awk '{print $10}')

            # Convert hex IP:port to decimal
            local_ip=$(printf "%d.%d.%d.%d" $(echo "$local_addr" | awk -F: '{printf "0x%s 0x%s 0x%s 0x%s", substr($1,7,2), substr($1,5,2), substr($1,3,2), substr($1,1,2)}'))
            local_port=$(printf "%d" "0x$(echo "$local_addr" | awk -F: '{print $2}')")
            rem_ip=$(printf "%d.%d.%d.%d" $(echo "$rem_addr" | awk -F: '{printf "0x%s 0x%s 0x%s 0x%s", substr($1,7,2), substr($1,5,2), substr($1,3,2), substr($1,1,2)}'))
            rem_port=$(printf "%d" "0x$(echo "$rem_addr" | awk -F: '{print $2}')")

            # Get process information
            proc_info=$(get_process_info "$inode")

            # Get state name
            case $st in
                "01") state="ESTABLISHED" ;;
                "02") state="SYN_SENT" ;;
                "03") state="SYN_RECV" ;;
                "04") state="FIN_WAIT1" ;;
                "05") state="FIN_WAIT2" ;;
                "06") state="TIME_WAIT" ;;
                "07") state="CLOSE" ;;
                "08") state="CLOSE_WAIT" ;;
                "09") state="LAST_ACK" ;;
                "0A") state="LISTEN" ;;
                "0B") state="CLOSING" ;;
                "0C") state="NEW_SYN_RECV" ;;
                *) state="UNKNOWN" ;;
            esac

            # Only show listening ports
            if [ "$state" = "LISTEN" ]; then
                # Format the output
                printf "%-6s %-8s %-8s %-21s %-21s %-12s %s\n" \
                    "$proto" "$rx_queue" "$tx_queue" "$local_ip:$local_port" "$rem_ip:$rem_port" "$state" "$proc_info"
            fi
        done
    fi
    echo ""
}

# Function to get open ports information
get_open_ports() {
    print_2title "Active Ports"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#open-ports"

    # Try standard tools first
    if command -v netstat >/dev/null 2>&1; then
        print_3title "Active Ports (netstat)"
        netstat -punta 2>/dev/null | grep -i listen | sed -${E} "s,127.0.[0-9]+.[0-9]+|:::|::1:|0\.0\.0\.0,${SED_RED},g"
    elif command -v ss >/dev/null 2>&1; then
        print_3title "Active Ports (ss)"
        ss -nltpu 2>/dev/null | grep -i listen | sed -${E} "s,127.0.[0-9]+.[0-9]+|:::|::1:|0\.0\.0\.0,${SED_RED},g"
    else
        # Fallback to parsing /proc/net files
        parse_proc_net_ports "tcp"
        parse_proc_net_ports "udp"
    fi

    # Additional port information
    if [ "$EXTRA_CHECKS" ] || [ "$DEBUG" ]; then
        print_3title "Additional Port Information"

        # Check for listening ports in /proc/net/unix
        if [ -f "/proc/net/unix" ]; then
            echo "Unix Domain Sockets:"
            # Use awk to process the file in one go, avoiding duplicates and empty paths
            awk '$8 != "" && $8 != "@" && $8 != "00000000" {
                inode=$7
                socket=$8
                # Find process using inode
                cmd="find /proc/[0-9]*/fd -ls 2>/dev/null | grep " inode " | head -n1 | awk \"{print \\$11}\" | xargs -r readlink"
                pid=""
                while (cmd | getline pid_dir) {
                    if (pid_dir != "") {
                        split(pid_dir, parts, "/")
                        pid=parts[3]
                        break
                    }
                }
                close(cmd)
                if (pid != "") {
                    cmd="tr \\0 \" \" < /proc/" pid "/cmdline 2>/dev/null | cut -d\" \" -f1 | xargs -r basename"
                    cmd | getline prog
                    close(cmd)
                    if (prog != "") {
                        print "  " socket " (" pid "/" prog ")"
                    } else {
                        print "  " socket " (" pid ")"
                    }
                } else {
                    print "  " socket
                }
            }' /proc/net/unix 2>/dev/null | sort -u
        fi

        # Check for ports in use by systemd
        if command -v systemctl >/dev/null 2>&1; then
            echo -e "\nSystemd Socket Units:"
            systemctl list-sockets 2>/dev/null | while IFS= read -r line; do
                [ -z "$line" ] && continue
                if ! echo "$line" | grep -q "UNIT\|listed"; then
                    echo "  $line"
                fi
            done
        fi
    fi

    echo ""
}

get_open_ports
