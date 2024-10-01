# Title: Cloud - Azure VM
# ID: CL_Azure_VM
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Azure VM Enumeration
# License: GNU GPL
# Version: 1.0
# Functions Used: check_az_vm, exec_with_jq, print_2title, print_3title
# Global Variables: $is_az_vm
# Initial Functions: check_az_vm
# Generated Global Variables: $API_VERSION, $HEADER, $az_req, $URL
# Fat linpeas: 0
# Small linpeas: 1


if [ "$is_az_vm" = "Yes" ]; then
  print_2title "Azure VM Enumeration"

  HEADER="Metadata:true"
  URL="http://169.254.169.254/metadata"
  API_VERSION="2021-12-13" #https://learn.microsoft.com/en-us/azure/virtual-machines/instance-metadata-service?tabs=linux#supported-api-versions
  
  az_req=""
  if [ "$(command -v curl || echo -n '')" ]; then
      az_req="curl -s -f -L -H '$HEADER'"
  elif [ "$(command -v wget || echo -n '')" ]; then
      az_req="wget -q -O - -H '$HEADER'"
  else 
      echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
  fi

  if [ "$az_req" ]; then
    print_3title "Instance details"
    exec_with_jq eval $az_req "$URL/instance?api-version=$API_VERSION"

    print_3title "Load Balancer details"
    exec_with_jq eval $az_req "$URL/loadbalancer?api-version=$API_VERSION"

    print_3title "Management token"
    exec_with_jq eval $az_req "$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=https://management.azure.com/"

    print_3title "Graph token"
    exec_with_jq eval $az_req "$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=https://graph.microsoft.com/"
    
    print_3title "Vault token"
    exec_with_jq eval $az_req "$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=https://vault.azure.net/"

    print_3title "Storage token"
    exec_with_jq eval $az_req "$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=https://storage.azure.com/"
  fi
  echo ""
fi