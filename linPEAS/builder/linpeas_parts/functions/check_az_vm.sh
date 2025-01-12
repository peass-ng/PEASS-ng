# Title: Cloud - check_az_vm
# ID: check_az_vm
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the script is running in Azure VM
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $is_az_vm, $meta_response
# Fat linpeas: 0
# Small linpeas: 1


check_az_vm(){
  is_az_vm="No"

  # 1. Check if the Azure log directory exists
  if [ -d "/var/log/azure/" ]; then
    is_az_vm="Yes"

  # 2. Check if 'reddog.microsoft.com' is found in /etc/resolv.conf
  elif grep -q "search reddog.microsoft.com" /etc/resolv.conf 2>/dev/null; then
    is_az_vm="Yes"

  else
    # 3. Try querying the Azure Metadata Service for more wide support (e.g. Azure Container Registry tasks need this)
    if command -v curl &> /dev/null; then
      meta_response=$(curl -s --max-time 2 \
        "http://169.254.169.254/metadata/identity/oauth2/token")
      if echo "$meta_response" | grep -q "Missing"; then
        is_az_vm="Yes"
      fi
    elif command -v wget &> /dev/null; then
      meta_response=$(wget -qO- --timeout=2 \
        "http://169.254.169.254/metadata/identity/oauth2/token")
      if echo "$meta_response" | grep -q "Missing"; then
        is_az_vm="Yes"
      fi
    fi
  fi
}
