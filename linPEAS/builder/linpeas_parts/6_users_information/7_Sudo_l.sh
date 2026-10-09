# Title: Users Information - Sudo -l
# ID: UG_Sudo_l
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: Checking 'sudo -l', sudoers files, privileged config, process tracing, container exec and compose wrappers, packet-filter export, PDF attachments, PostScript conversion, Bash pattern comparisons, relative working-directory helpers, PyInstaller build wrappers, preset and PHP CLI loaders, and privileged Python imports, paths, caches, archive extraction, model loading, and Git transport
# License: GNU GPL
# Version: 1.6
# Mitre: T1548.003
# Functions Used: check_sudo_terraform_override, echo_not_found, print_2title, print_info
# Global Variables:$IAMROOT, $PASSWORD, $TIMEOUT, $ROOT_FOLDER, $TMPDIR, $sudoB, $sudoG, $sudoVB1, $sudoVB2
# Initial Functions:
# Generated Global Variables: $sudo_l_output, $sudo_l_password_output, $sudo_l_cached_output, $sudo_adduser_main, $sudo_adduser_dir, $sudo_adduser_groups, $sudo_adduser_candidates, $sudo_adduser_group, $sudo_adduser_group_status, $sudo_adduser_visible, $sudo_adduser_unknown, $sudo_adduser_files, $sudo_adduser_file, $sudo_adduser_policy, $secure_path_candidate, $secure_path_index, $secure_path_entry, $secure_path_remaining, $secure_path_part, $secure_path_walk, $secure_path_symlink, $sudo_needrestart_dir, $sudo_needrestart_config, $sudo_needrestart_timeout, $sudo_needrestart_query, $sudo_npbackup_rules, $sudo_npbackup_dir, $sudo_npbackup_timeout, $sudo_npbackup_count, $sudo_npbackup_started, $sudo_npbackup_now, $sudo_npbackup_command, $sudo_npbackup_config, $sudo_npbackup_query, $sudo_backup_wrapper_rules, $sudo_backup_wrapper_script, $sudo_backup_wrapper_remaining, $sudo_backup_wrapper_walk, $sudo_backup_wrapper_part, $sudo_backup_wrapper_symlink, $sudo_backup_wrapper_result, $sudo_python_scripts, $python_sudo_script, $python_loader_lines, $python_loader_root, $python_loader_roots, $python_loader_path_lines, $python_loader_literal, $python_loader_parent, $python_candidate_dir, $python_seen_dirs, $python_pth_file, $python_pth_imports, $python_writable_pth, $python_script_dir, $sudo_python_import_rules, $sudo_python_import_script, $sudo_python_import_runas, $sudo_python_import_size, $sudo_python_import_shebang, $sudo_python_import_dir, $sudo_python_import_line, $sudo_python_import_kind, $sudo_python_import_name, $sudo_python_import_member, $sudo_python_import_statement, $sudo_python_import_candidate, $sudo_python_import_parent, $sudo_python_import_access, $sudo_python_import_sticky, $sudo_python_import_walk, $sudo_python_import_remaining, $sudo_python_import_part, $sudo_python_import_probe_count, $sudo_python_cache_scripts, $sudo_python_cache_script, $sudo_python_cache_size, $sudo_python_cache_shebang, $sudo_python_cache_interpreter, $sudo_python_cache_version, $sudo_python_cache_tag, $sudo_python_cache_dir, $sudo_python_cache_line, $sudo_python_cache_module, $sudo_python_cache_source, $sudo_python_cache_pyc, $sudo_python_cache_sticky, $sudo_python_cache_owner, $sudo_python_cache_access, $sudo_python_tar_commands, $sudo_python_tar_command, $sudo_python_binary, $sudo_python_version, $sudo_python_tar_dir, $sudo_python_tar_size, $script, $kept_names, $script_size, $matched_name, $sudo_nmap_walk, $sudo_rsync_rules, $sudo_nmap_result, $sudo_nmap_part, $sudo_rsync_dest, $sudo_nmap_remaining, $sudo_nmap_rules, $sudo_nmap_symlink, $sudo_nmap_path, $sudo_rsync_dir, $sudo_nmap_size, $sudo_rsync_source, $sudo_rsync_safe, $sudo_rsync_check, $sudo_rsync_walk, $sudo_rsync_remaining, $sudo_rsync_part, $sudo_rsync_owner, $sudo_rsync_current_uid, $sudo_gitpython_clone_rules, $sudo_gitpython_parser, $sudo_gitpython_binary, $sudo_gitpython_script, $sudo_gitpython_size, $sudo_gitpython_result, $sudo_model_loader_rules, $sudo_model_loader_script, $sudo_model_loader_dir, $sudo_model_loader_size, $sudo_model_loader_helper, $sudo_model_loader_line, $sudo_qpdf_path, $sudo_relative_cwd_dir, $sudo_relative_cwd_candidate, $sudo_relative_cwd_rules, $sudo_relative_cwd_script, $sudo_relative_cwd_size, $sudo_relative_cwd_shebang, $sudo_relative_cwd_helper, $sudo_relative_cwd_yaml, $sudo_bash_pattern_rules, $sudo_bash_pattern_script, $sudo_bash_pattern_size, $sudo_bash_pattern_shebang, $sudo_bash_pattern_line, $sudo_ps2pdf_rules, $sudo_ps2pdf_script, $sudo_ps2pdf_size, $sudo_ps2pdf_shebang, $sudo_ps2pdf_line, $sudo_compose_rules, $sudo_compose_script, $sudo_compose_size, $sudo_compose_shebang, $sudo_compose_match, $sudo_pyinstaller_rules, $sudo_pyinstaller_script, $sudo_pyinstaller_size, $sudo_pyinstaller_shebang, $sudo_pyinstaller_match, $sudo_setenv_path_rules, $sudo_setenv_path_script, $sudo_setenv_path_size, $sudo_setenv_path_shebang, $sudo_setenv_path_match
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Checking 'sudo -l', sudoers files, and privileged Python paths" "T1548.003"
print_info "https://book.hacktricks.wiki/en/linux-hardening/linux-basics/linux-privilege-escalation/index.html#sudo-and-suid"

sudo_l_colorize() {
  sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s@$sudoB@${SED_RED}@g"
}

sudo_l_colorize_output() {
  printf "%s\n" "$1" | sudo_l_colorize | sed "s,\!root,${SED_RED},"
}

sudo_l_colorize_file() {
  grep -Iv "^$" "$1" | grep -v "#" | sudo_l_colorize | sed "s,pwfeedback,${SED_RED},g"
}

# Correlate sudo-preserved variables with command words in fixed shell scripts.
# This reads only short, explicitly allowed scripts; it never invokes a rule.
sudo_env_keep_script_candidates() {
  [ -n "$1" ] || return 0
  case "$1" in *env_keep*) ;; *) return 0 ;; esac
  printf '%s\n' "$1" | awk '
    NR > 3000 || length($0) > 2048 { ambiguous = 1; exit }
    /![[:space:]]*\/[A-Za-z0-9_\/.+-]*\/(sh|bash|dash|ksh|zsh)([[:space:]]|$)/ {
      ambiguous = 1
      exit
    }
    match($0, /env_keep[[:space:]]*\+?=[[:space:]]*/) {
      line = substr($0, RSTART + RLENGTH)
      if (substr(line, 1, 1) == "\"") {
        line = substr(line, 2)
        sub(/".*/, "", line)
      } else {
        sub(/,.*/, "", line)
      }
      n = split(line, words, /[[:space:]]+/)
      for (i = 1; i <= n; i++)
        if (words[i] ~ /^[A-Za-z_][A-Za-z0-9_]*$/ && !kept[words[i]] && kept_count < 16) {
          kept[words[i]] = 1
          kept_order[++kept_count] = words[i]
        }
    }
    /^[[:space:]]*\((ALL|root)([[:space:]:]|\))/ {
      line = $0
      sub(/^[[:space:]]*[^)]*\)[[:space:]]*/, "", line)
      while (sub(/^[A-Z_]+:[[:space:]]*/, "", line)) {}
      n = split(line, words, /[[:space:]]+/)
      if (n < 2 || words[1] !~ /^\/[A-Za-z0-9_\/.+-]*\/(sh|bash|dash|ksh|zsh)$/)
        next
      script = words[2]
      if (script ~ /^\/[A-Za-z0-9_\/.+-]+$/ && !seen[script] && script_count < 12) {
        seen[script] = 1
        scripts[++script_count] = script
      }
    }
    END {
      if (ambiguous || !kept_count) exit
      names = kept_order[1]
      for (i = 2; i <= kept_count; i++) names = names " " kept_order[i]
      for (i = 1; i <= script_count; i++) print scripts[i] "\t" names
    }
  ' | while IFS="$(printf '\t')" read -r script kept_names; do
    [ -f "$script" ] && [ -r "$script" ] || continue
    script_size=$(stat -c %s "$script" 2>/dev/null)
    case "$script_size" in
      ''|*[!0-9]*) script_size=$(stat -f %z "$script" 2>/dev/null) ;;
    esac
    case "$script_size" in
      ''|*[!0-9]*) continue ;;
    esac
    [ "$script_size" -le 65536 ] || continue
    matched_name=$(awk -v names="$kept_names" '
      BEGIN { count = split(names, preserved, / /) }
      NR > 200 { exit }
      {
        for (i = 1; i <= count; i++) {
          name = preserved[i]
          if ($0 ~ "^[[:space:]]*(if[[:space:]]+|while[[:space:]]+|until[[:space:]]+|![[:space:]]+)?\\$" name "([[:space:];|&()]|$)" ||
              $0 ~ "^[[:space:]]*(if[[:space:]]+|while[[:space:]]+|until[[:space:]]+|![[:space:]]+)?\\$\\{" name "\\}([[:space:];|&()]|$)") {
            print name
            exit
          }
        }
      }
    ' "$script" 2>/dev/null)
    if [ -n "$matched_name" ]; then
      printf 'Potential sudo script command execution: %s (preserved variable %s used as a command; review reachability)\n' "$script" "$matched_name"
    fi
  done
}

if [ "$(command -v sudo 2>/dev/null || echo -n '')" ]; then
  if [ "$TIMEOUT" ]; then
    sudo_l_output=$(printf '\n' | "$TIMEOUT" 15 sudo -S -l 2>/dev/null)
  else
    sudo_l_output=$(sudo -n -l 2>/dev/null)
  fi
  sudo_l_colorize_output "$sudo_l_output"

  if [ "$PASSWORD" ]; then
    if [ "$TIMEOUT" ]; then
      sudo_l_password_output=$(printf "%s\n" "$PASSWORD" | "$TIMEOUT" 15 sudo -S -l 2>/dev/null)
    else
      sudo_l_password_output=$(printf "%s\n" "$PASSWORD" | sudo -S -l 2>/dev/null)
    fi
    printf "%s\n" "$sudo_l_password_output" | sudo_l_colorize
  fi

  sudo_l_cached_output=$(sudo -n -l 2>/dev/null)
  if [ "$sudo_l_cached_output" ]; then
    sudo_l_colorize_output "$sudo_l_cached_output"
  else
    echo "No cached sudo token (sudo -n -l)"
  fi
else
  echo_not_found "sudo"
fi

sudo_env_keep_script_candidates "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")"

