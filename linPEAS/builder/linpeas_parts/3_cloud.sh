###########################################
#-----------) Cloud functions (-----------#
###########################################

GCP_GOOD_SCOPES="/devstorage.read_only|/logging.write|/monitoring|/servicecontrol|/service.management.readonly|/trace.append"
GCP_BAD_SCOPES="/cloud-platform|/compute"

exec_with_jq(){
  if [ "$(command -v jq)" ]; then 
    $@ | jq;
   else 
    $@;
   fi
}

check_gcp(){
  is_gcp="No"
  if grep -q metadata.google.internal /etc/hosts 2>/dev/null || (curl --connect-timeout 2 metadata.google.internal >/dev/null 2>&1 && [ "$?" -eq "0" ]) || (wget --timeout 2 --tries 1 metadata.google.internal >/dev/null 2>&1 && [ "$?" -eq "0" ]); then
    is_gcp="Yes"
  fi
}

check_aws_ecs(){
  is_aws_ecs="No"
  if (env | grep -q ECS_CONTAINER_METADATA_URI_v4); then
    is_aws_ecs="Yes";
    aws_ecs_metadata_uri=$ECS_CONTAINER_METADATA_URI_v4;
    aws_ecs_service_account_uri="http://169.254.170.2$AWS_CONTAINER_CREDENTIALS_RELATIVE_URI"
  
  elif (env | grep -q ECS_CONTAINER_METADATA_URI); then
    is_aws_ecs="Yes";
    aws_ecs_metadata_uri=$ECS_CONTAINER_METADATA_URI;
    aws_ecs_service_account_uri="http://169.254.170.2$AWS_CONTAINER_CREDENTIALS_RELATIVE_URI"
  
  elif (env | grep -q AWS_CONTAINER_CREDENTIALS_RELATIVE_URI); then
    is_aws_ecs="Yes";
    
  
  elif (curl --connect-timeout 2 "http://169.254.170.2/v2/credentials/" >/dev/null 2>&1 && [ "$?" -eq "0" ]) || (wget --timeout 2 --tries 1 "http://169.254.170.2/v2/credentials/" >/dev/null 2>&1 && [ "$?" -eq "0" ]); then
    is_aws_ecs="Yes";

  fi
  
  if [ "$AWS_CONTAINER_CREDENTIALS_RELATIVE_URI" ]; then
    aws_ecs_service_account_uri="http://169.254.170.2$AWS_CONTAINER_CREDENTIALS_RELATIVE_URI"
  fi
}

check_aws_ec2(){
  is_aws_ec2="No"

  if [ -d "/var/log/amazon/" ]; then
    is_aws_ec2="Yes"
    EC2_TOKEN=$(curl --connect-timeout 2 -X PUT "http://169.254.169.254/latest/api/token" -H "X-aws-ec2-metadata-token-ttl-seconds: 21600" 2>/dev/null || wget --timeout 2 --tries 1 -q -O - --method PUT "http://169.254.169.254/latest/api/token" --header "X-aws-ec2-metadata-token-ttl-seconds: 21600" 2>/dev/null)

  else
    EC2_TOKEN=$(curl --connect-timeout 2 -X PUT "http://169.254.169.254/latest/api/token" -H "X-aws-ec2-metadata-token-ttl-seconds: 21600" 2>/dev/null || wget --timeout 2 --tries 1 -q -O - --method PUT "http://169.254.169.254/latest/api/token" --header "X-aws-ec2-metadata-token-ttl-seconds: 21600" 2>/dev/null)
    if [ "$(echo $EC2_TOKEN | cut -c1-2)" = "AQ" ]; then
      is_aws_ec2="Yes"
    fi
  fi
}

check_aws_lambda(){
  is_aws_lambda="No"

  if (env | grep -q AWS_LAMBDA_); then
    is_aws_lambda="Yes"
  fi
}


check_gcp
print_list "Google Cloud Platform? ............... $is_gcp\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
check_aws_ecs
print_list "AWS ECS? ............................. $is_aws_ecs\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
check_aws_ec2
print_list "AWS EC2? ............................. $is_aws_ec2\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"
check_aws_lambda
print_list "AWS Lambda? .......................... $is_aws_lambda\n"$NC | sed "s,Yes,${SED_RED}," | sed "s,No,${SED_GREEN},"

echo ""

