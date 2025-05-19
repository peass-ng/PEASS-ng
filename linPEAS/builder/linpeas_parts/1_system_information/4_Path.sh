# Title: System Information - Path
# ID: SY_Path
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Check for PATH environment misconfigurations that could lead to privilege escalation:
#   - Writable directories in PATH
#   - Current directory (.) in PATH
#   - Common vulnerable scenarios:
#     * Writable system directories in PATH
#     * Current directory in PATH
#     * Relative paths in PATH
#   - Exploitation methods:
#     * PATH hijacking: Place malicious executables in writable PATH directories
#     * Common attack vectors:
#       - Replace common binaries (ls, cat, etc.)
#       - Create malicious executables with common names
#       - Abuse sudo PATH inheritance
#     * Exploit techniques:
#       - Binary replacement
#       - Symbolic link attacks
#       - PATH manipulation
#       - Sudo PATH abuse
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $IAMROOT, $OLDPATH, $PATH, $Wfolders
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


print_2title "PATH"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#writable-path-abuses"
if ! [ "$IAMROOT" ]; then
    echo "$OLDPATH" 2>/dev/null | sed -${E} "s,$Wfolders|\./|\.:|:\.,${SED_RED_YELLOW},g"
fi

if [ "$DEBUG" ]; then
     echo "New path exported: $PATH"
fi
echo ""