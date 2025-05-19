# Title: System Information - Operative System
# ID: SY_Operative_system
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for operating system information relevant to privilege escalation:
#   - OS version and distribution
#   - Kernel version
#   - Architecture
#   - Common vulnerable scenarios:
#     * Outdated OS versions
#     * Unpatched systems
#     * Known vulnerable distributions
#     * Architecture-specific vulnerabilities
#   - Exploitation methods:
#     * Version-specific exploits: Use known exploits for the OS version
#     * Common attack vectors:
#       - OS version exploits
#       - Distribution-specific vulnerabilities
#       - Architecture-specific exploits
#       - Kernel version exploits
#     * Exploit techniques:
#       - Version-specific payloads
#       - Distribution-specific attacks
#       - Architecture-specific techniques
#       - Kernel exploitation
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info, warn_exec
# Global Variables: $MACPEAS, $kernelDCW_Ubuntu_Precise_1, $kernelB, $kernelDCW_Ubuntu_Precise_2, $kernelDCW_Ubuntu_Precise_3, $kernelDCW_Ubuntu_Precise_4, $kernelDCW_Ubuntu_Precise_5, $kernelDCW_Ubuntu_Precise_6, $kernelDCW_Rhel5_1, $kernelDCW_Rhel5_2, $kernelDCW_Rhel5_3, $kernelDCW_Rhel6_1, $kernelDCW_Rhel6_2, $kernelDCW_Rhel6_3, $kernelDCW_Rhel6_4, $kernelDCW_Rhel7, $kernelDCW_Ubuntu_Trusty_1, $kernelDCW_Ubuntu_Trusty_2, $kernelDCW_Ubuntu_Trusty_3, $kernelDCW_Ubuntu_Trusty_4, $kernelDCW_Ubuntu_Xenial
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

print_2title "Operative system"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#kernel-exploits"
(cat /proc/version || uname -a ) 2>/dev/null | sed -${E} "s,$kernelDCW_Ubuntu_Precise_1,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_2,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_3,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_4,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_5,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_6,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Trusty_1,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Trusty_2,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Trusty_3,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Trusty_4,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Xenial,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel5_1,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel5_2,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel5_3,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel6_1,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel6_2,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel6_3,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel6_4,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel7,${SED_RED_YELLOW}," | sed -${E} "s,$kernelB,${SED_RED},"
warn_exec lsb_release -a 2>/dev/null
if [ "$MACPEAS" ]; then
    warn_exec system_profiler SPSoftwareDataType
fi
echo ""