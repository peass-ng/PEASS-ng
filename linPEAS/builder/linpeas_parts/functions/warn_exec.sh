# Title: LinPeasBase - warn_exec
# ID: warn_exec
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Warn if a command is not found
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


warn_exec(){
  $* 2>/dev/null || echo_not_found $1
}