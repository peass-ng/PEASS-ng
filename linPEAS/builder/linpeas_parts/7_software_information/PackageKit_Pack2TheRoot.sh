# Title: Software Information - PackageKit Pack2TheRoot (CVE-2026-41651)
# ID: SI_PackageKit_Pack2TheRoot
# Author: Samuel Monsempes
# Last Update: 22-04-2026
# Description: Check for the Pack2TheRoot vulnerability (CVE-2026-41651) in PackageKit:
#   - Cross-distro local privilege escalation in the PackageKit daemon
#   - Affects all PackageKit versions >= 1.0.2 and <= 1.3.4
#   - Allows any unprivileged local user to install/remove packages and gain root
#   - Confirmed on default installs of Ubuntu (18.04 - 26.04), Debian Trixie 13.4,
#     RockyLinux 10.1, Fedora 43 (Desktop & Server). Cockpit installs may also be affected.
#   - Exploitation methods:
#     * Trigger an unauthorized package install/removal via the PackageKit D-Bus API
#     * Achieve arbitrary root code execution from a low-privileged session
#   - IOC: PackageKit daemon crashes with an "emitted_finished" assertion failure
#     after successful exploitation (visible in journalctl).
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_2title, print_3title, print_info, echo_not_found
# Global Variables:
# Initial Functions:
# Generated Global Variables: $pk_version, $pk_min_vuln, $pk_max_vuln, $pk_lower, $pk_higher, $pk_ioc_count
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Checking for PackageKit Pack2TheRoot (CVE-2026-41651)" "T1068"
print_info "https://github.security.telekom.com/2026/04/pack2theroot-linux-local-privilege-escalation.html"

pk_version=""
if command -v dpkg >/dev/null 2>&1; then
  pk_version="$(dpkg -l 2>/dev/null | grep -iE '^ii\s+packagekit\s' | awk '{print $3}' | sed -E 's/^[0-9]+://; s/[-+~].*$//' | head -n1)"
fi
if [ -z "$pk_version" ] && command -v rpm >/dev/null 2>&1; then
  pk_version="$(rpm -qa 2>/dev/null | grep -iE '^PackageKit-[0-9]' | head -n1 | sed -E 's/^[Pp]ackage[Kk]it-([0-9.]+)-.*/\1/')"
fi

if [ -z "$pk_version" ]; then
  echo_not_found "PackageKit"
else
  echo "PackageKit version detected: $pk_version"

  # Vulnerable range: >= 1.0.2 and <= 1.3.4
  pk_min_vuln="1.0.2"
  pk_max_vuln="1.3.4"
  pk_lower="$(printf '%s\n%s\n' "$pk_min_vuln" "$pk_version" | sort -V | head -n1)"
  pk_higher="$(printf '%s\n%s\n' "$pk_version" "$pk_max_vuln" | sort -V | tail -n1)"

  if [ "$pk_lower" = "$pk_min_vuln" ] && [ "$pk_higher" = "$pk_max_vuln" ]; then
    echo "Vulnerable to CVE-2026-41651 (Pack2TheRoot) - PackageKit $pk_version is in the vulnerable range >=1.0.2 <=1.3.4" | sed -${E} "s,.*,${SED_RED_YELLOW},"

    # Daemon reachability check (loaded via systemd or activatable via D-Bus)
    echo ""
    print_3title "PackageKit daemon reachability"
    if command -v systemctl >/dev/null 2>&1 && systemctl status packagekit >/dev/null 2>&1; then
      echo "PackageKit service is loaded/running - exploitation likely possible" | sed -${E} "s,.*,${SED_RED},"
    elif command -v pkcon >/dev/null 2>&1 || command -v pkmon >/dev/null 2>&1; then
      echo "pkcon/pkmon present - daemon can be activated on demand via D-Bus" | sed -${E} "s,.*,${SED_RED},"
    else
      echo "PackageKit daemon does not appear to be reachable from this session" | sed -${E} "s,.*,${SED_GREEN},"
    fi

    # Indicator of compromise: emitted_finished assertion failures
    echo ""
    print_3title "IOC: emitted_finished assertion failures"
    if command -v journalctl >/dev/null 2>&1; then
      pk_ioc_count="$(journalctl --no-pager -u packagekit 2>/dev/null | grep -c emitted_finished)"
      if [ "${pk_ioc_count:-0}" -gt 0 ] 2>/dev/null; then
        echo "Found ${pk_ioc_count} 'emitted_finished' crashes in PackageKit logs - possible prior exploitation" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      else
        echo "No emitted_finished assertion failures found in PackageKit logs"
      fi
    else
      echo "journalctl not available - cannot check IOC"
    fi
  else
    echo "PackageKit $pk_version is not in the vulnerable range for CVE-2026-41651" | sed -${E} "s,.*,${SED_GREEN},"
  fi
fi
echo ""
