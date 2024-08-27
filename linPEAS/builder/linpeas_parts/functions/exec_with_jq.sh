# Title: Cloud - exec_with_jq
# ID: exec_with_jq
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Execute a command and if jq is installed, format the output
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


exec_with_jq(){
  if [ "$(command -v jq || echo -n '')" ]; then 
    $@ | jq 2>/dev/null;
    if ! [ $? -eq 0 ]; then
      $@;
    fi
   else 
    $@;
   fi
}