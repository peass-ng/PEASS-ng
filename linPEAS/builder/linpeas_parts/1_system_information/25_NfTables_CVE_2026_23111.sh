# Title: System Information - nftables Catchall Map UAF (CVE-2026-23111)
# ID: SY_NfTables_CVE_2026_23111
# Author: HT Bot
# Last Update: 01-10-2026
# Description: Passively check for the nftables catchall verdict-map transaction rollback use-after-free tracked as CVE-2026-23111.
# Description: Correlates upstream and vendor kernel fix evidence with nf_tables/pipapo availability, user and network namespace controls, and the current AppArmor, SELinux, seccomp, and capability context. It never creates a namespace, loads a module, or changes nftables state.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: checkNfTablesCVE202623111
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


checkNfTablesCVE202623111
echo ""
