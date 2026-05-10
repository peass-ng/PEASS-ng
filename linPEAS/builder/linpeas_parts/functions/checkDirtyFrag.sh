# Title: Function - checkDirtyFrag
# ID: checkDirtyFrag
# Author: Samuel Monsempes
# Last Update: 10-05-2026
# Description: Check whether the current Linux kernel looks exposed to Dirty Frag (CVE-2026-43284 and CVE-2026-43500).
# Description: Per-CVE module state (xfrm-ESP and rxrpc), built-in detection, modprobe.d blacklist, user-namespace mitigation, CAP_NET_ADMIN, kernel build-date heuristic.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $E, $SED_GREEN, $SED_YELLOW, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $DF43_KERNEL_OS, $DF43_KERNEL_RELEASE, $DF43_KBUILD, $DF43_ESP_MODS, $DF43_RXRPC_MODS, $DF43_LOADED_ESP, $DF43_LOADED_RXRPC, $DF43_AUTO_ESP, $DF43_AUTO_RXRPC, $DF43_MODDEP, $DF43_MOD, $DF43_BUILTIN_ESP, $DF43_BUILTIN_RXRPC, $DF43_KCFG, $DF43_KCAT, $DF43_C, $DF43_MITIG_ESP, $DF43_MITIG_RXRPC, $DF43_USERNS_OFF, $DF43_CAP_NET_ADMIN, $DF43_CAPEFF, $DF43_CAPLO, $DF43_OLDBUILD, $DF43_BDATE, $DF43_BE, $DF43_FE, $DF43_ESP_REACH, $DF43_RXRPC_REACH, $DF43_RC
# Fat linpeas: 0
# Small linpeas: 1

