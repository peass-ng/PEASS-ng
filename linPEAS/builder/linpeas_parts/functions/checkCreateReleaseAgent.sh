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
  release_agent_breakout3="${release_agent_breakout3:-No}"
  for ss in $(awk -F: '/^[0-9]+:/{print $2}' /proc/$$/cgroup 2>/dev/null); do
      if unshare -UrmC --propagation=unchanged sh -c "mount -t cgroup -o $ss cgroup /tmp/cgroup_3628d4 >/dev/null 2>&1 && test -w /tmp/cgroup_3628d4/release_agent" >/dev/null 2>&1 ; then
          release_agent_breakout3="Yes (unshare with $ss)"
          umount /tmp/cgroup_3628d4 >/dev/null 2>&1
          rm -rf /tmp/cgroup_3628d4 >/dev/null 2>&1
          break
      fi
      umount /tmp/cgroup_3628d4 >/dev/null 2>&1
      rm -rf /tmp/cgroup_3628d4 >/dev/null 2>&1
  done
}
