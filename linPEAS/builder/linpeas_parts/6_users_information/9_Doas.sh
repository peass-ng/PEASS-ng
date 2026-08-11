# Title: Users Information - Doas
# ID: UG_Doas
# Author: Carlos Polop
# Last Update: 11-08-2026
# Description: Check doas/OpenDoas configuration, effective rules, binary permissions, and known vulnerable versions.
#   Detects unrestricted and nopass root rules, GTFOBins-capable commands (including the HTB Soccer dstat path), dangerous environment preservation, writable configuration paths, and applicable CVEs.
# License: GNU GPL
# Version: 1.1
# Mitre: T1548.003
# Functions Used: doas_check_command, doas_command_is_dangerous, doas_config_syntax_valid, doas_extract_upstream_version, doas_get_package_details, doas_read_rules, doas_rule_applies_to_current_user, doas_rule_command, doas_rule_has_dangerous_environment, doas_rule_has_option, doas_rule_targets_root, doas_version_ge, doas_version_le, doas_version_lt, echo_not_found, print_2title, print_3title, print_info
# Global Variables: $doas_package_full_version, $doas_package_homepage, $doas_package_implementation, $doas_package_manager, $doas_package_name
# Initial Functions:
# Generated Global Variables: $conf_file, $doas_active_rules, $doas_bin, $doas_bin_mode, $doas_bin_owner, $doas_bin_trusted, $doas_conf_candidates, $doas_conf_dir, $doas_conf_dir_mode, $doas_conf_found, $doas_conf_mode, $doas_conf_owner, $doas_conf_trusted, $doas_current_gids, $doas_current_groups, $doas_current_uid, $doas_current_user, $doas_package_label, $doas_rule_applies, $doas_rule_cmd_value, $doas_rule_dangerous, $doas_rule_env, $doas_rule_line, $doas_rule_nopass, $doas_rule_number, $doas_rule_root, $doas_rule_unrestricted, $doas_seen_configs, $doas_seen_test_commands, $doas_strings_conf, $doas_test_cmd, $doas_test_commands, $doas_check_output, $doas_tiocsti, $doas_upstream_version
# Fat linpeas: 0
# Small linpeas: 1


doas_bin="$(command -v doas 2>/dev/null)"
doas_current_user="$(id -un 2>/dev/null)"
doas_current_uid="$(id -u 2>/dev/null)"
doas_current_groups="$(id -Gn 2>/dev/null)"
doas_current_gids="$(id -G 2>/dev/null)"
doas_bin_trusted="no"

doas_conf_candidates="/etc/doas.conf
/usr/local/etc/doas.conf
/opt/local/etc/doas.conf
/usr/pkg/etc/doas.conf"

if [ -n "$doas_bin" ]; then
  doas_conf_candidates="$doas_conf_candidates
$(dirname "$doas_bin")/doas.conf
$(dirname "$doas_bin")/../etc/doas.conf
$(dirname "$doas_bin")/etc/doas.conf"
  if command -v strings >/dev/null 2>&1; then
    doas_strings_conf="$(strings "$doas_bin" 2>/dev/null | grep -E '^/[^[:space:]]*/doas\.conf$' | head -n 10)"
    [ -n "$doas_strings_conf" ] && doas_conf_candidates="$doas_conf_candidates
$doas_strings_conf"
  fi
fi

doas_conf_found="no"
for conf_file in /etc/doas.conf /usr/local/etc/doas.conf /opt/local/etc/doas.conf /usr/pkg/etc/doas.conf; do
  [ -e "$conf_file" ] && doas_conf_found="yes"
done