checkDirtyFrag() {
    (
        DF43_KERNEL_OS=$(uname -s 2>/dev/null || echo unknown)
        if [ "$DF43_KERNEL_OS" != "Linux" ]; then
            echo "NOT APPLICABLE: Dirty Frag (CVE-2026-43284 / CVE-2026-43500) affects Linux kernels only." | sed -${E} "s,.*,${SED_GREEN},"
            exit 0
        fi

        DF43_KERNEL_RELEASE=$(uname -r 2>/dev/null || echo unknown)
        DF43_KBUILD=$(uname -v 2>/dev/null || echo unknown)
        DF43_ESP_MODS="esp4 esp6 xfrm_user ipcomp4 ipcomp6"
        DF43_RXRPC_MODS="rxrpc"

        DF43_LOADED_ESP=""
        DF43_LOADED_RXRPC=""
        for DF43_MOD in $DF43_ESP_MODS; do
            grep -qE "^${DF43_MOD} " /proc/modules 2>/dev/null \
                && DF43_LOADED_ESP="$DF43_LOADED_ESP $DF43_MOD"
        done
        for DF43_MOD in $DF43_RXRPC_MODS; do
            grep -qE "^${DF43_MOD} " /proc/modules 2>/dev/null \
                && DF43_LOADED_RXRPC="$DF43_LOADED_RXRPC $DF43_MOD"
        done

        DF43_AUTO_ESP=""
        DF43_AUTO_RXRPC=""
        DF43_MODDEP="/lib/modules/${DF43_KERNEL_RELEASE}/modules.dep"
        if [ -r "$DF43_MODDEP" ]; then
            for DF43_MOD in $DF43_ESP_MODS; do
                if grep -qE "(^|/)${DF43_MOD}\.ko(\.[a-z]+)?:" "$DF43_MODDEP" 2>/dev/null; then
                    case " $DF43_LOADED_ESP " in
                        *" $DF43_MOD "*) : ;;
                        *) DF43_AUTO_ESP="$DF43_AUTO_ESP $DF43_MOD" ;;
                    esac
                fi
            done
            for DF43_MOD in $DF43_RXRPC_MODS; do
                if grep -qE "(^|/)${DF43_MOD}\.ko(\.[a-z]+)?:" "$DF43_MODDEP" 2>/dev/null; then
                    case " $DF43_LOADED_RXRPC " in
                        *" $DF43_MOD "*) : ;;
                        *) DF43_AUTO_RXRPC="$DF43_AUTO_RXRPC $DF43_MOD" ;;
                    esac
                fi
            done
        fi

        DF43_BUILTIN_ESP=""
        DF43_BUILTIN_RXRPC=""
        DF43_KCFG=""
        for DF43_C in /proc/config.gz "/boot/config-${DF43_KERNEL_RELEASE}" /boot/config; do
            [ -r "$DF43_C" ] && { DF43_KCFG="$DF43_C"; break; }
        done
        if [ -n "$DF43_KCFG" ]; then
            case "$DF43_KCFG" in
                *.gz) DF43_KCAT="zcat" ;;
                *)    DF43_KCAT="cat" ;;
            esac
            $DF43_KCAT "$DF43_KCFG" 2>/dev/null \
                | grep -qE '^(CONFIG_INET_ESP|CONFIG_INET6_ESP|CONFIG_XFRM_USER|CONFIG_INET_IPCOMP|CONFIG_INET6_IPCOMP)=y' \
                && DF43_BUILTIN_ESP="yes"
            $DF43_KCAT "$DF43_KCFG" 2>/dev/null \
                | grep -qE '^CONFIG_AF_RXRPC=y' \
                && DF43_BUILTIN_RXRPC="yes"
        fi

        DF43_MITIG_ESP=""
        DF43_MITIG_RXRPC=""
        for DF43_MOD in $DF43_ESP_MODS; do
            if grep -rEhsq "^[[:space:]]*(blacklist|install)[[:space:]]+${DF43_MOD}\b" \
                 /etc/modprobe.d/ /run/modprobe.d/ /usr/lib/modprobe.d/ /lib/modprobe.d/ 2>/dev/null; then
                DF43_MITIG_ESP="yes"
                break
            fi
        done
        for DF43_MOD in $DF43_RXRPC_MODS; do
            if grep -rEhsq "^[[:space:]]*(blacklist|install)[[:space:]]+${DF43_MOD}\b" \
                 /etc/modprobe.d/ /run/modprobe.d/ /usr/lib/modprobe.d/ /lib/modprobe.d/ 2>/dev/null; then
                DF43_MITIG_RXRPC="yes"
                break
            fi
        done

        DF43_USERNS_OFF=""
        if [ -r /proc/sys/kernel/unprivileged_userns_clone ]; then
            [ "$(cat /proc/sys/kernel/unprivileged_userns_clone 2>/dev/null)" = "0" ] \
                && DF43_USERNS_OFF="yes"
        fi
        if [ -r /proc/sys/user/max_user_namespaces ]; then
            [ "$(cat /proc/sys/user/max_user_namespaces 2>/dev/null)" = "0" ] \
                && DF43_USERNS_OFF="yes"
        fi

        DF43_CAP_NET_ADMIN=""
        if [ -r /proc/self/status ]; then
            DF43_CAPEFF=$(awk '/^CapEff:/ {print $2}' /proc/self/status 2>/dev/null)
            case "$DF43_CAPEFF" in
                "" | *[!0-9a-fA-F]*) : ;;
                *)
                    DF43_CAPLO=$(printf '%s' "$DF43_CAPEFF" | tail -c 4)
                    [ "$(( 0x${DF43_CAPLO} & 0x1000 ))" -ne 0 ] && DF43_CAP_NET_ADMIN="yes"
                    ;;
            esac
        fi

        DF43_OLDBUILD=""
        DF43_BDATE=$(printf '%s' "$DF43_KBUILD" | sed -nE 's/.*([A-Z][a-z]{2} [A-Z][a-z]{2} +[0-9]{1,2} [0-9:]+ (UTC )?[0-9]{4}).*/\1/p')
        if [ -n "$DF43_BDATE" ]; then
            DF43_BE=$(date -d "$DF43_BDATE" +%s 2>/dev/null)
            DF43_FE=$(date -d '2026-05-08' +%s 2>/dev/null)
            if [ -n "$DF43_BE" ] && [ -n "$DF43_FE" ] && [ "$DF43_BE" -lt "$DF43_FE" ]; then
                DF43_OLDBUILD="yes"
            fi
        fi

        if [ -n "$DF43_LOADED_ESP" ]; then
            echo "CVE-2026-43284 (xfrm-ESP): loaded:$DF43_LOADED_ESP" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        elif [ "$DF43_BUILTIN_ESP" = "yes" ]; then
            echo "CVE-2026-43284 (xfrm-ESP): built into kernel (modprobe blacklist ineffective)" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        elif [ -n "$DF43_AUTO_ESP" ]; then
            echo "CVE-2026-43284 (xfrm-ESP): autoloadable:$DF43_AUTO_ESP" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        else
            echo "CVE-2026-43284 (xfrm-ESP): not reachable on this kernel" | sed -${E} "s,.*,${SED_GREEN},"
        fi
        if [ -n "$DF43_LOADED_RXRPC" ]; then
            echo "CVE-2026-43500 (rxrpc): loaded:$DF43_LOADED_RXRPC" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        elif [ "$DF43_BUILTIN_RXRPC" = "yes" ]; then
            echo "CVE-2026-43500 (rxrpc): built into kernel (modprobe blacklist ineffective)" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        elif [ -n "$DF43_AUTO_RXRPC" ]; then
            echo "CVE-2026-43500 (rxrpc): autoloadable:$DF43_AUTO_RXRPC" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        else
            echo "CVE-2026-43500 (rxrpc): not reachable on this kernel" | sed -${E} "s,.*,${SED_GREEN},"
        fi

        if [ "$DF43_MITIG_ESP" = "yes" ]; then
            echo "modprobe mitigation (xfrm-ESP): present" | sed -${E} "s,.*,${SED_GREEN},"
        else
            echo "modprobe mitigation (xfrm-ESP): not found" | sed -${E} "s,.*,${SED_YELLOW},"
        fi
        if [ "$DF43_MITIG_RXRPC" = "yes" ]; then
            echo "modprobe mitigation (rxrpc): present" | sed -${E} "s,.*,${SED_GREEN},"
        else
            echo "modprobe mitigation (rxrpc): not found" | sed -${E} "s,.*,${SED_YELLOW},"
        fi
        if [ "$DF43_USERNS_OFF" = "yes" ]; then
            echo "Unprivileged user namespaces: disabled (breaks the public PoC)" | sed -${E} "s,.*,${SED_GREEN},"
        else
            echo "Unprivileged user namespaces: enabled" | sed -${E} "s,.*,${SED_YELLOW},"
        fi
        if [ "$DF43_CAP_NET_ADMIN" = "yes" ]; then
            echo "Current process: CAP_NET_ADMIN present (matches public PoC requirement)" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        fi
        if [ "$DF43_OLDBUILD" = "yes" ]; then
            echo "Kernel build predates upstream fix (2026-05-08): likely unpatched unless distro backport." | sed -${E} "s,.*,${SED_YELLOW},"
        fi

        DF43_ESP_REACH=""
        [ -n "$DF43_LOADED_ESP$DF43_AUTO_ESP" ] && DF43_ESP_REACH="yes"
        [ "$DF43_BUILTIN_ESP" = "yes" ] && DF43_ESP_REACH="yes"
        DF43_RXRPC_REACH=""
        [ -n "$DF43_LOADED_RXRPC$DF43_AUTO_RXRPC" ] && DF43_RXRPC_REACH="yes"
        [ "$DF43_BUILTIN_RXRPC" = "yes" ] && DF43_RXRPC_REACH="yes"

        DF43_RC=0
        if [ "$DF43_ESP_REACH" = "yes" ] && [ "$DF43_MITIG_ESP" != "yes" ]; then
            if [ "$DF43_USERNS_OFF" = "yes" ]; then
                echo "CVE-2026-43284 reachable but public PoC blocked by disabled user namespaces." | sed -${E} "s,.*,${SED_YELLOW},"
                [ $DF43_RC -lt 1 ] && DF43_RC=1
            else
                echo "LIKELY VULNERABLE to CVE-2026-43284 (xfrm-ESP)." | sed -${E} "s,.*,${SED_RED_YELLOW},"
                DF43_RC=2
            fi
        fi
        if [ "$DF43_RXRPC_REACH" = "yes" ] && [ "$DF43_MITIG_RXRPC" != "yes" ]; then
            if [ "$DF43_USERNS_OFF" = "yes" ]; then
                echo "CVE-2026-43500 reachable but public PoC blocked by disabled user namespaces." | sed -${E} "s,.*,${SED_YELLOW},"
                [ $DF43_RC -lt 1 ] && DF43_RC=1
            else
                echo "LIKELY VULNERABLE to CVE-2026-43500 (rxrpc)." | sed -${E} "s,.*,${SED_RED_YELLOW},"
                DF43_RC=2
            fi
        fi

        if [ $DF43_RC -gt 0 ]; then
            echo "Mitigation: 'install esp4/esp6/rxrpc /bin/false' in /etc/modprobe.d/, then rmmod;"
            echo "or sysctl kernel.unprivileged_userns_clone=0; or apply distro patches."
        fi
        exit $DF43_RC
    )
}
