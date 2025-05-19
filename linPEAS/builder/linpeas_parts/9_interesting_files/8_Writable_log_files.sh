# Title: Interesting Files - Writable log files
# ID: IF_Writable_log_files
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Writable log files
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $IAMROOT, $ROOT_FOLDER, $Wfolders
# Initial Functions:
# Generated Global Variables: $lastWlogFolder, $logfind, $log
# Fat linpeas: 0
# Small linpeas: 0


if command -v logrotate >/dev/null && logrotate --version | head -n 1 | grep -Eq "[012]\.[0-9]+\.|3\.[0-9]\.|3\.1[0-7]\.|3\.18\.0"; then #3.18.0 and below
print_2title "Writable log files (logrotten) (limit 50)"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#logrotate-exploitation"
  logrotate --version 2>/dev/null || echo_not_found "logrotate"
  lastWlogFolder="ImPOsSiBleeElastWlogFolder"
  logfind=$(find $ROOT_FOLDER -type f -name "*.log" -o -name "*.log.*" 2>/dev/null | awk -F/ '{line_init=$0; if (!cont){ cont=0 }; $NF=""; act=$0; if (act == pre){(cont += 1)} else {cont=0}; if (cont < 3){ print line_init; }; if (cont == "3"){print "#)You_can_write_more_log_files_inside_last_directory"}; pre=act}' | head -n 50)
  printf "%s\n" "$logfind" | while read log; do
    if ! [ "$IAMROOT" ] && [ "$log" ] && [ -w "$log" ] || ! [ "$IAMROOT" ] && echo "$log" | grep -qE "$Wfolders"; then #Only print info if something interesting found
      if echo "$log" | grep -q "You_can_write_more_log_files_inside_last_directory"; then printf $ITALIC"$log\n"$NC;
      elif ! [ "$IAMROOT" ] && [ -w "$log" ] && [ "$(command -v logrotate 2>/dev/null)" ] && logrotate --version 2>&1 | grep -qE ' 1| 2| 3.1'; then printf "Writable:$RED $log\n"$NC; #Check vuln version of logrotate is used and print red in that case
      elif ! [ "$IAMROOT" ] && [ -w "$log" ]; then echo "Writable: $log";
      elif ! [ "$IAMROOT" ] && echo "$log" | grep -qE "$Wfolders" && [ "$log" ] && [ ! "$lastWlogFolder" == "$log" ]; then lastWlogFolder="$log"; echo "Writable folder: $log" | sed -${E} "s,$Wfolders,${SED_RED},g";
      fi
    fi
  done
fi

# Check syslog configuration
print_2title "Syslog configuration (limit 50)"
if [ -f "/etc/rsyslog.conf" ]; then
    grep -v "^#" /etc/rsyslog.conf 2>/dev/null | sed -${E} "s,.*,${SED_RED},g" | head -n 50
elif [ -f "/etc/syslog.conf" ]; then
    grep -v "^#" /etc/syslog.conf 2>/dev/null | sed -${E} "s,.*,${SED_RED},g" | head -n 50
else
    echo_not_found "syslog configuration"
fi


# Check auditd configuration
print_2title "Auditd configuration (limit 50)"
if [ -f "/etc/audit/auditd.conf" ]; then
    grep -v "^#" /etc/audit/auditd.conf 2>/dev/null | sed -${E} "s,.*,${SED_RED},g" | head -n 50
else
    echo_not_found "auditd configuration"
fi

# Check for log files with weak permissions
print_2title "Log files with potentially weak perms (limit 50)"
find /var/log -type f -ls 2>/dev/null | grep -Ev "root\s+root|root\s+systemd-journal|root\s+syslog|root\s+utmp" | sed -${E} "s,.*,${SED_RED},g" | head -n 50


echo ""