# Review only an explicitly sudo-allowed Nmap executable. A small shell
# wrapper that filters --script but forwards argv may still leave NSE's data
# directory loader available. This is a passive review lead, not execution.
sudo_nmap_wrapper_review() {
  [ -n "$1" ] || return 0
  case "$1" in *nmap*) ;; *) return 0 ;; esac
  sudo_nmap_rules=$(printf '%s\n' "$1" | LC_ALL=C awk '
    function review(specs, count, commands, i, command, path, args) {
      count = split(specs, commands, ",")
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/)
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        if (command ~ /^!/) {
          if (command ~ /^![[:space:]]*(ALL|\/[A-Za-z0-9_\/.+-]*\/nmap)([[:space:]]|$)/) denied = 1
          continue
        }
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/[A-Za-z0-9_\/.+-]*\/nmap$/) continue
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        if (args == "" || args == "*") found[path] = 1
      }
    }
    NR > 3000 || length($0) > 2048 { partial = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      if (active) review(specs)
      active = 0
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      if (users !~ /(^|[[:space:],])(root|ALL|#0)([[:space:],]|$)/ ||
          users ~ /(^|[[:space:],])!(root|ALL|#0)([[:space:],]|$)/) next
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      specs = line
      active = 1
      next
    }
    active && /^[[:space:]]+[^[:space:]]/ { active = 0; partial = 1; next }
    active { review(specs); active = 0 }
    END {
      if (active) review(specs)
      if (partial) { print "#PARTIAL"; exit }
      if (denied) exit
      for (path in found) {
        if (++count > 8) { print "#PARTIAL"; exit }
        print path
      }
    }
  ')
  [ -n "$sudo_nmap_rules" ] || return 0
  printf '%s\n' "$sudo_nmap_rules" | while IFS= read -r sudo_nmap_path; do
    if [ "$sudo_nmap_path" = '#PARTIAL' ]; then
      echo 'Sudo Nmap wrapper review incomplete (policy limit or continuation); inspect the displayed rule manually.'
      continue
    fi
    case "$sudo_nmap_path" in
      /*/nmap) ;;
      *) continue ;;
    esac
    case "$sudo_nmap_path" in *'/../'*|*'/./'*|*'//'*) continue ;; esac
    sudo_nmap_remaining=${sudo_nmap_path#/}
    sudo_nmap_walk=
    sudo_nmap_symlink=
    while [ -n "$sudo_nmap_remaining" ]; do
      sudo_nmap_part=${sudo_nmap_remaining%%/*}
      sudo_nmap_walk="$sudo_nmap_walk/$sudo_nmap_part"
      if [ -L "$sudo_nmap_walk" ]; then sudo_nmap_symlink=1; break; fi
      case "$sudo_nmap_remaining" in
        */*) sudo_nmap_remaining=${sudo_nmap_remaining#*/} ;;
        *) sudo_nmap_remaining= ;;
      esac
    done
    [ -z "$sudo_nmap_symlink" ] || continue
    [ -f "$sudo_nmap_path" ] && [ -r "$sudo_nmap_path" ] || continue
    sudo_nmap_size=$(stat -c %s "$sudo_nmap_path" 2>/dev/null)
    case "$sudo_nmap_size" in ''|*[!0-9]*) sudo_nmap_size=$(stat -f %z "$sudo_nmap_path" 2>/dev/null) ;; esac
    case "$sudo_nmap_size" in ''|*[!0-9]*) continue ;; esac
    if [ "$sudo_nmap_size" -gt 65536 ]; then
      echo "Sudo Nmap wrapper review incomplete (64 KiB file limit): $sudo_nmap_path"
      continue
    fi
    sudo_nmap_result=$(LC_ALL=C awk '
      NR > 200 || length($0) > 2048 { partial = 1; exit }
      NR == 1 && /^#!.*\/(sh|bash|dash|ksh|zsh)([[:space:]]|$)/ { shell = 1 }
      /^[[:space:]]*#/ { next }
      /--script/ { blocked_script = 1 }
      /--datadir/ { blocked_datadir = 1 }
      /\$\*/ { checks_argv = 1 }
      /(^|[[:space:];])exit[[:space:]]+[1-9]/ { stops = 1 }
      /^[[:space:]]*exec[[:space:]]+\/[A-Za-z0-9_\/.+-]*\/nmap([.-][A-Za-z0-9_.+-]+)?[[:space:]]+"\$@"/ { forwards = 1 }
      END {
        if (partial) print "partial"
        else if (shell && blocked_script && !blocked_datadir && checks_argv && stops && forwards) print "candidate"
      }
    ' "$sudo_nmap_path" 2>/dev/null)
    case "$sudo_nmap_result" in
      candidate)
        echo "Sudo Nmap wrapper NSE data-loader review candidate: $sudo_nmap_path (--script filter seen, --datadir filter not seen, argv forwarded; verify effective policy, wrapper logic, Nmap version, and MAC policy)" ;;
      partial)
        echo "Sudo Nmap wrapper review incomplete (200-line/2048-column scan limit): $sudo_nmap_path" ;;
    esac
  done
}
sudo_nmap_wrapper_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# A sudoers argument glob can match spaces. For a root rsync archive copy,
# this may admit an extra --chown option while preserving source mode bits.
# Check only the exact source and destination named in a captured rule.
sudo_rsync_wildcard_review() {
  [ -n "$1" ] || return 0
  case "$1" in *rsync*) ;; *) return 0 ;; esac
  sudo_rsync_rules=$(printf '%s\n' "$1" | LC_ALL=C awk '
    function review(specs, count, commands, i, command, path, args, n, words, j, archive, blocked, source, dest) {
      count = split(specs, commands, ",")
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/)
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        if (command ~ /^!/) {
          if (command ~ /^![[:space:]]*(ALL|\/[A-Za-z0-9_\/.+-]*\/rsync)([[:space:]]|$)/) denied = 1
          continue
        }
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/[A-Za-z0-9_\/.+-]*\/rsync$/) continue
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        n = split(args, words, /[[:space:]]+/)
        archive = blocked = 0
        for (j = 1; j <= n; j++) {
          if (words[j] == "-a" || words[j] == "--archive" || words[j] ~ /^-[A-Za-z]*a[A-Za-z]*$/) archive = 1
          if (words[j] == "--" || words[j] == "--no-perms" ||
              words[j] == "--no-owner" || words[j] == "--no-group") blocked = 1
        }
        if (!archive || blocked || n < 3) continue
        source = words[n-1]
        dest = words[n]
        if (source !~ /^\/[A-Za-z0-9_\/.+-]+\/\*$/ ||
            dest !~ /^\/[A-Za-z0-9_\/.+-]+\/?$/) continue
        found[source "|" dest] = 1
      }
    }
    NR > 3000 || length($0) > 2048 { partial = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      if (active) review(specs)
      active = 0
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      if (users !~ /(^|[[:space:],])(root|ALL|#0)([[:space:],]|$)/ ||
          users ~ /(^|[[:space:],])!(root|ALL|#0)([[:space:],]|$)/) next
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      specs = line
      active = 1
      next
    }
    active && /^[[:space:]]+[^[:space:]]/ { active = 0; partial = 1; next }
    active { review(specs); active = 0 }
    END {
      if (active) review(specs)
      if (partial) { print "#PARTIAL"; exit }
      if (denied) exit
      for (pair in found) {
        if (++count > 8) { print "#PARTIAL"; exit }
        print pair
      }
    }
  ')
  [ -n "$sudo_rsync_rules" ] || return 0
  printf '%s\n' "$sudo_rsync_rules" | while IFS='|' read -r sudo_rsync_source sudo_rsync_dest; do
    if [ "$sudo_rsync_source" = '#PARTIAL' ]; then
      echo 'Sudo rsync wildcard review incomplete (policy limit or continuation); inspect the displayed rule manually.'
      continue
    fi
    sudo_rsync_dir=${sudo_rsync_source%/*}
    case "$sudo_rsync_dir:$sudo_rsync_dest" in *'/../'*|*'/./'*|*'//'*) continue ;; esac
    [ -d "$sudo_rsync_dir" ] && [ -w "$sudo_rsync_dir" ] && [ -x "$sudo_rsync_dir" ] || continue
    [ -d "$sudo_rsync_dest" ] || continue
    sudo_rsync_safe=1
    for sudo_rsync_check in "$sudo_rsync_dir" "$sudo_rsync_dest"; do
      sudo_rsync_walk=
      sudo_rsync_remaining=${sudo_rsync_check#/}
      while [ -n "$sudo_rsync_remaining" ]; do
        sudo_rsync_part=${sudo_rsync_remaining%%/*}
        sudo_rsync_walk="$sudo_rsync_walk/$sudo_rsync_part"
        if [ -L "$sudo_rsync_walk" ]; then sudo_rsync_safe=0; break; fi
        case "$sudo_rsync_remaining" in
          */*) sudo_rsync_remaining=${sudo_rsync_remaining#*/} ;;
          *) sudo_rsync_remaining= ;;
        esac
      done
      [ "$sudo_rsync_safe" -eq 1 ] || break
    done
    [ "$sudo_rsync_safe" -eq 1 ] || continue
    sudo_rsync_owner=$(stat -c %u "$sudo_rsync_dest" 2>/dev/null)
    case "$sudo_rsync_owner" in ''|*[!0-9]*) sudo_rsync_owner=$(stat -f %u "$sudo_rsync_dest" 2>/dev/null) ;; esac
    case "$sudo_rsync_owner" in ''|*[!0-9]*) continue ;; esac
    sudo_rsync_current_uid=$(id -u 2>/dev/null) || continue
    [ "$sudo_rsync_owner" != "$sudo_rsync_current_uid" ] || continue
    echo "Sudo rsync argument wildcard review candidate: $sudo_rsync_source -> $sudo_rsync_dest (writable source directory; sudoers * may admit extra options while archive mode preserves permissions; verify effective policy, destination mount, rsync version, and authentication)"
  done
}
sudo_rsync_wildcard_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# A fixed hg pull source does not fix the receiving repository: without -R,
# Mercurial uses the caller's current repository and may run its local hook.
# Mercurial trust policy can prevent this, so report only a conditional cue.
sudo_hg_pull_hook_review() {
  [ -n "$1" ] || return 0
  case "$1" in *hg*pull*) ;; *) return 0 ;; esac
  printf '%s\n' "$1" | LC_ALL=C awk -v current="$(id -un 2>/dev/null)" '
    function review(specs, runas, count, commands, i, command, path, args) {
      count = split(specs, commands, ",")
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/)
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        if (command ~ /^!/) {
          if (command ~ /^![[:space:]]*(ALL|\/[A-Za-z0-9_\/.+-]*\/hg)([[:space:]]|$)/) denied = 1
          continue
        }
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/[A-Za-z0-9_\/.+-]*\/hg$/) continue
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        if (args !~ /^pull[[:space:]]+\/[A-Za-z0-9_\/.+-]+\/?$/) continue
        if (runas == current) continue
        found[runas] = 1
      }
    }
    NR > 3000 || length($0) > 2048 { partial = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      if (active) review(specs, runas)
      active = 0
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      runas = parts[1]
      gsub(/[[:space:]]/, "", runas)
      if (runas !~ /^(root|ALL|#[0-9]+|[A-Za-z_][A-Za-z0-9_-]*)$/) next
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      specs = line
      active = 1
      next
    }
    active && /^[[:space:]]+[^[:space:]]/ { active = 0; partial = 1; next }
    active { review(specs, runas); active = 0 }
    END {
      if (active) review(specs, runas)
      if (partial) { print "Sudo Mercurial hook review incomplete (policy limit or continuation)."; exit }
      if (denied) exit
      for (runas in found)
        print "Sudo Mercurial receiving-repo hook review candidate: hg pull as " runas " may load a hook from the caller-selected current repository; verify Mercurial trusted users/groups, effective policy, working directory, and authentication."
    }
  '
}
sudo_hg_pull_hook_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# A bare Forge grant can select a compiler or output path under any RunAs
# identity. A bare root-capable pacman grant can select a local package or
# hook directory. Review only captured policy; never invoke either program.
sudo_build_package_tool_review() {
  [ -n "$1" ] || return 0
  case "$1" in *forge*|*pacman*) ;; *) return 0 ;; esac
  printf '%s\n' "$1" | LC_ALL=C awk '
    function review(specs, root_capable, count, commands, i, command, path, args) {
      count = split(specs, commands, ",")
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        sub(/[[:space:]]*$/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/)
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        if (command ~ /^!/) {
          sub(/^![[:space:]]*/, "", command)
          path = command
          sub(/[[:space:]].*$/, "", path)
          if (path ~ /^\/([[:alnum:]_.+-]+\/)*forge$/) forge_denied = 1
          if (path ~ /^\/([[:alnum:]_.+-]+\/)*pacman$/) pacman_denied = 1
          continue
        }
        path = command
        sub(/[[:space:]].*$/, "", path)
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        if (args != "" && args != "*") continue
        if (path ~ /^\/([[:alnum:]_.+-]+\/)*forge$/) forge_found = 1
        if (root_capable && path ~ /^\/([[:alnum:]_.+-]+\/)*pacman$/)
          pacman_found = 1
      }
    }
    NR > 3000 || length($0) > 2048 { ambiguous = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      if (active) review(specs, root_capable)
      active = 0
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      if (users !~ /(^|[[:space:],])(ALL|root|#[0-9]+|[A-Za-z_][A-Za-z0-9_-]*)([[:space:],]|$)/)
        next
      root_capable = users ~ /(^|[[:space:],])(ALL|root|#0)([[:space:],]|$)/ &&
                     users !~ /(^|[[:space:],])!(ALL|root|#0)([[:space:],]|$)/
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      specs = line
      active = 1
      next
    }
    active && /^[[:space:]]+[^[:space:]]/ { active = 0; ambiguous = 1; next }
    active { review(specs, root_capable); active = 0 }
    END {
      if (active) review(specs, root_capable)
      if (ambiguous) exit
      if (forge_found && !forge_denied)
        print "Sudo Forge unrestricted RunAs review candidate: caller-selected compiler and output paths may cross the allowed identity boundary; verify effective policy, binary identity, options, environment, and authentication."
      if (pacman_found && !pacman_denied)
        print "Sudo pacman unrestricted root review candidate: caller-selected local packages or hook directories may cause privileged writes or scripts; verify effective policy, options, and authentication."
    }
  '
}
sudo_build_package_tool_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# A slicer project can carry post-processing commands. Flag only an effective
# root-capable sudo grant with caller-selectable arguments; never load a model.
sudo_prusaslicer_project_review() {
  [ -n "$1" ] || return 0
  case "$1" in *prusaslicer*|*prusa-slicer*) ;; *) return 0 ;; esac
  printf '%s\n' "$1" | LC_ALL=C awk '
    function review(specs, root_capable, count, commands, i, command, path, args, noexec) {
      count = split(specs, commands, ",")
      noexec = 0
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        sub(/[[:space:]]*$/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/) {
          if (command ~ /^NOEXEC:/) noexec = 1
          if (command ~ /^EXEC:/) noexec = 0
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        }
        if (command ~ /^!/) {
          sub(/^![[:space:]]*/, "", command)
          path = command
          sub(/[[:space:]].*$/, "", path)
          if (path == "ALL" || path ~ /^\/([[:alnum:]_.+-]+\/)*(prusaslicer|prusa-slicer)$/)
            denied = 1
          continue
        }
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/([[:alnum:]_.+-]+\/)*(prusaslicer|prusa-slicer)$/) continue
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        if (root_capable && !noexec && (args == "" || args == "*")) found = 1
      }
    }
    NR > 3000 || length($0) > 2048 { partial = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      if (active) review(specs, root_capable)
      active = 0
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      root_capable = users ~ /(^|[[:space:],])(ALL|root|#0)([[:space:],]|$)/ &&
                     users !~ /(^|[[:space:],])!(ALL|root|#0)([[:space:],]|$)/
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      specs = line
      active = 1
      next
    }
    active && /^[[:space:]]+[^[:space:]]/ { active = 0; partial = 1; next }
    active { review(specs, root_capable); active = 0 }
    END {
      if (active) review(specs, root_capable)
      if (!partial && !denied && found)
        print "Sudo PrusaSlicer project post-processing review candidate: unrestricted root-capable slicer grant may process a caller-selected project; verify effective policy, binary/version, project format, and whether post-processing execution is enabled."
    }
  '
}
sudo_prusaslicer_project_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# A root-capable ProcMon grant can observe another process's syscall buffers.
# Inspect only already-captured sudo policy; never start a trace or read one.
sudo_procmon_trace_review() {
  [ -n "$1" ] || return 0
  case "$1" in *procmon*) ;; *) return 0 ;; esac
  printf '%s\n' "$1" | LC_ALL=C awk '
    function review(specs, root_capable, count, commands, i, command, path, args, noexec) {
      count = split(specs, commands, ",")
      noexec = 0
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        sub(/[[:space:]]*$/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/) {
          if (command ~ /^NOEXEC:/) noexec = 1
          if (command ~ /^EXEC:/) noexec = 0
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        }
        if (command ~ /^!/) {
          sub(/^![[:space:]]*/, "", command)
          path = command
          sub(/[[:space:]].*$/, "", path)
          if (path == "ALL" || path ~ /^\/([[:alnum:]_.+-]+\/)*procmon$/) denied = 1
          continue
        }
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/([[:alnum:]_.+-]+\/)*procmon$/) continue
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        if (root_capable && !noexec && (args == "" || args == "*")) found = 1
        if (noexec) denied = 1
      }
    }
    NR > 3000 || length($0) > 2048 { ambiguous = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      if (active) review(specs, root_capable)
      active = 0
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      root_capable = users ~ /(^|[[:space:],])(ALL|root|#0)([[:space:],]|$)/ &&
                     users !~ /(^|[[:space:],])!(ALL|root|#0)([[:space:],]|$)/
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      specs = line
      active = 1
      after_rule = 1
      next
    }
    after_rule && /^[[:space:]]+[^[:space:](]/ { ambiguous = 1; next }
    /^[[:space:]]*$/ { after_rule = 0 }
    active { review(specs, root_capable); active = 0 }
    END {
      if (active) review(specs, root_capable)
      if (!ambiguous && !denied && found)
        print "Sudo ProcMon root-process trace review candidate: unrestricted root-capable ProcMon may expose another process\047s syscall data; verify effective policy, tracer support, and target process before manual review."
    }
  '
}
sudo_procmon_trace_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# qpdf 10.2+ can embed an arbitrary readable file in a PDF. Only a captured,
# unrestricted root-capable sudo rule is a candidate; never run qpdf here.
sudo_qpdf_attachment_review() {
  [ -n "$1" ] || return 0
  case "$1" in *qpdf*) ;; *) return 0 ;; esac
  printf '%s\n' "$1" | LC_ALL=C awk '
    function review(specs, root_capable, count, commands, i, command, path, args) {
      count = split(specs, commands, ",")
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        sub(/[[:space:]]*$/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/)
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        if (command ~ /^!/) {
          sub(/^![[:space:]]*/, "", command)
          path = command
          sub(/[[:space:]].*$/, "", path)
          if (path == "ALL") deny_all = 1
          if (path ~ /^\/([[:alnum:]_.+-]+\/)*qpdf$/) denied[path] = 1
          continue
        }
        if (!root_capable) continue
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/([[:alnum:]_.+-]+\/)*qpdf$/) continue
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        if (args == "" || args == "*") found[path] = 1
      }
    }
    NR > 3000 || length($0) > 2048 { partial = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      previous_rule = 1
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      root_capable = users ~ /(^|[[:space:],])(ALL|root|#0)([[:space:],]|$)/ &&
                     users !~ /(^|[[:space:],])!(ALL|root|#0)([[:space:],]|$)/
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      review(line, root_capable)
      next
    }
    previous_rule && /^[[:space:]]+[^[:space:]]/ { partial = 1; exit }
    { previous_rule = 0 }
    END {
      if (partial || deny_all) exit
      for (path in found) {
        if (denied[path]) continue
        if (++count > 8) exit
        print path
      }
    }
  ' | while IFS= read -r sudo_qpdf_path; do
    [ -f "$sudo_qpdf_path" ] || continue
    echo "Sudo qpdf attachment file-read review candidate: $sudo_qpdf_path (unrestricted root-capable rule; confirm qpdf >=10.2, authentication, source readability by root, and writable output path)"
  done
}
sudo_qpdf_attachment_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# A single-name adduser grant can create a missing group with the same name.
# Inspect only captured sudo output and local policy metadata; never add a user.
# A visible group rule is a review lead, not proof of effective sudo policy.
sudo_adduser_group_review() (
  sudo_adduser_main=$4
  sudo_adduser_dir=$5
  sudo_adduser_groups=$6
  [ -r "$sudo_adduser_groups" ] && [ -f "$sudo_adduser_groups" ] || exit 0
  sudo_adduser_candidates=$(printf '%s\n%s\n%s\n' "$1" "$2" "$3" | LC_ALL=C awk '
    function review(specs, count, commands, i, command, path, args) {
      count = split(specs, commands, ",")
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/)
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        if (command ~ /^![[:space:]]*\/(usr\/)?sbin\/adduser([[:space:]]|$)/) { denied = 1; continue }
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/(usr\/)?sbin\/adduser$/) continue
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        if (args == "^[a-zA-Z0-9]+$" || args == "^[[:alnum:]]+$") all = 1
        else if (args == "admin" || args == "^admin$") admin = 1
        else if (args == "wheel" || args == "^wheel$") wheel = 1
        else if (args == "sudo" || args == "^sudo$") sudo = 1
      }
    }
    NR > 3000 { ambiguous = 1; exit }
    length($0) > 2048 { active = 0; ambiguous = 1; next }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      if (active) review(specs)
      active = 0
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      if (users ~ /(^|[[:space:],])!(root|ALL|#0)([[:space:],]|$)/ ||
          users !~ /(^|[[:space:],])(root|ALL|#0)([[:space:],]|$)/) next
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      specs = line
      active = 1
      next
    }
    active && /^[[:space:]]+[^[:space:]]/ {
      # A continuation may carry an exclusion or further arguments.
      active = 0
      ambiguous = 1
      next
    }
    active { review(specs); active = 0 }
    END {
      if (active) review(specs)
      if (denied || ambiguous) exit
      if (all || admin) print "admin"
      if (all || wheel) print "wheel"
      if (all || sudo) print "sudo"
    }
  ')
  [ -n "$sudo_adduser_candidates" ] || exit 0
  printf '%s\n' "$sudo_adduser_candidates" | while IFS= read -r sudo_adduser_group; do
    [ -n "$sudo_adduser_group" ] || continue
    LC_ALL=C awk -F: -v group="$sudo_adduser_group" '
      NR > 10000 { partial = 1; exit }
      $1 == group { found = 1; exit }
      END { exit found ? 0 : (partial ? 2 : 1) }
    ' "$sudo_adduser_groups"
    sudo_adduser_group_status=$?
    [ "$sudo_adduser_group_status" -eq 1 ] || continue
    sudo_adduser_visible=
    sudo_adduser_unknown=
    sudo_adduser_files=0
    [ -e "$sudo_adduser_main" ] || sudo_adduser_unknown=1
    for sudo_adduser_file in "$sudo_adduser_main" "$sudo_adduser_dir"/*; do
      [ "$sudo_adduser_files" -lt 16 ] || { sudo_adduser_unknown=1; break; }
      [ -e "$sudo_adduser_file" ] || continue
      sudo_adduser_files=$((sudo_adduser_files + 1))
      if [ ! -f "$sudo_adduser_file" ] || [ -L "$sudo_adduser_file" ] ||
         [ ! -r "$sudo_adduser_file" ] ||
         [ "$(find "$sudo_adduser_file" -prune -size -65537c -print 2>/dev/null)" != "$sudo_adduser_file" ]; then
        sudo_adduser_unknown=1
        continue
      fi
      sudo_adduser_policy=$(LC_ALL=C awk -v group="$sudo_adduser_group" '
        FNR > 400 { print "PARTIAL"; exit }
        length($0) > 1024 { partial = 1; next }
        {
          line = $0
          sub(/[[:space:]]*#.*/, "", line)
          rule = "^[[:space:]]*%" group "[[:space:]]+ALL[[:space:]]*=[[:space:]]*\\([[:space:]]*(ALL|root)[[:space:]]*(:[[:space:]]*ALL[[:space:]]*)?\\)[[:space:]]*(NOPASSWD:[[:space:]]*)?ALL[[:space:]]*$"
          if (line ~ rule) found = 1
        }
        END { if (found) print "FOUND"; if (partial) print "PARTIAL" }
      ' "$sudo_adduser_file" 2>/dev/null)
      case "$sudo_adduser_policy" in *FOUND*) sudo_adduser_visible=1 ;; esac
      case "$sudo_adduser_policy" in *PARTIAL*) sudo_adduser_unknown=1 ;; esac
    done
    if [ "$sudo_adduser_visible" ]; then
      printf 'Sudo adduser missing-group review candidate: constrained root-capable adduser grant, local %s group absent, visible %%%s root sudoers rule; verify effective policy, NSS, authentication, and group creation.\n' "$sudo_adduser_group" "$sudo_adduser_group"
    elif [ "$sudo_adduser_unknown" ]; then
      printf 'Sudo adduser missing-group conditional lead: constrained root-capable adduser grant and local %s group absent; sudoers policy unreadable or partial, so group privilege is unknown.\n' "$sudo_adduser_group"
    fi
  done
)
sudo_adduser_group_review "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" /etc/sudoers /etc/sudoers.d /etc/group |
  sed -${E} "s,.*,${SED_RED_YELLOW},"

