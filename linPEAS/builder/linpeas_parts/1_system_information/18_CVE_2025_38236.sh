# Title: System Information - CVE_2025_38236
# ID: SY_CVE_2025_38236
# Author: HT Bot
# Last Update: 17-12-2025
# Description: Detect Linux kernels exposed to CVE-2025-38236 (AF_UNIX MSG_OOB UAF) that allow local privilege escalation:
#   - Vulnerable scope:
#     * Linux kernels 6.9+ before commit 32ca245464e1479bfea8592b9db227fdc1641705
#     * AF_UNIX stream sockets with MSG_OOB enabled (CONFIG_AF_UNIX_OOB or implicit support)
#   - Exploitation summary:
#     * send/recv MSG_OOB pattern leaves zero-length SKBs in the receive queue
#     * manage_oob() skips cleanup, freeing the OOB SKB while u->oob_skb still points to it
#     * Subsequent recv(MSG_OOB) dereferences the dangling pointer → kernel UAF → LPE
#   - Mitigations:
#     * Update to a kernel that includes commit 32ca245464e1479bfea8592b9db227fdc1641705 (or newer)
#     * Disable CONFIG_AF_UNIX_OOB or block MSG_OOB in sandboxed processes
#     * Backport vendor fixes or follow Chrome's MSG_OOB filtering approach
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $MACPEAS, $SED_RED_YELLOW, $SED_GREEN, $E
# Initial Functions:
# Generated Global Variables: $cve38236_kernel_release, $cve38236_kernel_version, $cve38236_oob_line, $cve38236_unix_line, $cve38236_oob_status, $CVE38236_CONFIG_SOURCE, $cve38236_conf_file, $cve38236_config_key, $cve38236_release, $cve38236_cfg, $cve38236_config_line
# Fat linpeas: 0
# Small linpeas: 1

_cve38236_version_to_number() {
    if [ -z "$1" ]; then
        printf '0\n'
        return
    fi
    echo "$1" | awk -F. '{
        major=$1+0
        if (NF>=2) minor=$2+0; else minor=0
        if (NF>=3) patch=$3+0; else patch=0
        printf "%d\n", (major*1000000)+(minor*1000)+patch
    }'
}

_cve38236_version_ge() {
    local v1 v2
    v1=$(_cve38236_version_to_number "$1")
    v2=$(_cve38236_version_to_number "$2")
    [ "$v1" -ge "$v2" ]
}

_cve38236_cat_config_file() {
    local cve38236_conf_file="$1"
    if [ -z "$cve38236_conf_file" ] || ! [ -r "$cve38236_conf_file" ]; then
        return 1
    fi
    if printf '%s' "$cve38236_conf_file" | grep -q '\\.gz$'; then
        if command -v zcat >/dev/null 2>&1; then
            zcat "$cve38236_conf_file" 2>/dev/null
        elif command -v gzip >/dev/null 2>&1; then
            gzip -dc "$cve38236_conf_file" 2>/dev/null
        else
            cat "$cve38236_conf_file" 2>/dev/null
        fi
    else
        cat "$cve38236_conf_file" 2>/dev/null
    fi
}

_cve38236_read_config_line() {
    local cve38236_config_key="$1"
    local cve38236_release cve38236_config_line cve38236_cfg
    cve38236_release="$(uname -r 2>/dev/null)"
    for cve38236_cfg in /proc/config.gz \
        "/boot/config-${cve38236_release}" \
        "/usr/lib/modules/${cve38236_release}/build/.config" \
        "/lib/modules/${cve38236_release}/build/.config"; do
        if [ -r "$cve38236_cfg" ]; then
            cve38236_config_line=$(_cve38236_cat_config_file "$cve38236_cfg" | grep -E "^(${cve38236_config_key}=|# ${cve38236_config_key} is not set)" | head -n1)
            if [ -n "$cve38236_config_line" ]; then
                CVE38236_CONFIG_SOURCE="$cve38236_cfg"
                printf '%s\n' "$cve38236_config_line"
                return 0
            fi
        fi
    done
    return 1
}


if [ ! "$MACPEAS" ]; then
    cve38236_kernel_release="$(uname -r 2>/dev/null)"
    cve38236_kernel_version="$(printf '%s' "$cve38236_kernel_release" | sed 's/[^0-9.].*//')"

    if [ -n "$cve38236_kernel_version" ] && _cve38236_version_ge "$cve38236_kernel_version" "6.9.0"; then
        print_2title "CVE-2025-38236 - AF_UNIX MSG_OOB UAF"
    
        cve38236_oob_line=$(_cve38236_read_config_line "CONFIG_AF_UNIX_OOB")
        cve38236_oob_status="unknown"
    
        if printf '%s' "$cve38236_oob_line" | grep -q '=y\|=m'; then
            cve38236_oob_status="enabled"
        elif printf '%s' "$cve38236_oob_line" | grep -q 'not set'; then
            cve38236_oob_status="disabled"
        fi
    
        if [ "$cve38236_oob_status" = "unknown" ]; then
            cve38236_unix_line=$(_cve38236_read_config_line "CONFIG_UNIX")
            if printf '%s' "$cve38236_unix_line" | grep -q 'not set'; then
                cve38236_oob_status="disabled"
            elif printf '%s' "$cve38236_unix_line" | grep -q '=y\|=m'; then
                cve38236_oob_status="enabled"
            fi
        fi
    
        if [ "$cve38236_oob_status" = "disabled" ]; then
            printf 'Kernel %s >= 6.9 but MSG_OOB support is disabled (%s).\n' "$cve38236_kernel_release" "${cve38236_oob_line:-CONFIG_AF_UNIX disabled}" | sed -${E} "s,.*,${SED_GREEN},"
            print_info "CVE-2025-38236 requires AF_UNIX MSG_OOB; disabling CONFIG_AF_UNIX_OOB/CONFIG_UNIX mitigates it."
        else
            printf 'Kernel %s (parsed %s) may be vulnerable to CVE-2025-38236 - AF_UNIX MSG_OOB UAF.\n' "$cve38236_kernel_release" "$cve38236_kernel_version" | sed -${E} "s,.*,${SED_RED_YELLOW},"
            [ -n "$cve38236_oob_line" ] && print_info "Config hint: $cve38236_oob_line"
            if [ "$cve38236_oob_status" = "unknown" ]; then
                print_info "Could not read CONFIG_AF_UNIX_OOB directly; AF_UNIX appears enabled, so assume MSG_OOB reachable."
            fi
            print_info "Exploit chain: crafted MSG_OOB send/recv frees the OOB SKB while u->oob_skb still points to it, enabling kernel UAF → arbitrary read/write primitives (Project Zero 2025/08)."
            print_info "Mitigations: update to a kernel containing commit 32ca245464e1479bfea8592b9db227fdc1641705, disable CONFIG_AF_UNIX_OOB, or filter MSG_OOB in sandbox policies."
            print_info "Heuristic detection: based solely on uname -r and kernel config; vendor kernels with backported fixes should be verified manually."
        fi
        echo ""
    fi

fi
