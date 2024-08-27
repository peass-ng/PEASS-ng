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
# Generated Global Variables: $is_az_vm
# Fat linpeas: 0
# Small linpeas: 1


check_az_vm(){
  is_az_vm="No"

  if [ -d "/var/log/azure/" ]; then
    is_az_vm="Yes"
  
  elif cat /etc/resolv.conf 2>/dev/null | grep -q "search reddog.microsoft.com"; then
    is_az_vm="Yes"
  fi
}