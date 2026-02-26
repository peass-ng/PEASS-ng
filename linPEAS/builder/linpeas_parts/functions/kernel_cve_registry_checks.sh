# Title: Function - kernel_cve_registry_checks
# ID: kernel_cve_registry_checks
# Author: Carlos Polop
# Last Update: 25-02-2026
# Description: Evaluate declared kernel CVE rules using kernel version, arch, kernel config, sysctl and command prerequisites.
# Description: Data source chunks KERNEL_CVE_DATA_1..21 are capped to 25 rows each.
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_3title, print_list
# Global Variables: $E, $KERNEL_CVE_DATA_1, $KERNEL_CVE_DATA_2, $KERNEL_CVE_DATA_3, $KERNEL_CVE_DATA_4, $KERNEL_CVE_DATA_5, $KERNEL_CVE_DATA_6, $KERNEL_CVE_DATA_7, $KERNEL_CVE_DATA_8, $KERNEL_CVE_DATA_9, $KERNEL_CVE_DATA_10, $KERNEL_CVE_DATA_11, $KERNEL_CVE_DATA_12, $KERNEL_CVE_DATA_13, $KERNEL_CVE_DATA_14, $KERNEL_CVE_DATA_15, $KERNEL_CVE_DATA_16, $KERNEL_CVE_DATA_17, $KERNEL_CVE_DATA_18, $KERNEL_CVE_DATA_19, $KERNEL_CVE_DATA_20, $KERNEL_CVE_DATA_21, $SED_GREEN, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $KERNEL_CVE_CFG_FILE, $KERNEL_CVE_CFG_SOURCE, $KERNEL_CVE_CFG_LINE, $KERNEL_CVE_CFG_KEY, $KERNEL_CVE_CFG_EXPR, $KERNEL_CVE_CFG_EXPECT, $KERNEL_CVE_CFG_OP, $KERNEL_CVE_CFG_CUR, $KERNEL_CVE_SYS_EXPR, $KERNEL_CVE_SYS_KEY, $KERNEL_CVE_SYS_OP, $KERNEL_CVE_SYS_VAL, $KERNEL_CVE_SYS_CUR, $KERNEL_CVE_REQS, $KERNEL_CVE_REQ, $KERNEL_CVE_REQ_LINES, $KERNEL_CVE_ID, $KERNEL_CVE_ID_NORM, $KERNEL_CVE_NAME, $KERNEL_CVE_TAGS, $KERNEL_CVE_RANK, $KERNEL_CVE_COMMENTS, $KERNEL_CVE_EXPL, $KERNEL_CVE_VERS, $KERNEL_CVE_VER_LINES, $KERNEL_CVE_ALT, $KERNEL_CVE_MIL, $KERNEL_CVE_TOKEN_OK, $KERNEL_CVE_MATCHES, $KERNEL_CVE_KERNEL_RELEASE, $KERNEL_CVE_KERNEL_VERSION, $KERNEL_CVE_KERNEL_ARCH, $KERNEL_CVE_KERNEL_OS, $KERNEL_CVE_VER, $KERNEL_CVE_OP, $KERNEL_CVE_REQVER, $KERNEL_CVE_CURVER, $KERNEL_CVE_CMP, $KERNEL_CVE_PRINT_ID, $KERNEL_CVE_PRINT_REASON, $KERNEL_CVE_ID_RAW, $KERNEL_CVE_ID_ITEM, $KERNEL_CVE_ID_OUT, $KERNEL_CVE_ALL_DATA
# Fat linpeas: 0
# Small linpeas: 1

KERNEL_CVE_EXPL=""
KERNEL_CVE_ALT=""
KERNEL_CVE_MIL=""

kercve_norm_ver() {
    printf "%s" "$1" | tr '-' '.' | sed 's/[^0-9.].*$//' | sed 's/\.\./\./g' | sed 's/^\.//' | sed 's/\.$//'
}

