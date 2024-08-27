# Title: LinPeasBase - su_try_pwd
# ID: su_try_pwd
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Try to login as user using a password
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables:
# Initial Functions:
# Generated Global Variables: $BFUSER, $PASSWORDTRY, $trysu, $SED_RED_YELLOW
# Fat linpeas: 0
# Small linpeas: 1


su_try_pwd(){
  BFUSER=$1
  PASSWORDTRY=$2
  trysu=$(echo "$PASSWORDTRY" | timeout 1 su $BFUSER -c whoami 2>/dev/null)
  if [ "$trysu" ]; then
    echo "  You can login as $BFUSER using password: $PASSWORDTRY" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  fi
}