# A bare root-capable restic command permits caller-selected arguments under
# sudoers syntax. This is a review lead only; later exclusions, authentication,
# and effective policy still need checking. Never invoke the password helper.
printf "%s\n%s\n%s\n" "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" | awk '
  NR > 3000 { exit }
  length($0) > 2048 { next }
  /^[[:space:]]*\([^)]*\)[[:space:]]/ {
    line = $0
    sub(/^[[:space:]]*\(/, "", line)
    runas = line
    sub(/\).*/, "", runas)
    split(runas, parts, ":")
    users = parts[1]
    if (users ~ /(^|[[:space:],])!(root|ALL|#0)([[:space:],]|$)/ ||
        users !~ /(^|[[:space:],])(root|ALL|#0)([[:space:],]|$)/) next
    sub(/^[^)]*\)[[:space:]]*/, "", line)
    count = split(line, commands, ",")
    for (i = 1; i <= count; i++) {
      command = commands[i]
      sub(/^[[:space:]]*/, "", command)
      sub(/[[:space:]]*$/, "", command)
      while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/)
        sub(/^[A-Z_]+:[[:space:]]*/, "", command)
      if (command ~ /^!\/[^[:space:]]*\/restic([[:space:]]|$)/) denied = 1
      else if (command ~ /^\/[^[:space:]]*\/restic$/) found = 1
    }
  }
  END {
    if (found && !denied) print "Sudo restic password-command review candidate: a root-capable argument-free grant may allow a caller-selected --password-command; verify effective policy and authentication."
  }
' | sed -${E} "s,.*,${SED_RED_YELLOW},"

# Bare root-capable BBOT or Bee grants permit caller-selected arguments.
# BBOT presets can load Python modules; Bee can evaluate PHP in a Backdrop site.
# Review only captured sudo policy text. Never run either program.
printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" | LC_ALL=C awk '
  function review(specs, count, commands, i, command, path) {
    count = split(specs, commands, ",")
    for (i = 1; i <= count; i++) {
      command = commands[i]
      sub(/^[[:space:]]*/, "", command)
      sub(/[[:space:]]*$/, "", command)
      while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/)
        sub(/^[A-Z_]+:[[:space:]]*/, "", command)
      if (command ~ /^!/) {
        path = command
        sub(/^![[:space:]]*/, "", path)
        sub(/[[:space:]].*$/, "", path)
        if (path ~ /^\/([[:alnum:]_.-]+\/)*bbot$/) denied = 1
        if (path ~ /^\/([[:alnum:]_.-]+\/)*bee(\.php)?$/) bee_denied = 1
        continue
      }
      if (command ~ /^\/([[:alnum:]_.-]+\/)*bbot$/) found = 1
      if (command ~ /^\/([[:alnum:]_.-]+\/)*bee(\.php)?$/) bee_found = 1
    }
  }
  NR > 3000 { ambiguous = 1; exit }
  length($0) > 2048 { ambiguous = 1; active = 0; next }
  /^[[:space:]]*\([^)]*\)[[:space:]]/ {
    if (active) review(specs)
    active = 0
    line = $0
    sub(/^[[:space:]]*\(/, "", line)
    runas = line
    sub(/\).*/, "", runas)
    split(runas, parts, ":")
    users = parts[1]
    if (users ~ /(^|[[:space:],])!(root|ALL|#0)([[:space:],]|$)/ ||
        users !~ /(^|[[:space:],])(root|ALL|#0)([[:space:],]|$)/) next
    sub(/^[^)]*\)[[:space:]]*/, "", line)
    specs = line
    active = 1
    next
  }
  active && /^[[:space:]]+[^[:space:]]/ { active = 0; ambiguous = 1; next }
  active { review(specs); active = 0 }
  END {
    if (active) review(specs)
    if (found && !denied && !ambiguous)
      print "Sudo BBOT preset-loader review candidate: unrestricted root-capable arguments may select a preset and Python module_dirs; verify effective policy, authentication, installed version, and caller control of preset/module paths."
    if (bee_found && !bee_denied && !ambiguous)
      print "Sudo Bee PHP CLI review candidate: unrestricted root-capable arguments may permit eval/php-script in a Backdrop site; verify effective policy, authentication, binary identity, and site bootstrap."
  }
' | sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# A root-capable sudo grant for docker exec can expose its --privileged and
# --user options even when the caller cannot read the Docker socket directly.
# Only inspect captured sudo policy; no container or daemon command is run.
printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" | LC_ALL=C awk '
  function review(specs, count, commands, i, command, path, args, rest) {
    count = split(specs, commands, ",")
    for (i = 1; i <= count; i++) {
      command = commands[i]
      sub(/^[[:space:]]*/, "", command)
      sub(/[[:space:]]*$/, "", command)
      while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/)
        sub(/^[A-Z_]+:[[:space:]]*/, "", command)
      if (command ~ /^!/) {
        sub(/^![[:space:]]*/, "", command)
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path ~ /^\/([[:alnum:]_.+-]+\/)*docker$/) {
          args = substr(command, length(path) + 1)
          sub(/^[[:space:]]+/, "", args)
          if (args == "" || args ~ /^exec([[:space:]]|$)/) denied = 1
        }
        continue
      }
      path = command
      sub(/[[:space:]].*$/, "", path)
      if (path !~ /^\/([[:alnum:]_.+-]+\/)*docker$/) continue
      args = substr(command, length(path) + 1)
      sub(/^[[:space:]]+/, "", args)
      sub(/[[:space:]]+$/, "", args)
      if (args !~ /^exec([[:space:]]|$)/) continue
      rest = args
      sub(/^exec[[:space:]]*/, "", rest)
      # A trailing bare wildcard is useful only before the container name;
      # fixed container/command arguments are intentionally not inferred.
      while (rest ~ /^(-i|-t|-it|-ti|--privileged|(-u|--user)[[:space:]]+(root|0)|--user=(root|0))[[:space:]]+/) {
        sub(/^(-i|-t|-it|-ti|--privileged|(-u|--user)[[:space:]]+(root|0)|--user=(root|0))[[:space:]]+/, "", rest)
      }
      if (rest == "*") found = 1
    }
  }
  NR > 3000 { ambiguous = 1; exit }
  length($0) > 2048 { ambiguous = 1; active = 0; next }
  /^[[:space:]]*\([^)]*\)[[:space:]]/ {
    if (active) review(specs)
    active = 0
    line = $0
    sub(/^[[:space:]]*\(/, "", line)
    runas = line
    sub(/\).*/, "", runas)
    split(runas, parts, ":")
    users = parts[1]
    if (users ~ /(^|[[:space:],])!(root|ALL|#0)([[:space:],]|$)/ ||
        users !~ /(^|[[:space:],])(root|ALL|#0)([[:space:],]|$)/) next
    sub(/^[^)]*\)[[:space:]]*/, "", line)
    specs = line
    active = 1
    next
  }
  active && /^[[:space:]]+[^[:space:]]/ { active = 0; ambiguous = 1; next }
  active { review(specs); active = 0 }
  END {
    if (active) review(specs)
    if (found && !denied && !ambiguous)
      print "Sudo Docker exec privilege review candidate: root-capable docker exec arguments may allow --privileged and --user root even without direct socket access; verify effective policy, authentication, a running container, rootful daemon, authorization controls, user namespaces, and host-device or writable-mount access."
  }
' | sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# A pair of unrestricted root-capable packet-filter commands can let the
# caller add rule comments and export the ruleset to a chosen pathname.
# Only inspect captured sudo policy; never change or export firewall rules.
sudo_packet_filter_export_review() {
  printf '%s\n' "$1" | LC_ALL=C awk '
    function review(specs, count, commands, i, command, path, name, kind) {
      count = split(specs, commands, ",")
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        sub(/[[:space:]]*$/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/)
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        if (command ~ /(^|[[:space:]])(sha224|sha256|sha384|sha512):/ || command ~ /\\,/) {
          ambiguous = 1
          continue
        }
        kind = "allow"
        if (command ~ /^!/) {
          kind = "deny"
          sub(/^![[:space:]]*/, "", command)
        }
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/([[:alnum:]_.+-]+\/)*[[:alnum:]_.+-]+$/) {
          if (kind == "deny") ambiguous = 1
          continue
        }
        name = path
        sub(/^.*\//, "", name)
        if (name !~ /^(ip6?tables)(-(nft|legacy))?(-save)?$/) {
          if (kind == "deny" && (name == "ALL" || name ~ /tables/)) ambiguous = 1
          continue
        }
        if (kind == "deny") {
          denied[name] = 1
          continue
        }
        # A bare sudoers command admits caller-selected arguments. A trailing
        # quoted empty argument or a fixed option does not.
        if (command == path) allowed[name] = 1
      }
    }
    NR > 3000 { ambiguous = 1; exit }
    length($0) > 2048 { ambiguous = 1; active = 0; next }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      if (active) review(specs)
      active = 0
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      if (users ~ /(^|[[:space:],])!(root|ALL|#0)([[:space:],]|$)/ ||
          users !~ /(^|[[:space:],])(root|ALL|#0)([[:space:],]|$)/) next
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      specs = line
      active = 1
      next
    }
    active && /^[[:space:]]+[^[:space:]]/ { active = 0; ambiguous = 1; next }
    active { review(specs); active = 0 }
    END {
      if (active) review(specs)
      if (ambiguous) exit
      names[1] = "iptables"
      names[2] = "ip6tables"
      names[3] = "iptables-nft"
      names[4] = "ip6tables-nft"
      names[5] = "iptables-legacy"
      names[6] = "ip6tables-legacy"
      for (i = 1; i <= 6; i++) {
        name = names[i]
        save = name "-save"
        if (allowed[name] && allowed[save] && !denied[name] && !denied[save])
          print "Sudo packet-filter export review candidate (" name " + " save "): unrestricted root-capable commands may permit rule-comment input and file output; verify effective policy, authentication, backend formatting, and target permissions."
      }
    }
  '
}
{
  sudo_packet_filter_export_review "$sudo_l_cached_output"
  sudo_packet_filter_export_review "$sudo_l_password_output"
  sudo_packet_filter_export_review "$sudo_l_output"
} | LC_ALL=C sort -u | sed -"${E}" "s,.*,${SED_RED_YELLOW},"

if command -v check_sudo_terraform_override >/dev/null 2>&1; then
  check_sudo_terraform_override "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output"
fi

# A sudoers argument wildcard can match spaces and path separators. Review only
# captured sudo -l command specifications: a wildcard in the executable path,
# another command, or a non-root RunAs rule is not this candidate. A displayed
# grant alone does not establish authentication, effective options, or confinement.
printf "%s\n%s\n%s\n" "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" | awk '
  function review(specs, count, commands, i, command, path, args) {
    count = split(specs, commands, ",")
    for (i = 1; i <= count; i++) {
      command = commands[i]
      sub(/^[[:space:]]*/, "", command)
      while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/)
        sub(/^[A-Z_]+:[[:space:]]*/, "", command)
      if (command ~ /^!/) continue
      path = command
      sub(/[[:space:]].*$/, "", path)
      if (path !~ /^\/[^[:space:]]*\/tcpdump$/) continue
      args = substr(command, length(path) + 1)
      if (args ~ /(^|[^\\])\*/) found = 1
    }
  }
  NR > 3000 { exit }
  length($0) > 2048 { if (active) review(specs); active = 0; next }
  /^[[:space:]]*\([^)]*\)[[:space:]]/ {
    if (active) review(specs)
    active = 0
    wrapped = 0
    indent = match($0, /[^[:space:]]/) - 1
    line = $0
    sub(/^[[:space:]]*\(/, "", line)
    runas = line
    sub(/\).*/, "", runas)
    split(runas, parts, ":")
    users = parts[1]
    if (users ~ /(^|[[:space:],])!(root|ALL|#0)([[:space:],]|$)/ ||
        users !~ /(^|[[:space:],])(root|ALL|#0)([[:space:],]|$)/) next
    sub(/^[^)]*\)[[:space:]]*/, "", line)
    specs = line
    active = 1
    next
  }
  active {
    continuation = $0
    sub(/^[[:space:]]*/, "", continuation)
    # A newly listed absolute command without a comma is not a wrapped arg.
    if (continuation == "" || match($0, /[^[:space:]]/) <= indent + 1 ||
        (continuation ~ /^\// && specs !~ /,[[:space:]]*$/ &&
         specs !~ /[[:space:]](-w|-F)[[:space:]]*$/)) {
      review(specs)
      active = 0
      next
    }
    if (length(specs) + length(continuation) > 2048 || ++wrapped > 8) {
      active = 0
      next
    }
    specs = specs " " continuation
    next
  }
  END {
    if (active) review(specs)
    if (found) print "Sudo tcpdump argument wildcard review candidate: root-capable rule may let * span spaces and path separators; verify effective options, authentication, and target confinement."
  }
' | sed -${E} "s,.*,${SED_RED_YELLOW},"

# sudo -l can show several Defaults scopes and repeated output from different
# authentication attempts. These are candidates, not necessarily the effective
# command-specific policy. Bound both candidate values and entries per value.
printf "%s\n%s\n%s\n" "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" | awk '
  /^Matching Defaults entries for / { defaults = 1; next }
  /^[[:space:]]*User .* may run the following commands/ { defaults = 0 }
  defaults && /^[[:space:]]*$/ { defaults = 0 }
  defaults || /^[[:space:]]*Defaults([[:space:]:!>@]|$)/ {
    line = $0
    while (match(line, /secure_path=[^,[:space:]]+/)) {
      value = substr(line, RSTART + 12, RLENGTH - 12)
      line = substr(line, RSTART + RLENGTH)
      if (length(value) > 1024 || seen[value]++ || count >= 8) continue
      count++
      gsub(/\\:/, ":", value)
      entries = split(value, paths, ":")
      for (i = 1; i <= entries && i <= 16; i++)
        if (length(paths[i]) <= 512 && paths[i] ~ /^\// &&
            paths[i] !~ /[\\|[:space:]]/)
          print count "|" i "|" paths[i]
    }
  }
' | while IFS='|' read -r secure_path_candidate secure_path_index secure_path_entry; do
  case "$secure_path_entry" in *'//'*|*'/./'*|*'/../'*|*'/.'|*'/..') continue ;; esac
  secure_path_remaining=${secure_path_entry#/}
  secure_path_walk=
  secure_path_symlink=
  while [ "$secure_path_remaining" ]; do
    secure_path_part=${secure_path_remaining%%/*}
    secure_path_walk="$secure_path_walk/$secure_path_part"
    if [ -L "$secure_path_walk" ]; then
      secure_path_symlink=$secure_path_walk
      break
    fi
    case "$secure_path_remaining" in
      */*) secure_path_remaining=${secure_path_remaining#*/} ;;
      *) break ;;
    esac
  done
  if [ "$secure_path_symlink" ]; then
    echo "Sudo secure_path candidate $secure_path_candidate entry $secure_path_index has symlink component $secure_path_symlink; target access is ambiguous"
  elif [ -d "$secure_path_entry" ] && [ -w "$secure_path_entry" ] && [ -x "$secure_path_entry" ]; then
    echo "Writable/traversable sudo secure_path candidate $secure_path_candidate entry $secure_path_index: $secure_path_entry (current user; verify effective Defaults for the permitted command)" |
      sed -${E} "s,.*,${SED_RED},g"
  fi
done

# A bare sudoers command permits arbitrary arguments; sudoers "" permits none.
# Ask sudo to authorize the exact -c invocation as root so argument restrictions,
# negations, and run-as rules are resolved by sudo itself. The config is never
# created and needrestart is never executed. This is separate from CVE-2024-48990.
if { [ -z "$ROOT_FOLDER" ] || [ "$ROOT_FOLDER" = / ]; } &&
   printf "%s\n%s\n%s\n" "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" |
     grep -Eq '(^|[[:space:],!])/usr/sbin/needrestart([[:space:],]|$)'; then
  sudo_needrestart_dir=${TMPDIR:-/tmp}
  if [ -d "$sudo_needrestart_dir" ] && [ -x "$sudo_needrestart_dir" ] &&
     [ -w "$sudo_needrestart_dir" ]; then
    sudo_needrestart_config="$sudo_needrestart_dir/linpeas-needrestart-$$.conf"
    if [ ! -e "$sudo_needrestart_config" ] && [ ! -L "$sudo_needrestart_config" ]; then
      sudo_needrestart_timeout=${TIMEOUT:-$(command -v timeout 2>/dev/null)}
      if [ "$sudo_needrestart_timeout" ]; then
        if [ "$PASSWORD" ]; then
          sudo_needrestart_query=$(printf '%s\n' "$PASSWORD" |
            "$sudo_needrestart_timeout" 5 sudo -S -l -u root -- /usr/sbin/needrestart -c "$sudo_needrestart_config" 2>&1)
        else
          sudo_needrestart_query=$("$sudo_needrestart_timeout" 5 sudo -n -l -u root -- /usr/sbin/needrestart -c "$sudo_needrestart_config" 2>&1)
        fi
        if [ "$?" -eq 0 ]; then
          echo "sudo permits root needrestart with a caller-controlled -c config; Perl config may execute as root: $sudo_needrestart_config" |
            sed -${E} "s,.*,${SED_RED_YELLOW},"
        else
          echo "sudo needrestart -c authorization unverified (policy denied or authentication unavailable)"
        fi
      else
        echo "sudo needrestart -c authorization unverified (timeout unavailable)"
      fi
    fi
  fi
fi

# Only an exact, argument-free command in a root-capable rule is a candidate.
# Reject ambiguous quoting, escaped spaces, and fixed arguments. Sudo resolves
# the full policy, including later negations, for the exact proposed invocation.
if { [ -z "$ROOT_FOLDER" ] || [ "$ROOT_FOLDER" = / ]; } &&
   [ "$(command -v sudo 2>/dev/null)" ]; then
  sudo_npbackup_rules=$(printf "%s\n%s\n%s\n" "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" | awk '
    /\([^)]*\)/ && /\/npbackup-cli/ {
      line = $0
      runas = line
      sub(/^[^(]*\(/, "", runas)
      sub(/\).*/, "", runas)
      split(runas, runas_parts, ":")
      users = runas_parts[1]
      if (users ~ /!root/ || users !~ /(^|[[:space:],])(root|ALL)([[:space:],]|$)/) next
      sub(/^.*\)/, "", line)
      n = split(line, commands, ",")
      for (i = 1; i <= n; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/) {
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        }
        sub(/[[:space:]]*$/, "", command)
        if (command ~ /^\/[A-Za-z0-9_.\/-]*\/npbackup-cli$/ && !seen[command]++ && ++count <= 10)
          print command
      }
    }
  ')
  if [ "$sudo_npbackup_rules" ]; then
    sudo_npbackup_dir=${TMPDIR:-/tmp}
    sudo_npbackup_timeout=${TIMEOUT:-$(command -v timeout 2>/dev/null)}
    if [ -d "$sudo_npbackup_dir" ] && [ -w "$sudo_npbackup_dir" ] &&
       [ -x "$sudo_npbackup_dir" ] && [ ! -L "$sudo_npbackup_dir" ]; then
      sudo_npbackup_count=0
      sudo_npbackup_started=$(date +%s 2>/dev/null)
      printf '%s\n' "$sudo_npbackup_rules" | while IFS= read -r sudo_npbackup_command; do
        if [ "$sudo_npbackup_started" ]; then
          sudo_npbackup_now=$(date +%s 2>/dev/null)
          [ "$sudo_npbackup_now" ] && [ $((sudo_npbackup_now - sudo_npbackup_started)) -lt 5 ] || break
        fi
        sudo_npbackup_count=$((sudo_npbackup_count + 1))
        sudo_npbackup_config="$sudo_npbackup_dir/linpeas-npbackup-$$-$sudo_npbackup_count.conf"
        [ ! -e "$sudo_npbackup_config" ] && [ ! -L "$sudo_npbackup_config" ] || continue
        if [ "$sudo_npbackup_timeout" ]; then
          if [ "$PASSWORD" ]; then
            sudo_npbackup_query=$(printf '%s\n' "$PASSWORD" |
              "$sudo_npbackup_timeout" 5 sudo -S -l -u root -- "$sudo_npbackup_command" -c "$sudo_npbackup_config" -b 2>&1)
          else
            sudo_npbackup_query=$("$sudo_npbackup_timeout" 5 sudo -n -l -u root -- "$sudo_npbackup_command" -c "$sudo_npbackup_config" -b 2>&1)
          fi
          if [ "$?" -eq 0 ]; then
            echo "Root-capable backup client accepts caller-selected config: $sudo_npbackup_command; review repo access, backup paths, and read/restore options" |
              sed -${E} "s,.*,${SED_RED_YELLOW},"
          else
            echo "sudo backup-client -c authorization unverified for $sudo_npbackup_command (policy denied or authentication unavailable)"
          fi
        else
          echo "sudo backup-client -c authorization unverified for $sudo_npbackup_command (timeout unavailable)"
        fi
      done
    fi
  fi