kercve_ver_cmp() {
    KERNEL_CVE_CURVER=$(kercve_norm_ver "$1")
    KERNEL_CVE_REQVER=$(kercve_norm_ver "$3")
    KERNEL_CVE_OP="$2"

    [ -z "$KERNEL_CVE_CURVER" ] && return 1
    [ -z "$KERNEL_CVE_REQVER" ] && return 1

    KERNEL_CVE_CMP=$(awk -v a="$KERNEL_CVE_CURVER" -v b="$KERNEL_CVE_REQVER" '
    function clean(v){gsub(/[^0-9]/,"",v); if(v=="")v=0; return v+0}
    BEGIN{
      na=split(a,A,"."); nb=split(b,B,"."); n=(na>nb?na:nb);
      for(i=1;i<=n;i++){
        va=(i<=na?clean(A[i]):0); vb=(i<=nb?clean(B[i]):0);
        if(va<vb){print -1; exit}
        if(va>vb){print 1; exit}
      }
      print 0
    }')

    case "$KERNEL_CVE_OP" in
        '=') [ "$KERNEL_CVE_CMP" -eq 0 ] ;;
        '>') [ "$KERNEL_CVE_CMP" -gt 0 ] ;;
        '<') [ "$KERNEL_CVE_CMP" -lt 0 ] ;;
        '>=') [ "$KERNEL_CVE_CMP" -ge 0 ] ;;
        '<=') [ "$KERNEL_CVE_CMP" -le 0 ] ;;
        *) return 1 ;;
    esac
}

kercve_get_cfg_line() {
    KERNEL_CVE_CFG_KEY="$1"

    if [ -z "$KERNEL_CVE_CFG_SOURCE" ] || ! [ -r "$KERNEL_CVE_CFG_SOURCE" ]; then
        return 1
    fi

    if printf "%s" "$KERNEL_CVE_CFG_SOURCE" | grep -q '\\.gz$'; then
        KERNEL_CVE_CFG_LINE=$(gzip -dc "$KERNEL_CVE_CFG_SOURCE" 2>/dev/null | grep -E "^(${KERNEL_CVE_CFG_KEY}=|# ${KERNEL_CVE_CFG_KEY} is not set)" | head -n1)
    else
        KERNEL_CVE_CFG_LINE=$(grep -E "^(${KERNEL_CVE_CFG_KEY}=|# ${KERNEL_CVE_CFG_KEY} is not set)" "$KERNEL_CVE_CFG_SOURCE" 2>/dev/null | head -n1)
    fi

    [ -n "$KERNEL_CVE_CFG_LINE" ]
}

kercve_eval_config_req() {
    KERNEL_CVE_CFG_EXPR="$1"

    [ -z "$KERNEL_CVE_CFG_SOURCE" ] && return 0

    if printf "%s" "$KERNEL_CVE_CFG_EXPR" | grep -q '!='; then
        KERNEL_CVE_CFG_OP='!='
        KERNEL_CVE_CFG_KEY=$(printf "%s" "$KERNEL_CVE_CFG_EXPR" | awk -F'!=' '{print $1}')
        KERNEL_CVE_CFG_EXPECT=$(printf "%s" "$KERNEL_CVE_CFG_EXPR" | awk -F'!=' '{print $2}')
    elif printf "%s" "$KERNEL_CVE_CFG_EXPR" | grep -q '='; then
        KERNEL_CVE_CFG_OP='='
        KERNEL_CVE_CFG_KEY=$(printf "%s" "$KERNEL_CVE_CFG_EXPR" | awk -F'=' '{print $1}')
        KERNEL_CVE_CFG_EXPECT=$(printf "%s" "$KERNEL_CVE_CFG_EXPR" | awk -F'=' '{print $2}')
    else
        KERNEL_CVE_CFG_OP='present'
        KERNEL_CVE_CFG_KEY="$KERNEL_CVE_CFG_EXPR"
        KERNEL_CVE_CFG_EXPECT='[my]'
    fi

    if ! kercve_get_cfg_line "$KERNEL_CVE_CFG_KEY"; then
        return 0
    fi

    if printf "%s" "$KERNEL_CVE_CFG_LINE" | grep -q '# .* is not set'; then
        KERNEL_CVE_CFG_CUR='n'
    else
        KERNEL_CVE_CFG_CUR=$(printf "%s" "$KERNEL_CVE_CFG_LINE" | awk -F'=' '{print $2}')
    fi

    if [ "$KERNEL_CVE_CFG_OP" = '!=' ]; then
        if printf "%s" "$KERNEL_CVE_CFG_EXPECT" | grep -q '\\[my\\]'; then
            ! printf "%s" "$KERNEL_CVE_CFG_CUR" | grep -Eq '^[my]$'
        else
            [ "$KERNEL_CVE_CFG_CUR" != "$KERNEL_CVE_CFG_EXPECT" ]
        fi
        return
    fi

    if printf "%s" "$KERNEL_CVE_CFG_EXPECT" | grep -q '\\[my\\]'; then
        printf "%s" "$KERNEL_CVE_CFG_CUR" | grep -Eq '^[my]$'
        return
    fi

    [ "$KERNEL_CVE_CFG_CUR" = "$KERNEL_CVE_CFG_EXPECT" ]
}

