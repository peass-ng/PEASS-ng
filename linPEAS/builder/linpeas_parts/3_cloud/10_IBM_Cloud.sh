# Title: Cloud - IBM Cloud
# ID: CL_IBM_Cloud
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: IBM Cloud Enumeration
# License: GNU GPL
# Version: 1.0
# Functions Used: check_ibm_vm, print_2title, print_3title
# Global Variables: $IBM_TOKEN, $is_ibm_vm
# Initial Functions: check_ibm_vm
# Generated Global Variables: $TOKEN_HEADER, $ACCEPT_HEADER, $URL, $ibm_req
# Fat linpeas: 0
# Small linpeas: 0


if [ "$is_ibm_vm" = "Yes" ]; then
  print_2title "IBM Cloud Enumeration"

  if ! [ "$IBM_TOKEN" ]; then
    echo "Couldn't get the metadata token:("

  else
    TOKEN_HEADER="Authorization: Bearer $IBM_TOKEN"
    ACCEPT_HEADER="Accept: application/json"
    URL="http://169.254.169.254/latest/meta-data"
    
    ibm_req=""
    if [ "$(command -v curl || echo -n '')" ]; then
        ibm_req="curl -s -f -L -H '$TOKEN_HEADER' -H '$ACCEPT_HEADER'"
    elif [ "$(command -v wget || echo -n '')" ]; then
        ibm_req="wget -q -O - -H '$TOKEN_HEADER' -H '$ACCEPT_HEADER'"
    else 
        echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
    fi

    if [ "$ibm_req" ]; then
      print_3title "Instance Details"
      exec_with_jq eval $ibm_req "http://169.254.169.254/metadata/v1/instance?version=2022-03-01"

      print_3title "Keys and User data"
      exec_with_jq eval $ibm_req "http://169.254.169.254/metadata/v1/instance/initialization?version=2022-03-01"
      exec_with_jq eval $ibm_req "http://169.254.169.254/metadata/v1/keys?version=2022-03-01"

      print_3title "Placement Groups"
      exec_with_jq eval $ibm_req "http://169.254.169.254/metadata/v1/placement_groups?version=2022-03-01"

      print_3title "IAM credentials"
      exec_with_jq eval $ibm_req -X POST "http://169.254.169.254/instance_identity/v1/iam_token?version=2022-03-01"
    fi
  fi
  echo ""
fi