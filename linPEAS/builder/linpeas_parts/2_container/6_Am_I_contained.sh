# Title: Container - Am I Containered 
# ID: CT_Am_I_contained
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Am I Containered tool
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, execBin
# Global Variables:
# Initial Functions:
# Generated Global Variables: $FAT_LINPEAS_AMICONTAINED
# Fat linpeas: 1
# Small linpeas: 0


if [ "$$FAT_LINPEAS_AMICONTAINED" ]; then
  print_2title "Am I Containered?"
  FAT_LINPEAS_AMICONTAINED="peass{https://github.com/genuinetools/amicontained/releases/latest/download/amicontained-linux-amd64}"
  execBin "AmIContainered" "https://github.com/genuinetools/amicontained" "$FAT_LINPEAS_AMICONTAINED"
fi
