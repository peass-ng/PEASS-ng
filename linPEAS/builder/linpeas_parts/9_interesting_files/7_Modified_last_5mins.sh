# Title: Interesting Files - Files modified last 5 mins
# ID: IF_Modified_last_5mins
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Modified interesting files into specific folders in the last 5mins
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title
# Global Variables:$ROOT_FOLDER, $Wfolders 
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


print_2title "Modified interesting files in the last 5mins (limit 100)"
find $ROOT_FOLDER -type f -mmin -5 ! -path "/proc/*" ! -path "/sys/*" ! -path "/run/*" ! -path "/dev/*" ! -path "/var/lib/*" ! -path "/private/var/*" 2>/dev/null | grep -v "/linpeas" | head -n 100 | sed -${E} "s,$Wfolders,${SED_RED},"
echo ""
