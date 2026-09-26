# Title: Variables - capsVB
# ID: capsVB
# Author: Carlos Polop, HT Bot
# Last Update: 25-09-2026
# Description: Very dangerous capability and executable combinations to search
# License: GNU GPL
# Version: 1.1
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $capsVB
# Fat linpeas: 0
# Small linpeas: 1


capsVB="cap_sys_admin:mount|python|perl|ruby|php|node|lua|bash \
cap_sys_ptrace:python \
cap_sys_module:kmod|python \
cap_dac_override:python|perl|ruby|php|node|lua|bash|vim|cp|dd \
cap_dac_read_search:python|perl|ruby|php|node|lua|bash|vim|cp|dd|tar \
cap_chown:chown|python \
cap_fowner:chown|python \
cap_setfcap:python|perl|ruby|php|node|lua|bash \
cap_setpcap:python|perl|ruby|php|node|lua|bash \
cap_setuid:peass{CAP_SETUID_HERE} \
cap_setgid:peass{CAP_SETGID_HERE} \
cap_net_raw:python|tcpdump|dumpcap|tcpflow"
