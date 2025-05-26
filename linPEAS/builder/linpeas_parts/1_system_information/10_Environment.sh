# Title: System Information - Environment
# ID: SY_Environment
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for sensitive information in environment variables that could lead to privilege escalation:
#   - Credentials in environment variables
#   - API keys and tokens
#   - Sensitive configuration data
#   - Common vulnerable scenarios:
#     * Hardcoded credentials in environment
#     * API keys exposed in environment
#     * Database credentials in environment
#     * Service account tokens
#   - Exploitation methods:
#     * Credential harvesting: Extract sensitive data from environment
#     * Common attack vectors:
#       - Password/credential extraction
#       - API key abuse
#       - Token theft
#       - Configuration data leakage
#     * Exploit techniques:
#       - Environment variable dumping
#       - Credential reuse
#       - Token reuse
#       - Configuration abuse
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $NoEnvVars, $EnvVarsRed
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Environment"
print_info "Any private information inside environment variables?"
(env || printenv || set) 2>/dev/null | grep -Eiv "$NoEnvVars" | sed -${E} "s,$EnvVarsRed,${SED_RED},g" || echo_not_found "env || set"
echo ""