kercve_eval_sysctl_req() {
    KERNEL_CVE_SYS_EXPR="$1"

    if printf "%s" "$KERNEL_CVE_SYS_EXPR" | grep -q '!='; then
        KERNEL_CVE_SYS_OP='!='
        KERNEL_CVE_SYS_KEY=$(printf "%s" "$KERNEL_CVE_SYS_EXPR" | awk -F'!=' '{print $1}')
        KERNEL_CVE_SYS_VAL=$(printf "%s" "$KERNEL_CVE_SYS_EXPR" | awk -F'!=' '{print $2}')
    elif printf "%s" "$KERNEL_CVE_SYS_EXPR" | grep -q '=='; then
        KERNEL_CVE_SYS_OP='=='
        KERNEL_CVE_SYS_KEY=$(printf "%s" "$KERNEL_CVE_SYS_EXPR" | awk -F'==' '{print $1}')
        KERNEL_CVE_SYS_VAL=$(printf "%s" "$KERNEL_CVE_SYS_EXPR" | awk -F'==' '{print $2}')
    else
        return 1
    fi

    KERNEL_CVE_SYS_CUR=$(sysctl -n "$KERNEL_CVE_SYS_KEY" 2>/dev/null)
    [ -z "$KERNEL_CVE_SYS_CUR" ] && return 0

    if [ "$KERNEL_CVE_SYS_OP" = '==' ]; then
        [ "$KERNEL_CVE_SYS_CUR" = "$KERNEL_CVE_SYS_VAL" ]
    else
        [ "$KERNEL_CVE_SYS_CUR" != "$KERNEL_CVE_SYS_VAL" ]
    fi
}

kercve_eval_req_token() {
    KERNEL_CVE_REQ="$1"

    [ -z "$KERNEL_CVE_REQ" ] && return 0

    if printf "%s" "$KERNEL_CVE_REQ" | grep -q '^pkg='; then
        [ "$KERNEL_CVE_REQ" = 'pkg=linux-kernel' ]
        return
    fi

    if printf "%s" "$KERNEL_CVE_REQ" | grep -q '^ver'; then
        KERNEL_CVE_OP=$(printf "%s" "$KERNEL_CVE_REQ" | sed -E 's/^ver(<=|>=|=|<|>).*/\1/')
        KERNEL_CVE_VER=$(printf "%s" "$KERNEL_CVE_REQ" | sed -E 's/^ver(<=|>=|=|<|>)//')
        kercve_ver_cmp "$KERNEL_CVE_KERNEL_VERSION" "$KERNEL_CVE_OP" "$KERNEL_CVE_VER"
        return
    fi

    if [ "$KERNEL_CVE_REQ" = 'x86_64' ]; then
        [ "$KERNEL_CVE_KERNEL_ARCH" = 'x86_64' ]
        return
    fi

    if [ "$KERNEL_CVE_REQ" = 'x86' ]; then
        [ "$KERNEL_CVE_KERNEL_ARCH" = 'i386' ] || [ "$KERNEL_CVE_KERNEL_ARCH" = 'i686' ] || [ "$KERNEL_CVE_KERNEL_ARCH" = 'x86' ]
        return
    fi

    if printf "%s" "$KERNEL_CVE_REQ" | grep -q '^CONFIG_'; then
        kercve_eval_config_req "$KERNEL_CVE_REQ"
        return
    fi

    if printf "%s" "$KERNEL_CVE_REQ" | grep -q '^sysctl:'; then
        kercve_eval_sysctl_req "${KERNEL_CVE_REQ#sysctl:}"
        return
    fi

    if printf "%s" "$KERNEL_CVE_REQ" | grep -q '^cmd:'; then
        eval "${KERNEL_CVE_REQ#cmd:}" >/dev/null 2>&1
        return
    fi

    return 1
}

