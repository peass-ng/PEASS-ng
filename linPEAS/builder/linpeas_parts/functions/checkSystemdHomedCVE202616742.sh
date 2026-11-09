# Title: Functions - checkSystemdHomedCVE202616742
# ID: checkSystemdHomedCVE202616742
# Author: Chack Agent
# Last Update: 09-11-2026
# Description: Identify a current-user systemd-homed CVE-2026-16742 review candidate without authenticating or reading identity data.
# License: GNU GPL
# Version: 1.0
# Functions Used: print_3title, print_info
# Global Variables: $HOME, $IAMROOT
# Initial Functions:
# Generated Global Variables: $homed16742_root, $homed16742_home, $homed16742_release, $homed16742_nss_size
# Fat linpeas: 0
# Small linpeas: 1
# shellcheck shell=sh

checkSystemdHomedCVE202616742() {
    [ -n "$IAMROOT" ] && return 0
    homed16742_release=$1
    homed16742_root=${2:-/}
    homed16742_home=${3:-$HOME}

    # systemctl --version reports the upstream major release, not the distro's
    # patch level. A matched range is a review candidate, never proof of a flaw.
    case "$homed16742_release" in ''|*[!0-9]*) return 0 ;; esac
    [ "$homed16742_release" -ge 245 ] && [ "$homed16742_release" -le 261 ] || return 0
    [ -S "${homed16742_root%/}/run/systemd/userdb/io.systemd.Home" ] || return 0
    [ -f "$homed16742_home/.identity" ] || return 0
    [ -r "${homed16742_root%/}/etc/nsswitch.conf" ] || return 0
    homed16742_nss_size=$(stat -c %s "${homed16742_root%/}/etc/nsswitch.conf" 2>/dev/null ||
        stat -f %z "${homed16742_root%/}/etc/nsswitch.conf" 2>/dev/null) || return 0
    case "$homed16742_nss_size" in ''|*[!0-9]*) return 0 ;; esac
    [ "$homed16742_nss_size" -le 65536 ] || return 0

    # initgroups, when explicitly configured, supersedes group for supplementary
    # membership. Do not report a candidate unless it can consult nss-systemd.
    awk '
        $1 == "passwd:" {
            for (i = 2; i <= NF; i++) if ($i == "systemd") passwd_systemd = 1
        }
        $1 == "group:" {
            for (i = 2; i <= NF; i++) if ($i == "systemd") group_systemd = 1
        }
        $1 == "initgroups:" {
            initgroups_seen = 1
            for (i = 2; i <= NF; i++) if ($i == "systemd") initgroups_systemd = 1
        }
        END { exit !(passwd_systemd && group_systemd && (!initgroups_seen || initgroups_systemd)) }
    ' "${homed16742_root%/}/etc/nsswitch.conf" || return 0

    print_3title "systemd-homed group escalation review (CVE-2026-16742)" "T1068"
    print_info "https://github.com/systemd/systemd/security/advisories/GHSA-jm29-p7hh-vjhv"
    printf 'Current home has .identity; homed userdb socket and NSS mappings are present; systemd upstream release %s is in the affected major range.\n' "$homed16742_release"
    printf 'Review the installed package for a vendor backport, homed account ownership and privileged-group policy. Upstream fixes: 258.10, 259.8, 260.4, 261.2, 262. No authentication attempted.\n'
}
