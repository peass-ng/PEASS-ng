# Title: Cloud - Ali Cloud
# ID: CL_Ali_Cloud
# Author: Esonhugh
# Last Update: 22-01-2024
# Description: Ali Cloud Platform Enumeration
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, print_info
# Global Variables: $is_aliyun_ecs
# Initial Functions: check_aliyun_ecs
# Generated Global Variables: $aliyun_req, $aliyun_token, $i_hostname, $i_instance_id, $i_instance_name, $i_instance_type, $i_aliyun_owner_account, $i_region_id, $i_zone_id, $i_pub_ipv4, $i_priv_ipv4, $net_dns, $mac, $sa, $key
# Fat linpeas: 0
# Small linpeas: 1



if [ "$is_aliyun_ecs" = "Yes" ]; then
  aliyun_req=""
  aliyun_token=""
  if [ "$(command -v curl)" ]; then 
    aliyun_token=$(curl -X PUT "http://100.100.100.200/latest/api/token" -H "X-aliyun-ecs-metadata-token-ttl-seconds:1000")
    aliyun_req='curl -s -f -L -H "X-aliyun-ecs-metadata-token: $aliyun_token"'
  elif [ "$(command -v wget)" ]; then
    aliyun_token=$(wget -q -O - --method PUT "http://100.100.100.200/latest/api/token" --header "X-aliyun-ecs-metadata-token-ttl-seconds:1000")
    aliyun_req='wget -q -O --header "X-aliyun-ecs-metadata-token: $aliyun_token"'
  else 
    echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
  fi

  if [ "$aliyun_token" ]; then
    print_2title "Aliyun ECS Enumeration"
    print_info "https://help.aliyun.com/zh/ecs/user-guide/view-instance-metadata"

    echo ""
    print_3title "Instance Info"
    i_hostname=$(eval $aliyun_req http://100.100.100.200/latest/meta-data/hostname)
    [ "$i_hostname" ] && echo "Hostname: $i_hostname"
    i_instance_id=$(eval $aliyun_req http://100.100.100.200/latest/meta-data/instance-id)
    [ "$i_instance_id" ] && echo "Instance ID: $i_instance_id"
    # no dup of hostname if in ACK it possibly leaks aliyun cluster service ClusterId
    i_instance_name=$(eval $aliyun_req http://100.100.100.200/latest/meta-data/instance/instance-name)
    [ "$i_instance_name" ] && echo "Instance Name: $i_instance_name"
    i_instance_type=$(eval $aliyun_req http://100.100.100.200/latest/meta-data/instance/instance-type)
    [ "$i_instance_type" ] && echo "Instance Type: $i_instance_type"
    i_aliyun_owner_account=$(eval $aliyun_req http://i00.100.100.200/latest/meta-data/owner-account-id)
    [ "$i_aliyun_owner_account" ] && echo "Aliyun Owner Account: $i_aliyun_owner_account"
    i_region_id=$(eval $aliyun_req http://100.100.100.200/latest/meta-data/region-id)
    [ "$i_region_id" ] && echo "Region ID: $i_region_id"
    i_zone_id=$(eval $aliyun_req http://100.100.100.200/latest/meta-data/zone-id)
    [ "$i_zone_id" ] && echo "Zone ID: $i_zone_id"

    echo ""
    print_3title "Network Info"
    i_pub_ipv4=$(eval $aliyun_req http://100.100.100.200/latest/meta-data/public-ipv4)
    [ "$i_pub_ipv4" ] && echo "Public IPv4: $i_pub_ipv4"
    i_priv_ipv4=$(eval $aliyun_req http://100.100.100.200/latest/meta-data/private-ipv4)
    [ "$i_priv_ipv4" ] && echo "Private IPv4: $i_priv_ipv4"
    net_dns=$(eval $aliyun_req  http://100.100.100.200/latest/meta-data/dns-conf/nameservers)
    [ "$net_dns" ] && echo "DNS: $net_dns"
    
    echo "========"
    for mac in $(eval $aliyun_req  http://100.100.100.200/latest/meta-data/network/interfaces/macs/); do
      echo "  Mac: $mac"
      echo "  Mac interface id: "$(eval $aliyun_req http://100.100.100.200/latest/meta-data/network/interfaces/macs/$mac/network-interface-id)
      echo "  Mac netmask: "$(eval $aliyun_req http://100.100.100.200/latest/meta-data/network/interfaces/macs/$mac/netmask)
      echo "  Mac vpc id: "$(eval $aliyun_req http://100.100.100.200/latest/meta-data/network/interfaces/macs/$mac/vpc-id)
      echo "  Mac vpc cidr: "$(eval $aliyun_req http://100.100.100.200/latest/meta-data/network/interfaces/macs/$mac/vpc-cidr-block)
      echo "  Mac vpc cidr (v6): "$(eval $aliyun_req http://100.100.100.200/latest/meta-data/network/interfaces/macs/$mac/vpc-ipv6-cidr-blocks)
      echo "  Mac vswitch id: "$(eval $aliyun_req http://100.100.100.200/latest/meta-data/network/interfaces/macs/$mac/vswitch-id)
      echo "  Mac vswitch cidr: "$(eval $aliyun_req http://100.100.100.200/latest/meta-data/network/interfaces/macs/$mac/vswitch-cidr-block)
      echo "  Mac vswitch cidr (v6): "$(eval $aliyun_req http://100.100.100.200/latest/meta-data/network/interfaces/macs/$mac/vswitch-ipv6-cidr-block)
      echo "  Mac private ips: "$(eval $aliyun_req http://100.100.100.200/latest/meta-data/network/interfaces/macs/$mac/private-ipv4s)
      echo "  Mac private ips (v6): "$(eval $aliyun_req http://100.100.100.200/latest/meta-data/network/interfaces/macs/$mac/ipv6s)
      echo "  Mac gateway: "$(eval $aliyun_req http://100.100.100.200/latest/meta-data/network/interfaces/macs/$mac/gateway)
      echo "  Mac gateway (v6): "$(eval $aliyun_req http://100.100.100.200/latest/meta-data/network/interfaces/macs/$mac/ipv6-gateway)
      echo "======="
    done

    echo ""
    print_3title "Service account "
    for sa in $(eval $aliyun_req "http://100.100.100.200/latest/meta-data/ram/security-credentials/"); do 
      echo "  Name: $sa"
      echo "  STS Token: "$(eval $aliyun_req "http://100.100.100.200/latest/meta-data/ram/security-credentials/$sa")
      echo "  =============="
    done

    echo ""
    print_3title "Possbile admin ssh Public keys"
    for key in $(eval $aliyun_req "http://100.100.100.200/latest/meta-data/public-keys/"); do
      echo "  Name: $key"
      echo "  Key: "$(eval $aliyun_req "http://100.100.100.200/latest/meta-data/public-keys/${key}openssh-key")
      echo "  =============="
    done


  fi
fi

