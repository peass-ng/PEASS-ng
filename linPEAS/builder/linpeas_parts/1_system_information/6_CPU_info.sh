# Title: System Information - CPU info
# ID: SY_CPU_info
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for CPU information relevant to privilege escalation:
#   - CPU architecture
#   - CPU features
#   - CPU vulnerabilities
#   - Common vulnerable scenarios:
#     * CPU-specific vulnerabilities (Spectre, Meltdown, etc.)
#     * Missing CPU mitigations
#     * Architecture-specific exploits
#     * CPU feature abuse
#   - Exploitation methods:
#     * CPU-based attacks: Abuse CPU vulnerabilities
#     * Common attack vectors:
#       - Spectre/Meltdown exploitation
#       - CPU feature abuse
#       - Architecture-specific attacks
#       - CPU timing attacks
#     * Exploit techniques:
#       - Side-channel attacks
#       - CPU feature exploitation
#       - Architecture-specific techniques
#       - CPU timing exploitation
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, warn_exec
# Global Variables: $DEBUG, $EXTRA_CHECKS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$EXTRA_CHECKS" ] || [ "$DEBUG" ]; then
    print_2title "CPU info"
    warn_exec lscpu 2>/dev/null
    echo ""
fi