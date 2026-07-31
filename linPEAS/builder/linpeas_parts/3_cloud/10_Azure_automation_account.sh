# Title: Cloud - Azure Automation Account
# ID: CL_Azure_automation_account
# Author: Carlos Polop
# Last Update: 31-07-2026
# Description: Azure Automation Account Service Enumeration
# License: GNU GPL
# Version: 1.0
# Mitre: T1552.005,T1580
# Functions Used: check_az_automation_acc, print_2title, set_azure_request_command, print_azure_standard_identity_tokens
# Global Variables: $is_az_automation_acc,
# Initial Functions: check_az_automation_acc
# Generated Global Variables: $API_VERSION, $HEADER, $az_req
# Fat linpeas: 0
# Small linpeas: 0


API_VERSION="2019-08-01" #https://learn.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=portal%2Chttp

if [ "$is_az_automation_acc" = "Yes" ]; then
  print_2title "Azure Automation Account Service Enumeration" "T1552.005,T1580"
  HEADER="X-IDENTITY-HEADER:$IDENTITY_HEADER"

  set_azure_request_command

  if [ "$az_req" ]; then
    print_azure_standard_identity_tokens
  fi
  echo ""
fi
