# Title: System Information - MacOS OS checks
# ID: SY_Macos_os_checks
# Author: Carlos Polop
# Last Update: 21-11-2026
# Description: Check for MacOS-specific vulnerabilities and misconfigurations that could lead to privilege escalation:
#   - Unsigned kernel extensions
#   - Non-Apple kernel extensions
#   - System Integrity Protection (SIP) status
#   - Gatekeeper status
#   - Conditional SMB/WebDAV kernel exposure on macOS 26 before the 26.6 fixes
#   - Common vulnerable scenarios:
#     * Disabled SIP
#     * Unsigned kernel extensions
#     * Third-party kernel extensions
#     * Disabled Gatekeeper
#   - Exploitation methods:
#     * Kernel extension injection: Load malicious kernel extensions
#     * Common attack vectors:
#       - SIP bypass
#       - Kernel extension loading
#       - Gatekeeper bypass
#       - System modification
#     * Exploit techniques:
#       - Kernel extension injection
#       - SIP bypass
#       - Gatekeeper bypass
#       - System modification
# License: GNU GPL
# Version: 1.1
# Mitre: T1068,T1082
# Functions Used: bounded_command, macosNotSigned, print_2title, print_info
# Global Variables: $MACPEAS, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $macos_mie_version, $macos_mie_build, $macos_mie_mounts
# Fat linpeas: 0
# Small linpeas: 0


if [ "$MACPEAS" ]; then
    print_2title "Kernel Extensions not belonging to apple" "T1082"
    kextstat 2>/dev/null | grep -Ev " com.apple."
    echo ""

    print_2title "Unsigned Kernel Extensions" "T1082"
    macosNotSigned /Library/Extensions
    macosNotSigned /System/Library/Extensions
    echo ""
fi

# Passive only: mount output identifies filesystem type and endpoint without
# touching remote files or sending requests to an untrusted server.
if [ "$MACPEAS" ] && ! [ "$SEARCH_IN_FOLDER" ] && command -v sw_vers >/dev/null 2>&1; then
    macos_mie_version=$(sw_vers -productVersion 2>/dev/null)
    case "$macos_mie_version" in
        26.[0-5]|26.[0-5].*)
            macos_mie_mounts=$(mount 2>/dev/null | awk '
                /\((smbfs|webdav|webdav_fs),/ {
                    if (/\(smbfs,/) {
                        if (++smb_count <= 8) print "SMB: " substr($0, 1, 512)
                        else if (smb_count == 9) print "SMB: further mounts omitted"
                    } else {
                        if (++dav_count <= 8) print "WebDAV: " substr($0, 1, 512)
                        else if (dav_count == 9) print "WebDAV: further mounts omitted"
                    }
                }
            ')
            if [ -n "$macos_mie_mounts" ]; then
                macos_mie_build=$(sw_vers -buildVersion 2>/dev/null)
                print_2title "macOS remote filesystem kernel exposure review" "T1068"
                print_info "https://support.apple.com/en-us/128067"
                print_info "https://blog.calif.io/p/apple-mie-exploitation-challenge"
                printf 'macOS %s (build %s) predates the 26.6 SMB/WebDAV fixes; this is not proof that this build or mount is exploitable.\n' "$macos_mie_version" "$macos_mie_build"
                printf '%s\n' "$macos_mie_mounts"
                case "$macos_mie_mounts" in *'SMB: '*) echo 'SMB (CVE-2026-64704): a malicious server can return a mismatched CREATE context; negotiated capabilities and server control are not known from mount output.' ;; esac
                case "$macos_mie_mounts" in *'WebDAV: '*) echo 'WebDAV (CVE-2026-64699): a malicious server can induce a short successful read; server control and mount access are not known from mount output.' ;; esac
                echo 'Review trust and access to these mounts; update to macOS 26.6 or later and remove or restrict untrusted remote mounts.'
                echo ''
            fi
            ;;
    esac
fi

if [ "$MACPEAS" ] && [ "$(command -v brew 2>/dev/null || echo -n '')" ]; then
    print_2title "Brew Doctor Suggestions" "T1082"
    bounded_command 20 brew doctor
    case "$?" in 124|143) echo 'brew doctor reached 20 seconds; suggestions may be incomplete';; esac
    echo ""
fi
