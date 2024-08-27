# Title: Variables - NOUSEPS
# ID: NOUSEPS
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Don't use ps
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables:
# Initial Functions:
# Generated Global Variables: $NOUSEPS
# Fat linpeas: 0
# Small linpeas: 1


if [ "$(ps auxwww 2>/dev/null | wc -l 2>/dev/null)" -lt 8 ]; then
  NOUSEPS="1"
fi