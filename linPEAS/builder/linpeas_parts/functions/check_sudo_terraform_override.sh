# Title: Function - check_sudo_terraform_override
# ID: check_sudo_terraform_override
# Author: PEASS-ng contributors
# Last Update: 09-10-2026
# Description: Passively correlate a root Terraform sudo rule with a caller-writable provider override.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $HOME, $ROOT_FOLDER
# Initial Functions:
# Generated Global Variables: $tf_policy, $tf_rules, $tf_config, $tf_config_size, $tf_rule_dir, $tf_source, $tf_override_dir, $tf_provider_name, $tf_binary, $tf_binary_access, $tf_project_file, $tf_project_size, $tf_project_count, $tf_source_seen, $tf_walk, $tf_rest, $tf_part
# Fat linpeas: 0
# Small linpeas: 1


tf_plain_path() {
  case "$1" in /*) ;; *) return 1 ;; esac
  case "$1" in *'//'*|*'/./'*|*'/../'*|*'/.'|*'/..') return 1 ;; esac
  tf_walk=
  tf_rest=${1#/}
  while [ "$tf_rest" ]; do
    tf_part=${tf_rest%%/*}
    tf_walk="$tf_walk/$tf_part"
    [ ! -L "$tf_walk" ] || return 1
    case "$tf_rest" in
      */*) tf_rest=${tf_rest#*/} ;;
      *) break ;;
    esac
  done
}

