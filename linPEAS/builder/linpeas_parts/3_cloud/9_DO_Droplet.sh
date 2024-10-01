# Title: Cloud - DO Droplet
# ID: CL_DO_Droplet
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: DO Droplet Enumeration
# License: GNU GPL
# Version: 1.0
# Functions Used: check_do, print_2title
# Global Variables: $is_do
# Initial Functions: check_do
# Generated Global Variables: $do_req, $URL
# Fat linpeas: 0
# Small linpeas: 1


if [ "$is_do" = "Yes" ]; then
  print_2title "DO Droplet Enumeration"

  do_req=""
  if [ "$(command -v curl || echo -n '')" ]; then
      do_req='curl -s -f -L '
  elif [ "$(command -v wget || echo -n '')" ]; then
      do_req='wget -q -O - '
  else 
      echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
  fi

  if [ "$do_req" ]; then
    URL="http://169.254.169.254/metadata"
    printf "Id: "; eval $do_req "$URL/v1/id"; echo ""
    printf "Region: "; eval $do_req "$URL/v1/region"; echo ""
    printf "Public keys: "; eval $do_req "$URL/v1/public-keys"; echo ""
    printf "User data: "; eval $do_req "$URL/v1/user-data"; echo ""
    printf "Dns: "; eval $do_req "$URL/v1/dns/nameservers" | tr '\n' ','; echo ""
    printf "Interfaces: "; eval $do_req "$URL/v1.json" | jq ".interfaces";
    printf "Floating_ip: "; eval $do_req "$URL/v1.json" | jq ".floating_ip";
    printf "Reserved_ip: "; eval $do_req "$URL/v1.json" | jq ".reserved_ip";
    printf "Tags: "; eval $do_req "$URL/v1.json" | jq ".tags";
    printf "Features: "; eval $do_req "$URL/v1.json" | jq ".features";
  fi
  echo ""
fi