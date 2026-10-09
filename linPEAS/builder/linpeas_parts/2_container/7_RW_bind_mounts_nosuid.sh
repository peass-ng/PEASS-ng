# Title: Container - Possible shared writable mounts (SUID conditions)
# ID: CT_RW_bind_mounts_nosuid
# Author: HT Bot
# Last Update: 09-10-2026
# Description: Show ordinary writable mounts that may be shared with a host.
#   Mountinfo does not identify bind mounts or prove that a path is host-backed.
# License: GNU GPL
# Version: 1.1
# Mitre: T1611
# Functions Used: containerCheck, print_2title, print_list, print_info
# Global Variables: $inContainer
# Initial Functions: containerCheck
# Generated Global Variables: $CT_RW_bind_mounts_matches
# Fat linpeas: 0
# Small linpeas: 1

# Keep mountinfo's octal path escapes intact so spaces and tabs cannot split rows.
# The two groups put non-root filesystem roots first, but root=/ is retained:
# a bind of an entire filesystem has root=/ too. At most ten rows are printed.
ct_rw_mountinfo_candidates() {
  awk '
    function hasopt(opts, opt) { return index("," opts ",", "," opt ",") != 0 }
    function ordinary(fs, target) {
      if (fs ~ /^(proc|sysfs|devtmpfs|devpts|tmpfs|cgroup|cgroup2|mqueue|pstore|securityfs|debugfs|tracefs|configfs|fusectl|hugetlbfs|autofs|rpc_pipefs|binfmt_misc|nsfs|bpf)$/) return 0
      if (target == "/" && fs == "overlay") return 0
      if (target ~ /^\/(proc|sys|dev)(\/|$)/) return 0
      if (target == "/etc/hosts" || target == "/etc/hostname" || target == "/etc/resolv.conf" || target ~ /^\/run\/secrets(\/|$)/) return 0
      return 1
    }
    {
      sep = 0
      for (i = 7; i <= NF; i++) if ($i == "-") { sep = i; break }
      if (!sep || sep + 3 > NF) next
      root = $4; target = $5; opts = $6
      fs = $(sep + 1); source = $(sep + 2); superopts = $(sep + 3)
      if (!ordinary(fs, target) || !hasopt(opts, "rw") || !hasopt(superopts, "rw")) next
      suid = hasopt(opts, "nosuid") ? "blocked-here" : "not-blocked-here"
      exec = hasopt(opts, "noexec") ? "blocked-here" : "not-blocked-here"
      group = root == "/" ? 2 : 1
      count[group]++
      if (count[group] <= 10) rows[group, count[group]] = "  mount(raw)=" target " root(raw)=" root " fs=" fs " source(raw)=" source " opts=" opts " local-suid=" suid " local-exec=" exec
    }
    END {
      shown = 0
      for (group = 1; group <= 2; group++)
        for (i = 1; i <= count[group] && shown < 10; i++) { print rows[group, i]; shown++ }
      total = count[1] + count[2]
      if (total > shown) print "  ... " total - shown " more ordinary writable mounts omitted"
    }
  ' "$1" 2>/dev/null
}

# /proc/mounts has no filesystem-root field, so this is only a generic list.
ct_rw_mounts_fallback() {
  awk '
    function hasopt(opts, opt) { return index("," opts ",", "," opt ",") != 0 }
    $3 !~ /^(proc|sysfs|devtmpfs|devpts|tmpfs|cgroup|cgroup2|mqueue|pstore|securityfs|debugfs|tracefs|configfs|fusectl|hugetlbfs|autofs|rpc_pipefs|binfmt_misc|nsfs|bpf)$/ &&
    $2 !~ /^\/(proc|sys|dev)(\/|$)/ && $2 != "/" &&
    $2 != "/etc/hosts" && $2 != "/etc/hostname" && $2 != "/etc/resolv.conf" &&
    hasopt($4, "rw") {
      count++
      if (count <= 10) print "  mount(raw)=" $2 " fs=" $3 " source(raw)=" $1 " opts=" $4
    }
    END { if (count > 10) print "  ... " count - 10 " more writable mounts omitted" }
  ' "$1" 2>/dev/null
}

ct_rw_uid_note() {
  if [ "$2" != "0" ]; then
    echo "  Current effective UID is not 0; container-root access is an additional condition."
  elif [ ! -r "$1" ]; then
    echo "  UID 0 mapping is unknown."
  else
    awk '
      $1 == 0 && $3 > 0 {
        if ($2 == 0) print "  UID 0 maps to UID 0 in the parent user namespace; host-root mapping is unverified."
        else print "  UID 0 is remapped in the parent user namespace; host-root ownership is less likely."
        found = 1; exit
      }
      END { if (!found) print "  UID 0 mapping is unknown." }
    ' "$1" 2>/dev/null
  fi
}

containerCheck

if [ "$inContainer" ]; then
  echo ""
  print_2title "Container - Possible shared writable mounts (SUID conditions)" "T1611"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/containers-namespaces/container-security/sensitive-host-mounts.html"

  if [ -r /proc/self/mountinfo ]; then
    CT_RW_bind_mounts_matches=$(ct_rw_mountinfo_candidates /proc/self/mountinfo)
    print_list "Ordinary writable mounts (mountinfo) ......."
  elif [ -r /proc/mounts ]; then
    CT_RW_bind_mounts_matches=$(ct_rw_mounts_fallback /proc/mounts)
    print_list "Generic writable mounts (/proc/mounts) ...."
    echo "  Mountinfo unavailable; filesystem-root and bind classification unavailable."
  else
    CT_RW_bind_mounts_matches=""
    print_list "Writable mount data ......................."
    echo "  Unavailable"
  fi

  if [ -n "$CT_RW_bind_mounts_matches" ]; then
    printf '%s\n' "$CT_RW_bind_mounts_matches"
  elif [ -r /proc/self/mountinfo ] || [ -r /proc/mounts ]; then
    echo "  No ordinary writable mounts observed; this does not rule out host shares."
  fi
  ct_rw_uid_note /proc/self/uid_map "$(id -u 2>/dev/null)"
  echo "  nosuid/noexec describe this mount view only; host mount options may differ."
  echo "  Verify host path identity, ownership, mount options, security policy and host access separately."
  echo ""
fi
