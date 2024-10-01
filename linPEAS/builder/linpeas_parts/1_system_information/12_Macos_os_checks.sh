# Title: System Information - MacOS OS checks
# ID: SY_Macos_os_checks
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Macos OS checks
# License: GNU GPL
# Version: 1.0
# Functions Used:macosNotSigned, print_2title
# Global Variables: $MACPEAS
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ]; then
    print_2title "Kernel Extensions not belonging to apple"
    kextstat 2>/dev/null | grep -Ev " com.apple."
    echo ""

    print_2title "Unsigned Kernel Extensions"
    macosNotSigned /Library/Extensions
    macosNotSigned /System/Library/Extensions
    echo ""
fi

if [ "$MACPEAS" ] && [ "$(command -v brew 2>/dev/null || echo -n '')" ]; then
    print_2title "Brew Doctor Suggestions"
    brew doctor
    echo ""
fi