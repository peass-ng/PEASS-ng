# Title: Container - containerCheck
# ID: containerCheck
# Author: Carlos Polop
# Last Update: 21-03-2026
# Description: Check whether the current process appears to be running inside a Linux container and identify common runtime hints.
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_no
# Global Variables: 
# Initial Functions:
# Generated Global Variables: $inContainer, $containerType
# Fat linpeas: 0
# Small linpeas: 1


containerCheck() {
  inContainer=""
  containerType="$(echo_no)"

  # Are we inside docker?
  if [ -f "/.dockerenv" ] ||
    grep "/docker/" /proc/1/cgroup -qa 2>/dev/null ||
    grep -qai docker /proc/self/cgroup  2>/dev/null ||
    [ -f "/run/.dockerenv" ] ; then

    inContainer="1"
    containerType="docker\n"
  fi

  # Are we inside kubenetes?
  if grep "/kubepod" /proc/1/cgroup -qa 2>/dev/null ||
    grep -qai kubepods /proc/self/cgroup 2>/dev/null; then

    inContainer="1"
    if [ "$containerType" ]; then containerType="$containerType (kubernetes)\n"
    else containerType="kubernetes\n"
    fi
  fi
  
  # Inside concourse?
  if grep "/concourse" /proc/1/mounts -qa 2>/dev/null; then
    inContainer="1"
    if [ "$containerType" ]; then 
      containerType="$containerType (concourse)\n"
    fi
  fi

  # Are we inside LXC?
  if env | grep "container=lxc" -qa 2>/dev/null ||
      grep "/lxc/" /proc/1/cgroup -qa 2>/dev/null; then

    inContainer="1"
    if echo "$containerType" | grep -qv "lxc"; then
      if [ "$containerType" ] && [ "$containerType" != "$(echo_no)" ]; then containerType="$containerType (lxc)\n"
      else containerType="lxc\n"
      fi
    fi
  fi

  # Are we inside podman?
  if [ -f "/run/.containerenv" ] ||
      env | grep -qa "container=podman" 2>/dev/null ||
      grep -qa "container=podman" /proc/1/environ 2>/dev/null; then

    inContainer="1"
    if echo "$containerType" | grep -qv "podman"; then
      if [ "$containerType" ] && [ "$containerType" != "$(echo_no)" ]; then containerType="$containerType (podman)\n"
      else containerType="podman\n"
      fi
    fi
  fi

  # Check for other container platforms that report themselves in PID 1 env
  if [ -z "$inContainer" ]; then
    if grep -qa 'container=' /proc/1/environ 2>/dev/null; then
      inContainer="1"
      containerType="$(tr '\000' '\n' < /proc/1/environ 2>/dev/null | awk -F= '/^container=/{print $2; exit}')\n"
    fi
  fi
}
