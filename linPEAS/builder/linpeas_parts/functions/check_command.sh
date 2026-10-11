# Title: Function - check_command
# ID: check_command
# Author: PEASS-ng contributors
# Last Update: 10-10-2026
# Description: Check whether a command resolves to an executable file.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $cmd
# Fat linpeas: 0
# Small linpeas: 1

check_command() {
    local cmd=$1
    if command -v "$cmd" >/dev/null 2>&1; then
        if [ -x "$(command -v "$cmd")" ]; then
            return 0
        fi
    fi
    return 1
}
