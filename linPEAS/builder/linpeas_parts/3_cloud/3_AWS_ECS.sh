# Title: Cloud - AWS ECS
# ID: CL_AWS_ECS
# Author: Carlos Polop
# Last Update: 17-01-2026
# Description: AWS ECS Enumeration
# License: GNU GPL
# Version: 1.0
# Functions Used: check_aws_ecs, exec_with_jq, print_2title, print_3title
# Global Variables: $aws_ecs_metadata_uri, $aws_ecs_service_account_uri, $is_aws_ecs
# Initial Functions: check_aws_ecs
# Generated Global Variables: $aws_ecs_req, $aws_exec_env, $ecs_task_metadata, $launch_type, $network_modes, $imds_tool, $imds_token, $imds_roles, $imds_http_code, $ecs_block_line, $ecs_host_line, $iptables_cmd, $docker_rules, $first_role
# Fat linpeas: 0
# Small linpeas: 1


if [ "$is_aws_ecs" = "Yes" ]; then
    print_2title "AWS ECS Enumeration"
    
    aws_ecs_req=""
    if [ "$(command -v curl || echo -n '')" ]; then
        aws_ecs_req='curl -s -f'
    elif [ "$(command -v wget || echo -n '')" ]; then
        aws_ecs_req='wget -q -O -'
    else 
        echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
    fi

    if [ "$aws_ecs_metadata_uri" ]; then
        print_3title "Container Info"
        exec_with_jq eval $aws_ecs_req "$aws_ecs_metadata_uri"
        echo ""
        
        print_3title "Task Info"
        exec_with_jq eval $aws_ecs_req "$aws_ecs_metadata_uri/task"
        echo ""
    else
        echo "I couldn't find ECS_CONTAINER_METADATA_URI env var to get container info"
    fi

    if [ "$aws_ecs_service_account_uri" ]; then
        print_3title "IAM Role"
        exec_with_jq eval $aws_ecs_req "$aws_ecs_service_account_uri"
        echo ""
    else
        echo "I couldn't find AWS_CONTAINER_CREDENTIALS_RELATIVE_URI env var to get IAM role info (the task is running without a task role probably)"
    fi

    print_3title "ECS task metadata hints"
    aws_exec_env=$(printenv AWS_EXECUTION_ENV 2>/dev/null)
    if [ "$aws_exec_env" ]; then
        printf "AWS_EXECUTION_ENV=%s\n" "$aws_exec_env"
    fi

    ecs_task_metadata=""
    if [ "$aws_ecs_metadata_uri" ]; then
        ecs_task_metadata=$(eval $aws_ecs_req "$aws_ecs_metadata_uri/task" 2>/dev/null)
    fi

    if [ "$ecs_task_metadata" ]; then
        launch_type=$(printf "%s" "$ecs_task_metadata" | grep -oE '"LaunchType":"[^"]+"' | head -n 1 | cut -d '"' -f4)
        if [ "$launch_type" ]; then
            printf "ECS LaunchType reported: %s\n" "$launch_type"
        fi
        network_modes=$(printf "%s" "$ecs_task_metadata" | grep -oE '"NetworkMode":"[^"]+"' | cut -d '"' -f4 | sort -u | tr '\n' ' ')
        if [ "$network_modes" ]; then
            printf "Reported NetworkMode(s): %s\n" "$network_modes"
        fi
    else
        echo "Unable to fetch task metadata (check ECS_CONTAINER_METADATA_URI)."
    fi
    echo ""

    print_3title "IMDS reachability from this task"
    imds_token=""
    imds_roles=""
    imds_http_code=""
    imds_tool=""

    if command -v curl >/dev/null 2>&1; then
        imds_tool="curl"
    elif command -v wget >/dev/null 2>&1; then
        imds_tool="wget"
    fi

    if [ "$imds_tool" = "curl" ]; then
        imds_token=$(curl -s --connect-timeout 2 --max-time 2 -X PUT "http://169.254.169.254/latest/api/token" -H "X-aws-ec2-metadata-token-ttl-seconds: 21600" 2>/dev/null)
        if [ "$imds_token" ]; then
            printf "[!] IMDSv2 token request succeeded (metadata reachable from this task).\n"
            imds_roles=$(curl -s --connect-timeout 2 --max-time 2 -H "X-aws-ec2-metadata-token: $imds_token" "http://169.254.169.254/latest/meta-data/iam/security-credentials/" 2>/dev/null | tr '\n' ' ')
            if [ "$imds_roles" ]; then
                printf "    Instance profile role(s) exposed via IMDS: %s\n" "$imds_roles"
                first_role=$(printf "%s" "$imds_roles" | awk '{print $1}')
                if [ "$first_role" ]; then
                    printf "    Example: curl -H 'X-aws-ec2-metadata-token: <TOKEN>' http://169.254.169.254/latest/meta-data/iam/security-credentials/%s\n" "$first_role"
                fi
            else
                printf "    No IAM role names returned (instance profile might be missing).\n"
            fi
        else
            imds_http_code=$(curl -s -o /dev/null -w "%{http_code}" --connect-timeout 2 --max-time 2 "http://169.254.169.254/latest/meta-data/" 2>/dev/null)
            case "$imds_http_code" in
                000|"")
                    printf "[i] IMDS endpoint did not respond (likely blocked via hop-limit or host firewalling).\n"
                    ;;
                401)
                    printf "[i] IMDS requires v2 tokens but token requests are being blocked (bridge-mode tasks rely on this when hop limit = 1).\n"
                    ;;
                *)
                    printf "[i] IMDS GET returned HTTP %s (investigate host configuration).\n" "$imds_http_code"
                    ;;
            esac
        fi
    elif [ "$imds_tool" = "wget" ]; then
        imds_token=$(wget -q -O - --timeout=2 --tries=1 --method=PUT --header="X-aws-ec2-metadata-token-ttl-seconds: 21600" "http://169.254.169.254/latest/api/token" 2>/dev/null)
        if [ "$imds_token" ]; then
            printf "[!] IMDSv2 token request succeeded (metadata reachable from this task).\n"
            imds_roles=$(wget -q -O - --timeout=2 --tries=1 --header="X-aws-ec2-metadata-token: $imds_token" "http://169.254.169.254/latest/meta-data/iam/security-credentials/" 2>/dev/null | tr '\n' ' ')
            if [ "$imds_roles" ]; then
                printf "    Instance profile role(s) exposed via IMDS: %s\n" "$imds_roles"
            else
                printf "    No IAM role names returned (instance profile might be missing).\n"
            fi
        else
            wget --server-response -O /dev/null --timeout=2 --tries=1 "http://169.254.169.254/latest/meta-data/" 2>&1 | awk 'BEGIN{code=""} /^  HTTP/{code=$2} END{ if(code!="") { printf("[i] IMDS GET returned HTTP %s (token could not be retrieved).\n", code); } else { print "[i] IMDS endpoint did not respond (likely blocked)."; } }'
        fi
    else
        echo "Neither curl nor wget were found, I can't test IMDS reachability."
    fi
    echo ""

    print_3title "ECS agent IMDS settings"
    if [ -r "/etc/ecs/ecs.config" ]; then
        ecs_block_line=$(grep -E "^ECS_AWSVPC_BLOCK_IMDS=" /etc/ecs/ecs.config 2>/dev/null | tail -n 1)
        ecs_host_line=$(grep -E "^ECS_ENABLE_TASK_IAM_ROLE_NETWORK_HOST=" /etc/ecs/ecs.config 2>/dev/null | tail -n 1)
        if [ "$ecs_block_line" ]; then
            printf "%s\n" "$ecs_block_line"
            if echo "$ecs_block_line" | grep -qi "=true"; then
                echo "    -> awsvpc-mode tasks should be blocked from IMDS by the ECS agent."
            else
                echo "    -> awsvpc-mode tasks can still reach IMDS (set this to true to block)."
            fi
        else
            echo "ECS_AWSVPC_BLOCK_IMDS not set (awsvpc tasks inherit host IMDS reachability)."
        fi

        if [ "$ecs_host_line" ]; then
            printf "%s\n" "$ecs_host_line"
            if echo "$ecs_host_line" | grep -qi "=false"; then
                echo "    -> Host-network tasks lose IAM task roles but IMDS is blocked."
            else
                echo "    -> Host-network tasks keep IAM task roles and retain IMDS access."
            fi
        else
            echo "ECS_ENABLE_TASK_IAM_ROLE_NETWORK_HOST not set (defaults keep IMDS reachable for host-mode tasks)."
        fi
    else
        echo "Cannot read /etc/ecs/ecs.config (file missing or permissions denied)."
    fi
    echo ""

    print_3title "DOCKER-USER IMDS filtering"
    iptables_cmd=""
    if command -v iptables >/dev/null 2>&1; then
        iptables_cmd=$(command -v iptables)
    elif command -v iptables-nft >/dev/null 2>&1; then
        iptables_cmd=$(command -v iptables-nft)
    fi

    if [ "$iptables_cmd" ]; then
        docker_rules=$($iptables_cmd -S DOCKER-USER 2>/dev/null)
        if [ $? -eq 0 ]; then
            if [ "$docker_rules" ]; then
                echo "$docker_rules"
            else
                echo "(DOCKER-USER chain exists but no rules were found)"
            fi
            if echo "$docker_rules" | grep -q "169\\.254\\.169\\.254"; then
                echo "    -> IMDS traffic is explicitly filtered before Docker NAT."
            else
                echo "    -> No DOCKER-USER rule drops 169.254.169.254 traffic (bridge tasks rely on hop limit or host firewalling)."
            fi
        else
            echo "Unable to read DOCKER-USER chain (missing chain or insufficient permissions)."
        fi
    else
        echo "iptables binary not found; cannot inspect DOCKER-USER chain."
    fi
    echo ""
fi