if [ -n "$doas_bin" ] || [ "$doas_conf_found" = "yes" ]; then
  print_2title "Doas/OpenDoas configuration and vulnerabilities" "T1548.003"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#doas"

  if [ -n "$doas_bin" ]; then
    print_3title "Doas binary and version" "T1548.003"
    # -L makes permission checks describe the executable target, not a package-manager symlink.
    doas_bin_owner="$(ls -ldLn "$doas_bin" 2>/dev/null | awk '{print $3}')"
    doas_bin_mode="$(ls -ldL "$doas_bin" 2>/dev/null | awk '{print $1}')"
    echo "Doas binary found at: $doas_bin" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
    ls -ld "$doas_bin" 2>/dev/null

    doas_bin_trusted="yes"
    if [ "$doas_bin_owner" != "0" ]; then
      echo "WARNING: doas is not owned by root (owner UID: ${doas_bin_owner:-unknown})" | sed -${E} "s,.*,${SED_RED},g"
      doas_bin_trusted="no"
    fi
    if [ -u "$doas_bin" ]; then
      echo "Doas has the expected SUID bit set (normal for a privilege-delegation binary)" | sed -${E} "s,.*,${SED_GREEN},g"
    else
      echo "Doas does not have its usual SUID bit; verify how privileges are granted" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
      doas_bin_trusted="no"
    fi
    if { [ "$doas_current_uid" != "0" ] && [ -w "$doas_bin" ]; } || printf "%s" "$doas_bin_mode" | cut -c6,9 | grep -q w; then
      echo "CRITICAL: doas is writable by a non-root context or by group/other" | sed -${E} "s,.*,${SED_RED},g"
      doas_bin_trusted="no"
    fi

    doas_get_package_details "$doas_bin"
    doas_upstream_version="$(doas_extract_upstream_version "$doas_package_full_version")"
    if [ -n "$doas_package_full_version" ]; then
      doas_package_label="$doas_package_name $doas_package_full_version (${doas_package_manager:-unknown manager}, implementation: $doas_package_implementation)"
      echo "Package: $doas_package_label"
      [ -n "$doas_package_homepage" ] && echo "Homepage: $doas_package_homepage"

      if { [ "$doas_package_implementation" = "opendoas" ] || [ "$doas_package_implementation" = "unknown" ]; } && \
         [ -n "$doas_upstream_version" ] && doas_version_ge "$doas_upstream_version" "6.6" && doas_version_lt "$doas_upstream_version" "6.8.1"; then
        echo "Potentially vulnerable to CVE-2019-25016: OpenDoas 6.6 through 6.8 may inherit an attacker-controlled PATH for unrestricted rules (verify distro backports)" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
      fi

      if [ "$doas_package_implementation" = "slicer69" ] && [ -n "$doas_upstream_version" ] && doas_version_lt "$doas_upstream_version" "6.2"; then
        echo "Potentially vulnerable to CVE-2019-15900 and CVE-2019-15901: slicer69/doas before 6.2 can mishandle identities/groups on non-OpenBSD platforms" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
      elif [ "$doas_package_implementation" = "unknown" ] && [ -n "$doas_upstream_version" ] && doas_version_lt "$doas_upstream_version" "6.2"; then
        echo "Old doas version detected; if this is the slicer69 portable implementation, review CVE-2019-15900 and CVE-2019-15901" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
      fi

      if [ "$(uname -s 2>/dev/null)" = "Linux" ] && \
         { [ "$doas_package_implementation" = "opendoas" ] || [ "$doas_package_implementation" = "unknown" ]; } && \
         [ -n "$doas_upstream_version" ] && doas_version_le "$doas_upstream_version" "6.8.2"; then
        if [ -r /proc/sys/dev/tty/legacy_tiocsti ]; then
          doas_tiocsti="$(cat /proc/sys/dev/tty/legacy_tiocsti 2>/dev/null)"
          if [ "$doas_tiocsti" = "1" ]; then
            echo "Potentially vulnerable to CVE-2023-28339: OpenDoas <=6.8.2 shares the terminal and legacy TIOCSTI is enabled" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
          else
            echo "CVE-2023-28339 TIOCSTI path appears mitigated (dev.tty.legacy_tiocsti=$doas_tiocsti)" | sed -${E} "s,.*,${SED_GREEN},g"
          fi
        else
          echo "OpenDoas <=6.8.2 detected; review CVE-2023-28339 because the kernel TIOCSTI mitigation state could not be read" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
        fi
      fi
    else
      echo "Could not determine the installed doas package/version; check vendor advisories manually" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
    fi
  else
    echo_not_found "doas"
  fi

  echo ""
  print_3title "Doas configuration rules" "T1548.003"
  doas_conf_found="no"
  doas_seen_configs="|"
  while IFS= read -r conf_file; do
    [ -n "$conf_file" ] || continue
    case "$doas_seen_configs" in *"|$conf_file|"*) continue ;; esac
    doas_seen_configs="$doas_seen_configs$conf_file|"
    [ -e "$conf_file" ] || continue
    doas_conf_found="yes"

    # Follow the final symlink for ownership/mode checks, but still report the indirection below.
    doas_conf_owner="$(ls -ldLn "$conf_file" 2>/dev/null | awk '{print $3}')"
    doas_conf_mode="$(ls -ldL "$conf_file" 2>/dev/null | awk '{print $1}')"
    doas_conf_dir="$(dirname "$conf_file")"
    doas_conf_dir_mode="$(ls -ld "$doas_conf_dir" 2>/dev/null | awk '{print $1}')"
    echo "Found: $conf_file ($doas_conf_mode owner UID ${doas_conf_owner:-unknown})" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"

    doas_conf_trusted="yes"
    if [ -L "$conf_file" ]; then
      echo "WARNING: $conf_file is a symbolic link; verify its target and ownership" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
    fi
    if [ "$doas_conf_owner" != "0" ]; then
      echo "CRITICAL: $conf_file is not owned by root" | sed -${E} "s,.*,${SED_RED},g"
      doas_conf_trusted="no"
    fi
    if { [ "$doas_current_uid" != "0" ] && [ -w "$conf_file" ]; } || printf "%s" "$doas_conf_mode" | cut -c6,9 | grep -q w; then
      echo "CRITICAL: $conf_file is writable by the current user, group, or other users" | sed -${E} "s,.*,${SED_RED},g"
      doas_conf_trusted="no"
    fi
    if { [ "$doas_current_uid" != "0" ] && [ -w "$doas_conf_dir" ]; } || printf "%s" "$doas_conf_dir_mode" | cut -c6,9 | grep -q w; then
      echo "CRITICAL: configuration directory $doas_conf_dir is writable; doas.conf may be replaceable" | sed -${E} "s,.*,${SED_RED},g"
      doas_conf_trusted="no"
    fi

    if [ -r "$conf_file" ]; then
      doas_active_rules="$(doas_read_rules "$conf_file")"
      if [ -z "$doas_active_rules" ]; then
        echo "No active permit/deny rules found in $conf_file"
      else
        while IFS="	" read -r doas_rule_number doas_rule_line; do
          [ -n "$doas_rule_line" ] || continue
          doas_rule_applies="no"
          doas_rule_root="no"
          doas_rule_nopass="no"
          doas_rule_unrestricted="no"
          doas_rule_dangerous="no"
          doas_rule_env="no"
          doas_rule_cmd_value="$(doas_rule_command "$doas_rule_line")"
          doas_rule_applies_to_current_user "$doas_rule_line" && doas_rule_applies="yes"
          doas_rule_targets_root "$doas_rule_line" && doas_rule_root="yes"
          doas_rule_has_option "$doas_rule_line" nopass && doas_rule_nopass="yes"
          [ -z "$doas_rule_cmd_value" ] && doas_rule_unrestricted="yes"
          [ -n "$doas_rule_cmd_value" ] && doas_command_is_dangerous "$doas_rule_cmd_value" && doas_rule_dangerous="yes"
          doas_rule_has_dangerous_environment "$doas_rule_line" && doas_rule_env="yes"

          if printf "%s" "$doas_rule_line" | grep -q '^deny'; then
            echo "  $conf_file:$doas_rule_number $doas_rule_line"
            continue
          fi

          if [ "$doas_rule_applies" = "yes" ] && [ "$doas_rule_root" = "yes" ]; then
            echo "  $conf_file:$doas_rule_number $doas_rule_line" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
            if [ "$doas_rule_unrestricted" = "yes" ] && [ "$doas_rule_nopass" = "yes" ]; then
              echo "POTENTIAL: a matching permit rule allows arbitrary root commands without a password; a later rule may override it" | sed -${E} "s,.*,${SED_RED},g"
            elif [ "$doas_rule_unrestricted" = "yes" ]; then
              echo "POTENTIAL: a matching permit rule allows arbitrary root commands after authentication; a later rule may override it" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
            elif [ "$doas_rule_dangerous" = "yes" ] && [ "$doas_rule_nopass" = "yes" ]; then
              echo "POTENTIAL: a matching permit rule allows GTFOBins-capable command $doas_rule_cmd_value as root without a password; a later rule may override it" | sed -${E} "s,.*,${SED_RED},g"
              if [ "${doas_rule_cmd_value##*/}" = "dstat" ]; then
                echo "This is the HTB Soccer privilege-escalation pattern: a user-controlled dstat plugin can execute as root" | sed -${E} "s,.*,${SED_RED},g"
              fi
            elif [ "$doas_rule_dangerous" = "yes" ]; then
              echo "POTENTIAL: a matching permit rule allows GTFOBins-capable command $doas_rule_cmd_value as root after authentication; a later rule may override it" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
            elif [ "$doas_rule_nopass" = "yes" ]; then
              echo "POTENTIAL: a matching permit rule allows $doas_rule_cmd_value as root without a password; inspect command-specific escapes and later rules" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
            fi
            if [ "$doas_rule_env" = "yes" ]; then
              echo "Dangerous environment preservation is enabled for an applicable root rule (keepenv or sensitive setenv variable)" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
            fi
          elif [ "$doas_rule_root" = "yes" ] && { [ "$doas_rule_nopass" = "yes" ] || [ "$doas_rule_unrestricted" = "yes" ] || [ "$doas_rule_dangerous" = "yes" ]; }; then
            echo "  $conf_file:$doas_rule_number $doas_rule_line" | sed -${E} "s,.*,${SED_LIGHT_CYAN},g"
          else
            echo "  $conf_file:$doas_rule_number $doas_rule_line"
          fi
        done <<EOF
