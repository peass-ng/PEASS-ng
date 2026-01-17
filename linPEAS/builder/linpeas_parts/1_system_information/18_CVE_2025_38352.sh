# Title: System Information - CVE_2025_38352
# ID: SY_CVE_2025_38352
# Author: HT Bot
# Last Update: 22-12-2025
# Description: Detect Linux kernels that may still be vulnerable to CVE-2025-38352 (race-condition UAF in POSIX CPU timers)
#   - Highlights kernels built without CONFIG_POSIX_CPU_TIMERS_TASK_WORK
#   - Flags 6.12.x builds older than the fix commit f90fff1e152dedf52b932240ebbd670d83330eca (first shipped in 6.12.34)
#   - Provides quick risk scoring so operators can decide whether to attempt the publicly available PoC
#   - Core requirements for exploitation:
#       * CONFIG_POSIX_CPU_TIMERS_TASK_WORK disabled (common on 32-bit Android / custom kernels)
#       * Lack of the upstream exit_state guard in run_posix_cpu_timers()
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_list
# Global Variables: $E, $SED_GREEN, $SED_RED, $SED_RED_YELLOW, $SED_YELLOW
# Initial Functions:
# Generated Global Variables: $cve38352_kernel_release, $cve38352_kernel_version_cmp, $cve38352_symbol, $cve38352_task_work_state, $cve38352_config_status, $cve38352_config_source, $cve38352_config_candidates, $cve38352_cfg, $cve38352_line, $cve38352_patch_state, $cve38352_patch_label, $cve38352_fix_tag, $cve38352_last_vuln_tag, $cve38352_risk_msg, $cve38352_risk_color, $cve38352_task_line, $cve38352_patch_line, $cve38352_risk_line
# Fat linpeas: 0
# Small linpeas: 1

cve38352_version_lt(){
    awk -v v1="$1" -v v2="$2" '
    function cleannum(val) {
        gsub(/[^0-9].*/, "", val)
        if (val == "") {
            val = 0
        }
        return val + 0
    }
    BEGIN {
        n = split(v1, a, ".")
        m = split(v2, b, ".")
        max = (n > m ? n : m)
        for (i = 1; i <= max; i++) {
            av = (i <= n ? cleannum(a[i]) : 0)
            bv = (i <= m ? cleannum(b[i]) : 0)
            if (av < bv) {
                exit 0
            }
            if (av > bv) {
                exit 1
            }
        }
        exit 1
    }'
}

cve38352_sanitize_version(){
    printf "%s" "$1" | tr '-' '.' | sed 's/[^0-9.].*$//' | sed 's/\.\./\./g' | sed 's/^\.//' | sed 's/\.$//'
}

print_2title "CVE-2025-38352 - POSIX CPU timers race"

cve38352_kernel_release=$(uname -r 2>/dev/null)
if [ -z "$cve38352_kernel_release" ]; then
    echo_not_found "uname -r"
    echo ""
