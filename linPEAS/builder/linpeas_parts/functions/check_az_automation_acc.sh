# Title: Cloud - check_az_automation_acc
# ID: check_az_automation_acc
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the script is running in Azure App Service
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $is_az_automation_acc
# Fat linpeas: 0
# Small linpeas: 1


check_az_automation_acc(){
  is_az_automation_acc="No"

  if env | grep -iq "azure" && env | grep -iq "AutomationServiceEndpoint"; then
    is_az_automation_acc="Yes"
  fi
}