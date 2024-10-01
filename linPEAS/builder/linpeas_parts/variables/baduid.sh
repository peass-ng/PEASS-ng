# Title: Variables - baduid
# ID: baduid
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Bad UID if greater than 2147483646
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $MyUID
# Initial Functions:
# Generated Global Variables: $baduid, $myuid
# Fat linpeas: 0
# Small linpeas: 1

if [ "$MyUID" ]; then 
    myuid=$MyUID; 
elif [ $(id -u $(whoami) 2>/dev/null) ]; then
    myuid=$(id -u $(whoami) 2>/dev/null);
elif [ "$(id 2>/dev/null | cut -d "=" -f 2 | cut -d "(" -f 1)" ]; then 
    myuid=$(id 2>/dev/null | cut -d "=" -f 2 | cut -d "(" -f 1); 
fi


if [ $myuid -gt 2147483646 ]; then baduid="|$myuid"; fi
