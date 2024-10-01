# Title: Container - checkCreateReleaseAgent
# ID: checkCreateReleaseAgent
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the container is vulnerable to release agent breakout
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $release_agent_breakout3
# Fat linpeas: 0
# Small linpeas: 1


checkCreateReleaseAgent(){
  cat /proc/$$/cgroup 2>/dev/null | grep -Eo '[0-9]+:[^:]+' | grep -Eo '[^:]+$' | while read -r ss
  do
      if unshare -UrmC --propagation=unchanged bash -c "mount -t cgroup -o $ss cgroup /tmp/cgroup_3628d4 2>&1 >/dev/null && test -w /tmp/cgroup_3628d4/release_agent" >/dev/null 2>&1 ; then
          release_agent_breakout3="Yes (unshare with $ss)";
          rm -rf /tmp/cgroup_3628d4
          break
      fi
  done
}