fi

# Review only root-capable, argument-free sudo shell-script grants. A caller
# may then choose the script's JSON argument. This is static inspection: never
# run the permitted script or its backup program, nor read the JSON itself.
sudo_backup_wrapper_rules=$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" | awk '
  NR > 3000 { limited = 1; exit }
  length($0) > 2048 { limited = 1; next }
  /^[[:space:]]*\([^)]*\)[[:space:]]/ {
    line = $0
    sub(/^[[:space:]]*\(/, "", line)
    runas = line
    sub(/\).*/, "", runas)
    split(runas, parts, ":")
    users = parts[1]
    if (users ~ /(^|[[:space:],])!(root|ALL|#0)([[:space:],]|$)/ ||
        users !~ /(^|[[:space:],])(root|ALL|#0)([[:space:],]|$)/) next
    sub(/^[^)]*\)[[:space:]]*/, "", line)
    count = split(line, commands, ",")
    for (i = 1; i <= count; i++) {
      command = commands[i]
      sub(/^[[:space:]]*/, "", command)
      sub(/[[:space:]]*$/, "", command)
      while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/)
        sub(/^[A-Z_]+:[[:space:]]*/, "", command)
      if (command ~ /^!\/[A-Za-z0-9_.\/-]+\.sh$/) {
        sub(/^!/, "", command)
        denied[command] = 1
      } else if (command ~ /^\/[A-Za-z0-9_.\/-]+\.sh$/ && !seen[command]++) {
        if (stored < 12) paths[++stored] = command
        else limited = 1
      }
    }
  }
  END {
    for (i = 1; i <= stored; i++) if (!denied[paths[i]]) print paths[i]
    if (limited) print "__LINPEAS_SUDO_WRAPPER_LIMIT__"
  }
')
if [ "$sudo_backup_wrapper_rules" ]; then
  printf '%s\n' "$sudo_backup_wrapper_rules" | while IFS= read -r sudo_backup_wrapper_script; do
    if [ "$sudo_backup_wrapper_script" = __LINPEAS_SUDO_WRAPPER_LIMIT__ ]; then
      echo "Sudo backup-wrapper review incomplete (sudo output or script-count limit)"
      continue
    fi
    # Reject path aliases and symlinks in every component, not just the file.
    case "$sudo_backup_wrapper_script" in
      *'//'*|*'/./'*|*'/../'*|*'/.'|*'/..')
        echo "Sudo shell-wrapper review unverified (non-canonical path): $sudo_backup_wrapper_script"
        continue ;;
    esac
    sudo_backup_wrapper_remaining=${sudo_backup_wrapper_script#/}
    sudo_backup_wrapper_walk=
    sudo_backup_wrapper_symlink=
    while [ "$sudo_backup_wrapper_remaining" ]; do
      sudo_backup_wrapper_part=${sudo_backup_wrapper_remaining%%/*}
      sudo_backup_wrapper_walk="$sudo_backup_wrapper_walk/$sudo_backup_wrapper_part"
      if [ -L "$sudo_backup_wrapper_walk" ]; then
        sudo_backup_wrapper_symlink=$sudo_backup_wrapper_walk
        break
      fi
      case "$sudo_backup_wrapper_remaining" in
        */*) sudo_backup_wrapper_remaining=${sudo_backup_wrapper_remaining#*/} ;;
        *) break ;;
      esac
    done
    if [ "$sudo_backup_wrapper_symlink" ]; then
      echo "Sudo shell-wrapper review unverified (symlink component): $sudo_backup_wrapper_script"
      continue
    fi
    if [ ! -f "$sudo_backup_wrapper_script" ] || [ ! -r "$sudo_backup_wrapper_script" ]; then
      echo "Sudo shell-wrapper review unverified (unreadable script): $sudo_backup_wrapper_script"
      continue
    fi
    if [ ! "$(find "$sudo_backup_wrapper_script" -prune -type f -size -65537c -print 2>/dev/null)" ]; then
      echo "Sudo shell-wrapper review incomplete (64 KiB script limit): $sudo_backup_wrapper_script"
      continue
    fi
    sudo_backup_wrapper_result=$(awk '
      NR > 200 { partial = 1; exit }
      length($0) > 2048 { partial = 1; next }
      /^[[:space:]]*#/ { next }
      {
        line = $0
        if (line ~ /^[[:space:]]*[A-Za-z_][A-Za-z0-9_]*="?\$1("|[[:space:]]|$)/) {
          variable = line
          sub(/^[[:space:]]*/, "", variable)
          sub(/=.*/, "", variable)
          arg_line = NR
        }
        clean = line
        gsub(/\\/, "", clean)
        if (index(clean, "gsub(") && index(clean, "../") && index(line, "jq")) jq_line = NR
        if (index(line, "directories_to_archive")) json_line = NR
        if (index(line, "allowed_paths")) allow_line = NR
        if (line ~ /==[[:space:]]*\$[A-Za-z_][A-Za-z0-9_]*\*/) prefix_line = NR
        if (variable != "" && index(line, ">") && index(line, "$" variable) &&
            (index(line, "echo") || index(line, "printf"))) rewrite_line = NR
        if (variable != "" && index(line, "$" variable) &&
            line ~ /(^|[[:space:]])(\/[A-Za-z0-9_.\/-]+\/)?(backy|[A-Za-z0-9_.-]*backup[A-Za-z0-9_.-]*)[[:space:]]/) backend_line = NR
      }
      END {
        if (arg_line && jq_line && json_line && allow_line && prefix_line && rewrite_line && backend_line &&
            arg_line < jq_line && jq_line <= rewrite_line && rewrite_line < backend_line &&
            json_line < backend_line && allow_line < backend_line && prefix_line < backend_line)
          printf "candidate:%d,%d,%d,%d,%d,%d", arg_line, jq_line, json_line, allow_line, rewrite_line, backend_line
        if (partial) printf " partial"
      }
    ' "$sudo_backup_wrapper_script" 2>/dev/null)
    case "$sudo_backup_wrapper_result" in
      candidate:*)
        echo "Sudo backup-wrapper JSON review candidate: $sudo_backup_wrapper_script (indicator lines ${sudo_backup_wrapper_result#candidate:}; verify caller input, resolved paths, rewrite failure, output access, and effective policy)" |
          sed -"${E}" "s,.*,${SED_RED_YELLOW}," ;;
      *partial*)
        echo "Sudo shell-wrapper review incomplete (200-line/2048-column scan limit): $sudo_backup_wrapper_script" ;;
    esac
  done
fi

# Correlate root-capable sudo Python commands with dynamic import/site-directory
# loading. site.addsitedir() and site.addpackage() process .pth files, whose
# "import " lines are executed by Python. A writable plugin/site directory can
# therefore turn an otherwise fixed sudo command into code execution as root.
sudo_python_scripts=$(printf "%s\n%s\n%s\n" "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" | awk '
  /python[0-9.]*/ && /\((root|ALL)([[:space:]:,)]|$)/ && !/!root/ {
    for (i = 1; i <= NF; i++) {
      token = $i
      quote = sprintf("%c", 39)
      gsub("^[\"" quote "]|[\"" quote ",:]$", "", token)
      if (token ~ /^\/.*\.py$/) print token
    }
  }
' | sort -u)

if [ "$sudo_python_scripts" ]; then
  printf "%s\n" "$sudo_python_scripts" | while IFS= read -r python_sudo_script; do
    [ -f "$python_sudo_script" ] && [ -r "$python_sudo_script" ] || continue

    python_loader_lines=$(grep -nE 'site\.(addsitedir|addpackage)[[:space:]]*\(|sys\.path\.(append|insert|extend)[[:space:]]*\(|(spec_from_file_location|SourceFileLoader|exec_module)[[:space:]]*\(' "$python_sudo_script" 2>/dev/null)
    [ "$python_loader_lines" ] || continue

    python_script_dir=$(dirname "$python_sudo_script")
    python_loader_path_lines=$(grep -E 'site\.(addsitedir|addpackage)|sys\.path\.(append|insert|extend)|(_DIR|_PATH)[[:space:]]*=' "$python_sudo_script" 2>/dev/null)
    python_loader_roots=$(
      printf "%s\n" "$python_loader_path_lines" | grep -Eo "['\"]/[^'\"]+['\"]" 2>/dev/null | tr -d "'\""
      printf "%s\n" "$python_loader_path_lines" | sed -nE "s/.*[(\/=][[:space:]]*['\"]([^'\"]+)['\"].*/\1/p" | while IFS= read -r python_loader_literal; do
        if [ "${python_loader_literal#/}" = "$python_loader_literal" ] &&
           ! printf "%s" "$python_loader_literal" | grep -q '://'; then
          printf "%s/%s\n" "$python_script_dir" "$python_loader_literal"
        fi
      done
    )
    # Fall back to the script tree only when static path extraction is not
    # possible (for example when addsitedir() receives a computed value).
    [ "$python_loader_roots" ] || python_loader_roots="$python_script_dir"

    python_seen_dirs="|"
    printf "%s\n" "$python_loader_roots" | sort -u | while IFS= read -r python_loader_root; do
      if ! [ -d "$python_loader_root" ]; then
        python_loader_parent=$(dirname "$python_loader_root")
        if [ -d "$python_loader_parent" ] && [ -w "$python_loader_parent" ]; then
          echo "Privileged sudo Python script loads code/paths dynamically: $python_sudo_script"
          printf "%s\n" "$python_loader_lines"
          echo "Python loader path can be created in writable parent: $python_loader_root (parent: $python_loader_parent)" | sed -${E} "s,.*,${SED_RED_YELLOW},"
          echo ""
        fi
        continue
      fi
      # A bounded traversal keeps this quick and also catches loaders that
      # iterate over plugin subdirectories before calling addsitedir().
      for python_candidate_dir in "$python_loader_root" "$python_loader_root"/* "$python_loader_root"/*/*; do
        [ -d "$python_candidate_dir" ] || continue
        python_writable_pth=""
        for python_pth_file in "$python_candidate_dir"/*.pth; do
          [ -f "$python_pth_file" ] && [ -w "$python_pth_file" ] && python_writable_pth=1
        done
        [ -w "$python_candidate_dir" ] || [ "$python_writable_pth" ] || continue
        case "$python_seen_dirs" in
          *"|$python_candidate_dir|"*) continue ;;
        esac
        python_seen_dirs="${python_seen_dirs}${python_candidate_dir}|"

        echo "Privileged sudo Python script loads code/paths dynamically: $python_sudo_script"
        printf "%s\n" "$python_loader_lines"
        if printf "%s\n" "$python_loader_lines" | grep -qE 'site\.(addsitedir|addpackage)'; then
          echo "Writable directory processed by privileged Python site loader: $python_candidate_dir (.pth import lines may execute as root)" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        else
          echo "Writable directory near privileged Python import path: $python_candidate_dir (possible module/path hijack)" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        fi
        ls -ld "$python_candidate_dir" 2>/dev/null

        for python_pth_file in "$python_candidate_dir"/*.pth; do
          [ -f "$python_pth_file" ] || continue
          [ -w "$python_candidate_dir" ] || [ -w "$python_pth_file" ] || continue
          echo "  Python path configuration file: $python_pth_file"
          python_pth_imports=$(grep -nE '^import[[:space:]]' "$python_pth_file" 2>/dev/null)
          if [ "$python_pth_imports" ]; then
            printf "%s\n" "$python_pth_imports" | sed -${E} "s,.*,${SED_RED_YELLOW},"
          fi
        done
        echo ""
      done
    done
  done
fi

# Review literal imports in exact sudo-permitted Python entry points, including
# commands run as another non-root user. Only existing local files are reported.
# This is static evidence: Python search paths and the reachable action can differ.
sudo_python_import_rules=$(printf "%s\n%s\n%s\n" "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" | awk '
  /^[[:space:]]*\([^)]*\)[[:space:]]/ {
    rule = $0
    sub(/^[[:space:]]*\(/, "", rule)
    runas = rule
    sub(/\).*/, "", runas)
    split(runas, users, /[[:space:],:]+/)
    runas = users[1]
    if (runas == "" || runas ~ /[!|]/) next
    sub(/^[^)]*\)[[:space:]]*/, "", rule)
    sub(/^([^[:space:]:]+:[[:space:]]*)*/, "", rule)
    if (rule ~ /^!/) next
    command_count = split(rule, commands, /,[[:space:]]*/)
    for (i = 1; i <= command_count; i++) {
      command = commands[i]
      sub(/^[[:space:]]*/, "", command)
      denied = (command ~ /^!/)
      if (denied) sub(/^![[:space:]]*/, "", command)
      split(command, words, /[[:space:]]+/)
      path = words[1]
      if (path ~ /^\/[^[:space:]*?\[\],!|]+\.py$/ &&
          path !~ /\/\.\.?\// && path !~ /\/\// &&
          command !~ /[*!?\[\]]/) {
        key = runas "|" path
        if (denied) negated[key] = 1
        else if (!seen[key]++) ordered[++total] = key
      }
    }
  }
  END {
    for (i = 1; i <= total; i++)
      if (!negated[ordered[i]]) print ordered[i]
  }