else

    cve38352_kernel_version_cmp=$(cve38352_sanitize_version "$cve38352_kernel_release")
    if [ -z "$cve38352_kernel_version_cmp" ]; then
        cve38352_kernel_version_cmp="unknown"
    fi

    cve38352_symbol="CONFIG_POSIX_CPU_TIMERS_TASK_WORK"
    cve38352_task_work_state="unknown"
    cve38352_config_status="Unknown ($cve38352_symbol not found)"
    cve38352_config_source=""

    cve38352_config_candidates="/boot/config-$cve38352_kernel_release /proc/config.gz /lib/modules/$cve38352_kernel_release/build/.config /usr/lib/modules/$cve38352_kernel_release/build/.config /usr/src/linux/.config"
    for cve38352_cfg in $cve38352_config_candidates; do
        [ -r "$cve38352_cfg" ] || continue
        if printf "%s" "$cve38352_cfg" | grep -q '\\.gz$'; then
            cve38352_line=$(gzip -dc "$cve38352_cfg" 2>/dev/null | grep -E "^(# )?$cve38352_symbol" | head -n1)
        else
            cve38352_line=$(grep -E "^(# )?$cve38352_symbol" "$cve38352_cfg" 2>/dev/null | head -n1)
        fi
        [ -z "$cve38352_line" ] && continue
        cve38352_config_source="$cve38352_cfg"
        case "$cve38352_line" in
            "$cve38352_symbol=y")
                cve38352_task_work_state="enabled"
                cve38352_config_status="Enabled (y)"
                ;;
            "$cve38352_symbol=m")
                cve38352_task_work_state="enabled"
                cve38352_config_status="Built as module (m)"
                ;;
            "$cve38352_symbol=n")
                cve38352_task_work_state="disabled"
                cve38352_config_status="Disabled (n)"
                ;;
            "# $cve38352_symbol is not set")
                cve38352_task_work_state="disabled"
                cve38352_config_status="Not set"
                ;;
            *)
                cve38352_config_status="Found: $cve38352_line"
                ;;
        esac
        break
    done

    cve38352_patch_state="unknown_branch"
    cve38352_patch_label="Unable to determine kernel train"
    cve38352_fix_tag="6.12.34"
    cve38352_last_vuln_tag="6.12.33"
    case "$cve38352_kernel_version_cmp" in
        6.12|6.12.*)
            if cve38352_version_lt "$cve38352_kernel_version_cmp" "$cve38352_fix_tag"; then
                cve38352_patch_state="pre_fix"
                cve38352_patch_label="6.12.x build < $cve38352_fix_tag (last known vulnerable LTS: $cve38352_last_vuln_tag)"
            else
                cve38352_patch_state="post_fix"
                cve38352_patch_label="6.12.x build >= $cve38352_fix_tag (should include fix f90fff1e152d)"
            fi
            ;;
        unknown)
            cve38352_patch_label="Kernel version string could not be parsed"
            ;;
        *)
            cve38352_patch_label="Kernel train $cve38352_kernel_version_cmp (verify commit f90fff1e152dedf52b932240ebbd670d83330eca manually)"
            ;;
    esac

    cve38352_risk_msg="Unknown - missing configuration data"
    cve38352_risk_color=""
    if [ "$cve38352_task_work_state" = "enabled" ]; then
        cve38352_risk_msg="Low - CONFIG_POSIX_CPU_TIMERS_TASK_WORK is enabled"
        cve38352_risk_color="green"
    elif [ "$cve38352_task_work_state" = "disabled" ]; then
        if [ "$cve38352_patch_state" = "pre_fix" ]; then
            cve38352_risk_msg="High - task_work disabled & kernel predates fix f90fff1e152d"
            cve38352_risk_color="red"
        else
            cve38352_risk_msg="Review - task_work disabled, ensure fix f90fff1e152d is backported"
            cve38352_risk_color="yellow"
        fi
    fi

    print_list "Kernel release ............... $cve38352_kernel_release\n"
    print_list "Comparable version ........... $cve38352_kernel_version_cmp\n"

    cve38352_task_line="Task_work config ............. $cve38352_config_status"
    if [ -n "$cve38352_config_source" ]; then
        cve38352_task_line="$cve38352_task_line (from $cve38352_config_source)"
    fi
    cve38352_task_line="$cve38352_task_line\n"
    if [ "$cve38352_task_work_state" = "disabled" ]; then
        print_list "$cve38352_task_line" | sed -${E} "s,.*,${SED_RED},"
    elif [ "$cve38352_task_work_state" = "enabled" ]; then
        print_list "$cve38352_task_line" | sed -${E} "s,.*,${SED_GREEN},"
    else
        print_list "$cve38352_task_line"
    fi

    cve38352_patch_line="Patch status ................. $cve38352_patch_label\n"
    if [ "$cve38352_patch_state" = "pre_fix" ]; then
        print_list "$cve38352_patch_line" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    elif [ "$cve38352_patch_state" = "post_fix" ]; then
        print_list "$cve38352_patch_line" | sed -${E} "s,.*,${SED_GREEN},"
    else
        print_list "$cve38352_patch_line" | sed -${E} "s,.*,${SED_YELLOW},"
    fi

    cve38352_risk_line="CVE-2025-38352 risk .......... $cve38352_risk_msg\n"
    case "$cve38352_risk_color" in
        red)
            print_list "$cve38352_risk_line" | sed -${E} "s,.*,${SED_RED_YELLOW},"
            ;;
        green)
            print_list "$cve38352_risk_line" | sed -${E} "s,.*,${SED_GREEN},"
            ;;
        yellow)
            print_list "$cve38352_risk_line" | sed -${E} "s,.*,${SED_YELLOW},"
            ;;
        *)
            print_list "$cve38352_risk_line"
            ;;
    esac

    echo ""
fi
