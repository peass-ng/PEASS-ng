# Title: Processes & Cron & Services & Timers - Analyzing .service files
# ID: PR_Service_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Analyze .service files
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $IAMROOT, $SEARCH_IN_FOLDER, $WRITABLESYSTEMDPATH
# Initial Functions:
# Generated Global Variables: $relpath1, $relpath2, $servicebinpaths
# Fat linpeas: 0
# Small linpeas: 0


#TODO: .service files in MACOS are folders
print_2title "Analyzing .service files"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#services"
printf "%s\n" "$PSTORAGE_SYSTEMD" | while read s; do
  if [ ! -O "" ] || [ "$SEARCH_IN_FOLDER" ]; then #Remove services that belongs to the current user or if firmware see everything
    if ! [ "$IAMROOT" ] && [ -w "$s" ] && [ -f "$s" ] && ! [ "$SEARCH_IN_FOLDER" ]; then
      echo "$s" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
    fi
    servicebinpaths=$(grep -Eo '^Exec.*?=[!@+-]*[a-zA-Z0-9_/\-]+' "$s" 2>/dev/null | cut -d '=' -f2 | sed 's,^[@\+!-]*,,') #Get invoked paths
    printf "%s\n" "$servicebinpaths" | while read sp; do
      if [ -w "$sp" ]; then
        echo "$s is calling this writable executable: $sp" | sed "s,writable.*,${SED_RED_YELLOW},g"
      fi
    done
    relpath1=$(grep -E '^Exec.*=(?:[^/]|-[^/]|\+[^/]|![^/]|!![^/]|)[^/@\+!-].*' "$s" 2>/dev/null | grep -Iv "=/")
    relpath2=$(grep -E '^Exec.*=.*/bin/[a-zA-Z0-9_]*sh ' "$s" 2>/dev/null)
    if [ "$relpath1" ] || [ "$relpath2" ]; then
      if [ "$WRITABLESYSTEMDPATH" ]; then
        echo "$s could be executing some relative path" | sed -${E} "s,.*,${SED_RED},";
      else
        echo "$s could be executing some relative path"
      fi
    fi
  fi
done
if [ ! "$WRITABLESYSTEMDPATH" ]; then echo "You can't write on systemd PATH" | sed -${E} "s,.*,${SED_GREEN},"; fi
echo ""