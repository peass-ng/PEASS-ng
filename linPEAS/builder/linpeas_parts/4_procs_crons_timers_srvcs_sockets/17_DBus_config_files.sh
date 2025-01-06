# Title: Processes & Cron & Services & Timers - D-Bus config files
# ID: PR_DBus_config_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Enumerate D-Bus config files
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $IAMROOT, $mygroups, $nosh_usrs, $SEARCH_IN_FOLDER, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables: $genpol, $userpol, $grppol
# Fat linpeas: 0
# Small linpeas: 0

print_2title "D-Bus config files"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#d-bus"
if [ "$PSTORAGE_DBUS" ]; then
  printf "%s\n" "$PSTORAGE_DBUS" | while read d; do
    for f in $d/*; do
      if ! [ "$IAMROOT" ] && [ -w "$f" ] && ! [ "$SEARCH_IN_FOLDER" ]; then
        echo "Writable $f" | sed -${E} "s,.*,${SED_RED},g"
      fi

      genpol=$(grep "<policy>" "$f" 2>/dev/null)
      if [ "$genpol" ]; then printf "Weak general policy found on $f ($genpol)\n" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_RED},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$mygroups,${SED_RED},g"; fi
      #if [ "`grep \"<policy user=\\\"$USER\\\">\" \"$f\" 2>/dev/null`" ]; then printf "Possible weak user policy found on $f () \n" | sed "s,$USER,${SED_RED},g"; fi

      userpol=$(grep "<policy user=" "$f" 2>/dev/null | grep -v "root")
      if [ "$userpol" ]; then printf "Possible weak user policy found on $f ($userpol)\n" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_RED},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$mygroups,${SED_RED},g"; fi
      #for g in `groups`; do
      #  if [ "`grep \"<policy group=\\\"$g\\\">\" \"$f\" 2>/dev/null`" ]; then printf "Possible weak group ($g) policy found on $f\n" | sed "s,$g,${SED_RED},g"; fi
      #done
      grppol=$(grep "<policy group=" "$f" 2>/dev/null | grep -v "root")
      if [ "$grppol" ]; then printf "Possible weak user policy found on $f ($grppol)\n" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed "s,$USER,${SED_RED},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed -${E} "s,$mygroups,${SED_RED},g"; fi

      #TODO: identify allows in context="default"
    done
  done
fi
echo ""