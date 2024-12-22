# Title: Cloud - Azure App Service 
# ID: CL_Azure_app_service
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Azure App Service Enumeration
# License: GNU GPL
# Version: 1.0
# Functions Used: check_az_app, exec_with_jq, print_2title, print_3title
# Global Variables: $is_az_app,
# Initial Functions: check_az_app
# Generated Global Variables: $API_VERSION, $HEADER, $az_req
# Fat linpeas: 0
# Small linpeas: 0


API_VERSION="2019-08-01" #https://learn.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=portal%2Chttp

if [ "$is_az_app" = "Yes" ]; then
  print_2title "Azure App Service Enumeration"

  HEADER="X-IDENTITY-HEADER:$IDENTITY_HEADER"

  az_req=""
  if [ "$(command -v curl || echo -n '')" ]; then
      az_req="curl -s -f -L -H '$HEADER'"
  elif [ "$(command -v wget || echo -n '')" ]; then
      az_req="wget -q -O - -H '$HEADER'"
  else 
      echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
  fi

  if [ "$az_req" ]; then
    print_3title "Management token"
    exec_with_jq eval $az_req "$IDENTITY_ENDPOINT?api-version=$API_VERSION\&resource=https://management.azure.com/"
    echo
    print_3title "Graph token"
    exec_with_jq eval $az_req "$IDENTITY_ENDPOINT?api-version=$API_VERSION\&resource=https://graph.microsoft.com/"
    echo
    print_3title "Vault token"
    exec_with_jq eval $az_req "$IDENTITY_ENDPOINT?api-version=$API_VERSION\&resource=https://vault.azure.net/"
    echo
    print_3title "Storage token"
    exec_with_jq eval $az_req "$IDENTITY_ENDPOINT?api-version=$API_VERSION\&resource=https://storage.azure.com/"
  fi
  echo ""
fi
