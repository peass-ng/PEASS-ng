# Title: System Information - Mounts
# ID: SY_Mounts
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: Check mount point misconfigurations, bounded local NFS export policy, udisks2 CVE-2026-7867 exposure, and XFSTango CVE-2026-80530 exposure:
#   - Unmounted filesystems
#   - Mount point permissions
#   - Mount options
#   - Common vulnerable scenarios:
#     * Writable mount points
#     * Insecure mount options
#     * Unmounted sensitive filesystems
#     * Shared mount points
#   - Exploitation methods:
#     * Mount point abuse: Exploit mount misconfigurations
#     * Common attack vectors:
#       - Mount point modification
#       - Filesystem remounting
#       - Mount option abuse
#       - Shared mount exploitation
#     * Exploit techniques:
#       - Mount point manipulation
#       - Filesystem remounting
#       - Mount option exploitation
#       - Shared mount abuse
# License: GNU GPL
# Version: 1.2
# Mitre: T1068,T1082,T1120
# Functions Used: checkLibblockdevCVE20256019, checkUDisksCVE20267867, checkXFSTangoCVE202680530, print_2title, print_info
# Global Variables: $DEBUG, $SEARCH_IN_FOLDER, $mountG, $mountpermsB, $mountpermsG, $notmounted, $Wfolders, $mounted
# Initial Functions:
# Generated Global Variables: $nfs_exports_file, $nfs_exports_size, $nfs_exports_seen, $nfs_exports_output, $nfs_exports_result
# Fat linpeas: 0
# Small linpeas: 1


fuse_allow_other_state() {
    [ -f "$1" ] && [ -r "$1" ] || { printf 'unknown\n'; return; }
    awk '
        NR > 256 { truncated = 1; exit }
        /^[[:space:]]*user_allow_other[[:space:]]*$/ { enabled = 1; exit }
        END {
            if (enabled) print "enabled"
            else if (truncated) print "unknown"
            else print "disabled"
        }
    ' "$1" 2>/dev/null
}

# Inspect only short, regular local policy files. The server's effective
# exports, security flavor, clients, and numeric UID mapping remain unknown.
nfs_export_file_candidates() {
    [ -f "$1" ] && [ -r "$1" ] && [ ! -L "$1" ] || return 0
    nfs_exports_size=$(dd if="$1" bs=16385 count=1 2>/dev/null | wc -c | tr -d '[:space:]') || :
    case "$nfs_exports_size" in ''|*[!0-9]*) return 0 ;; esac
    if [ "$nfs_exports_size" -gt 16384 ]; then
        printf 'NFS export review partial (16 KiB file limit): %s\n' "$1"
        return 0
    fi
    [ "$nfs_exports_size" -gt 0 ] || return 0
    dd if="$1" bs=16384 count=1 2>/dev/null | awk -v source="$1" '
        NR > 128 { print "NFS export review partial (128-line limit): " source; exit }
        length($0) > 512 {
            if (!long_line++) print "NFS export review partial (long line): " source
            next
        }
        {
            line = $0
            sub(/[[:space:]]+#.*/, "", line)
            if (line ~ /^[[:space:]]*#/) next
            candidate = 0
            while (match(line, /\([^)]*\)/)) {
                options = substr(line, RSTART + 1, RLENGTH - 2)
                gsub(/[[:space:]]/, "", options)
                if (options ~ /(^|,)rw(,|$)/ &&
                    options !~ /(^|,)all_squash(,|$)/ &&
                    (options !~ /(^|,)sec=/ || options ~ /(^|,)sec=([^,:]*:)*sys([,:]|$)/)) candidate = 1
                line = substr(line, RSTART + RLENGTH)
            }
            if (!candidate) next
            if (++matches <= 4) printf "NFS writable export identity review candidate: %s:%d\n", source, NR
            else if (matches == 5) { print "NFS export review partial (4-candidate file limit): " source; exit }
        }
    ' 2>/dev/null
}

if ! [ "$SEARCH_IN_FOLDER" ]; then
    checkLibblockdevCVE20256019
    checkUDisksCVE20267867
    checkXFSTangoCVE202680530
fi

if [ -e /etc/fuse.conf ] || [ -L /etc/fuse.conf ]; then
    print_2title "FUSE cross-user mount policy" "T1082"
    case "$(fuse_allow_other_state /etc/fuse.conf)" in
        enabled) echo "user_allow_other enabled: non-root mounts may opt into access by other users, including root; review privileged writes into caller-controlled paths" ;;
        disabled) echo "user_allow_other disabled in /etc/fuse.conf" ;;
        *) echo "user_allow_other unknown (unreadable, non-regular, or beyond first 256 lines)" ;;
    esac
    echo ""
fi

# Live-host policy must not be mixed into an extracted filesystem review.
if ! [ "$SEARCH_IN_FOLDER" ]; then
    nfs_exports_seen=0
    nfs_exports_output=""
    for nfs_exports_file in /etc/exports /etc/exports.d/*.exports; do
        case "$nfs_exports_file" in
            /etc/exports.d/*)
                [ -d /etc/exports.d ] && [ ! -L /etc/exports.d ] || continue
                nfs_exports_seen=$((nfs_exports_seen + 1))
                if [ "$nfs_exports_seen" -gt 8 ]; then
                    nfs_exports_output="${nfs_exports_output}NFS export review partial (8 drop-in limit).
"
                    break
                fi
                ;;
        esac
        nfs_exports_result=$(nfs_export_file_candidates "$nfs_exports_file") || :
        if [ -n "$nfs_exports_result" ]; then
            nfs_exports_output="${nfs_exports_output}${nfs_exports_result}
"
        fi
    done
    if [ -n "$nfs_exports_output" ]; then
        print_2title "NFS writable export identity mapping (passive review)" "T1082"
        printf '%s' "$nfs_exports_output"
        echo "Verify active export policy, allowed clients, AUTH_SYS, numeric UID/GID mapping, ACLs, and nosuid/SELinux before treating a writable export as cross-user execution."
        echo ""
    fi
fi

if [ -f "/etc/fstab" ] || [ "$DEBUG" ]; then
    print_2title "Unmounted file-system?" "T1082,T1120"
    print_info "Check if you can mount umounted devices"
    grep -v "^#" /etc/fstab 2>/dev/null | grep -Ev "\W+\#|^#" | sed -${E} "s,$mountG,${SED_GREEN},g" | sed -${E} "s,$notmounted,${SED_RED},g" | sed -${E} "s%$mounted%${SED_BLUE}%g" | sed -${E} "s,$Wfolders,${SED_RED}," | sed -${E} "s,$mountpermsB,${SED_RED},g" | sed -${E} "s,$mountpermsG,${SED_GREEN},g"
    echo ""
fi
