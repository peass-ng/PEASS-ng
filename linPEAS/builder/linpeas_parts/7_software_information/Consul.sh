# Title: Software Information - Consul agent script checks
# ID: SO_Consul
# Author: Carlos Polop
# Last Update: 2026-10-09
# Description: Passive review of script checks and loaded configuration directories for root-run Consul agents
# License: GNU GPL
# Version: 1.1
# Mitre: T1548.003
# Functions Used: print_2title, print_info
# Global Variables: $SEARCH_IN_FOLDER, $IAMROOT
# Initial Functions:
# Generated Global Variables: $consul_root_agents, $consul_agent_args, $consul_cli_enabled, $consul_config_paths, $consul_config_path, $consul_config_file, $consul_file_count, $consul_file_result, $consul_enabled_file, $consul_disabled_file, $consul_writable_dirs, $consul_writable_dir, $consul_partial
# Fat linpeas: 0
# Small linpeas: 0

# Only inspect the current process list and readable, bounded configuration.
# Never call the agent API or register a health check during enumeration.
if [ ! "$SEARCH_IN_FOLDER" ] && command -v ps >/dev/null 2>&1; then
  consul_root_agents=$(LC_ALL=C ps -eo user=,args= 2>/dev/null | awk '
    NR > 4096 { exit }
    length($0) > 2048 { next }
    $0 ~ /^[[:space:]]*root[[:space:]]+([^[:space:]]*\/)?consul[[:space:]]+agent([[:space:]]|$)/ {
      sub(/^[[:space:]]*root[[:space:]]+/, "")
      if (seen++ < 4) print
    }
  ')
  if [ "$consul_root_agents" ]; then
    printf '%s\n' "$consul_root_agents" | while IFS= read -r consul_agent_args; do
      consul_cli_enabled=
      case " $consul_agent_args " in
        *' -enable-script-checks '*|*' -enable-script-checks=true '*) consul_cli_enabled=1 ;;
      esac
      # Inspect only paths named by this running agent. An unused default
      # directory could contain stale configuration and create a false alarm.
      consul_config_paths=$(printf '%s\n' "$consul_agent_args" | awk '
        {
          count = split($0, args, /[[:space:]]+/)
          for (i = 1; i <= count && found < 8; i++) {
            if (args[i] == "-config-dir" || args[i] == "-config-file") {
              if (i < count) { print args[++i]; found++ }
            } else if (args[i] ~ /^-config-(dir|file)=/) {
              sub(/^-config-(dir|file)=/, "", args[i])
              print args[i]
              found++
            }
          }
        }
      ')
      consul_file_count=0
      consul_enabled_file=
      consul_disabled_file=
      consul_writable_dirs=
      consul_partial=
      while IFS= read -r consul_config_path; do
        case "$consul_config_path" in
          /*) ;;
          *) continue ;;
        esac
        case "$consul_config_path" in
          *'//'*|*'/./'*|*'/../'*|*'/.'|*'/..'|*'"'*|*"'"*) continue ;;
        esac
        if [ -d "$consul_config_path" ] && [ ! -L "$consul_config_path" ]; then
          if [ ! "$IAMROOT" ] && [ -w "$consul_config_path" ] && [ -x "$consul_config_path" ]; then
            if [ "$consul_writable_dirs" ]; then
              consul_writable_dirs="$consul_writable_dirs
$consul_config_path"
            else
              consul_writable_dirs=$consul_config_path
            fi
          fi
          set -- "$consul_config_path"/*.json "$consul_config_path"/*.hcl
        else
          set -- "$consul_config_path"
        fi
        for consul_config_file do
          case "$consul_config_file" in
            *.json|*.hcl) ;;
            *) continue ;;
          esac
          [ -f "$consul_config_file" ] && [ -r "$consul_config_file" ] &&
            [ ! -L "$consul_config_file" ] || continue
          consul_file_count=$((consul_file_count + 1))
          if [ "$consul_file_count" -gt 24 ]; then
            consul_partial=1
            break
          fi
          if [ ! "$(find "$consul_config_file" -prune -type f -size -65537c -print 2>/dev/null)" ]; then
            consul_partial=1
            continue
          fi
          consul_file_result=$(LC_ALL=C awk '
            NR > 400 { partial = 1; exit }
            length($0) > 2048 { partial = 1; next }
            {
              line = $0
              sub(/^[[:space:]]*/, "", line)
              if (line ~ /^(#|\/\/)/) next
              if (line ~ /(^|[{,])[[:space:]]*"?enable_script_checks"?[[:space:]]*[:=][[:space:]]*true([,[:space:]}]|$)/) enabled = 1
              if (line ~ /(^|[{,])[[:space:]]*"?enable_script_checks"?[[:space:]]*[:=][[:space:]]*false([,[:space:]}]|$)/) disabled = 1
            }
            END {
              if (enabled) print "enabled"
              if (disabled) print "disabled"
              if (partial) print "partial"
            }
          ' "$consul_config_file" 2>/dev/null)
          case "$consul_file_result" in
            *enabled*) consul_enabled_file=$consul_config_file ;;
          esac
          case "$consul_file_result" in
            *disabled*) consul_disabled_file=$consul_config_file ;;
          esac
          case "$consul_file_result" in
            *partial*) consul_partial=1 ;;
          esac
        done
        [ "$consul_file_count" -gt 24 ] && break
      done <<_PEAS_CONSUL_CONFIG_PATHS_
$consul_config_paths
_PEAS_CONSUL_CONFIG_PATHS_
      if [ "$consul_enabled_file" ] || [ "$consul_cli_enabled" ] || [ "$consul_writable_dirs" ]; then
        if [ "$consul_enabled_file" ] || [ "$consul_cli_enabled" ]; then
          print_2title "Root-run Consul script-check review candidate" "T1548.003"
        else
          print_2title "Root-run Consul config-directory write review candidate" "T1548.003"
        fi
        print_info "https://book.hacktricks.wiki/en/linux-hardening/processes-crontab-systemd-dbus/process-enumeration-and-service-paths.html#consul-agent-script-checks"
        if [ "$consul_enabled_file" ]; then
          printf 'Root-run Consul agent has enable_script_checks=true in %s\n' "$consul_enabled_file"
        fi
        if [ "$consul_cli_enabled" ]; then
          printf '%s\n' 'Root-run Consul agent enables script checks on its command line'
        fi
        if [ "$consul_disabled_file" ]; then
          printf 'Conflicting enable_script_checks=false in %s; effective setting unknown\n' "$consul_disabled_file"
        fi
        if [ "$consul_partial" ]; then
          printf '%s\n' 'Consul config review incomplete (file size, line length, line count, or file count limit)'
        fi
        if [ "$consul_writable_dirs" ]; then
          printf '%s\n' "$consul_writable_dirs" | while IFS= read -r consul_writable_dir; do
            printf 'Loaded Consul config-dir writable/searchable by current user: %s\n' "$consul_writable_dir"
          done
          printf '%s\n' 'Config-file execution requires a loaded directory, effective script-check policy, and an authorized reload or restart; verify parent path/symlink and ACL policy. No configuration was written or reloaded.'
        fi
        if [ "$consul_enabled_file" ] || [ "$consul_cli_enabled" ]; then
          printf '%s\n' 'API script execution requires an authorized service registration and the agent running as privileged user; static config does not prove either API access or effective ACL policy.'
        fi
      fi
    done
  fi
fi
