###########################################
#-------------) System Info (-------------#
###########################################

#-- SY) OS
print_2title "Operative system"
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#kernel-exploits"
(cat /proc/version || uname -a ) 2>/dev/null | sed -${E} "s,$kernelDCW_Ubuntu_Precise_1,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_2,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_3,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_4,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_5,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Precise_6,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Trusty_1,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Trusty_2,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Trusty_3,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Trusty_4,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Ubuntu_Xenial,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel5_1,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel5_2,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel5_3,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel6_1,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel6_2,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel6_3,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel6_4,${SED_RED_YELLOW}," | sed -${E} "s,$kernelDCW_Rhel7,${SED_RED_YELLOW}," | sed -${E} "s,$kernelB,${SED_RED},"
warn_exec lsb_release -a 2>/dev/null
if [ "$MACPEAS" ]; then
    warn_exec system_profiler SPSoftwareDataType
fi
echo ""

#-- SY) Sudo
print_2title "Sudo version"
if [ "$(command -v sudo 2>/dev/null)" ]; then
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#sudo-version"
sudo -V 2>/dev/null | grep "Sudo ver" | sed -${E} "s,$sudovB,${SED_RED},"
else echo_not_found "sudo"
fi
echo ""

#--SY) USBCreator
if (busctl list 2>/dev/null | grep -q com.ubuntu.USBCreator) || [ "$DEBUG" ]; then
    print_2title "USBCreator"
    print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation/d-bus-enumeration-and-command-injection-privilege-escalation"

    pc_version=$(dpkg -l 2>/dev/null | grep policykit-desktop-privileges | grep -oP "[0-9][0-9a-zA-Z\.]+")
    if [ -z "$pc_version" ]; then
        pc_version=$(apt-cache policy policykit-desktop-privileges 2>/dev/null | grep -oP "\*\*\*.*" | cut -d" " -f2)
    fi
    if [ -n "$pc_version" ]; then
        pc_length=${#pc_version}
        pc_major=$(echo "$pc_version" | cut -d. -f1)
        pc_minor=$(echo "$pc_version" | cut -d. -f2)
        if [ "$pc_length" -eq 4 ] && [ "$pc_major" -eq 0 ] && [ "$pc_minor"  -lt 21 ]; then
            echo "Vulnerable!!" | sed -${E} "s,.*,${SED_RED},"
        fi
    fi
fi
echo ""

#-- SY) PATH

print_2title "PATH"
print_info "https://book.hacktricks.xyz/linux-hardening/privilege-escalation#writable-path-abuses"
if ! [ "$IAMROOT" ]; then
    echo "$OLDPATH" 2>/dev/null | sed -${E} "s,$Wfolders|\./|\.:|:\.,${SED_RED_YELLOW},g"
fi

if [ "$DEBUG" ]; then
     echo "New path exported: $PATH"
fi
echo ""

#-- SY) Date
print_2title "Date & uptime"
warn_exec date 2>/dev/null
warn_exec uptime 2>/dev/null
echo ""

#-- SY) System stats
if [ "$EXTRA_CHECKS" ]; then
    print_2title "System stats"
    (df -h || lsblk) 2>/dev/null || echo_not_found "df and lsblk"
    warn_exec free 2>/dev/null
    echo ""
fi

#-- SY) CPU info
if [ "$EXTRA_CHECKS" ]; then
    print_2title "CPU info"
    warn_exec lscpu 2>/dev/null
    echo ""
fi

if [ -d "/dev" ] || [ "$DEBUG" ] ; then
    print_2title "Any sd*/disk* disk in /dev? (limit 20)"
    ls /dev 2>/dev/null | grep -Ei "^sd|^disk" | sed "s,crypt,${SED_RED}," | head -n 20
    echo ""
fi

if [ -f "/etc/fstab" ] || [ "$DEBUG" ]; then
    print_2title "Unmounted file-system?"
    print_info "Check if you can mount umounted devices"
    grep -v "^#" /etc/fstab 2>/dev/null | grep -Ev "\W+\#|^#" | sed -${E} "s,$mountG,${SED_GREEN},g" | sed -${E} "s,$notmounted,${SED_RED},g" | sed -${E} "s%$mounted%${SED_BLUE}%g" | sed -${E} "s,$Wfolders,${SED_RED}," | sed -${E} "s,$mountpermsB,${SED_RED},g" | sed -${E} "s,$mountpermsG,${SED_GREEN},g"
    echo ""
fi

if ([ "$(command -v diskutil)" ] || [ "$DEBUG" ]) && [ "$EXTRA_CHECKS" ]; then
    print_2title "Mounted disks information"
    warn_exec diskutil list
    echo ""
fi

if [ "$(command -v smbutil)" ] || [ "$DEBUG" ]; then
    print_2title "Mounted SMB Shares"
    warn_exec smbutil statshares -a
    echo ""
fi
