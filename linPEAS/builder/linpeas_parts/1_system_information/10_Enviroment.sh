# Title: System Information - Enviroment
# ID: SY_Enviroment
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get Information inside environment variables
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Environment"
print_info "Any private information inside environment variables?"
(env || printenv || set) 2>/dev/null | grep -v "RELEVANT*|FIND*|^VERSION=|dbuslistG|mygroups|ldsoconfdG|pwd_inside_history|kernelDCW_Ubuntu_Precise|kernelDCW_Ubuntu_Trusty|kernelDCW_Ubuntu_Xenial|kernelDCW_Rhel|^sudovB=|^rootcommon=|^mounted=|^mountG=|^notmounted=|^mountpermsB=|^mountpermsG=|^kernelB=|^C=|^RED=|^GREEN=|^Y=|^B=|^NC=|TIMEOUT=|groupsB=|groupsVB=|knw_grps=|sidG|sidB=|sidVB=|sidVB2=|sudoB=|sudoG=|sudoVB=|timersG=|capsB=|notExtensions=|Wfolders=|writeB=|writeVB=|_usrs=|compiler=|PWD=|LS_COLORS=|pathshG=|notBackup=|processesDump|processesB|commonrootdirs|USEFUL_SOFTWARE|PSTORAGE_" | sed -${E} "s,[pP][wW][dD]|[pP][aA][sS][sS][wW]|[aA][pP][iI][kK][eE][yY]|[aA][pP][iI][_][kK][eE][yY]|KRB5CCNAME,${SED_RED},g" || echo_not_found "env || set"
echo ""