# Title: System Information - USBCreator
# ID: SY_USBCreator
# Author: Carlos Polop
# Last Update: 2026-10-09
# Description: Passive review of the USB imaging D-Bus service and legacy passwordless group policy
# License: GNU GPL
# Version: 1.1
# Mitre: T1548.003,T1068
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG
# Initial Functions:
# Generated Global Variables: $usbcreator_service, $usbcreator_policy, $usbcreator_size, $usbcreator_groups
# Fat linpeas: 0
# Small linpeas: 0

# Read only the exact legacy vendor policy file, after a metadata size check.
# A matching stanza is a review lead: later rules and the installed helper's
# implementation can change the effective authorization and file-copy behavior.
usbcreator_passwordless_group_policy() (
    usbcreator_policy=$1
    [ -f "$usbcreator_policy" ] && [ ! -L "$usbcreator_policy" ] &&
        [ -r "$usbcreator_policy" ] || exit 1
    usbcreator_size=$(LC_ALL=C ls -ldn "$usbcreator_policy" 2>/dev/null | awk 'NR == 1 { print $5 }')
    case "$usbcreator_size" in ''|*[!0-9]*) exit 1 ;; esac
    [ "$usbcreator_size" -le 16384 ] || exit 1
    awk '
        function match_stanza() { return identity && action && active }
        /^\[/ { if (match_stanza()) found = 1; identity = action = active = 0; next }
        NR > 200 { overflow = 1; exit }
        {
            line = $0
            sub(/^[[:space:]]*/, "", line)
            sub(/[[:space:]]*$/, "", line)
            if (line ~ /^Identity[[:space:]]*=/) {
                sub(/^[^=]*=[[:space:]]*/, "", line)
                identity = line ~ /(^|;)unix-group:(admin|sudo)(;|$)/
            } else if (line ~ /^Action[[:space:]]*=/) {
                sub(/^[^=]*=[[:space:]]*/, "", line)
                action = line ~ /(^|;)com[.]ubuntu[.]usbcreator[.]image(;|$)/
            } else if (line ~ /^ResultActive[[:space:]]*=/) {
                sub(/^[^=]*=[[:space:]]*/, "", line)
                active = line == "yes"
            }
        }
        END { if (overflow) exit 1; if (match_stanza()) found = 1; if (found) exit 0; exit 1 }
    ' "$usbcreator_policy"
)

usbcreator_service=''
for usbcreator_policy in /etc/dbus-1/system-services/com.ubuntu.USBCreator.service \
    /run/dbus-1/system-services/com.ubuntu.USBCreator.service \
    /usr/local/share/dbus-1/system-services/com.ubuntu.USBCreator.service \
    /usr/share/dbus-1/system-services/com.ubuntu.USBCreator.service; do
    if [ -f "$usbcreator_policy" ] && [ ! -L "$usbcreator_policy" ]; then
        usbcreator_service=$usbcreator_policy
        break
    fi
done

if [ -n "$usbcreator_service" ] || [ "$DEBUG" ]; then
    print_2title "USBCreator" "T1548.003,T1068"
    print_info "https://book.hacktricks.wiki/en/linux-hardening/processes-crontab-systemd-dbus/d-bus-enumeration-and-command-injection-privilege-escalation.html"
    if [ -n "$usbcreator_service" ]; then
        printf 'USB imaging system-bus service descriptor: %s (installed/activatable candidate; effective service and patch state unverified)\n' "$usbcreator_service"
    fi
    if [ -n "$usbcreator_service" ] && [ "$(id -u 2>/dev/null)" != 0 ]; then
        usbcreator_groups=$(id -Gn 2>/dev/null)
        case " $usbcreator_groups " in
            *' sudo '*|*' admin '*)
                usbcreator_policy=/var/lib/polkit-1/localauthority/10-vendor.d/com.ubuntu.desktop.pkla
                if usbcreator_passwordless_group_policy "$usbcreator_policy"; then
                    printf 'USB imaging legacy group policy candidate: %s (image action permits an admin group without a prompt in an active session; effective rules and helper behavior unverified)\n' "$usbcreator_policy"
                fi
                ;;
        esac
    fi
fi
echo ""
