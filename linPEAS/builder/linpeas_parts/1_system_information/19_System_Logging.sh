# Title: System Information - System Logging
# ID: SY_System_Logging
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for logging system misconfigurations that could lead to privilege escalation:
#   - Syslog/rsyslog configurations that log sensitive information
#   - Auditd configurations that could be abused
#   - Log files with weak permissions that could be modified
#   - Log rotation configurations that could be exploited
#   - Exploitation methods:
#     * Sensitive info in logs: Extract credentials or sensitive data from logs
#     * Weak permissions: Modify log files to inject malicious content
#     * Log rotation: Abuse log rotation to execute malicious code
#     * Log injection: Inject malicious content into logs that get executed
#     * Common targets: /var/log/auth.log, /var/log/syslog, audit logs
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info, print_list, warn_exec
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

print_2title "System Logging Information"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/logs-privilege-escalation"

# Check syslog configuration
print_list "Syslog configuration? ......... "$NC
if [ -f "/etc/rsyslog.conf" ]; then
    grep -v "^#" /etc/rsyslog.conf 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
elif [ -f "/etc/syslog.conf" ]; then
    grep -v "^#" /etc/syslog.conf 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
else
    echo_not_found "syslog configuration"
fi

# Check auditd configuration
print_list "Auditd configuration? ......... "$NC
if [ -f "/etc/audit/auditd.conf" ]; then
    grep -v "^#" /etc/audit/auditd.conf 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
else
    echo_not_found "auditd configuration"
fi

# Check for log files with weak permissions
print_list "Log files with weak perms? .... "$NC
find /var/log -type f -ls 2>/dev/null | grep -v "root root" | sed -${E} "s,.*,${SED_RED},g"

# Check for log rotation configurations
print_list "Log rotation configuration? ... "$NC
if [ -d "/etc/logrotate.d" ]; then
    for conf in /etc/logrotate.d/*; do
        if [ -f "$conf" ]; then
            grep -v "^#" "$conf" 2>/dev/null | sed -${E} "s,.*,${SED_RED},g"
        fi
    done
else
    echo_not_found "logrotate configuration"
fi

echo "" 