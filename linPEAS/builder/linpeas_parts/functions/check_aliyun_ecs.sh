# Title: Cloud - check_aliyun_ecs
# ID: check_aliyun_ecs
# Author: Carlos Polop
# Last Update: 24-01-2024
# Description: Check if the script is running in Alibaba
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $is_aliyun_ecs
# Fat linpeas: 0
# Small linpeas: 1


check_aliyun_ecs(){
  is_aliyun_ecs="No"
  if [ -f "/etc/cloud/cloud.cfg.d/aliyun_cloud.cfg" ]; then 
    is_aliyun_ecs="Yes"
  fi
}