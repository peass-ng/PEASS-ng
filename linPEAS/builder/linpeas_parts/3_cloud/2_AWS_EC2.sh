# Title: Cloud - AWS EC2
# ID: CL_AWS_EC2
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: AWS EC2 Enumeration
# License: GNU GPL
# Version: 1.0
# Functions Used: check_aws_ec2, exec_with_jq, print_2title, print_3title
# Global Variables: $is_aws_ec2
# Initial Functions: check_aws_ec2
# Generated Global Variables: $aws_req, $HEADER, $URL, $mac, $role, $TOKEN, $TOKEN_HEADER, $TOKEN_TTL
# Fat linpeas: 0
# Small linpeas: 1


if [ "$is_aws_ec2" = "Yes" ]; then
    print_2title "AWS EC2 Enumeration"
    
    TOKEN=""
    TOKEN_HEADER="X-aws-ec2-metadata-token"
    TOKEN_TTL="X-aws-ec2-metadata-token-ttl-seconds: 21600"
    URL="http://169.254.169.254/latest/meta-data"
    
    aws_req=""
    if [ "$(command -v curl || echo -n '')" ]; then
        # Get token for IMDSv2
        TOKEN=$(curl -s -f -X PUT "http://169.254.169.254/latest/api/token" -H "$TOKEN_TTL" 2>/dev/null)
        aws_req="curl -s -f -L -H '$TOKEN_HEADER: $TOKEN'"
    elif [ "$(command -v wget || echo -n '')" ]; then
        # Get token for IMDSv2
        TOKEN=$(wget -q -O - --method=PUT --header="$TOKEN_TTL" "http://169.254.169.254/latest/api/token" 2>/dev/null)
        aws_req="wget -q -O - --header '$TOKEN_HEADER: $TOKEN'"
    else 
        echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
    fi
  
    if [ "$aws_req" ]; then
        printf "ami-id: "; eval $aws_req "$URL/ami-id"; echo ""
        printf "instance-action: "; eval $aws_req "$URL/instance-action"; echo ""
        printf "instance-id: "; eval $aws_req "$URL/instance-id"; echo ""
        printf "instance-life-cycle: "; eval $aws_req "$URL/instance-life-cycle"; echo ""
        printf "instance-type: "; eval $aws_req "$URL/instance-type"; echo ""
        printf "region: "; eval $aws_req "$URL/placement/region"; echo ""

        echo ""
        print_3title "Account Info"
        exec_with_jq eval $aws_req "$URL/identity-credentials/ec2/info"; echo ""

        echo ""
        print_3title "Network Info"
        for mac in $(eval $aws_req "$URL/network/interfaces/macs/" 2>/dev/null); do 
          echo "Mac: $mac"
          printf "Owner ID: "; eval $aws_req "$URL/network/interfaces/macs/$mac/owner-id"; echo ""
          printf "Public Hostname: "; eval $aws_req "$URL/network/interfaces/macs/$mac/public-hostname"; echo ""
          printf "Security Groups: "; eval $aws_req "$URL/network/interfaces/macs/$mac/security-groups"; echo ""
          echo "Private IPv4s:"; eval $aws_req "$URL/network/interfaces/macs/$mac/ipv4-associations/"; echo ""
          printf "Subnet IPv4: "; eval $aws_req "$URL/network/interfaces/macs/$mac/subnet-ipv4-cidr-block"; echo ""
          echo "PrivateIPv6s:"; eval $aws_req "$URL/network/interfaces/macs/$mac/ipv6s"; echo ""
          printf "Subnet IPv6: "; eval $aws_req "$URL/network/interfaces/macs/$mac/subnet-ipv6-cidr-blocks"; echo ""
          echo "Public IPv4s:"; eval $aws_req "$URL/network/interfaces/macs/$mac/public-ipv4s"; echo ""
          echo ""
        done

        echo ""
        print_3title "IAM Role"
        exec_with_jq eval $aws_req "$URL/iam/info"; echo ""
        for role in $(eval $aws_req "$URL/iam/security-credentials/" 2>/dev/null); do 
          echo "Role: $role"
          exec_with_jq eval $aws_req "$URL/iam/security-credentials/$role"; echo ""
          echo ""
        done
        
        echo ""
        print_3title "User Data"
        eval $aws_req "http://169.254.169.254/latest/user-data"; echo ""
        
        echo ""
        print_3title "EC2 Security Credentials"
        exec_with_jq eval $aws_req "$URL/identity-credentials/ec2/security-credentials/ec2-instance"; echo ""
        
        print_3title "SSM Runnig"
        ps aux 2>/dev/null | grep "ssm-agent" | grep -Ev "grep|sed s,ssm-agent" | sed "s,ssm-agent,${SED_RED},"
    fi
    echo ""
fi