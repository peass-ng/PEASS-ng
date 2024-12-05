# Title: Cloud - check_tencent_cvm
# ID: check_tencent_cvm
# Author: Ahadowabi
# Last Update: 24-01-2024
# Description: Check if the script is running in tencent
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $is_tencent_cvm
# Fat linpeas: 0
# Small linpeas: 1



check_tencent_cvm () {
  is_tencent_cvm="No"
  if grep -qi Tencent /etc/cloud/cloud.cfg 2>/dev/null; then
      is_tencent_cvm="Yes"
  fi
}