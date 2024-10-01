# Title: Variables - capsVB
# ID: capsVB
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Very dangerous capabilities to search
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $capsVB
# Fat linpeas: 0
# Small linpeas: 1


capsVB="cap_sys_admin:mount|python \
cap_sys_ptrace:python \
cap_sys_module:kmod|python \
cap_dac_override:python|vim \
cap_chown:chown|python \
cap_former:chown|python \
cap_setuid:peass{CAP_SETUID_HERE} \
cap_setgid:peass{CAP_SETGID_HERE} \
cap_net_raw:python|tcpdump"