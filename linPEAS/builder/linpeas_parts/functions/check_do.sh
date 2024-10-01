# Title: Cloud - check_do
# ID: check_do
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the script is running in Digital Ocean
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $is_do
# Fat linpeas: 0
# Small linpeas: 1


check_do(){
  is_do="No"
  if [ -f "/etc/cloud/cloud.cfg.d/90-digitalocean.cfg" ]; then
    is_do="Yes"
  fi
}