' | head -n 10)

# Reject symlink components, so a displayed same-directory path is not an
# alias into another tree. All candidates share the checked script directory.
sudo_python_import_plain_path() {
  case "$1" in /*) ;; *) return 1 ;; esac
  sudo_python_import_walk=
  sudo_python_import_remaining=${1#/}
  while [ "$sudo_python_import_remaining" ]; do
    sudo_python_import_part=${sudo_python_import_remaining%%/*}
    sudo_python_import_walk="$sudo_python_import_walk/$sudo_python_import_part"
    [ ! -L "$sudo_python_import_walk" ] || return 1
    case "$sudo_python_import_remaining" in
      */*) sudo_python_import_remaining=${sudo_python_import_remaining#*/} ;;
      *) break ;;
    esac
  done
}

sudo_python_import_check_candidate() {
  [ "$sudo_python_import_probe_count" -lt 40 ] || return
  sudo_python_import_probe_count=$((sudo_python_import_probe_count + 1))
  sudo_python_import_candidate="$sudo_python_import_dir/$1"
  [ -f "$sudo_python_import_candidate" ] &&
    sudo_python_import_plain_path "$sudo_python_import_candidate" || return
  sudo_python_import_access=
  if [ -w "$sudo_python_import_candidate" ]; then
    sudo_python_import_access="file is writable"
  else
    sudo_python_import_parent=${sudo_python_import_candidate%/*}
    if [ -w "$sudo_python_import_parent" ] && [ -x "$sudo_python_import_parent" ]; then
      sudo_python_import_sticky=$(ls -ld "$sudo_python_import_parent" 2>/dev/null | awk '{print substr($1, 10, 1)}')
      case "$sudo_python_import_sticky" in
        t|T) ;;
        *) sudo_python_import_access="parent permits replacement" ;;
      esac
    fi
  fi
  [ "$sudo_python_import_access" ] || return
  echo "Sudo Python import review: $sudo_python_import_script as $sudo_python_import_runas" |
    sed -${E} "s,.*,${SED_RED_YELLOW},"
  echo "Literal import at line $sudo_python_import_line: $sudo_python_import_statement"
  echo "Caller-writable local import candidate: $sudo_python_import_candidate ($sudo_python_import_access)" |
    sed -${E} "s,.*,${SED_RED_YELLOW},"
  echo "Review Python import resolution and the permitted action."
  echo ""
}

if [ "$sudo_python_import_rules" ]; then
  printf "%s\n" "$sudo_python_import_rules" | while IFS='|' read -r sudo_python_import_runas sudo_python_import_script; do
    [ -f "$sudo_python_import_script" ] && [ -r "$sudo_python_import_script" ] &&
      [ -x "$sudo_python_import_script" ] &&
      sudo_python_import_plain_path "$sudo_python_import_script" || continue
    sudo_python_import_size=$(stat -c %s "$sudo_python_import_script" 2>/dev/null) ||
      sudo_python_import_size=$(stat -f %z "$sudo_python_import_script" 2>/dev/null) || continue
    case "$sudo_python_import_size" in ''|*[!0-9]*) continue ;; esac
    [ "$sudo_python_import_size" -le 65536 ] || continue
    sudo_python_import_shebang=$(sed -n '1p;1q' "$sudo_python_import_script" 2>/dev/null)
    printf "%s\n" "$sudo_python_import_shebang" |
      grep -Eq '^#![[:space:]]*/([^[:space:]]*/)?python([0-9]+(\.[0-9]+)?)?([[:space:]]|$)|^#![[:space:]]*/usr/bin/env[[:space:]]+python([0-9]+(\.[0-9]+)?)?([[:space:]]|$)' ||
      continue
    sudo_python_import_dir=${sudo_python_import_script%/*}
    sudo_python_import_probe_count=0
    sed -n '1,400p;401q' "$sudo_python_import_script" 2>/dev/null | awk '
      /^import[[:space:]]+[A-Za-z_][A-Za-z_0-9]*(\.[A-Za-z_][A-Za-z_0-9]*)?([[:space:],#]|$)/ {
        name = $2
        sub(/[,#].*$/, "", name)
        if (name ~ /^[A-Za-z_][A-Za-z_0-9]*(\.[A-Za-z_][A-Za-z_0-9]*)?$/)
          print NR "|import|" name "|"
        if (++count == 20) exit
      }
      /^from[[:space:]]+[A-Za-z_][A-Za-z_0-9]*[[:space:]]+import[[:space:]]+[A-Za-z_][A-Za-z_0-9]*([[:space:],#]|$)/ {
        name = $2
        member = $4
        sub(/[,#].*$/, "", member)
        if (member ~ /^[A-Za-z_][A-Za-z_0-9]*$/)
          print NR "|from|" name "|" member
        if (++count == 20) exit
      }
    ' | while IFS='|' read -r sudo_python_import_line sudo_python_import_kind sudo_python_import_name sudo_python_import_member; do
      sudo_python_import_statement="$sudo_python_import_kind $sudo_python_import_name${sudo_python_import_member:+ import $sudo_python_import_member}"
      if [ "$sudo_python_import_kind" = from ]; then
        [ -f "$sudo_python_import_dir/$sudo_python_import_name/__init__.py" ] ||
          sudo_python_import_check_candidate "$sudo_python_import_name.py"
        sudo_python_import_check_candidate "$sudo_python_import_name/__init__.py"
        if [ -f "$sudo_python_import_dir/$sudo_python_import_name/__init__.py" ] ||
           [ ! -f "$sudo_python_import_dir/$sudo_python_import_name.py" ]; then
          sudo_python_import_check_candidate "$sudo_python_import_name/$sudo_python_import_member.py"
          sudo_python_import_check_candidate "$sudo_python_import_name/$sudo_python_import_member/__init__.py"
        fi
      else
        case "$sudo_python_import_name" in
          *.*)
            sudo_python_import_member=${sudo_python_import_name#*.}
            sudo_python_import_name=${sudo_python_import_name%%.*}
            if [ -f "$sudo_python_import_dir/$sudo_python_import_name/__init__.py" ] ||
               [ ! -f "$sudo_python_import_dir/$sudo_python_import_name.py" ]; then
              sudo_python_import_check_candidate "$sudo_python_import_name/__init__.py"
              sudo_python_import_check_candidate "$sudo_python_import_name/$sudo_python_import_member.py"
              sudo_python_import_check_candidate "$sudo_python_import_name/$sudo_python_import_member/__init__.py"
            fi
            ;;
          *)
            [ -f "$sudo_python_import_dir/$sudo_python_import_name/__init__.py" ] ||
              sudo_python_import_check_candidate "$sudo_python_import_name.py"
            sudo_python_import_check_candidate "$sudo_python_import_name/__init__.py"
            ;;
        esac
      fi
    done
  done
fi

# Review a root sudo rule that lets the caller choose a PyTorch checkpoint.
# A small shell wrapper must forward its first argument to one fixed Python
# helper containing a model loader. This never reads or executes model data.
sudo_model_loader_review() {
  [ -n "$1" ] || return 0
  case "$1" in *'.pth'*|*'.pt'*|*'.ckpt'*) ;; *) return 0 ;; esac
  sudo_model_loader_rules=$(printf '%s\n' "$1" | LC_ALL=C awk '
    NR > 3000 || length($0) > 2048 { incomplete = 1; exit }
    /^[[:space:]]*$/ { after_rule = 0; next }
    after_rule && /^[[:space:]]+[^[:space:](]/ { incomplete = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      after_rule = 1
      line = $0
      runas = line
      sub(/^[[:space:]]*\(/, "", runas)
      sub(/\).*/, "", runas)
      split(runas, users, ":")
      if (users[1] !~ /(^|[[:space:],])(root|ALL|#0)([[:space:],]|$)/ ||
          users[1] ~ /(^|[[:space:],])!(root|ALL|#0)([[:space:],]|$)/) next
      sub(/^[[:space:]]*[^)]*\)[[:space:]]*/, "", line)
      if (line ~ /(^|[[:space:]])NOEXEC:/ || line ~ /(^|[[:space:]])!/ ||
          line ~ /,/) { blocked = 1; next }
      while (sub(/^[A-Z_]+:[[:space:]]*/, "", line)) {}
      n = split(line, words, /[[:space:]]+/)
      if (n != 2) next
      script = words[1]
      model = words[2]
      if (script !~ /^\/[A-Za-z0-9_.+\/-]+$/ ||
          script ~ /\/(\.\/|\.\.\/|\/)/ ||
          model !~ /^\/[A-Za-z0-9_.+\/-]+\/\*\.(pt|pth|ckpt)$/ ||
          model ~ /\/(\.\/|\.\.\/|\/)/) next
      dir = model
      sub(/\/\*\.(pt|pth|ckpt)$/, "", dir)
      key = script "|" dir
      if (!seen[key]++ && total < 8) ordered[++total] = key
    }
    END {
      if (incomplete || blocked) exit
      for (i = 1; i <= total; i++) print ordered[i]
    }
  ')
  [ -n "$sudo_model_loader_rules" ] || return 0
  printf '%s\n' "$sudo_model_loader_rules" |
    while IFS='|' read -r sudo_model_loader_script sudo_model_loader_dir; do
      [ -d "$sudo_model_loader_dir" ] && [ -w "$sudo_model_loader_dir" ] &&
        [ -x "$sudo_model_loader_dir" ] &&
        sudo_python_import_plain_path "$sudo_model_loader_dir" || continue
      [ -f "$sudo_model_loader_script" ] && [ -r "$sudo_model_loader_script" ] &&
        [ -x "$sudo_model_loader_script" ] &&
        sudo_python_import_plain_path "$sudo_model_loader_script" || continue
      sudo_model_loader_size=$(stat -c %s "$sudo_model_loader_script" 2>/dev/null) ||
        sudo_model_loader_size=$(stat -f %z "$sudo_model_loader_script" 2>/dev/null) || continue
      case "$sudo_model_loader_size" in ''|*[!0-9]*) continue ;; esac
      [ "$sudo_model_loader_size" -le 65536 ] || continue
      sudo_model_loader_helper=$(LC_ALL=C awk '
        NR > 200 || length($0) > 2048 { incomplete = 1; exit }
        /^[[:space:]]*#/ { next }
        {
          line = $0
          sub(/^[[:space:]]*/, "", line)
          if (line ~ /^[A-Za-z_][A-Za-z0-9_]*=["\047]\$1["\047][[:space:]]*(#.*)?$/) {
            modelvar = line
            sub(/=.*/, "", modelvar)
          }
          if (line ~ /^[A-Za-z_][A-Za-z0-9_]*=["\047]\/[A-Za-z0-9_.+\/-]+[.]py["\047][[:space:]]*(#.*)?$/) {
            helpervar = line
            sub(/=.*/, "", helpervar)
            helperpath = line
            sub(/^[^=]*=["\047]/, "", helperpath)
            sub(/["\047].*$/, "", helperpath)
          }
          if (modelvar != "" && helpervar != "" &&
              line ~ /^\/[A-Za-z0-9_.+\/-]*python(3([.][0-9]+)?)?[[:space:]]/ &&
              index(line, "$" modelvar "\"") && index(line, "$" helpervar "\""))
            forwarded = 1
        }
        END { if (!incomplete && forwarded) print helperpath }
      ' "$sudo_model_loader_script" 2>/dev/null)
      case "$sudo_model_loader_helper" in
        /*.py) ;;
        *) continue ;;
      esac
      case "$sudo_model_loader_helper" in
        *'/../'*|*'/./'*|*'//'*) continue ;;
      esac
      [ -f "$sudo_model_loader_helper" ] && [ -r "$sudo_model_loader_helper" ] &&
        sudo_python_import_plain_path "$sudo_model_loader_helper" || continue
      sudo_model_loader_size=$(stat -c %s "$sudo_model_loader_helper" 2>/dev/null) ||
        sudo_model_loader_size=$(stat -f %z "$sudo_model_loader_helper" 2>/dev/null) || continue
      case "$sudo_model_loader_size" in ''|*[!0-9]*) continue ;; esac
      [ "$sudo_model_loader_size" -le 65536 ] || continue
      sudo_model_loader_line=$(LC_ALL=C awk '
        NR > 200 || length($0) > 2048 { incomplete = 1; exit }
        /^[[:space:]]*#/ { next }
        /^[[:space:]]*([A-Za-z_][A-Za-z0-9_]*[[:space:]]*=[[:space:]]*|return[[:space:]]+)?(torch[.]load|pickle[.]load|joblib[.]load)[[:space:]]*\(/ {
          if (!found) found = NR
        }
        END { if (!incomplete && found) print found }
      ' "$sudo_model_loader_helper" 2>/dev/null)
      [ -n "$sudo_model_loader_line" ] || continue
      printf 'Sudo model-loader review candidate: %s accepts checkpoints from writable %s and forwards them to %s (loader at line %s); verify helper dataflow, effective sudo policy, loader version, and weights_only behavior.\n' "$sudo_model_loader_script" "$sudo_model_loader_dir" "$sudo_model_loader_helper" "$sudo_model_loader_line"
    done
}
sudo_model_loader_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# Correlate direct root sudo rules with an existing cache for a top-level,
# same-directory import. Only a bounded prefix of each script is read. The
# interpreter is queried for its version, but the sudo target is never run.
# A matching filename and permissions are review evidence, not proof that
# Python will accept the bytecode header or that the import will be reached.
sudo_python_cache_scripts=$(printf "%s\n%s\n%s\n" "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" | awk '
  /^[[:space:]]*\((root|ALL)([[:space:]:,)]|$)/ && !/!root/ {
    sub(/^[^)]*\)[[:space:]]*/, "")
    sub(/^[^:]*:[[:space:]]*/, "")
    if ($1 ~ /^\/[^[:space:]*?,\[\]]*\.py$/) print $1
  }
' | sort -u | head -n 10)

if [ "$sudo_python_cache_scripts" ]; then
  printf "%s\n" "$sudo_python_cache_scripts" | while IFS= read -r sudo_python_cache_script; do
    [ -f "$sudo_python_cache_script" ] && [ -r "$sudo_python_cache_script" ] &&
      [ -x "$sudo_python_cache_script" ] &&
      [ ! -L "$sudo_python_cache_script" ] || continue
    sudo_python_cache_size=$(stat -c %s "$sudo_python_cache_script" 2>/dev/null) ||
      sudo_python_cache_size=$(stat -f %z "$sudo_python_cache_script" 2>/dev/null) || continue
    case "$sudo_python_cache_size" in ''|*[!0-9]*) continue ;; esac
    [ "$sudo_python_cache_size" -le 65536 ] || continue

    sudo_python_cache_shebang=$(sed -n '1p;1q' "$sudo_python_cache_script" 2>/dev/null)
    case "$sudo_python_cache_shebang" in '#!'/*) ;; *) continue ;; esac
    sudo_python_cache_interpreter=${sudo_python_cache_shebang#'#!'}
    sudo_python_cache_interpreter=${sudo_python_cache_interpreter%%[[:space:]]*}
    case "${sudo_python_cache_interpreter##*/}" in
      python3|python3.[0-9]|python3.[0-9][0-9]) ;;
      *) continue ;;
    esac
    [ -x "$sudo_python_cache_interpreter" ] || continue
    if [ "$TIMEOUT" ]; then
      sudo_python_cache_version=$("$TIMEOUT" -k 1 2 "$sudo_python_cache_interpreter" -I -S --version 2>&1 | head -c 4096)
    elif command -v timeout >/dev/null 2>&1; then
      sudo_python_cache_version=$(timeout -k 1 2 "$sudo_python_cache_interpreter" -I -S --version 2>&1 | head -c 4096)
    else
      continue
    fi
    sudo_python_cache_tag=$(printf "%s\n" "$sudo_python_cache_version" | awk '
      NF == 2 && $1 == "Python" && $2 ~ /^3\.[0-9]+\.[0-9]+$/ {
        split($2, v, ".")
        if (v[2] <= 99) print "cpython-3" v[2]
        exit
      }
    ')
    [ "$sudo_python_cache_tag" ] || continue

    sudo_python_cache_dir=$(dirname "$sudo_python_cache_script")
    [ -x "$sudo_python_cache_dir" ] || continue
    sudo_python_cache_dir="$sudo_python_cache_dir/__pycache__"
    [ -d "$sudo_python_cache_dir" ] && [ -x "$sudo_python_cache_dir" ] &&
      [ ! -L "$sudo_python_cache_dir" ] || continue
    sudo_python_cache_sticky=$(ls -ld "$sudo_python_cache_dir" 2>/dev/null | awk '{print substr($1, 10, 1)}')

    sed -n '1,400p;401q' "$sudo_python_cache_script" 2>/dev/null | awk '
      /^import[[:space:]]+[A-Za-z_][A-Za-z_0-9]*([[:space:],#]|$)/ {
        module = $2
        sub(/[,#].*$/, "", module)
      }
      /^from[[:space:]]+[A-Za-z_][A-Za-z_0-9]*[[:space:]]+import[[:space:]]+/ {
        module = $2
      }
      module != "" && !seen[module]++ {
        print NR "|" module
        if (++count == 20) exit
      }
      { module = "" }
    ' | while IFS='|' read -r sudo_python_cache_line sudo_python_cache_module; do
      sudo_python_cache_source="${sudo_python_cache_dir%/__pycache__}/$sudo_python_cache_module.py"
      sudo_python_cache_pyc="$sudo_python_cache_dir/$sudo_python_cache_module.$sudo_python_cache_tag.pyc"
      [ -f "$sudo_python_cache_source" ] && [ -r "$sudo_python_cache_source" ] &&
        [ -f "$sudo_python_cache_pyc" ] && [ -r "$sudo_python_cache_pyc" ] &&
        [ ! -L "$sudo_python_cache_pyc" ] || continue

      sudo_python_cache_access=""
      if [ -w "$sudo_python_cache_pyc" ]; then
        sudo_python_cache_access="bytecode file is writable"
      elif [ -w "$sudo_python_cache_dir" ]; then
        # Sticky directories only permit replacing a file owned by this user.
        case "$sudo_python_cache_sticky" in
          t|T)
            sudo_python_cache_owner=$(stat -c %u "$sudo_python_cache_pyc" 2>/dev/null) ||
              sudo_python_cache_owner=$(stat -f %u "$sudo_python_cache_pyc" 2>/dev/null)
            [ "$sudo_python_cache_owner" = "$(id -u)" ] &&
              sudo_python_cache_access="bytecode file is owned by this user in writable sticky cache"
            ;;
          *) sudo_python_cache_access="cache directory permits bytecode replacement" ;;
        esac
      fi
      [ "$sudo_python_cache_access" ] || continue
      echo "Privileged sudo Python bytecode cache review: $sudo_python_cache_script" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      echo "Top-level import at line $sudo_python_cache_line: $sudo_python_cache_module ($sudo_python_cache_source)"
      echo "Matching $sudo_python_cache_tag bytecode: $sudo_python_cache_pyc ($sudo_python_cache_access)" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      echo "Check the bytecode header and source validity before treating this as exploitable."
      echo ""
    done
  done
fi

# CVE-2025-4517: correlate a user-selected archive, a writable archive
# directory, and extraction by the same tarfile handle. This only reads a
# bounded script; it never runs the sudo-authorized command. Upstream fixes
# shipped in CPython 3.9.23, 3.10.18, 3.11.13, 3.12.11, and 3.13.4.
# Vendor backports may make an older version safe, so this is a review candidate.
# https://discuss.python.org/t/python-3-13-4-3-12-11-3-11-13-3-10-18-and-3-9-23-are-now-available/94367
sudo_python_tar_commands=$(printf "%s\n%s\n%s\n" "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" | awk '
  /^[[:space:]]*\((root|ALL)([[:space:]:,)]|$)/ && !/!root/ {
    sub(/^[^)]*\)[[:space:]]*/, "")
    sub(/^[^:]*:[[:space:]]*/, "")
    if ($1 ~ /^\/[^[:space:]]*\/python3(\.(9|10|11|12|13))?$/ &&
        $2 ~ /^\/[^[:space:]]*\.py$/ && $3 == "*") print $1 "|" $2
  }
' | sort -u | head -n 10)

if [ "$sudo_python_tar_commands" ]; then
  printf "%s\n" "$sudo_python_tar_commands" | while IFS= read -r sudo_python_tar_command; do
    sudo_python_binary=${sudo_python_tar_command%%|*}
    python_sudo_script=${sudo_python_tar_command#*|}
    [ -x "$sudo_python_binary" ] && [ -f "$python_sudo_script" ] && [ -r "$python_sudo_script" ] || continue
    # Keep inspection bounded and never run the sudo-authorized script.
    sudo_python_tar_size=$(stat -c %s "$python_sudo_script" 2>/dev/null) ||
      sudo_python_tar_size=$(stat -f %z "$python_sudo_script" 2>/dev/null) || continue
    case "$sudo_python_tar_size" in ''|*[!0-9]*) continue ;; esac
    [ "$sudo_python_tar_size" -le 65536 ] || continue
    if [ "$TIMEOUT" ]; then
      sudo_python_version=$("$TIMEOUT" -k 1 2 "$sudo_python_binary" -I -S --version 2>&1 | head -c 4096)
    elif command -v timeout >/dev/null 2>&1; then
      sudo_python_version=$(timeout -k 1 2 "$sudo_python_binary" -I -S --version 2>&1 | head -c 4096)
    else
      continue
    fi
    sudo_python_version=$(printf "%s\n" "$sudo_python_version" | awk '
      NF == 2 && $1 == "Python" && $2 ~ /^3\.(9|10|11|12|13)\.[0-9]+$/ {
        split($2, v, ".")
        if ((v[2] == 9 && v[3] < 23) || (v[2] == 10 && v[3] < 18) ||
            (v[2] == 11 && v[3] < 13) || (v[2] == 12 && v[3] < 11) ||
            (v[2] == 13 && v[3] < 4)) {
          print $2
          exit
        }
      }
    ')
    [ "$sudo_python_version" ] || continue

    sed -n '1,400p;401q' "$python_sudo_script" 2>/dev/null | awk '
      {
        line = $0
        sub(/^[[:space:]]*/, "", line)
        indent = length($0) - length(line)
        if (active && line !~ /^(#|$)/ && indent <= active_indent) active = 0
        if (active && line ~ ("^" handle "[.]extractall[[:space:]]*[(]") &&
            line ~ /filter[[:space:]]*=[[:space:]]*["\047]data["\047]/) {
          print active_dir
          active = 0
        }
        if (line ~ /^[A-Za-z_][A-Za-z_0-9]*[[:space:]]*=/) {
          name = line
          sub(/[[:space:]]*=.*/, "", name)
          if (active && name == handle) active = 0
          delete dirs[name]
          delete archives[name]
          rhs = line
          sub(/^[^=]*=[[:space:]]*/, "", rhs)
          quote = substr(rhs, 1, 1)
          if (quote == "\"" || quote == sprintf("%c", 39)) {
            end = index(substr(rhs, 2), quote)
            path = substr(rhs, 2, end - 1)
            rest = substr(rhs, end + 2)
            if (end > 1 && substr(path, 1, 1) == "/" && rest ~ /^[[:space:]]*(#.*)?$/)
              dirs[name] = path
          }
          if (rhs ~ /^os[.]path[.]join[[:space:]]*[(][[:space:]]*[A-Za-z_][A-Za-z_0-9]*[[:space:]]*,[[:space:]]*args[.][A-Za-z_][A-Za-z_0-9]*[[:space:]]*[)]/) {
            base = rhs
            sub(/^os[.]path[.]join[[:space:]]*[(][[:space:]]*/, "", base)
            sub(/[[:space:],].*/, "", base)
            if (base in dirs) archives[name] = dirs[base]
          }
        }
        if (line ~ /^with[[:space:]]+tarfile[.]open[[:space:]]*[(][[:space:]]*[A-Za-z_][A-Za-z_0-9]*[[:space:],)]/ &&
            line ~ /[[:space:]]as[[:space:]]+[A-Za-z_][A-Za-z_0-9]*[[:space:]]*:[[:space:]]*(#.*)?$/) {
          archive = line
          sub(/^with[[:space:]]+tarfile[.]open[[:space:]]*[(][[:space:]]*/, "", archive)
          sub(/[[:space:],)].*/, "", archive)
          if (archive in archives) {
            handle = line
            sub(/^.*[[:space:]]as[[:space:]]+/, "", handle)
            sub(/[[:space:]]*:.*$/, "", handle)
            active_dir = archives[archive]
            active_indent = indent
            active = 1
          }
        }
      }
    ' | sort -u | while IFS= read -r sudo_python_tar_dir; do
      [ -d "$sudo_python_tar_dir" ] && [ -w "$sudo_python_tar_dir" ] || continue
      echo "Privileged sudo Python archive extraction review: $python_sudo_script ($sudo_python_version)" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      echo "Writable archive directory: $sudo_python_tar_dir; tarfile.extractall(filter=\"data\") may allow CVE-2025-4517 on unpatched CPython" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      echo "Check vendor patches before treating the version as vulnerable."
      echo ""
    done
  done
fi

# A root sudo grant for one exact Python entry point can still pass a caller
# supplied Git remote to GitPython. Inspect only that named script; do not run
# the wrapper, import its modules, or contact a remote. A legacy GitPython
# clone with protocol.ext.allow=always may execute the remote-ext helper.
sudo_gitpython_clone_review() {
  [ -n "$1" ] || return 0
  sudo_gitpython_clone_rules=$(printf '%s\n' "$1" | awk '
    NR > 3000 || length($0) > 2048 { incomplete = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      line = $0
      runas = line
      sub(/^[[:space:]]*\(/, "", runas)
      sub(/\).*/, "", runas)
      if (runas !~ /^(root|ALL|#0)([[:space:]:,]|$)/ || runas ~ /!([[:space:]]*)?(root|ALL|#0)/) next
      sub(/^[[:space:]]*[^)]*\)[[:space:]]*/, "", line)
      if (line ~ /(^|[[:space:]])NOEXEC:/ || line ~ /(^|[[:space:]])!/) {
        blocked = 1
        next
      }
      while (sub(/^[A-Z_]+:[[:space:]]*/, "", line)) {}
      if (line ~ /,/) next
      n = split(line, words, /[[:space:]]+/)
      if (n != 3 || words[3] != "*") next
      binary = words[1]
      script = words[2]
      if (binary !~ /^\/[A-Za-z0-9_.+\/-]*\/python3([.][0-9]+)?$/ ||
          script !~ /^\/[A-Za-z0-9_.+\/-]+[.]py$/ ||
          binary ~ /\/(\.\/|\.\.\/|\/)/ || script ~ /\/(\.\/|\.\.\/|\/)/) next
      key = binary "|" script
      if (!seen[key]++ && total < 6) ordered[++total] = key
    }
    END {
      if (incomplete || blocked) exit
      for (i = 1; i <= total; i++) print ordered[i]
    }
  ')
  [ -n "$sudo_gitpython_clone_rules" ] || return 0
  if [ -x /usr/bin/python3 ]; then
    sudo_gitpython_parser=/usr/bin/python3
  elif [ -x /usr/local/bin/python3 ]; then
    sudo_gitpython_parser=/usr/local/bin/python3
  else
    return 0
  fi
  printf '%s\n' "$sudo_gitpython_clone_rules" | while IFS='|' read -r sudo_gitpython_binary sudo_gitpython_script; do
    [ -x "$sudo_gitpython_binary" ] && [ -f "$sudo_gitpython_script" ] &&
      [ -r "$sudo_gitpython_script" ] &&
      sudo_python_import_plain_path "$sudo_gitpython_script" || continue
    sudo_gitpython_size=$(stat -c %s "$sudo_gitpython_script" 2>/dev/null) ||
      sudo_gitpython_size=$(stat -f %z "$sudo_gitpython_script" 2>/dev/null) || continue
    case "$sudo_gitpython_size" in ''|*[!0-9]*) continue ;; esac
    [ "$sudo_gitpython_size" -le 65536 ] || continue
    sudo_gitpython_result=$("$sudo_gitpython_parser" -I -S - "$sudo_gitpython_script" 2>/dev/null <<'GITPYTHON_CLONE_AST'
import ast
import signal
import sys

signal.alarm(1)
try:
    with open(sys.argv[1], "rb") as source_file:
        source = source_file.read(65537)
    if len(source) > 65536 or source.count(b"\n") > 200:
        raise SystemExit(0)
    if any(len(line) > 2048 for line in source.splitlines()):
        raise SystemExit(0)
    tree = ast.parse(source)
except (OSError, SyntaxError, ValueError, RecursionError):
    raise SystemExit(0)


def name(node, value):
    return isinstance(node, ast.Name) and node.id == value


def constant(node, value):
    if isinstance(node, getattr(ast, "Constant", ())):
        return node.value == value
    if isinstance(node, ast.Str):
        return node.s == value
    if isinstance(node, ast.Num):
        return node.n == value
    return False


def argv_one(node):
    if not isinstance(node, ast.Subscript):
        return False
    target = node.value
    index = node.slice
    if isinstance(index, getattr(ast, "Index", ())):
        index = index.value
    return (isinstance(target, ast.Attribute) and target.attr == "argv"
            and name(target.value, "sys") and constant(index, 1))


def repo_init(node):
    return (isinstance(node, ast.Call) and
            isinstance(node.func, ast.Attribute) and
            node.func.attr == "init" and name(node.func.value, "Repo"))


def ext_enabled(node):
    return (isinstance(node, (ast.List, ast.Tuple)) and
            any(constant(item, "-c protocol.ext.allow=always") or
                constant(item, "--config protocol.ext.allow=always")
                for item in node.elts))


imports = any(isinstance(stmt, ast.ImportFrom) and stmt.module == "git"
              and any(alias.name == "Repo" and alias.asname is None
                      for alias in stmt.names) for stmt in tree.body)
sys_import = any(isinstance(stmt, ast.Import) and
                 any(alias.name == "sys" and alias.asname is None
                     for alias in stmt.names) for stmt in tree.body)
if not imports or not sys_import:
    raise SystemExit(0)

writes = {}
for node in ast.walk(tree):
    if isinstance(node, ast.Name) and isinstance(node.ctx, (ast.Store, ast.Del)):
        writes[node.id] = writes.get(node.id, 0) + 1
if writes.get("Repo") or writes.get("sys"):
    raise SystemExit(0)

url_names = set()
repo_names = set()
for stmt in tree.body:
    if isinstance(stmt, ast.Assign) and len(stmt.targets) == 1 and isinstance(stmt.targets[0], ast.Name):
        target = stmt.targets[0].id
        if writes.get(target) == 1:
            if argv_one(stmt.value):
                url_names.add(target)
            elif repo_init(stmt.value):
                repo_names.add(target)
    value = getattr(stmt, "value", None)
    if not isinstance(value, ast.Call) or not isinstance(value.func, ast.Attribute):
        continue
    if value.func.attr != "clone_from" or not value.args:
        continue
    receiver = value.func.value
    if not isinstance(receiver, ast.Name):
        continue
    if receiver.id != "Repo" and receiver.id not in repo_names:
        continue
    remote = value.args[0]
    if not argv_one(remote) and not (isinstance(remote, ast.Name) and remote.id in url_names):
        continue
    options = [kw.value for kw in value.keywords if kw.arg == "multi_options"]
    if len(options) != 1 or not ext_enabled(options[0]):
        continue
    print("GitPython privileged clone review candidate")
    break
GITPYTHON_CLONE_AST
)
    if [ "$sudo_gitpython_result" = 'GitPython privileged clone review candidate' ]; then
      printf 'GitPython privileged clone review candidate: %s (root sudo permits caller-selected URL with ext transport; verify GitPython version, patch status, effective sudo policy, and reachability; no clone attempted)\n' "$sudo_gitpython_script"
    fi
  done
}

sudo_gitpython_clone_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")"

# A sudo script that invokes ./helper inherits the caller's working directory
# unless it changes directory first. Inspect only bounded, readable scripts
# from exact root-capable rules; never run them or the helper.
sudo_relative_cwd_review() {
  [ -n "$1" ] || return 0
  case "$1" in *runchdir*|*RUNCHDIR*) return 0 ;; esac
  sudo_relative_cwd_dir=
  for sudo_relative_cwd_candidate in "$(pwd -P 2>/dev/null)" "${TMPDIR:-/tmp}" /tmp; do
    case "$sudo_relative_cwd_candidate" in /*) ;; *) continue ;; esac
    if [ -d "$sudo_relative_cwd_candidate" ] && [ -w "$sudo_relative_cwd_candidate" ] &&
       [ -x "$sudo_relative_cwd_candidate" ]; then
      sudo_relative_cwd_dir=$sudo_relative_cwd_candidate
      break
    fi
  done
  [ -n "$sudo_relative_cwd_dir" ] || return 0
  sudo_relative_cwd_rules=$(printf '%s\n' "$1" | LC_ALL=C awk '
    function review(specs, root_capable, count, commands, i, command, path, args, interpreter_rule, denied_args, denied_script) {
      count = split(specs, commands, ",")
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        sub(/[[:space:]]*$/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/) {
          if (command ~ /^NOEXEC:/) noexec = 1
          if (command ~ /^EXEC:/) noexec = 0
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        }
        if (command ~ /^!/) {
          sub(/^![[:space:]]*/, "", command)
          path = command
          sub(/[[:space:]].*$/, "", path)
          if (path == "ALL") deny_all = 1
          if (path ~ /^\/[[:alnum:]_.+\/-]+$/) denied[path] = 1
          if (path ~ /^\/(usr\/)?bin\/python3(\.[0-9]+)?$/) {
            denied_args = substr(command, length(path) + 1)
            sub(/^[[:space:]]+/, "", denied_args)
            sub(/[[:space:]]+$/, "", denied_args)
            if (denied_args == "" || denied_args == "*") denied_python = 1
            else if (denied_args ~ /^\/[[:alnum:]_.+\/-]+\.py([[:space:]]+\*)?$/) {
              denied_script = denied_args
              sub(/[[:space:]]+\*$/, "", denied_script)
              denied_python_script[denied_script] = 1
            }
          }
          if (path ~ /^\/(usr\/(local\/)?)?bin\/ruby([0-9]+(\.[0-9]+)?)?$/) {
            denied_args = substr(command, length(path) + 1)
            sub(/^[[:space:]]+/, "", denied_args)
            sub(/[[:space:]]+$/, "", denied_args)
            if (denied_args == "" || denied_args == "*") denied_ruby = 1
            else if (denied_args ~ /^\/[[:alnum:]_.+\/-]+\.rb([[:space:]]+\*)?$/) {
              denied_script = denied_args
              sub(/[[:space:]]+\*$/, "", denied_script)
              denied_ruby_script[denied_script] = 1
            }
          }
          continue
        }
        if (!root_capable || noexec) continue
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/[[:alnum:]_.+\/-]+$/) continue
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        interpreter_rule = ""
        # A fixed Python interpreter plus a fixed script is still a script rule.
        if (path ~ /^\/(usr\/)?bin\/python3(\.[0-9]+)?$/) {
          if (args !~ /^\/[[:alnum:]_.+\/-]+\.py([[:space:]]+\*)?$/) continue
          sub(/[[:space:]]+\*$/, "", args)
          path = args
          args = ""
          interpreter_rule = "python"
        } else if (path ~ /^\/(usr\/(local\/)?)?bin\/ruby([0-9]+(\.[0-9]+)?)?$/) {
          if (args !~ /^\/[[:alnum:]_.+\/-]+\.rb([[:space:]]+\*)?$/) continue
          sub(/[[:space:]]+\*$/, "", args)
          path = args
          args = ""
          interpreter_rule = "ruby"
        }
        if (args != "" && args != "*") continue
        if (interpreter_rule == "python") python_candidate[path] = 1
        else if (interpreter_rule == "ruby") ruby_candidate[path] = 1
        else direct_candidate[path] = 1
        if (!found[path]++) ordered[++total] = path
      }
    }
    NR > 3000 || length($0) > 2048 { partial = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      previous_rule = 1
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      root_capable = users ~ /(^|[[:space:],])(ALL|root|#0)([[:space:],]|$)/ &&
                     users !~ /(^|[[:space:],])!(ALL|root|#0)([[:space:],]|$)/
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      review(line, root_capable)
      next
    }
    previous_rule && /^[[:space:]]+[^[:space:]]/ { partial = 1; exit }
    { previous_rule = 0 }
    END {
      if (partial || deny_all) exit
      for (i = 1; i <= total; i++) {
        path = ordered[i]
        if (denied[path] ||
            (python_candidate[path] && !direct_candidate[path] &&
             (denied_python || denied_python_script[path])) ||
            (ruby_candidate[path] && !direct_candidate[path] &&
             (denied_ruby || denied_ruby_script[path]))) continue
        if (++count > 12) exit
        print path
      }
    }
  ')
  [ -n "$sudo_relative_cwd_rules" ] || return 0
  printf '%s\n' "$sudo_relative_cwd_rules" | while IFS= read -r sudo_relative_cwd_script; do
    case "$sudo_relative_cwd_script" in *'//'*|*'/./'*|*'/../'*|*'/.'|*'/..') continue ;; esac
    [ -f "$sudo_relative_cwd_script" ] &&
      sudo_python_import_plain_path "$sudo_relative_cwd_script" || continue
    if [ ! -r "$sudo_relative_cwd_script" ]; then
      case "$sudo_relative_cwd_script" in
        *.py) printf 'Sudo Python script source unreadable: %s (relative working-directory behavior unknown; review source and policy)\n' "$sudo_relative_cwd_script" ;;
        *.rb) printf 'Sudo Ruby script source unreadable: %s (relative working-directory and YAML loader behavior unknown; review source and policy)\n' "$sudo_relative_cwd_script" ;;
      esac
      continue
    fi
    sudo_relative_cwd_size=$(stat -c %s "$sudo_relative_cwd_script" 2>/dev/null) ||
      sudo_relative_cwd_size=$(stat -f %z "$sudo_relative_cwd_script" 2>/dev/null) || continue
    case "$sudo_relative_cwd_size" in ''|*[!0-9]*) continue ;; esac
    [ "$sudo_relative_cwd_size" -le 65536 ] || continue
    sudo_relative_cwd_shebang=$(sed -n '1p;1q' "$sudo_relative_cwd_script" 2>/dev/null)
    case "$sudo_relative_cwd_script:$sudo_relative_cwd_shebang" in
      *.rb:*|*:'#!'*'/ruby'*|*:'#!'*'/env ruby'*)
        # Relative YAML input plus a loader call is a version-dependent cue.
        # Do not read the YAML, invoke Ruby, or assume the loader is unsafe.
        sudo_relative_cwd_yaml=$(LC_ALL=C head -c 65537 "$sudo_relative_cwd_script" 2>/dev/null | awk '
          { bytes += length($0) + 1 }
          NR > 200 || bytes > 65536 || length($0) > 2048 { partial = 1; exit }
          /^[[:space:]]*#/ { next }
          /(^|[^[:alnum:]_])(Dir|Process)[.]chdir[[:space:]]*\(/ { changes_dir = 1 }
          /(^|[^[:alnum:]_])chdir[[:space:]]*\(/ { changes_dir = 1 }
          /(YAML|Psych)[.](load|unsafe_load)[[:space:]]*\([[:space:]]*File[.]read[[:space:]]*\([[:space:]]*["\047][[:alnum:]_.+-]+[.](yml|yaml)["\047][[:space:]]*\)/ {
            if (!yaml_line) yaml_line = NR
          }
          END { if (!partial && !changes_dir && yaml_line) print yaml_line }
        ')
        if [ -n "$sudo_relative_cwd_yaml" ]; then
          printf 'Sudo relative-CWD Ruby YAML review candidate: %s reads a relative YAML file at line %s; writable CWD example: %s (verify input path, loader/Psych version and permitted classes, reachability, and effective sudo policy)\n' \
            "$sudo_relative_cwd_script" "$sudo_relative_cwd_yaml" "$sudo_relative_cwd_dir"
        fi
        continue
        ;;
      *.py:*|*:'#!'*'/python3'*|*:'#!'*'/env python3'*)
        # A literal single-item helper list, its call, and subprocess use are
        # a static review cue; they do not prove the branch is reachable.
        sudo_relative_cwd_helper=$(LC_ALL=C head -c 65537 "$sudo_relative_cwd_script" 2>/dev/null | awk '
          { bytes += length($0) + 1 }
          NR > 200 || bytes > 65536 || length($0) > 2048 { partial = 1; exit }
          /^[[:space:]]*#/ { next }
          /os[.]chdir[[:space:]]*\(/ || /cwd[[:space:]]*=/ { changes_dir = 1 }
          /subprocess[.](run|call|check_call|check_output|Popen)[[:space:]]*\(/ { subprocess_seen = 1 }
          /^[[:space:]]*[A-Za-z_][A-Za-z0-9_]*[[:space:]]*=[[:space:]]*\[[[:space:]]*["\047]\.\/[[:alnum:]_.+-]+["\047][[:space:]]*\]/ {
            variable = $0
            sub(/[[:space:]]*=.*/, "", variable)
            sub(/^[[:space:]]*/, "", variable)
            candidate = $0
            sub(/^[^[]*\[[[:space:]]*["\047]\.\//, "", candidate)
            sub(/["\047][[:space:]]*\].*$/, "", candidate)
            if (candidate ~ /^[[:alnum:]_.+-]+$/) {
              helper = candidate
              helper_var = variable
            }
          }
          helper_var != "" && (/run_command[[:space:]]*\(/ || /subprocess[.](run|call|check_call|check_output|Popen)[[:space:]]*\(/) {
            if ($0 ~ ("\\([[:space:]]*" helper_var "[[:space:]]*[,)]")) helper_called = 1
          }
          END { if (!partial && !changes_dir && subprocess_seen && helper_called && helper != "") print helper }
        ')
        ;;
      *:'#!'*'/sh'|*:'#!'*'/bash'|*:'#!'*'/dash'|*:'#!'*'/ksh'|*:'#!'*'/zsh'|*:'#!'*'/env sh'|*:'#!'*'/env bash')
        sudo_relative_cwd_helper=$(LC_ALL=C awk '
      NR > 200 || length($0) > 2048 { partial = 1; exit }
      /^[[:space:]]*#/ { next }
      /(^|[;&|])[[:space:]]*(cd|pushd)[[:space:]]+/ { changes_dir = 1 }
      /^[[:space:]]*(exec[[:space:]]+)?\.\/[[:alnum:]_.+-]+([[:space:];|&]|$)/ {
        helper = $0
        sub(/^[[:space:]]*(exec[[:space:]]+)?\.\//, "", helper)
        sub(/[[:space:];|&].*$/, "", helper)
      }
      END { if (!partial && !changes_dir && helper != "") print helper }
    ' "$sudo_relative_cwd_script" 2>/dev/null)
        ;;
      *) continue ;;
    esac
    [ -n "$sudo_relative_cwd_helper" ] || continue
    printf 'Sudo relative-CWD helper review candidate: %s references ./%s; writable CWD example: %s (verify path flow, branch reachability, permissions, executable mount, and effective policy)\n' \
      "$sudo_relative_cwd_script" "$sudo_relative_cwd_helper" "$sudo_relative_cwd_dir"
  done
}
sudo_relative_cwd_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# An unquoted RHS in Bash [[ lhs == rhs ]] is a pattern. Only inspect short,
# readable Bash scripts from exact root-capable sudo rules; never execute them.
sudo_bash_pattern_auth_review() {
  [ -n "$1" ] || return 0
  sudo_bash_pattern_rules=$(printf '%s\n' "$1" | LC_ALL=C awk '
    function review(specs, root_capable, count, commands, i, command, path, args) {
      count = split(specs, commands, ",")
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        sub(/[[:space:]]*$/, "", command)
        noexec = 0
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/) {
          if (command ~ /^NOEXEC:/) noexec = 1
          if (command ~ /^EXEC:/) noexec = 0
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        }
        if (command ~ /^!/) {
          sub(/^![[:space:]]*/, "", command)
          path = command
          sub(/[[:space:]].*$/, "", path)
          if (path == "ALL") deny_all = 1
          if (path ~ /^\/[[:alnum:]_.+\/-]+$/) denied[path] = 1
          continue
        }
        if (!root_capable || noexec) continue
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/[[:alnum:]_.+\/-]+$/) continue
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        if ((args == "" || args == "*") && !found[path]++) ordered[++total] = path
      }
    }
    NR > 3000 || length($0) > 2048 { partial = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      previous_rule = 1
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      root_capable = users ~ /(^|[[:space:],])(ALL|root|#0)([[:space:],]|$)/ &&
                     users !~ /(^|[[:space:],])!(ALL|root|#0)([[:space:],]|$)/
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      review(line, root_capable)
      next
    }
    previous_rule && /^[[:space:]]+[^[:space:]]/ { partial = 1; exit }
    { previous_rule = 0 }
    END {
      if (partial || deny_all) exit
      for (i = 1; i <= total; i++) {
        path = ordered[i]
        if (denied[path]) continue
        if (++count > 12) exit
        print path
      }
    }
  ')
  [ -n "$sudo_bash_pattern_rules" ] || return 0
  printf '%s\n' "$sudo_bash_pattern_rules" | while IFS= read -r sudo_bash_pattern_script; do
    case "$sudo_bash_pattern_script" in *'//'*|*'/./'*|*'/../'*|*'/.'|*'/..') continue ;; esac
    [ -f "$sudo_bash_pattern_script" ] && [ -r "$sudo_bash_pattern_script" ] &&
      sudo_python_import_plain_path "$sudo_bash_pattern_script" || continue
    sudo_bash_pattern_size=$(stat -c %s "$sudo_bash_pattern_script" 2>/dev/null) ||
      sudo_bash_pattern_size=$(stat -f %z "$sudo_bash_pattern_script" 2>/dev/null) || continue
    case "$sudo_bash_pattern_size" in ''|*[!0-9]*) continue ;; esac
    [ "$sudo_bash_pattern_size" -le 65536 ] || continue
    sudo_bash_pattern_shebang=$(sed -n '1p;1q' "$sudo_bash_pattern_script" 2>/dev/null)
    case "$sudo_bash_pattern_shebang" in '#!'*'/bash'|'#!'*'/env bash') ;; *) continue ;; esac
    sudo_bash_pattern_line=$(LC_ALL=C awk '
      NR > 200 || length($0) > 2048 { partial = 1; exit }
      /^[[:space:]]*#/ { next }
      /^[[:space:]]*read[[:space:]]/ {
        name = $NF
        if (name ~ /^[A-Za-z_][A-Za-z0-9_]*$/) read_name[name] = 1
      }
      {
        expr = $0
        if (!sub(/^[[:space:]]*(if[[:space:]]+)?\[\[[[:space:]]*/, "", expr)) next
        pivot = index(expr, "==")
        if (!pivot) next
        lhs = substr(expr, 1, pivot - 1)
        rhs = substr(expr, pivot + 2)
        if (lhs !~ /^[[:space:]]*\$[A-Za-z_][A-Za-z0-9_]*[[:space:]]*$/ &&
            lhs !~ /^[[:space:]]*"\$[A-Za-z_][A-Za-z0-9_]*"[[:space:]]*$/) next
        if (!sub(/[[:space:]]*\]\][[:space:]]*(;[[:space:]]*then)?[[:space:]]*$/, "", rhs)) next
        sub(/^[[:space:]]*/, "", rhs)
        sub(/[[:space:]]*$/, "", rhs)
        if (rhs !~ /^\$[A-Za-z_][A-Za-z0-9_]*$/) next
        name = substr(rhs, 2)
        if (read_name[name] && !match_line) match_line = NR
      }
      END { if (!partial && match_line) print match_line }
    ' "$sudo_bash_pattern_script" 2>/dev/null)
    [ -n "$sudo_bash_pattern_line" ] || continue
    printf 'Sudo Bash pattern-comparison review candidate: %s (line %s; unquoted comparison RHS from read; verify input control, reachability, password semantics, and effective sudo policy)\n' \
      "$sudo_bash_pattern_script" "$sudo_bash_pattern_line"
  done
}
sudo_bash_pattern_auth_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# Inspect only exact root-capable sudo scripts already named by sudo -l. A
# literal ps2pdf -dNOSAFER pairing is a review cue, not proof that untrusted
# data reaches executable PostScript. Never run the script or converter.
sudo_ps2pdf_nosafer_review() {
  [ -n "$1" ] || return 0
  sudo_ps2pdf_rules=$(printf '%s\n' "$1" | LC_ALL=C awk '
    function review(specs, root_capable, count, commands, i, command, path, args, noexec) {
      count = split(specs, commands, ",")
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        sub(/[[:space:]]*$/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/) {
          if (command ~ /^NOEXEC:/) noexec = 1
          if (command ~ /^EXEC:/) noexec = 0
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        }
        if (command ~ /^!/) {
          sub(/^![[:space:]]*/, "", command)
          path = command
          sub(/[[:space:]].*$/, "", path)
          if (path == "ALL") deny_all = 1
          if (path ~ /^\/[[:alnum:]_.+\/-]+$/) denied[path] = 1
          continue
        }
        if (!root_capable || noexec) continue
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/[[:alnum:]_.+\/-]+$/) continue
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        if ((args == "" || args == "*") && !seen[path]++) ordered[++total] = path
      }
    }
    NR > 3000 || length($0) > 2048 { partial = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      previous_rule = 1
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      root_capable = users ~ /(^|[[:space:],])(ALL|root|#0)([[:space:],]|$)/ &&
                     users !~ /(^|[[:space:],])!(ALL|root|#0)([[:space:],]|$)/
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      review(line, root_capable)
      next
    }
    previous_rule && /^[[:space:]]+[^[:space:]]/ { partial = 1; exit }
    { previous_rule = 0 }
    END {
      if (partial || deny_all) exit
      for (i = 1; i <= total; i++) {
        path = ordered[i]
        if (denied[path]) continue
        if (++count > 10) exit
        print path
      }
    }
  ')
  [ -n "$sudo_ps2pdf_rules" ] || return 0
  printf '%s\n' "$sudo_ps2pdf_rules" | while IFS= read -r sudo_ps2pdf_script; do
    case "$sudo_ps2pdf_script" in *'//'*|*'/./'*|*'/../'*|*'/.'|*'/..') continue ;; esac
    sudo_python_import_plain_path "$sudo_ps2pdf_script" &&
      [ -f "$sudo_ps2pdf_script" ] && [ -r "$sudo_ps2pdf_script" ] &&
      [ -x "$sudo_ps2pdf_script" ] || continue
    sudo_ps2pdf_size=$(stat -c %s "$sudo_ps2pdf_script" 2>/dev/null) ||
      sudo_ps2pdf_size=$(stat -f %z "$sudo_ps2pdf_script" 2>/dev/null) || continue
    case "$sudo_ps2pdf_size" in ''|*[!0-9]*) continue ;; esac
    [ "$sudo_ps2pdf_size" -le 65536 ] || continue
    sudo_ps2pdf_shebang=$(sed -n '1p;1q' "$sudo_ps2pdf_script" 2>/dev/null)
    case "$sudo_ps2pdf_shebang" in '#!'*'/python'*|'#!'*'env python'*|'#!'*'/sh'|'#!'*'/bash'|'#!'*'/dash'|'#!'*'/ksh'|'#!'*'/zsh'|'#!'*'env sh'|'#!'*'env bash') ;; *) continue ;; esac
    sudo_ps2pdf_line=$(LC_ALL=C awk '
      NR > 400 || length($0) > 2048 { partial = 1; exit }
      /^[[:space:]]*#/ { next }
      /(^|[^[:alnum:]_.-])ps2pdf([^[:alnum:]_.-]|$)/ { tool_line = NR }
      /(^|[^[:alnum:]_-])-dNOSAFER([^[:alnum:]_]|$)/ { if (!flag_line) flag_line = NR }
      END { if (!partial && tool_line && flag_line) print flag_line }
    ' "$sudo_ps2pdf_script" 2>/dev/null)
    [ -n "$sudo_ps2pdf_line" ] || continue
    printf 'Sudo PostScript conversion review candidate: %s (literal ps2pdf and -dNOSAFER, flag line %s; verify caller-controlled PostScript reaches converter, SAFER state, and effective sudo policy)\n' \
      "$sudo_ps2pdf_script" "$sudo_ps2pdf_line"
  done
}
sudo_ps2pdf_nosafer_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# Inspect only exact root-capable Python rules that accept a caller-selected
# Compose YAML. This is a review cue: wrapper validation and daemon policy can
# still block dangerous service settings. Never run the wrapper or Docker.
sudo_compose_wrapper_review() {
  [ -n "$1" ] || return 0
  sudo_compose_rules=$(printf '%s\n' "$1" | LC_ALL=C awk '
    function review(specs, root_capable, count, commands, i, command, path, args, noexec) {
      count = split(specs, commands, ",")
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        sub(/[[:space:]]*$/, "", command)
        noexec = 0
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/) {
          if (command ~ /^NOEXEC:/) noexec = 1
          if (command ~ /^EXEC:/) noexec = 0
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        }
        if (command ~ /^!/) {
          sub(/^![[:space:]]*/, "", command)
          path = command
          sub(/[[:space:]].*$/, "", path)
          if (path == "ALL") deny_all = 1
          if (path ~ /^\/[[:alnum:]_.+\/-]+\.py$/) denied[path] = 1
          continue
        }
        if (!root_capable || noexec) continue
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/[[:alnum:]_.+\/-]+\.py$/) continue
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        if (args !~ /^(\*\.ya?ml|\/[[:alnum:]_.+\/-]*\*\.ya?ml)$/) continue
        if (!seen[path]++) ordered[++total] = path
      }
    }
    NR > 3000 || length($0) > 2048 { partial = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      previous_rule = 1
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      root_capable = users ~ /(^|[[:space:],])(ALL|root|#0)([[:space:],]|$)/ &&
                     users !~ /(^|[[:space:],])!(ALL|root|#0)([[:space:],]|$)/
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      review(line, root_capable)
      next
    }
    previous_rule && /^[[:space:]]+[^[:space:]]/ { partial = 1; exit }
    { previous_rule = 0 }
    END {
      if (partial || deny_all) exit
      for (i = 1; i <= total; i++) {
        path = ordered[i]
        if (denied[path]) continue
        if (++printed > 8) exit
        print path
      }
    }
  ')
  [ -n "$sudo_compose_rules" ] || return 0
  printf '%s\n' "$sudo_compose_rules" | while IFS= read -r sudo_compose_script; do
    case "$sudo_compose_script" in *'//'*|*'/./'*|*'/../'*|*'/.'|*'/..') continue ;; esac
    sudo_python_import_plain_path "$sudo_compose_script" &&
      [ -f "$sudo_compose_script" ] && [ -r "$sudo_compose_script" ] &&
      [ -x "$sudo_compose_script" ] || continue
    sudo_compose_size=$(stat -c %s "$sudo_compose_script" 2>/dev/null) ||
      sudo_compose_size=$(stat -f %z "$sudo_compose_script" 2>/dev/null) || continue
    case "$sudo_compose_size" in ''|*[!0-9]*) continue ;; esac
    [ "$sudo_compose_size" -le 65536 ] || continue
    sudo_compose_shebang=$(dd if="$sudo_compose_script" bs=256 count=1 2>/dev/null | sed -n '1p;1q')
    case "$sudo_compose_shebang" in '#!'*'/python'*|'#!'*'env python'*) ;; *) continue ;; esac
    sudo_compose_match=$(dd if="$sudo_compose_script" bs=65537 count=1 2>/dev/null | LC_ALL=C awk '
      BEGIN { single_quote = sprintf("%c", 39) }
      NR > 400 || length($0) > 2048 { partial = 1; exit }
      /^[[:space:]]*#/ { next }
      /sys\.argv\[1\]/ { argument = 1 }
      (index($0, "/docker-compose\"") || index($0, "/docker-compose" single_quote) ||
       index($0, "\"docker-compose\"") || index($0, single_quote "docker-compose" single_quote)) &&
        /subprocess|Popen|os\.system/ &&
        (index($0, "\"up\"") || index($0, single_quote "up" single_quote)) { compose = 1 }
      END { if (!partial && argument && compose) print "yes" }
    ')
    [ "$sudo_compose_match" = yes ] || continue
    printf 'Sudo Compose wrapper review candidate: %s has a caller-selected YAML sudo rule and invokes Docker Compose up; verify that this YAML reaches Compose, wrapper validation of capabilities, security options and volume syntax, effective sudo policy, and daemon restrictions.\n' "$sudo_compose_script"
  done
}
sudo_compose_wrapper_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"${E}" "s,.*,${SED_RED_YELLOW},"

# A PyInstaller .spec is Python executed at build time. Review only a root-
# capable sudo rule for a small, readable shell wrapper that passes argv[2]
# (directly or through a simple assignment) to PyInstaller. Never run it.
sudo_pyinstaller_wrapper_review() {
  [ -n "$1" ] || return 0
  sudo_pyinstaller_rules=$(printf '%s\n' "$1" | LC_ALL=C awk '
    function review(specs, root_capable, count, commands, i, command, path, args, noexec) {
      if (!root_capable) return
      count = split(specs, commands, ",")
      noexec = 0
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        sub(/[[:space:]]*$/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/) {
          if (command ~ /^NOEXEC:/) noexec = 1
          if (command ~ /^EXEC:/) noexec = 0
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        }
        if (command ~ /^!/) {
          sub(/^![[:space:]]*/, "", command)
          path = command
          sub(/[[:space:]].*$/, "", path)
          if (path == "ALL" || path ~ /\*/) ambiguous = 1
          else denied[path] = 1
          continue
        }
        if (noexec) continue
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/[[:alnum:]_.+\/-]+$/) continue
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        if (args != "" && args != "*") continue
        if (!seen[path]++) ordered[++total] = path
      }
    }
    NR > 3000 || length($0) > 2048 { ambiguous = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      previous_rule = 1
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      root_capable = users ~ /(^|[[:space:],])(ALL|root|#0)([[:space:],]|$)/ &&
                     users !~ /(^|[[:space:],])!(ALL|root|#0)([[:space:],]|$)/
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      review(line, root_capable)
      next
    }
    previous_rule && /^[[:space:]]+[^[:space:]]/ { ambiguous = 1; exit }
    { previous_rule = 0 }
    END {
      if (ambiguous) exit
      for (i = 1; i <= total; i++) {
        path = ordered[i]
        if (denied[path]) continue
        if (++printed > 8) exit
        print path
      }
    }
  ')
  [ -n "$sudo_pyinstaller_rules" ] || return 0
  printf '%s\n' "$sudo_pyinstaller_rules" | while IFS= read -r sudo_pyinstaller_script; do
    case "$sudo_pyinstaller_script" in *'//'*|*'/./'*|*'/../'*|*'/.'|*'/..') continue ;; esac
    sudo_python_import_plain_path "$sudo_pyinstaller_script" &&
      [ -f "$sudo_pyinstaller_script" ] && [ -r "$sudo_pyinstaller_script" ] &&
      [ -x "$sudo_pyinstaller_script" ] || continue
    sudo_pyinstaller_size=$(stat -c %s "$sudo_pyinstaller_script" 2>/dev/null) ||
      sudo_pyinstaller_size=$(stat -f %z "$sudo_pyinstaller_script" 2>/dev/null) || continue
    case "$sudo_pyinstaller_size" in ''|*[!0-9]*) continue ;; esac
    [ "$sudo_pyinstaller_size" -le 65536 ] || continue
    sudo_pyinstaller_shebang=$(dd if="$sudo_pyinstaller_script" bs=256 count=1 2>/dev/null | sed -n '1p;1q')
    case "$sudo_pyinstaller_shebang" in '#!'*'/sh'|'#!'*'/bash'|'#!'*'/dash'|'#!'*'env sh'|'#!'*'env bash') ;; *) continue ;; esac
    sudo_pyinstaller_match=$(dd if="$sudo_pyinstaller_script" bs=65537 count=1 2>/dev/null | LC_ALL=C awk '
      NR > 400 || length($0) > 2048 { partial = 1; exit }
      /^[[:space:]]*#/ { next }
      {
        line = $0
        sub(/^[[:space:]]*/, "", line)
        if (line ~ /^[A-Za-z_][A-Za-z0-9_]*=(\$2|"\$2")([[:space:];]|$)/) {
          alias = line
          sub(/=.*/, "", alias)
        }
        if (line ~ /^(if|elif)[[:space:]]/ &&
            line ~ /(^|[^[:alnum:]_])spec([^[:alnum:]_]|$)/) spec_line = NR
        if (line ~ /^(\/[[:alnum:]_.+\/-]+\/)?pyinstaller[[:space:]]/ &&
            spec_line && NR - spec_line <= 30 &&
            (line ~ /\$2([^0-9]|$)/ ||
             (alias != "" && line ~ ("\\$" alias "([^[:alnum:]_]|$)|\\$\\{" alias "\\}"))))
          build = 1
      }
      END { if (!partial && build) print "yes" }
    ')
    [ "$sudo_pyinstaller_match" = yes ] || continue
    printf 'Sudo PyInstaller wrapper review candidate: %s passes a caller argument to PyInstaller after a spec condition; verify branch reachability, file control, exact sudo policy, build identity, and artifact access.\n' "$sudo_pyinstaller_script"
  done
}
sudo_pyinstaller_wrapper_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"$E" "s,.*,$SED_RED_YELLOW,"

# A SETENV grant for an exact root-run Bash script may let the caller supply
# PATH for an unqualified external command. Read only short, plain scripts;
# never run a sudo rule or follow script-path symlinks.
sudo_setenv_path_review() {
  [ -n "$1" ] || return 0
  case "$1" in *SETENV:*) ;; *) return 0 ;; esac
  sudo_setenv_path_rules=$(printf '%s\n' "$1" | LC_ALL=C awk '
    function review(specs, root_capable, count, commands, i, command, path, args, setenv, noexec) {
      if (!root_capable) return
      count = split(specs, commands, ",")
      setenv = noexec = 0
      for (i = 1; i <= count; i++) {
        command = commands[i]
        sub(/^[[:space:]]*/, "", command)
        sub(/[[:space:]]*$/, "", command)
        while (command ~ /^(NOPASSWD|PASSWD|SETENV|NOSETENV|EXEC|NOEXEC|LOG_INPUT|NOLOG_INPUT|LOG_OUTPUT|NOLOG_OUTPUT):[[:space:]]*/) {
          if (command ~ /^SETENV:/) setenv = 1
          if (command ~ /^NOSETENV:/) setenv = 0
          if (command ~ /^NOEXEC:/) noexec = 1
          if (command ~ /^EXEC:/) noexec = 0
          sub(/^[A-Z_]+:[[:space:]]*/, "", command)
        }
        if (command ~ /^!/) {
          sub(/^![[:space:]]*/, "", command)
          path = command
          sub(/[[:space:]].*$/, "", path)
          if (path == "ALL" || path ~ /\*/) ambiguous = 1
          else denied[path] = 1
          continue
        }
        if (!setenv || noexec) continue
        path = command
        sub(/[[:space:]].*$/, "", path)
        if (path !~ /^\/[[:alnum:]_.+\/-]+$/) continue
        args = substr(command, length(path) + 1)
        sub(/^[[:space:]]+/, "", args)
        sub(/[[:space:]]+$/, "", args)
        if (args != "") continue
        if (!seen[path]++) ordered[++total] = path
      }
    }
    NR > 3000 || length($0) > 2048 { ambiguous = 1; exit }
    /^[[:space:]]*\([^)]*\)[[:space:]]/ {
      previous_rule = 1
      line = $0
      sub(/^[[:space:]]*\(/, "", line)
      runas = line
      sub(/\).*/, "", runas)
      split(runas, parts, ":")
      users = parts[1]
      root_capable = users ~ /(^|[[:space:],])(ALL|root|#0)([[:space:],]|$)/ &&
                     users !~ /(^|[[:space:],])!(ALL|root|#0)([[:space:],]|$)/
      sub(/^[^)]*\)[[:space:]]*/, "", line)
      review(line, root_capable)
      next
    }
    previous_rule && /^[[:space:]]+[^[:space:]]/ { ambiguous = 1; exit }
    { previous_rule = 0 }
    END {
      if (ambiguous) exit
      for (i = 1; i <= total; i++) {
        path = ordered[i]
        if (denied[path]) continue
        if (++printed > 8) exit
        print path
      }
    }
  ')
  [ -n "$sudo_setenv_path_rules" ] || return 0
  printf '%s\n' "$sudo_setenv_path_rules" | while IFS= read -r sudo_setenv_path_script; do
    case "$sudo_setenv_path_script" in *'//'*|*'/./'*|*'/../'*|*'/.'|*'/..') continue ;; esac
    sudo_python_import_plain_path "$sudo_setenv_path_script" &&
      [ -f "$sudo_setenv_path_script" ] && [ -r "$sudo_setenv_path_script" ] &&
      [ -x "$sudo_setenv_path_script" ] || continue
    sudo_setenv_path_size=$(stat -c %s "$sudo_setenv_path_script" 2>/dev/null) ||
      sudo_setenv_path_size=$(stat -f %z "$sudo_setenv_path_script" 2>/dev/null) || continue
    case "$sudo_setenv_path_size" in ''|*[!0-9]*) continue ;; esac
    [ "$sudo_setenv_path_size" -le 65536 ] || continue
    sudo_setenv_path_shebang=$(dd if="$sudo_setenv_path_script" bs=256 count=1 2>/dev/null | sed -n '1p;1q')
    case "$sudo_setenv_path_shebang" in '#!'*'/bash'|'#!'*'env bash') ;; *) continue ;; esac
    sudo_setenv_path_match=$(dd if="$sudo_setenv_path_script" bs=65537 count=1 2>/dev/null | LC_ALL=C awk '
      NR > 200 || length($0) > 2048 { partial = 1; exit }
      /^[[:space:]]*#/ { next }
      {
        line = $0
        sub(/^[[:space:]]*/, "", line)
        if (line ~ /^(find|chown|chmod|cp|mv|rm|tar|rsync|curl|wget|sed|awk|grep|truncate|systemctl|docker)([[:space:]]|$)/)
          bare = 1
        if (line ~ /^enable[[:space:]]+-n[[:space:]]+\[([[:space:];#]|$)/)
          disabled_test = 1
        if (line ~ /^(![[:space:]]*)?\[[[:space:]]/ || line ~ /^(if|elif|while|until)[[:space:]]+(![[:space:]]*)?\[[[:space:]]/)
          test_call = 1
      }
      END {
        if (!partial && bare) print "bare command"
        else if (!partial && disabled_test && test_call) print "disabled [ builtin"
      }
    ')
    [ -n "$sudo_setenv_path_match" ] || continue
    printf 'Sudo SETENV PATH review candidate: %s (%s; confirm effective PATH and script flow, sourced files, denial/NOEXEC policy, writable executable search directory, and command resolution).\n' "$sudo_setenv_path_script" "$sudo_setenv_path_match"
  done
}
sudo_setenv_path_review "$(printf '%s\n%s\n%s\n' "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output")" |
  sed -"$E" "s,.*,$SED_RED_YELLOW,"

(sudo_l_colorize_file /etc/sudoers) 2>/dev/null || echo_not_found "/etc/sudoers"
if ! [ "$IAMROOT" ] && [ -w '/etc/sudoers.d/' ]; then
  echo "You can create a file in /etc/sudoers.d/ and escalate privileges" | sed -${E} "s,.*,${SED_RED_YELLOW},"
fi
for f in /etc/sudoers.d/*; do
  if [ -w "$f" ]; then
    echo "Sudoers file: $f is writable and may allow privilege escalation" | sed -${E} "s,.*,${SED_RED_YELLOW},g"
  fi
  if [ -r "$f" ]; then
    echo "Sudoers file: $f is readable" | sed -${E} "s,.*,${SED_RED},g"
    sudo_l_colorize_file "$f"
  fi
done
echo ""
