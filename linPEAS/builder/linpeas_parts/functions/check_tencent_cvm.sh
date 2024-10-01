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
  if [ -f "/etc/cloud/cloud.cfg.d/05_logging.cfg" ] || grep -qi Tencent /etc/cloud/cloud.cfg; then
      is_tencent_cvm="Yes"
  fi
}