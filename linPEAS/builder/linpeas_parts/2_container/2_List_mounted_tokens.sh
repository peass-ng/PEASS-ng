# Title: Container - List mounted tokens
# ID: CT_List_mounted_tokens
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: List tokens mounted in the system if any
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables:
# Initial Functions:
# Generated Global Variables: $ALREADY_TOKENS, $TEMP_TOKEN
# Fat linpeas: 0
# Small linpeas: 1


if [ "$(mount | sed -n '/secret/ s/^tmpfs on \(.*default.*\) type tmpfs.*$/\1\/namespace/p')" ]; then
  print_2title "Listing mounted tokens"
  print_info "https://cloud.hacktricks.wiki/en/pentesting-cloud/kubernetes-security/attacking-kubernetes-from-inside-a-pod.html"
  ALREADY_TOKENS="IinItialVaaluE"
  for i in $(mount | sed -n '/secret/ s/^tmpfs on \(.*default.*\) type tmpfs.*$/\1\/namespace/p'); do
      TEMP_TOKEN=$(cat $(echo $i | sed 's/.namespace$/\/token/'))
      if ! [ $(echo $TEMP_TOKEN | grep -E $ALREADY_TOKENS) ]; then
          ALREADY_TOKENS="$ALREADY_TOKENS|$TEMP_TOKEN"
          echo "Directory: $i"
          echo "Namespace: $(cat $i)"
          echo ""
          echo $TEMP_TOKEN
          echo "================================================================================"
          echo ""
      fi
  done
fi