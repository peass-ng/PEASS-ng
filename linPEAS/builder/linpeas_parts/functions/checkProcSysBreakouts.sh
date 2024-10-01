# Title: Container - checkProcSysBreakouts
# ID: checkProcSysBreakouts
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the container is vulnerable to several breakouts abusing /sys and /proc folders
# License: GNU GPL
# Version: 1.0
# Functions Used: checkCreateReleaseAgent
# Global Variables:
# Initial Functions:
# Generated Global Variables: $dev_mounted, $proc_mounted, $run_unshare, $release_agent_breakout1, $release_agent_breakout2, $core_pattern_breakout, $modprobe_present, $panic_on_oom_dos, $panic_on_oom, $panic_on, $panic_sys_fs_dos, $binfmt_misc_breakout, $proc_configgz_readable, $sysreq_trigger_dos, $kmsg_readable, $kallsyms_readable, $self_mem_readable, $mem_readable, $kmem_readable, $kmem_writable, $mem_writable, $sched_debug_readable, $mountinfo_readable, $uevent_helper_breakout, $vmcoreinfo_readable, $security_present, $security_writable, $efi_vars_writable, $efi_efivars_writable, $kcore_readable
# Fat linpeas: 0
# Small linpeas: 1


checkProcSysBreakouts(){
  dev_mounted="No"
  if [ $(ls -l /dev | grep -E "^c" | wc -l) -gt 50 ]; then
    dev_mounted="Yes";
  fi

  proc_mounted="No"
  if [ $(ls /proc | grep -E "^[0-9]" | wc -l) -gt 50 ]; then
    proc_mounted="Yes";
  fi

  run_unshare=$(unshare -UrmC bash -c 'echo -n Yes' 2>/dev/null)
  if ! [ "$run_unshare" = "Yes" ]; then
    run_unshare="No"
  fi

  if [ "$(ls -l /sys/fs/cgroup/*/release_agent 2>/dev/null)" ]; then 
    release_agent_breakout1="Yes"
  else 
    release_agent_breakout1="No"
  fi
  
  release_agent_breakout2="No"
  mkdir /tmp/cgroup_3628d4
  mount -t cgroup -o memory cgroup /tmp/cgroup_3628d4 2>/dev/null
  if [ $? -eq 0 ]; then 
    release_agent_breakout2="Yes"; 
    rm -rf /tmp/cgroup_3628d4
  else 
    mount -t cgroup -o rdma cgroup /tmp/cgroup_3628d4 2>/dev/null
    if [ $? -eq 0 ]; then 
      release_agent_breakout2="Yes"; 
      rm -rf /tmp/cgroup_3628d4
    else 
      checkCreateReleaseAgent
    fi
  fi
  rm -rf /tmp/cgroup_3628d4 2>/dev/null
  
  core_pattern_breakout="$( (echo -n '' > /proc/sys/kernel/core_pattern && echo Yes) 2>/dev/null || echo No)"
  modprobe_present="$(ls -l `cat /proc/sys/kernel/modprobe` 2>/dev/null || echo No)"
  panic_on_oom_dos="$( (echo -n '' > /proc/sys/vm/panic_on_oom && echo Yes) 2>/dev/null || echo No)"
  panic_sys_fs_dos="$( (echo -n '' > /proc/sys/fs/suid_dumpable && echo Yes) 2>/dev/null || echo No)"
  binfmt_misc_breakout="$( (echo -n '' > /proc/sys/fs/binfmt_misc/register && echo Yes) 2>/dev/null || echo No)"
  proc_configgz_readable="$([ -r '/proc/config.gz' ] 2>/dev/null && echo Yes || echo No)"
  sysreq_trigger_dos="$( (echo -n '' > /proc/sysrq-trigger && echo Yes) 2>/dev/null || echo No)"
  kmsg_readable="$( (dmesg > /dev/null 2>&1 && echo Yes) 2>/dev/null || echo No)"  # Kernel Exploit Dev
  kallsyms_readable="$( (head -n 1 /proc/kallsyms > /dev/null && echo Yes )2>/dev/null || echo No)" # Kernel Exploit Dev
  self_mem_readable="$( (head -n 1 /proc/self/mem > /dev/null && echo Yes) 2>/dev/null || echo No)"
  if [ "$(head -n 1 /tmp/kcore 2>/dev/null)" ]; then kcore_readable="Yes"; else kcore_readable="No"; fi
  kmem_readable="$( (head -n 1 /proc/kmem > /dev/null && echo Yes) 2>/dev/null || echo No)"
  kmem_writable="$( (echo -n '' > /proc/kmem > /dev/null && echo Yes) 2>/dev/null || echo No)"
  mem_readable="$( (head -n 1 /proc/mem > /dev/null && echo Yes) 2>/dev/null || echo No)"
  mem_writable="$( (echo -n '' > /proc/mem > /dev/null && echo Yes) 2>/dev/null || echo No)"
  sched_debug_readable="$( (head -n 1 /proc/sched_debug > /dev/null && echo Yes) 2>/dev/null || echo No)"
  mountinfo_readable="$( (head -n 1 /proc/*/mountinfo > /dev/null && echo Yes) 2>/dev/null || echo No)"
  uevent_helper_breakout="$( (echo -n '' > /sys/kernel/uevent_helper && echo Yes) 2>/dev/null || echo No)"
  vmcoreinfo_readable="$( (head -n 1 /sys/kernel/vmcoreinfo > /dev/null && echo Yes) 2>/dev/null || echo No)"
  security_present="$( (ls -l /sys/kernel/security > /dev/null && echo Yes) 2>/dev/null || echo No)"
  security_writable="$( (echo -n '' > /sys/kernel/security/a && echo Yes) 2>/dev/null || echo No)"
  efi_vars_writable="$( (echo -n '' > /sys/firmware/efi/vars && echo Yes) 2>/dev/null || echo No)"
  efi_efivars_writable="$( (echo -n '' > /sys/firmware/efi/efivars && echo Yes) 2>/dev/null || echo No)"
}