check_sudo_terraform_override() { (
  [ -z "$ROOT_FOLDER" ] || [ "$ROOT_FOLDER" = / ] || return 0
  [ -n "$HOME" ] && [ -d "$HOME" ] || return 0
  # A selected alternate config, or a home-changing Defaults entry, makes the
  # caller's HOME config an unreliable candidate for this narrow check.
  env | grep -q '^TF_CLI_CONFIG_FILE=.' && return 0
  tf_policy=$(printf '%s\n%s\n%s\n' "$1" "$2" "$3" | sed -n '1,400p;401q')
  printf '%s\n' "$tf_policy" | grep -Eq '(^|[[:space:],])!env_reset([[:space:],]|$)' || return 0
  printf '%s\n' "$tf_policy" | grep -Eq '(^|[[:space:],])(always_set_home|set_home)([[:space:],]|$)' && return 0

  # Parse only an exact root-capable terraform -chdir=... apply command.
  # The output is a review clue; sudo's effective policy and HOME remain to be verified.
  tf_rules=$(printf '%s\n' "$tf_policy" | awk '
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      line = $0
      runas = line
      sub(/^[^(]*\(/, "", runas); sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      if (users ~ /!root/ || users !~ /(^|[[:space:],])(root|ALL)([[:space:],]|$)/) next
      sub(/^.*\)[[:space:]]*/, "", line)
      n = split(line, commands, /,[[:space:]]*/)
      for (i = 1; i <= n; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV):[[:space:]]*/)
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        sub(/[[:space:]]*$/, "", command)
        if (command ~ /^\/[A-Za-z0-9_.\/-]*\/terraform -chdir=\/[A-Za-z0-9_.\/-]+ apply$/ &&
            command !~ /\/\.\.?\// && ++count <= 4) {
          sub(/^.* -chdir=/, "", command); sub(/ apply$/, "", command)
          if (!seen[command]++) print command
        }
      }
    }
  ')
  [ -n "$tf_rules" ] || return 0

  tf_config="$HOME/.terraformrc"
  tf_plain_path "$tf_config" || return 0
  [ -f "$tf_config" ] && [ -r "$tf_config" ] && [ -w "$tf_config" ] && [ ! -L "$tf_config" ] || return 0
  tf_config_size=$(stat -c %s "$tf_config" 2>/dev/null) ||
    tf_config_size=$(stat -f %z "$tf_config" 2>/dev/null) || return 0
  case "$tf_config_size" in ''|*[!0-9]*) return 0 ;; esac
  [ "$tf_config_size" -le 32768 ] || return 0

  # Only report literal source-to-directory mappings inside dev_overrides.
  # Never print unrelated CLI configuration, which may contain credentials.
  sed -n '1,256p;257q' "$tf_config" 2>/dev/null | awk '
    /^[[:space:]]*provider_installation[[:space:]]*\{/ { installation = 1; next }
    installation && /^[[:space:]]*dev_overrides[[:space:]]*\{/ { overrides = 1; next }
    overrides && /^[[:space:]]*\}/ { overrides = 0; next }
    installation && /^[[:space:]]*\}/ { installation = 0; next }
    overrides && /^[[:space:]]*"[A-Za-z0-9._\/-]+"[[:space:]]*=[[:space:]]*"\/[A-Za-z0-9._\/-]+"[[:space:]]*(#.*)?$/ {
      split($0, fields, "\"")
      if (++count <= 8) print fields[2] "|" fields[4]
    }
  ' | while IFS='|' read -r tf_source tf_override_dir; do
    tf_plain_path "$tf_override_dir" || continue
    [ -d "$tf_override_dir" ] && [ -w "$tf_override_dir" ] &&
      [ -x "$tf_override_dir" ] && [ ! -L "$tf_override_dir" ] || continue
    tf_provider_name=${tf_source##*/}
    tf_binary="$tf_override_dir/terraform-provider-$tf_provider_name"
    tf_plain_path "$tf_binary" || continue
    if [ -e "$tf_binary" ] || [ -L "$tf_binary" ]; then
      [ -f "$tf_binary" ] && [ -w "$tf_binary" ] &&
        [ -x "$tf_binary" ] && [ ! -L "$tf_binary" ] || continue
      tf_binary_access='caller-writable executable exists'
    else
      tf_binary_access='caller can create executable in override directory'
    fi
    printf '%s\n' "$tf_rules" | while IFS= read -r tf_rule_dir; do
      tf_plain_path "$tf_rule_dir" || continue
      [ -d "$tf_rule_dir" ] && [ ! -L "$tf_rule_dir" ] || continue
      tf_source_seen=
      tf_project_count=0
      for tf_project_file in "$tf_rule_dir"/*.tf; do
        tf_plain_path "$tf_project_file" || continue
        [ -f "$tf_project_file" ] && [ ! -L "$tf_project_file" ] || continue
        tf_project_count=$((tf_project_count + 1))
        [ "$tf_project_count" -le 8 ] || break
        tf_project_size=$(stat -c %s "$tf_project_file" 2>/dev/null) ||
          tf_project_size=$(stat -f %z "$tf_project_file" 2>/dev/null) || continue
        case "$tf_project_size" in ''|*[!0-9]*) continue ;; esac
        [ "$tf_project_size" -le 32768 ] || continue
        if sed -n '1,256p;257q' "$tf_project_file" 2>/dev/null | awk -v wanted="$tf_source" '
          /^[[:space:]]*source[[:space:]]*=/ {
            split($0, fields, "\"")
            if (fields[2] == wanted) found = 1
          }
          END { exit !found }
        '; then
          tf_source_seen=1
          break
        fi
      done
      [ "$tf_source_seen" ] || continue
      printf 'Terraform sudo/provider override review candidate: root rule uses -chdir=%s apply; current HOME config %s is caller-writable\n' "$tf_rule_dir" "$tf_config"
      printf 'Provider source %s matches a bounded .tf file; override directory %s; %s: %s\n' "$tf_source" "$tf_override_dir" "$tf_binary_access" "$tf_binary"
      ls -ld "$tf_config" "$tf_override_dir" "$tf_binary" 2>/dev/null
      printf 'Verify effective sudo HOME, policy, provider loading, and execution context; this is candidate evidence, not proven root privilege.\n'
    done
  done
); }
