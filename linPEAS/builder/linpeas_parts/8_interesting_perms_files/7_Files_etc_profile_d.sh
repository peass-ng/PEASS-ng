# Title: Interesting Permissions Files - /etc/profile.d/
# ID: IP_Files_etc_profile_d
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: Global shell startup files and bounded virtual-environment source cues
# License: GNU GPL
# Version: 1.1
# Mitre: T1546.004
# Functions Used: check_critial_root_path, echo_not_found, print_2title, print_info
# Global Variables: $IAMROOT, $MACPEAS, $profiledG, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $global_startup, $global_activation
# Fat linpeas: 0
# Small linpeas: 1


check_global_venv_activation() {
  global_startup=$1
  [ -f "$global_startup" ] && [ -r "$global_startup" ] || return 0
  # Read only the first 64 KiB of each fixed startup file. Emit paths, never
  # source lines: a startup file may contain credentials alongside commands.
  dd if="$global_startup" bs=65536 count=1 2>/dev/null |
    awk '($1 == "source" || $1 == ".") && NF >= 2 {
      path = $2
      if (path ~ /^".*"$/ || path ~ /^\047.*\047$/)
        path = substr(path, 2, length(path) - 2)
      if (path ~ /^\/[[:alnum:]_.+\/-]+\/bin\/activate$/ && ++hits <= 8)
        print path
    }' |
    while IFS= read -r global_activation; do
      printf '  %s sources %s (review who runs this startup file and whether the target is writable)\n' "$global_startup" "$global_activation"
      ls -ld "$global_activation" 2>/dev/null || :
    done
}

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Files (scripts) in /etc/profile.d/" "T1546.004"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/linux-basics/shell-startup-aliases-and-history.html#review-startup-and-history-files"
  if [ ! "$MACPEAS" ] && ! [ "$IAMROOT" ]; then #Those folders don´t exist on a MacOS
    (ls -la /etc/profile.d/ 2>/dev/null | sed -${E} "s,$profiledG,${SED_GREEN},") || echo_not_found "/etc/profile.d/"
    check_critial_root_path "/etc/profile"
    check_critial_root_path "/etc/profile.d/"
    for global_startup in /etc/profile /etc/bash.bashrc /etc/bashrc; do
      check_global_venv_activation "$global_startup"
    done
  fi
  echo ""
fi
