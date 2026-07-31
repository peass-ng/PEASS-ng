# Title: Function - azureIdentityHelpers
# ID: azureIdentityHelpers
# Author: HT Bot
# Last Update: 31-07-2026
# Description: Shared helpers for Azure managed-identity checks to avoid repeating the same request-command setup and standard token enumeration blocks.
# License: GNU GPL
# Version: 1.0
# Functions Used: exec_with_jq, print_3title
# Global Variables:
# Initial Functions:
# Generated Global Variables: $HEADER, $API_VERSION, $az_req
# Fat linpeas: 0
# Small linpeas: 1

set_azure_request_command() {
  az_req=""
  if [ "$(command -v curl || echo -n '')" ]; then
      az_req="curl -s -f -L -H '$HEADER'"
  elif [ "$(command -v wget || echo -n '')" ]; then
      az_req="wget -q -O - --header '$HEADER'"
  else
      echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
  fi
}

print_azure_identity_token() {
  print_3title "$1" "T1552.005,T1580"
  exec_with_jq eval $az_req "$IDENTITY_ENDPOINT?api-version=$API_VERSION\\&resource=$2"
  echo
}

print_azure_standard_identity_tokens() {
  print_azure_identity_token "Management token" "https://management.azure.com/"
  print_azure_identity_token "Graph token" "https://graph.microsoft.com/"
  print_azure_identity_token "Vault token" "https://vault.azure.net/"
  print_azure_identity_token "Storage token" "https://storage.azure.com/"
}
