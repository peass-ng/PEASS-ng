# Title: System Information - CVE_2025_38352
# ID: SY_CVE_2025_38352
# Author: HT Bot
# Last Update: 22-12-2025
# Description: Detect kernels that match the prerequisites for CVE-2025-38352 (POSIX CPU timers race) by
#   - Inspecting the running kernel release (focus on Linux 6.12.x where the public PoC targets 6.12.33)
#   - Checking if CONFIG_POSIX_CPU_TIMERS_TASK_WORK is disabled in the kernel configuration
#   - Highlighting Android builds noted in the Sept 2025 Android bulletin as exploited in the wild
#   - Advising to re-enable task_work handling or patch to mitigate the race in handle_posix_cpu_timers()
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $cve38352_kernel_release, $cve38352_kernel_version_base, $cve38352_kernel_major, $cve38352_kernel_minor, $cve38352_kernel_branch, $cve38352_have_zgrep, $cve38352_task_work_value, $cve38352_task_work_source, $cve38352_config_path, $cve38352_match_line, $cve38352_highlight, $cve38352_config_origin, $cve38352_msg
# Fat linpeas: 0
# Small linpeas: 1

cve38352_kernel_release="$(uname -r 2>/dev/null)"
[ -n "$cve38352_kernel_release" ] || exit 0

cve38352_kernel_version_base="${cve38352_kernel_release%%-*}"
cve38352_kernel_major="$(printf '%s' "$cve38352_kernel_version_base" | cut -d. -f1)"
cve38352_kernel_minor="$(printf '%s' "$cve38352_kernel_version_base" | cut -d. -f2)"
cve38352_kernel_branch=""
if [ -n "$cve38352_kernel_major" ] && [ -n "$cve38352_kernel_minor" ]; then
    cve38352_kernel_branch="${cve38352_kernel_major}.${cve38352_kernel_minor}"
fi

cve38352_have_zgrep=""
if command -v zgrep >/dev/null 2>&1; then
    cve38352_have_zgrep="1"
fi

cve38352_task_work_value=""
cve38352_task_work_source=""
for cve38352_config_path in \
    /proc/config.gz \
    "/boot/config-$cve38352_kernel_release" \
    "/lib/modules/$cve38352_kernel_release/config" \
    "/usr/lib/modules/$cve38352_kernel_release/config" \
    "/usr/lib/modules/$cve38352_kernel_release/build/.config" \
    "/lib/modules/$cve38352_kernel_release/build/.config" \
    "/usr/src/linux/.config" \
    "/usr/src/linux-$cve38352_kernel_release/.config"; do
    [ -r "$cve38352_config_path" ] || continue

    if [ "${cve38352_config_path##*.}" = "gz" ]; then
        if [ "$cve38352_have_zgrep" ]; then
            cve38352_match_line="$(zgrep -E '^# CONFIG_POSIX_CPU_TIMERS_TASK_WORK is not set|^CONFIG_POSIX_CPU_TIMERS_TASK_WORK=' "$cve38352_config_path" 2>/dev/null | tail -n1)"
        else
            cve38352_match_line="$(gzip -dc "$cve38352_config_path" 2>/dev/null | grep -E '^# CONFIG_POSIX_CPU_TIMERS_TASK_WORK is not set|^CONFIG_POSIX_CPU_TIMERS_TASK_WORK=' | tail -n1)"
        fi
    else
        cve38352_match_line="$(grep -E '^# CONFIG_POSIX_CPU_TIMERS_TASK_WORK is not set|^CONFIG_POSIX_CPU_TIMERS_TASK_WORK=' "$cve38352_config_path" 2>/dev/null | tail -n1)"
    fi

    if [ -n "$cve38352_match_line" ]; then
        case "$cve38352_match_line" in
            CONFIG_POSIX_CPU_TIMERS_TASK_WORK=*)
                cve38352_task_work_value="${cve38352_match_line#*=}"
                ;;
            "# CONFIG_POSIX_CPU_TIMERS_TASK_WORK is not set")
                cve38352_task_work_value="n"
                ;;
        esac
        if [ -n "$cve38352_task_work_value" ]; then
            cve38352_task_work_source="$cve38352_config_path"
            break
        fi
    fi
done

[ "$cve38352_task_work_value" = "n" ] || exit 0

cve38352_highlight="$SED_YELLOW"
if [ "$cve38352_kernel_branch" = "6.12" ]; then
    cve38352_highlight="$SED_RED_YELLOW"
fi

cve38352_config_origin=""
if [ -n "$cve38352_task_work_source" ]; then
    cve38352_config_origin=" (source: $cve38352_task_work_source)"
fi

cve38352_msg="CVE-2025-38352 (POSIX CPU timers race): Kernel $cve38352_kernel_release built with CONFIG_POSIX_CPU_TIMERS_TASK_WORK disabled$cve38352_config_origin"
echo "$cve38352_msg" | sed -${E} "s,.*,$cve38352_highlight,"
if [ "$cve38352_kernel_branch" = "6.12" ]; then
    echo "  -> Public PoC targets Linux LTS 6.12.33/Android; patch or enable task_work handling to avoid the race in handle_posix_cpu_timers()." | sed -${E} "s,.*,$cve38352_highlight,"
else
    echo "  -> CONFIG_POSIX_CPU_TIMERS_TASK_WORK=n satisfies the CVE-2025-38352 prerequisite; verify if $cve38352_kernel_release is patched." | sed -${E} "s,.*,$cve38352_highlight,"
fi
if printf '%s\n' "$cve38352_kernel_release" | grep -qi "android"; then
    echo "  -> Android build detected; Sept 2025 bulletin reported limited in-the-wild exploitation." | sed -${E} "s,.*,$cve38352_highlight,"
fi
