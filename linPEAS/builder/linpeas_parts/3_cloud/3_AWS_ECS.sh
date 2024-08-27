# Title: Cloud - AWS ECS
# ID: CL_AWS_ECS
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: AWS ECS Enumeration
# License: GNU GPL
# Version: 1.0
# Functions Used: check_aws_ecs, exec_with_jq, print_2title, print_3title
# Global Variables: $aws_ecs_metadata_uri, $aws_ecs_service_account_uri, $is_aws_ecs
# Initial Functions: check_aws_ecs
# Generated Global Variables: $aws_ecs_req
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
    echo ""
fi