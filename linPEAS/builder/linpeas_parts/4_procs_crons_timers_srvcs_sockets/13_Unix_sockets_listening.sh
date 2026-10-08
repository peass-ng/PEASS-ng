# Title: Processes & Cron & Services & Timers - Unix Sockets Analysis
# ID: PR_Unix_sockets_listening
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: Analyze Unix sockets for privilege escalation vectors:
#   - Listening Unix sockets
#   - Socket file permissions
#   - Socket ownership
#   - Socket connectivity
#   - Socket protocol analysis
# License: GNU GPL
# Version: 1.1
# Mitre: T1571,T1049
# Functions Used: print_2title, print_info
# Global Variables: $EXTRA_CHECKS, $groupsB, $groupsVB, $IAMROOT, $idB, $knw_grps, $knw_usrs, $nosh_usrs, $SEARCH_IN_FOLDER, $sh_usrs, $USER, $SED_RED, $SED_GREEN
# Initial Functions:
# Generated Global Variables: $unix_scks_list, $unix_scks_list2, $ss_metadata, $socket_listing, $socket_info, $perms, $owner, $owner_info, $owner_uid, $response, $socket, $cmd, $mode, $group, $read_access, $write_access, $listener_info, $listener_pid, $listener_uid, $listener_is_active
# Fat linpeas: 0
# Small linpeas: 0

