# Title: Cloud - set_metadata_req_cmd
# ID: set_metadata_req_cmd
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Set a metadata service request command based on curl/wget availability
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


set_metadata_req_cmd(){
  local req_var="$1"
  local header="$2"

  if command -v curl >/dev/null 2>&1; then
    printf -v "$req_var" "curl -s -f -L -H '%s'" "$header"
  elif command -v wget >/dev/null 2>&1; then
    printf -v "$req_var" "wget -q -O - --header '%s'" "$header"
  else
    echo "Neither curl nor wget were found, I can't enumerate the metadata service :("
    printf -v "$req_var" ""
    return 1
  fi
}
