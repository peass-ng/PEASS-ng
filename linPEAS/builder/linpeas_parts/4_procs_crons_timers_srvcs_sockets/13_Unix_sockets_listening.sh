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
# Functions Used: print_2title, print_info
# Global Variables: $EXTRA_CHECKS, $groupsB, $groupsVB, $IAMROOT, $idB, $knw_grps, $knw_usrs, $nosh_usrs, $SEARCH_IN_FOLDER, $sh_usrs, $USER, $SED_RED, $SED_GREEN, $NC, $RED
# Initial Functions:
# Generated Global Variables: $unix_scks_list, $unix_scks_list2, $perms, $owner, $owner_info, $response, $socket, $cmd, $mode, $group
# Fat linpeas: 0
# Small linpeas: 0

if ! [ "$IAMROOT" ]; then
    if ! [ "$SEARCH_IN_FOLDER" ]; then
        print_2title "Unix Sockets Analysis"
        print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#sockets"


        # Function to get socket permissions
        get_socket_perms() {
            local socket="$1"
            local perms=""
            
            # Check read permission
            if [ -r "$socket" ]; then
                perms="Read "
            fi
            
            # Check write permission
            if [ -w "$socket" ]; then
                perms="${perms}Write "
            fi
            
            # Check execute permission
            if [ -x "$socket" ]; then
                perms="${perms}Execute "
            fi
            
            # Check socket mode
            local mode=$(stat -c "%a" "$socket" 2>/dev/null)
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

        # Function to get socket owner and group
        get_socket_owner() {
            local socket="$1"
            local owner=""
            local group=""
            
            if [ -e "$socket" ]; then
                owner=$(ls -l "$socket" 2>/dev/null | awk '{print $3}')
                group=$(ls -l "$socket" 2>/dev/null | awk '{print $4}')
                echo "$owner:$group"
            fi
        }

        # Collect listening sockets using multiple methods
        unix_scks_list=""
        for cmd in "ss -xlp -H state listening" "ss -l -p -A 'unix'" "netstat -a -p --unix"; do
            if [ -z "$unix_scks_list" ]; then
                unix_scks_list=$($cmd 2>/dev/null | grep -Eo "/[a-zA-Z0-9\._/\-]+" | grep -v " " | sort -u)
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
                perms=$(get_socket_perms "$socket")
                perms=$(check_socket_connectivity "$socket" "$perms")
                owner_info=$(get_socket_owner "$socket")
                
                # Print socket information
                if [ -z "$perms" ]; then
                    echo "$socket" | sed -${E} "s,$socket,${SED_GREEN},g"
                else
                    echo "$socket" | sed -${E} "s,$socket,${SED_RED},g"
                    echo "  └─(${RED}${perms}${NC})" | sed -${E} "s,Cannot Connect,${SED_GREEN},g"
                    
                    # Analyze socket protocol if we can connect
                    if echo "$perms" | grep -q "Can Connect"; then
                        analyze_socket_protocol "$socket" "$owner_info"
                    fi
                    
                    # Highlight dangerous ownership
                    if echo "$owner_info" | grep -q "root"; then
                        echo "  └─(${RED}Owned by root${NC})"
                    fi
                fi
            fi
        done
    fi
    echo ""
fi