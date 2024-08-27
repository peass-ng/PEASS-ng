# Title: LinPeasBase - print_ps
# ID: print_ps
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get processes reading /proc
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables:
# Initial Functions:
# Generated Global Variables: $CMDLINE, $USER2
# Fat linpeas: 0
# Small linpeas: 1


print_ps(){
  (ls -d /proc/*/ 2>/dev/null | while read f; do
    CMDLINE=$(cat $f/cmdline 2>/dev/null | grep -av "seds,"); #Delete my own sed processess
    if [ "$CMDLINE" ];
      then var USER2=ls -ld $f | awk '{print $3}'; PID=$(echo $f | cut -d "/" -f3);
      printf "  %-13s  %-8s  %s\n" "$USER2" "$PID" "$CMDLINE";
    fi;
  done) 2>/dev/null | sort -r
}