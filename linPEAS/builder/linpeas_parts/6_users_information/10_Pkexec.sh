# Title: Users Information - Pkexec
# ID: UG_Pkexec
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check Pkexec policy and related files for privilege escalation
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $Groups, $groupsB, $groupsVB, $nosh_usrs, $sh_usrs, $USER
# Initial Functions:
# Generated Global Variables: $pkexec_bin, $policy_dir, $policy_file
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Checking Pkexec and Polkit"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/interesting-groups-linux-pe/index.html#pe---method-2"

echo ""
print_3title "Polkit Binary"
# Check pkexec binary
pkexec_bin=$(command -v pkexec 2>/dev/null)
if [ -n "$pkexec_bin" ]; then
  echo "Pkexec binary found at: $pkexec_bin" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
  if [ -u "$pkexec_bin" ]; then
    echo "Pkexec binary has SUID bit set!" | sed -${E} "s,.*,${SED_RED},g"
  fi
  ls -l "$pkexec_bin" 2>/dev/null
  
  # Check polkit version for known vulnerabilities
  if command -v pkexec >/dev/null 2>&1; then
    pkexec --version 2>/dev/null
  fi
fi

# Check polkit policies
echo ""
print_3title "Polkit Policies"
for policy_dir in "/etc/polkit-1/localauthority.conf.d/" "/etc/polkit-1/rules.d/" "/usr/share/polkit-1/rules.d/"; do
  if [ -d "$policy_dir" ]; then
    echo "Checking $policy_dir:" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    if [ -w "$policy_dir" ]; then
      echo "WARNING: $policy_dir is writable!" | sed -${E} "s,.*,${SED_RED},g"
    fi
    for policy_file in "$policy_dir"/*; do
      if [ -f "$policy_file" ]; then
        if [ -w "$policy_file" ]; then
          echo "WARNING: $policy_file is writable!" | sed -${E} "s,.*,${SED_RED},g"
        fi
        cat "$policy_file" 2>/dev/null | grep -v "^#" | grep -Ev "\W+\#|^#" 2>/dev/null | sed -${E} "s,$groupsB,${SED_RED},g" | sed -${E} "s,$groupsVB,${SED_RED},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN},g" | sed -${E} "s,$nosh_usrs,${SED_BLUE},g" | sed "s,$USER,${SED_RED},g" | sed -${E} "s,$Groups,${SED_RED},g"
      fi
    done
  fi
done

# Check for polkit authentication agent
echo ""
print_3title "Polkit Authentication Agent"
ps aux 2>/dev/null | grep -i "polkit" | grep -v "grep"
echo ""
