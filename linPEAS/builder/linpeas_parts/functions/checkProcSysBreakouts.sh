# Title: Container - checkProcSysBreakouts
# ID: checkProcSysBreakouts
# Author: Carlos Polop
# Last Update: 21-03-2026
# Description: Check whether procfs/sysfs/cgroup surfaces exposed inside a container could be used for breakout, host discovery, or high-impact abuse.
# License: GNU GPL
# Version: 1.0
# Functions Used: checkCreateReleaseAgent
# Global Variables:
# Initial Functions:
# Generated Global Variables: $dev_mounted, $proc_mounted, $run_unshare, $release_agent_breakout1, $release_agent_breakout2, $core_pattern_breakout, $modprobe_binary, $modprobe_config_writable, $panic_on_oom_dos, $panic_on_oom, $panic_on, $panic_sys_fs_dos, $binfmt_misc_breakout, $proc_configgz_readable, $sysreq_trigger_dos, $kmsg_readable, $kallsyms_readable, $self_mem_readable, $mem_readable, $kmem_readable, $kmem_writable, $mem_writable, $sched_debug_readable, $mountinfo_readable, $mountinfo_file, $uevent_helper_breakout, $vmcoreinfo_readable, $security_present, $security_writable, $efi_vars_writable, $efi_efivars_writable, $kcore_readable, $proc_keys_readable, $proc_timer_list_readable, $sys_firmware_readable, $debugfs_present, $debugfs_readable, $thermal_present, $thermal_readable, $thermal_file
# Fat linpeas: 0
# Small linpeas: 1


checkProcSysBreakouts(){
  can_open_for_write() {
    if [ -e "$1" ] && command -v dd >/dev/null 2>&1 && dd if=/dev/null of="$1" bs=1 count=0 conv=notrunc >/dev/null 2>&1; then
      echo Yes
    else
      echo No
    fi
  }

  dev_mounted="No"
  if [ $(ls -l /dev | grep -E "^c" | wc -l) -gt 50 ]; then
    dev_mounted="Yes";
  fi

  proc_mounted="No"
  if [ $(ls /proc | grep -E "^[0-9]" | wc -l) -gt 50 ]; then
    proc_mounted="Yes";
  fi

  if command -v unshare >/dev/null 2>&1 && command -v sh >/dev/null 2>&1; then
    run_unshare=$(unshare -UrmC sh -c 'echo -n Yes' 2>/dev/null)
  fi
  if ! [ "$run_unshare" = "Yes" ]; then
    run_unshare="No"
  fi

  if [ "$(ls -l /sys/fs/cgroup/*/release_agent 2>/dev/null)" ]; then 
    release_agent_breakout1="Yes"
  else 
    release_agent_breakout1="No"
  fi
  
  release_agent_breakout2="No"
  mkdir -p /tmp/cgroup_3628d4
  mount -t cgroup -o memory cgroup /tmp/cgroup_3628d4 2>/dev/null
  if [ $? -eq 0 ]; then 
    release_agent_breakout2="Yes"; 
    umount /tmp/cgroup_3628d4 >/dev/null 2>&1
    rm -rf /tmp/cgroup_3628d4
  else 
    mount -t cgroup -o rdma cgroup /tmp/cgroup_3628d4 2>/dev/null
    if [ $? -eq 0 ]; then 
      release_agent_breakout2="Yes"; 
      umount /tmp/cgroup_3628d4 >/dev/null 2>&1
      rm -rf /tmp/cgroup_3628d4
    else 
      checkCreateReleaseAgent
    fi
  fi
  rm -rf /tmp/cgroup_3628d4 2>/dev/null
  
  # Prefer zero-byte open-for-write checks here so special files are validated more accurately without trying to change their contents.
  core_pattern_breakout="$(can_open_for_write /proc/sys/kernel/core_pattern)"
  modprobe_binary="$(ls -l "$(cat /proc/sys/kernel/modprobe 2>/dev/null)" 2>/dev/null || echo No)"
  modprobe_config_writable="$(can_open_for_write /proc/sys/kernel/modprobe)"
  panic_on_oom_dos="$(can_open_for_write /proc/sys/vm/panic_on_oom)"
  panic_sys_fs_dos="$(can_open_for_write /proc/sys/fs/suid_dumpable)"
  binfmt_misc_breakout="$(can_open_for_write /proc/sys/fs/binfmt_misc/register)"
  proc_configgz_readable="$([ -r '/proc/config.gz' ] 2>/dev/null && echo Yes || echo No)"
  sysreq_trigger_dos="$(can_open_for_write /proc/sysrq-trigger)"
  kmsg_readable="$( (dmesg > /dev/null 2>&1 && echo Yes) 2>/dev/null || echo No)"  # Kernel Exploit Dev
  kallsyms_readable="$( (head -n 1 /proc/kallsyms > /dev/null && echo Yes )2>/dev/null || echo No)" # Kernel Exploit Dev
  self_mem_readable="$( (head -n 1 /proc/self/mem > /dev/null && echo Yes) 2>/dev/null || echo No)"
  if [ "$(head -n 1 /proc/kcore 2>/dev/null)" ]; then kcore_readable="Yes"; else kcore_readable="No"; fi
  kmem_readable="$( (head -n 1 /proc/kmem > /dev/null && echo Yes) 2>/dev/null || echo No)"
  kmem_writable="$(can_open_for_write /proc/kmem)"
  mem_readable="$( (head -n 1 /proc/mem > /dev/null && echo Yes) 2>/dev/null || echo No)"
  mem_writable="$(can_open_for_write /proc/mem)"
  sched_debug_readable="$( (head -n 1 /proc/sched_debug > /dev/null && echo Yes) 2>/dev/null || echo No)"
  mountinfo_readable="No"
  for mountinfo_file in /proc/[0-9]*/mountinfo; do
    if [ -r "$mountinfo_file" ]; then
      mountinfo_readable="Yes"
      break
    fi
  done
  uevent_helper_breakout="$(can_open_for_write /sys/kernel/uevent_helper)"
  vmcoreinfo_readable="$( (head -n 1 /sys/kernel/vmcoreinfo > /dev/null && echo Yes) 2>/dev/null || echo No)"
  security_present="$( (ls -l /sys/kernel/security > /dev/null && echo Yes) 2>/dev/null || echo No)"
  security_writable="$([ -w /sys/kernel/security ] 2>/dev/null && echo Yes || echo No)"
  efi_vars_writable="$([ -w /sys/firmware/efi/vars ] 2>/dev/null && echo Yes || echo No)"
  efi_efivars_writable="$([ -w /sys/firmware/efi/efivars ] 2>/dev/null && echo Yes || echo No)"
  proc_keys_readable="$( (head -n 1 /proc/keys > /dev/null && echo Yes) 2>/dev/null || echo No)"
  proc_timer_list_readable="$( (head -n 1 /proc/timer_list > /dev/null && echo Yes) 2>/dev/null || echo No)"
  sys_firmware_readable="$([ -r /sys/firmware ] 2>/dev/null && echo Yes || echo No)"
  debugfs_present="$([ -d /sys/kernel/debug ] 2>/dev/null && echo Yes || echo No)"
  debugfs_readable="$( (ls -la /sys/kernel/debug > /dev/null && echo Yes) 2>/dev/null || echo No)"
  thermal_present="$([ -d /sys/class/thermal ] 2>/dev/null && echo Yes || echo No)"
  thermal_readable="No"
  for thermal_file in /sys/class/thermal/*/*; do
    if [ -f "$thermal_file" ] && [ -r "$thermal_file" ]; then
      thermal_readable="Yes"
      break
    fi
  done
}