$doas_active_rules
EOF
      fi
    else
      echo "Cannot read $conf_file directly; attempting safe effective-rule checks with doas -C"
      doas_active_rules=""
    fi

    if [ -n "$doas_bin" ] && [ "$doas_bin_trusted" = "yes" ] && [ "$doas_conf_trusted" = "yes" ]; then
      if doas_config_syntax_valid "$doas_bin" "$conf_file"; then
        doas_test_commands="/bin/sh
/bin/bash
/usr/bin/env
/usr/bin/dstat"
        while IFS="	" read -r doas_rule_number doas_rule_line; do
          doas_rule_cmd_value="$(doas_rule_command "$doas_rule_line")"
          [ -n "$doas_rule_cmd_value" ] && doas_test_commands="$doas_test_commands
$doas_rule_cmd_value"
        done <<EOF
$doas_active_rules
EOF
        doas_seen_test_commands="|"
        while IFS= read -r doas_test_cmd; do
          [ -n "$doas_test_cmd" ] || continue
          case "$doas_seen_test_commands" in *"|$doas_test_cmd|"*) continue ;; esac
          doas_seen_test_commands="$doas_seen_test_commands$doas_test_cmd|"
          doas_check_output="$(doas_check_command "$doas_bin" "$conf_file" "$doas_test_cmd")"
          case "$doas_check_output" in
            "permit nopass"*)
              echo "EFFECTIVE RULE: current user may run $doas_test_cmd as root without a password ($doas_check_output)" | sed -${E} "s,.*,${SED_RED},g"
              ;;
            permit*)
              echo "Effective rule: current user may run $doas_test_cmd as root after authentication ($doas_check_output)" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
              ;;
          esac
        done <<EOF
$doas_test_commands
EOF
      else
        echo "doas rejected or could not validate $conf_file with -C; inspect its syntax and security properties" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
      fi
    fi
  done <<EOF
$doas_conf_candidates
EOF

  if [ "$doas_conf_found" = "no" ]; then
    echo_not_found "doas.conf"
  fi
else
  echo_not_found "doas"
fi
echo ""
