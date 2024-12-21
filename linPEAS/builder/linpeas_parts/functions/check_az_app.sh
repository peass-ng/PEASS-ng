# Title: Cloud - check_az_app
# ID: check_az_app
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the script is running in Azure App Service
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $is_az_app
# Fat linpeas: 0
# Small linpeas: 1


check_az_app(){
  is_az_app="No"

  if [ -d "/opt/microsoft" ] && env | grep -iq "azure"; then
    is_az_app="Yes"
  fi
}