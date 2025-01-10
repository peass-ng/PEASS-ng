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
    echo ""

    print_3title "Load Balancer details"
    exec_with_jq eval $az_req "$URL/loadbalancer?api-version=$API_VERSION"
    echo ""

    print_3title "User Data"
    exec_with_jq eval $az_req "$URL/instance/compute/userData?api-version=$API_VERSION\&format=text" | base64 -d 2>/dev/null
    echo ""

    print_3title "Custom Data and other configs (root needed)"
    (cat /var/lib/waagent/ovf-env.xml || cat /var/lib/waagent/CustomData/ovf-env.xml) 2>/dev/null | sed "s,CustomData.*,${SED_RED},"
    echo ""

    print_3title "Management token"
    print_info "It's possible to assign 1 system MI and several user MI to a VM. LinPEAS can only get the token from the default one. More info in https://book.hacktricks.wiki/en/pentesting-web/ssrf-server-side-request-forgery/cloud-ssrf.html#azure-vm"
    exec_with_jq eval $az_req "$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=https://management.azure.com/"
    echo ""

    print_3title "Graph token"
    print_info "It's possible to assign 1 system MI and several user MI to a VM. LinPEAS can only get the token from the default one. More info in https://book.hacktricks.wiki/en/pentesting-web/ssrf-server-side-request-forgery/cloud-ssrf.html#azure-vm"
    exec_with_jq eval $az_req "$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=https://graph.microsoft.com/"
    echo ""
    
    print_3title "Vault token"
    print_info "It's possible to assign 1 system MI and several user MI to a VM. LinPEAS can only get the token from the default one. More info in https://book.hacktricks.wiki/en/pentesting-web/ssrf-server-side-request-forgery/cloud-ssrf.html#azure-vm"
    exec_with_jq eval $az_req "$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=https://vault.azure.net/"
    echo ""

    print_3title "Storage token"
    print_info "It's possible to assign 1 system MI and several user MI to a VM. LinPEAS can only get the token from the default one. More info in https://book.hacktricks.wiki/en/pentesting-web/ssrf-server-side-request-forgery/cloud-ssrf.html#azure-vm"
    exec_with_jq eval $az_req "$URL/identity/oauth2/token?api-version=$API_VERSION\&resource=https://storage.azure.com/"
    echo ""
  fi
  echo ""
fi