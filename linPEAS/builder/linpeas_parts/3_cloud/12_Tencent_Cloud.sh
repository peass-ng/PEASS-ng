# Title: Cloud - Tencent Cloud
# ID: CL_Tencent_Cloud
# Author: Shadowabi
# Last Update: 22-01-2024
# Description: Tencent Cloud Platform Enumeration
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, print_info
# Global Variables: $is_tencent_cvm
# Initial Functions: check_tencent_cvm
# Generated Global Variables: $tencent_req, $i_tencent_owner_account, $i_hostname, $i_instance_id, $i_instance_name, $i_instance_type, $i_region_id, $i_zone_id, $mac_tencent, $lipv4, $sa_tencent, $key_tencent
# Fat linpeas: 0
# Small linpeas: 1


if [ "$is_tencent_cvm" = "Yes" ]; then
  tencent_req=""
  if [ "$(command -v curl)" ]; then 
    tencent_req='curl --connect-timeout 2 -sfkG'
  elif [ "$(command -v wget)" ]; then
    tencent_req='wget -q --timeout 2 --tries 1  -O -'
  else 
    echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
  fi

  
    print_2title "Tencent CVM Enumeration"
    print_info "https://cloud.tencent.com/document/product/213/4934"
    # Todo: print_info "Hacktricks Documents needs to be updated"

    echo ""
    print_3title "Instance Info"
    i_tencent_owner_account=$(eval $tencent_req http://169.254.0.23/latest/meta-data/app-id)
    [ "$i_tencent_owner_account" ] && echo "Tencent Owner Account: $i_tencent_owner_account"
    i_hostname=$(eval $tencent_req http://169.254.0.23/latest/meta-data/hostname)
    [ "$i_hostname" ] && echo "Hostname: $i_hostname"
    i_instance_id=$(eval $tencent_req http://169.254.0.23/latest/meta-data/instance-id)
    [ "$i_instance_id" ] && echo "Instance ID: $i_instance_id"
    i_instance_id=$(eval $tencent_req http://169.254.0.23/latest/meta-data/uuid)
    [ "$i_instance_id" ] && echo "Instance ID: $i_instance_id"
    i_instance_name=$(eval $tencent_req http://169.254.0.23/latest/meta-data/instance-name)
    [ "$i_instance_name" ] && echo "Instance Name: $i_instance_name"
    i_instance_type=$(eval $tencent_req http://169.254.0.23/latest/meta-data/instance/instance-type)
    [ "$i_instance_type" ] && echo "Instance Type: $i_instance_type"
    i_region_id=$(eval $tencent_req http://169.254.0.23/latest/meta-data/placement/region)
    [ "$i_region_id" ] && echo "Region ID: $i_region_id"
    i_zone_id=$(eval $tencent_req http://169.254.0.23/latest/meta-data/placement/zone)
    [ "$i_zone_id" ] && echo "Zone ID: $i_zone_id"

    echo ""
    print_3title "Network Info"
    for mac_tencent in $(eval $tencent_req http://169.254.0.23/latest/meta-data/network/interfaces/macs/); do
      echo "  Mac: $mac_tencent"
      echo "  Primary IPv4: "$(eval $tencent_req http://169.254.0.23/latest/meta-data/network/interfaces/macs/$mac_tencent/primary-local-ipv4)
      echo "  Mac public ips: "$(eval $tencent_req http://169.254.0.23/latest/meta-data/network/interfaces/macs/$mac_tencent/public-ipv4s)
      echo "  Mac vpc id: "$(eval $tencent_req http://169.254.0.23/latest/meta-data/network/interfaces/macs/$mac_tencent/vpc-id)
      echo "  Mac subnet id: "$(eval $tencent_req http://169.254.0.23/latest/meta-data/network/interfaces/macs/$mac_tencent/subnet-id)
      
      for lipv4 in $(eval $tencent_req  http://169.254.0.23/latest/meta-data/network/interfaces/macs/$mac_tencent/local-ipv4s); do
        echo "  Mac local ips: "$(eval $tencent_req http://169.254.0.23/latest/meta-data/network/interfaces/macs/$mac_tencent/local-ipv4s/$lipv4/local-ipv4)
        echo "  Mac gateways: "$(eval $tencent_req http://169.254.0.23/latest/meta-data/network/interfaces/macs/$mac_tencent/local-ipv4s/$lipv4/gateway)
        echo "  Mac public ips: "$(eval $tencent_req http://169.254.0.23/latest/meta-data/network/interfaces/macs/$mac_tencent/local-ipv4s/$lipv4/public-ipv4)
        echo "  Mac public ips mode: "$(eval $tencent_req http://169.254.0.23/latest/meta-data/network/interfaces/macs/$mac_tencent/local-ipv4s/$lipv4/public-ipv4-mode)
        echo "  Mac subnet mask: "$(eval $tencent_req http://169.254.0.23/latest/meta-data/network/interfaces/macs/$mac_tencent/local-ipv4s/$lipv4/subnet-mask)
      done
    echo "======="
    done

    echo ""
    print_3title "Service account "
    for sa_tencent in $(eval $tencent_req "http://169.254.0.23/latest/meta-data/cam/security-credentials/"); do 
      echo "  Name: $sa_tencent"
      echo "  STS Token: "$(eval $tencent_req "http://169.254.0.23/latest/meta-data/cam/security-credentials/$sa_tencent")
      echo "  =============="
    done

    echo ""
    print_3title "Possbile admin ssh Public keys"
    for key_tencent in $(eval $tencent_req "http://169.254.0.23/latest/meta-data/public-keys/"); do
      echo "  Name: $key_tencent"
      echo "  Key: "$(eval $tencent_req "http://169.254.0.23/latest/meta-data/public-keys/${key_tencent}openssh-key")
      echo "  =============="
    done

    echo ""
    print_3title "User Data"
    eval $tencent_req http://169.254.0.23/latest/user-data; echo ""
fi