if ! [ "$IAMROOT" ]; then
    if ! [ "$SEARCH_IN_FOLDER" ]; then
        print_2title "Unix Sockets Analysis" "T1571,T1049"
        print_info "https://book.hacktricks.wiki/en/linux-hardening/network-information/local-network-and-socket-triage.html#unix-socket-interaction-and-command-injection"


        # Function to get socket permissions
        get_socket_perms() {
            local socket="$1"
            local mode="$2"
            local read_access="$3"
            local write_access="$4"
            local perms=""
            
            # Check read permission
            if [ "$read_access" = "yes" ]; then
                perms="Read "
            fi
            
            # Check write permission
            if [ "$write_access" = "yes" ]; then
                perms="${perms}Write "
            fi
            
            # Check execute permission
            if [ -x "$socket" ]; then
                perms="${perms}Execute "
            fi
            
            if [ "$mode" = "777" ] || [ "$mode" = "666" ]; then
                perms="${perms}(Weak Permissions: $mode) "
            fi
            
            echo "$perms"
        }

        # Function to check socket connectivity
        check_socket_connectivity() {
            local socket="$1"
            local perms="$2"
            
            if [ "$EXTRA_CHECKS" ] && command -v curl >/dev/null 2>&1; then
                # Try to connect to the socket
                if curl -v --unix-socket "$socket" --max-time 1 http:/linpeas 2>&1 | grep -iq "Permission denied"; then
                    perms="${perms} - Cannot Connect"
                else
                    perms="${perms} - Can Connect"
                fi
            fi
            
            echo "$perms"
        }

        # Function to analyze socket protocol
        analyze_socket_protocol() {
            local socket="$1"
            local owner="$2"
            local response=""
            
            # Try to get HTTP response
            if command -v curl >/dev/null 2>&1; then
                response=$(curl --max-time 2 --unix-socket "$socket" http:/index 2>/dev/null)
                if [ $? -eq 0 ]; then
                    echo "  └─ HTTP Socket (owned by $owner):" | sed -${E} "s,$groupsB,${SED_RED},g" | sed -${E} "s,$groupsVB,${SED_RED},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_LIGHT_MAGENTA},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$knw_usrs,${SED_GREEN},g" | sed "s,root,${SED_RED}," | sed -${E} "s,$knw_grps,${SED_GREEN},g" | sed -${E} "s,$idB,${SED_RED},g"
                    echo "     └─ Response to /index (limit 30):"
                    echo "$response" | head -n 30 | sed 's/^/       /'
                fi
            fi
        }

        # Collect listening sockets using multiple methods
        unix_scks_list=""
        ss_metadata=""
        for cmd in "ss -xlp -H state listening" "ss -l -p -A 'unix'" "netstat -a -p --unix"; do
            if [ -z "$unix_scks_list" ]; then
                case "$cmd" in
                    ss\ *)
                        socket_listing=$($cmd 2>/dev/null)
                        unix_scks_list=$(printf '%s\n' "$socket_listing" | grep -Eo "/[a-zA-Z0-9\._/\-]+" | grep -v " " | sort -u)
                        if [ -n "$unix_scks_list" ]; then
                            # Reuse the ss output already collected; never query processes per socket.
                            ss_metadata=$(printf '%s\n' "$socket_listing" | awk '
                                match($0, /\/[a-zA-Z0-9._\/-]+/) {
                                    path = substr($0, RSTART, RLENGTH)
                                    pid = ""; uid = ""
                                    if (match($0, /pid=[0-9]+/)) pid = substr($0, RSTART + 4, RLENGTH - 4)
                                    if (match($0, /uid[:=][0-9]+/)) uid = substr($0, RSTART + 4, RLENGTH - 4)
                                    print path "|" pid "|" uid
                                }')
                            ss_metadata="
$ss_metadata"
                        fi
                        ;;
                    *)
                        unix_scks_list=$($cmd 2>/dev/null | grep -Eo "/[a-zA-Z0-9\._/\-]+" | grep -v " " | sort -u)
                        ;;
                esac
            fi
        done

        # Get additional socket information
        if [ -z "$unix_scks_list" ]; then
            unix_scks_list=$(lsof -U 2>/dev/null | awk '{print $9}' | grep "/" | sort -u)
        fi

        # Find socket files
        if ! [ "$SEARCH_IN_FOLDER" ]; then
            unix_scks_list2=$(find / -type s 2>/dev/null)
        else
            unix_scks_list2=$(find "$SEARCH_IN_FOLDER" -type s 2>/dev/null)
        fi

        # Process all found sockets
        (printf "%s\n" "$unix_scks_list" && printf "%s\n" "$unix_scks_list2") | sort -u | while read -r socket; do
            if [ -n "$socket" ] && [ -e "$socket" ]; then
                # Get socket information
                socket_info=$(stat -c '%a|%U|%G|%u' "$socket" 2>/dev/null)
                [ -n "$socket_info" ] || socket_info=$(stat -f '%Lp|%Su|%Sg|%u' "$socket" 2>/dev/null)
                if [ -n "$socket_info" ]; then
                    mode=${socket_info%%|*}
                    socket_info=${socket_info#*|}
                    owner=${socket_info%%|*}
                    socket_info=${socket_info#*|}
                    group=${socket_info%%|*}
                    owner_uid=${socket_info#*|}
                else
                    mode="unknown"
                    owner="unknown"
                    group="unknown"
                    owner_uid=""
                fi
                owner_info="$owner:$group"
                read_access="no"
                write_access="no"
                [ -r "$socket" ] && read_access="yes"
                [ -w "$socket" ] && write_access="yes"
                perms=$(get_socket_perms "$socket" "$mode" "$read_access" "$write_access")
                perms=$(check_socket_connectivity "$socket" "$perms")

                echo "Path: $socket"
                echo "  └─ Mode: $mode; Owner/Group: $owner_info"
                echo "  └─ Current user access: read=$read_access, write=$write_access"
                if [ -n "$perms" ]; then
                    echo "  └─ $perms" | sed -${E} "s,Cannot Connect,${SED_GREEN},g"
                fi

                # A discovered socket file can remain after its listener exits.
                listener_is_active=no
                case "
$unix_scks_list
" in
                    *"
$socket
"*) listener_is_active=yes ;;
                esac

                # Only display listener details that came with the existing ss listing.
                case "$ss_metadata" in
                    *"
$socket|"*)
                        listener_info=${ss_metadata#*"
$socket|"}
                        listener_info=${listener_info%%"
"*}
                        listener_pid=${listener_info%%|*}
                        listener_uid=${listener_info#*|}
                        [ -n "$listener_pid" ] && echo "  └─ ss listener PID: $listener_pid"
                        [ -n "$listener_uid" ] && echo "  └─ ss socket UID: $listener_uid"
                        ;;
                esac

                # Access alone does not establish what a service executes.
                if [ "$listener_is_active" = "yes" ] && [ "$owner_uid" = "0" ] && [ "$write_access" = "yes" ]; then
                    echo "  └─ Review lead: current user can write to a root-owned Unix socket; inspect service behavior and privileges."
                fi

                # Preserve the optional protocol check when EXTRA_CHECKS found connectivity.
                if echo "$perms" | grep -q "Can Connect"; then
                    analyze_socket_protocol "$socket" "$owner_info"
                fi
            fi
        done
    fi
    echo ""
fi
