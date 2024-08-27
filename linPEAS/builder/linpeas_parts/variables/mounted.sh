# Title: Variables - mounted
# ID: mounted
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Find mounted folders
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: 
# Initial Functions:
# Generated Global Variables: $mounted
# Fat linpeas: 0
# Small linpeas: 1


mounted=$( (cat /proc/self/mountinfo || cat /proc/1/mountinfo) 2>/dev/null | cut -d " " -f5 | grep "^/" | tr '\n' '|')$(cat /etc/fstab 2>/dev/null | grep -v "#" | grep -E '\W/\W' | awk '{print $1}')
if ! [ "$mounted" ]; then
  mounted=$( (mount -l || cat /proc/mounts || cat /proc/self/mounts || cat /proc/1/mounts) 2>/dev/null | grep "^/" | cut -d " " -f1 | tr '\n' '|')$(cat /etc/fstab 2>/dev/null | grep -v "#" | grep -E '\W/\W' | awk '{print $1}')
fi
if ! [ "$mounted" ]; then mounted="ImPoSSssSiBlEee"; fi