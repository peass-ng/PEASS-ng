# Title: Cloud - check_gcp
# ID: check_gcp
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the script is running in GCP
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $is_gcp_vm, $is_gcp_function
# Fat linpeas: 0
# Small linpeas: 1


check_gcp(){
  is_gcp_vm="No"
  is_gcp_function="No"
  if grep -q metadata.google.internal /etc/hosts 2>/dev/null || (curl --connect-timeout 2 metadata.google.internal >/dev/null 2>&1 && [ "$?" -eq "0" ]) || (wget --timeout 2 --tries 1 metadata.google.internal >/dev/null 2>&1 && [ "$?" -eq "0" ]); then
    is_gcp_vm="Yes"
  fi
  # CHeck if /workspace exists
  if [ -d "/workspace" ] && [ -d "/layers" ]; then
    is_gcp_vm="No"
    is_gcp_function="Yes"
  fi
}