kercve_match_version_list() {
    KERNEL_CVE_VERS="$1"
    KERNEL_CVE_VER_LINES=$(printf "%s" "$KERNEL_CVE_VERS" | tr ',' '\n')

    while IFS= read -r KERNEL_CVE_VER; do
        KERNEL_CVE_VER=$(printf "%s" "$KERNEL_CVE_VER" | sed 's/^ *//;s/ *$//')
        [ -z "$KERNEL_CVE_VER" ] && continue
        if printf "%s" "$KERNEL_CVE_KERNEL_VERSION" | grep -Eq "^${KERNEL_CVE_VER}(\\.|-|$)"; then
            return 0
        fi
    done <<EOFV
$KERNEL_CVE_VER_LINES
EOFV

    return 1
}

kercve_normalize_cve_list() {
    KERNEL_CVE_ID_RAW="$1"
    KERNEL_CVE_ID_OUT=""

    KERNEL_CVE_ID_RAW=$(printf "%s" "$KERNEL_CVE_ID_RAW" | tr ';' ',' | tr '|' ',')
    while IFS= read -r KERNEL_CVE_ID_ITEM; do
        KERNEL_CVE_ID_ITEM=$(printf "%s" "$KERNEL_CVE_ID_ITEM" | sed 's/^ *//;s/ *$//' | tr '[:lower:]' '[:upper:]')
        [ -z "$KERNEL_CVE_ID_ITEM" ] && continue
        if printf "%s" "$KERNEL_CVE_ID_ITEM" | grep -Eq '^CVE-[0-9]{4}-[0-9]+$'; then
            if [ -z "$KERNEL_CVE_ID_OUT" ]; then KERNEL_CVE_ID_OUT="$KERNEL_CVE_ID_ITEM"; else KERNEL_CVE_ID_OUT="$KERNEL_CVE_ID_OUT,$KERNEL_CVE_ID_ITEM"; fi
            continue
        fi
        if printf "%s" "$KERNEL_CVE_ID_ITEM" | grep -Eq '^[0-9]{4}-[0-9]+$'; then
            if [ -z "$KERNEL_CVE_ID_OUT" ]; then KERNEL_CVE_ID_OUT="CVE-$KERNEL_CVE_ID_ITEM"; else KERNEL_CVE_ID_OUT="$KERNEL_CVE_ID_OUT,CVE-$KERNEL_CVE_ID_ITEM"; fi
            continue
        fi
    done <<EOFC
$(printf "%s" "$KERNEL_CVE_ID_RAW" | tr ',' '\n')
EOFC

    printf "%s" "$KERNEL_CVE_ID_OUT"
}

