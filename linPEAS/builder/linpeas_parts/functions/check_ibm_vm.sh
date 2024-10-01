# Title: Cloud - check_ibm_vm
# ID: check_ibm_vm
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the script is running in IBM VM
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $is_ibm_vm, $IBM_TOKEN
# Fat linpeas: 0
# Small linpeas: 1


check_ibm_vm(){
  is_ibm_vm="No"
  if grep -q "nameserver 161.26.0.10" "/etc/resolv.conf" && grep -q "nameserver 161.26.0.11" "/etc/resolv.conf"; then
    curl --connect-timeout 2  "http://169.254.169.254" > /dev/null 2>&1 || wget --timeout 2 --tries 1  "http://169.254.169.254" > /dev/null 2>&1
    if [ "$?" -eq 0 ]; then
      IBM_TOKEN=$( ( curl -s -X PUT "http://169.254.169.254/instance_identity/v1/token?version=2022-03-01" -H "Metadata-Flavor: ibm" -H "Accept: application/json" 2> /dev/null | cut -d '"' -f4 ) || ( wget --tries 1 -O - --method PUT "http://169.254.169.254/instance_identity/v1/token?version=2022-03-01" --header "Metadata-Flavor: ibm" --header "Accept: application/json" 2>/dev/null | cut -d '"' -f4 ) )
      is_ibm_vm="Yes"
    fi
  fi
}