# Title: Interesting Permissions Files - ACLs
# ID: IP_Files_ACLs
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Files with ACLs 
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $HOMESEARCH, $knw_usrs, $MACPEAS, $nosh_usrs, $SEARCH_IN_FOLDER, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Files with ACLs (limited to 50)"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#acls"
if ! [ "$SEARCH_IN_FOLDER" ]; then
  ( (getfacl -t -s -R -p /bin /etc $HOMESEARCH /opt /sbin /usr /tmp /root 2>/dev/null) || echo_not_found "files with acls in searched folders" ) | head -n 70 | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED},"
else
  ( (getfacl -t -s -R -p $SEARCH_IN_FOLDER 2>/dev/null) || echo_not_found "files with acls in searched folders" ) | head -n 70 | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED},"
fi

if [ "$MACPEAS" ] && ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && ! [ "$(command -v getfacl || echo -n '')" ]; then  #Find ACL files in macos (veeeery slow)
  ls -RAle / 2>/dev/null | grep -v "group:everyone deny delete" | grep -E -B1 "\d: " | head -n 70 | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed "s,$USER,${SED_RED},"
fi
echo ""