if [ "$is_gcp" = "Yes" ]; then
    gcp_req=""
    if [ "$(command -v curl)" ]; then
        gcp_req='curl -s -f  -H "X-Google-Metadata-Request: True"'
    elif [ "$(command -v wget)" ]; then
        gcp_req='wget -q -O - --header "X-Google-Metadata-Request: True"'
    else 
        echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
    fi


    if [ "$gcp_req" ]; then
        print_2title "Google CLoud Platform Enumeration"
        print_info "https://book.hacktricks.xyz/cloud-security/gcp-security"

        ## GC Project Info
        p_id=$(eval $gcp_req 'http://metadata.google.internal/computeMetadata/v1/project/project-id')
        [ "$p_id" ] && echo "Project-ID: $p_id"
        p_num=$(eval $gcp_req 'http://metadata.google.internal/computeMetadata/v1/project/numeric-project-id')
        [ "$p_num" ] && echo "Project Number: $p_num"
        pssh_k=$(eval $gcp_req 'http://metadata.google.internal/computeMetadata/v1/project/attributes/ssh-keys')
        [ "$pssh_k" ] && echo "Project SSH-Keys: $pssh_k"
        p_attrs=$(eval $gcp_req 'http://metadata.google.internal/computeMetadata/v1/project/attributes/?recursive=true')
        [ "$p_attrs" ] && echo "All Project Attributes: $p_attrs"

        # OSLogin Info
        osl_u=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/oslogin/users)
        [ "$osl_u" ] && echo "OSLogin users: $osl_u"
        osl_g=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/oslogin/groups)
        [ "$osl_g" ] && echo "OSLogin Groups: $osl_g"
        osl_sk=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/oslogin/security-keys)
        [ "$osl_sk" ] && echo "OSLogin Security Keys: $osl_sk"
        osl_au=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/oslogin/authorize)
        [ "$osl_au" ] && echo "OSLogin Authorize: $osl_au"

        # Instance Info
        inst_d=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/description)
        [ "$inst_d" ] && echo "Instance Description: "
        inst_hostn=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/hostname)
        [ "$inst_hostn" ] && echo "Hostname: $inst_hostn"
        inst_id=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/id)
        [ "$inst_id" ] && echo "Instance ID: $inst_id"
        inst_img=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/image)
        [ "$inst_img" ] && echo "Instance Image: $inst_img"
        inst_mt=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/machine-type)
        [ "$inst_mt" ] && echo "Machine Type: $inst_mt"
        inst_n=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/name)
        [ "$inst_n" ] && echo "Instance Name: $inst_n"
        inst_tag=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/scheduling/tags)
        [ "$inst_tag" ] && echo "Instance tags: $inst_tag"
        inst_zone=$(eval $gcp_req http://metadata.google.internal/computeMetadata/v1/instance/zone)
        [ "$inst_zone" ] && echo "Zone: $inst_zone"

        inst_k8s_loc=$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/attributes/cluster-location")
        [ "$inst_k8s_loc" ] && echo "K8s Cluster Location: $inst_k8s_loc"
        inst_k8s_name=$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/attributes/cluster-name")
        [ "$inst_k8s_name" ] && echo "K8s Cluster name: $inst_k8s_name"
        inst_k8s_osl_e=$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/attributes/enable-oslogin")
        [ "$inst_k8s_osl_e" ] && echo "K8s OSLoging enabled: $inst_k8s_osl_e"
        inst_k8s_klab=$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/attributes/kube-labels")
        [ "$inst_k8s_klab" ] && echo "K8s Kube-labels: $inst_k8s_klab"
        inst_k8s_kubec=$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/attributes/kubeconfig")
        [ "$inst_k8s_kubec" ] && echo "K8s Kubeconfig: $inst_k8s_kubec"
        inst_k8s_kubenv=$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/attributes/kube-env")
        [ "$inst_k8s_kubenv" ] && echo "K8s Kube-env: $inst_k8s_kubenv"

        echo ""
        print_3title "Interfaces"
        for iface in $(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/"); do 
            echo "  IP: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/$iface/ip")
            echo "  Subnetmask: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/$iface/subnetmask")
            echo "  Gateway: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/$iface/gateway")
            echo "  DNS: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/$iface/dns-servers")
            echo "  Network: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/network-interfaces/$iface/network")
            echo "  ==============  "
        done

        echo ""
        print_3title "Service Accounts"
        for sa in $(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/"); do 
            echo "  Name: $sa"
            echo "  Email: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/$sa/email")
            echo "  Aliases: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/$sa/aliases")
            echo "  Identity: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/$sa/identity")
            echo "  Scopes: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/$sa/scopes") | sed -${E} "s,${GCP_GOOD_SCOPES},${SED_GREEN},g" | sed -${E} "s,${GCP_BAD_SCOPES},${SED_RED},g"
            echo "  Token: "$(eval $gcp_req "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/$sa/token")
            echo "  ==============  "
        done
    fi
fi


if [ "$is_aws_ecs" = "Yes" ]; then
    print_2title "AWS ECS Enumeration"
    
    aws_ecs_req=""
    if [ "$(command -v curl)" ]; then
        aws_ecs_req='curl -s -f'
    elif [ "$(command -v wget)" ]; then
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
fi

if [ "$is_aws_ec2" = "Yes" ]; then
    print_2title "AWS EC2 Enumeration"
    
    HEADER="X-aws-ec2-metadata-token: $EC2_TOKEN"
    URL="http://169.254.169.254/latest/meta-data"
    
    aws_req=""
    if [ "$(command -v curl)" ]; then
        aws_req="curl -s -f -H '$HEADER'"
    elif [ "$(command -v wget)" ]; then
        aws_req="wget -q -O - -H '$HEADER'"
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
        eval $aws_req "http://169.254.169.254/latest/user-data"
    fi
fi

if [ "$is_aws_lambda" = "Yes" ]; then
  print_2title "AWS Lambda Enumeration"
  printf "Function name: "; env | grep AWS_LAMBDA_FUNCTION_NAME
  printf "Region: "; env | grep AWS_REGION
  printf "Secret Access Key: "; env | grep AWS_SECRET_ACCESS_KEY
  printf "Access Key ID: "; env | grep AWS_ACCESS_KEY_ID
  printf "Session token: "; env | grep AWS_SESSION_TOKEN
  printf "Security token: "; env | grep AWS_SECURITY_TOKEN
  printf "Runtime API: "; env | grep AWS_LAMBDA_RUNTIME_API
  printf "Event data: "; (curl -s "http://${AWS_LAMBDA_RUNTIME_API}/2018-06-01/runtime/invocation/next" 2>/dev/null || wget -q -O - "http://${AWS_LAMBDA_RUNTIME_API}/2018-06-01/runtime/invocation/next")
fi