kercve_run_registry() {
    KERNEL_CVE_KERNEL_OS=$(uname -s 2>/dev/null)
    KERNEL_CVE_KERNEL_RELEASE=$(uname -r 2>/dev/null)
    KERNEL_CVE_KERNEL_VERSION=$(kercve_norm_ver "$KERNEL_CVE_KERNEL_RELEASE")
    KERNEL_CVE_KERNEL_ARCH=$(uname -m 2>/dev/null)

    KERNEL_CVE_CFG_SOURCE=""
    for KERNEL_CVE_CFG_FILE in "/proc/config.gz" "/boot/config-$KERNEL_CVE_KERNEL_RELEASE" "/lib/modules/$KERNEL_CVE_KERNEL_RELEASE/build/.config" "/usr/lib/modules/$KERNEL_CVE_KERNEL_RELEASE/build/.config" "/usr/src/linux/.config"; do
        if [ -r "$KERNEL_CVE_CFG_FILE" ]; then
            KERNEL_CVE_CFG_SOURCE="$KERNEL_CVE_CFG_FILE"
            break
        fi
    done

    KERNEL_CVE_ALL_DATA=$(printf "%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s\n%s" \
        "$KERNEL_CVE_DATA_1" "$KERNEL_CVE_DATA_2" "$KERNEL_CVE_DATA_3" "$KERNEL_CVE_DATA_4" "$KERNEL_CVE_DATA_5" \
        "$KERNEL_CVE_DATA_6" "$KERNEL_CVE_DATA_7" "$KERNEL_CVE_DATA_8" "$KERNEL_CVE_DATA_9" "$KERNEL_CVE_DATA_10" \
        "$KERNEL_CVE_DATA_11" "$KERNEL_CVE_DATA_12" "$KERNEL_CVE_DATA_13" "$KERNEL_CVE_DATA_14" "$KERNEL_CVE_DATA_15" \
        "$KERNEL_CVE_DATA_16" "$KERNEL_CVE_DATA_17" "$KERNEL_CVE_DATA_18" "$KERNEL_CVE_DATA_19" "$KERNEL_CVE_DATA_20" \
        "$KERNEL_CVE_DATA_21")

    print_list "Operating system ............. $KERNEL_CVE_KERNEL_OS\n"
    print_list "Kernel release ............... $KERNEL_CVE_KERNEL_RELEASE\n"
    print_list "Comparable version ........... $KERNEL_CVE_KERNEL_VERSION\n"
    print_list "Data chunk limit ............. max 25 rows per KERNEL_CVE_DATA_* variable (1..21)\n"
    if [ -n "$KERNEL_CVE_CFG_SOURCE" ]; then
        print_list "Kernel config source ......... $KERNEL_CVE_CFG_SOURCE\n"
    else
        print_list "Kernel config source ......... "
        echo_not_found "not available"
    fi

    if [ "$KERNEL_CVE_KERNEL_OS" != "Linux" ]; then
        print_list "Registry status .............. Linux kernel CVE datasets are not applicable to $KERNEL_CVE_KERNEL_OS\n" | sed -${E} "s,.*,${SED_GREEN},"
        return 0
    fi

    KERNEL_CVE_MATCHES=0

    print_3title "Matched CVEs"
    while IFS="	" read -r KERNEL_CVE_ID KERNEL_CVE_NAME KERNEL_CVE_REQS KERNEL_CVE_TAGS KERNEL_CVE_RANK KERNEL_CVE_COMMENTS; do
        [ -z "$KERNEL_CVE_ID" ] && continue

        KERNEL_CVE_TOKEN_OK=1

        if printf "%s" "$KERNEL_CVE_REQS" | grep -Eq '^pkg=|^ver|CONFIG_|sysctl:|cmd:|,pkg=|,ver|,CONFIG_|,sysctl:|,cmd:'; then
            KERNEL_CVE_REQ_LINES=$(printf "%s" "$KERNEL_CVE_REQS" | tr ',' '\n')
            while IFS= read -r KERNEL_CVE_REQ; do
                KERNEL_CVE_REQ=$(printf "%s" "$KERNEL_CVE_REQ" | sed 's/^ *//;s/ *$//')
                if ! kercve_eval_req_token "$KERNEL_CVE_REQ"; then
                    KERNEL_CVE_TOKEN_OK=0
                    break
                fi
            done <<EOFR
$KERNEL_CVE_REQ_LINES
EOFR
        else
            if ! kercve_match_version_list "$KERNEL_CVE_REQS"; then
                KERNEL_CVE_TOKEN_OK=0
            fi
        fi

        [ "$KERNEL_CVE_TOKEN_OK" -eq 0 ] && continue

        # Some embedded datasets store rows as: <exploit_name> <cve_id> <versions> ...
        # while others store: <cve_id> <exploit_name> <reqs> ...
        # Normalize whichever column contains the CVE identifier and keep the printable name sensible.
        KERNEL_CVE_ID_RAW="$KERNEL_CVE_ID"
        KERNEL_CVE_ID_NORM=$(kercve_normalize_cve_list "$KERNEL_CVE_ID_RAW")
        if [ -z "$KERNEL_CVE_ID_NORM" ]; then
            KERNEL_CVE_ID_NORM=$(kercve_normalize_cve_list "$KERNEL_CVE_NAME")
            if [ -n "$KERNEL_CVE_ID_NORM" ]; then
                KERNEL_CVE_NAME="$KERNEL_CVE_ID_RAW"
            fi
        fi
        [ -z "$KERNEL_CVE_ID_NORM" ] && continue
        KERNEL_CVE_PRINT_ID="$KERNEL_CVE_ID_NORM"

        KERNEL_CVE_MATCHES=$((KERNEL_CVE_MATCHES + 1))
        printf "%-30s %s\n" "$KERNEL_CVE_PRINT_ID" "$KERNEL_CVE_NAME" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    done <<EOFD
$KERNEL_CVE_ALL_DATA
EOFD

    KERNEL_CVE_PRINT_REASON="Matched CVEs: $KERNEL_CVE_MATCHES"
    if [ "$KERNEL_CVE_MATCHES" -gt 0 ]; then
        print_list "$KERNEL_CVE_PRINT_REASON\n" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    else
        print_list "No rule matched current kernel/version prerequisites in embedded datasets.\n" | sed -${E} "s,.*,${SED_GREEN},"
    fi

    print_list "CVE format note: only normalized CVE-YYYY-NNNN identifiers are reported as findings.\n"
}
