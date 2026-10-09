# Title: Users Information - Sudo -l
# ID: UG_Sudo_l
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: Checking 'sudo -l', sudoers files, privileged config and preset loaders, and privileged Python imports, paths, caches, and archive extraction
# License: GNU GPL
# Version: 1.3
# Mitre: T1548.003
# Functions Used: check_sudo_terraform_override, echo_not_found, print_2title, print_info
# Global Variables:$IAMROOT, $PASSWORD, $TIMEOUT, $ROOT_FOLDER, $TMPDIR, $sudoB, $sudoG, $sudoVB1, $sudoVB2
# Initial Functions:
# Generated Global Variables: $sudo_l_output, $sudo_l_password_output, $sudo_l_cached_output, $sudo_adduser_main, $sudo_adduser_dir, $sudo_adduser_groups, $sudo_adduser_candidates, $sudo_adduser_group, $sudo_adduser_group_status, $sudo_adduser_visible, $sudo_adduser_unknown, $sudo_adduser_files, $sudo_adduser_file, $sudo_adduser_policy, $secure_path_candidate, $secure_path_index, $secure_path_entry, $secure_path_remaining, $secure_path_part, $secure_path_walk, $secure_path_symlink, $sudo_needrestart_dir, $sudo_needrestart_config, $sudo_needrestart_timeout, $sudo_needrestart_query, $sudo_npbackup_rules, $sudo_npbackup_dir, $sudo_npbackup_timeout, $sudo_npbackup_count, $sudo_npbackup_started, $sudo_npbackup_now, $sudo_npbackup_command, $sudo_npbackup_config, $sudo_npbackup_query, $sudo_backup_wrapper_rules, $sudo_backup_wrapper_script, $sudo_backup_wrapper_remaining, $sudo_backup_wrapper_walk, $sudo_backup_wrapper_part, $sudo_backup_wrapper_symlink, $sudo_backup_wrapper_result, $sudo_python_scripts, $python_sudo_script, $python_loader_lines, $python_loader_root, $python_loader_roots, $python_loader_path_lines, $python_loader_literal, $python_loader_parent, $python_candidate_dir, $python_seen_dirs, $python_pth_file, $python_pth_imports, $python_writable_pth, $python_script_dir, $sudo_python_import_rules, $sudo_python_import_script, $sudo_python_import_runas, $sudo_python_import_size, $sudo_python_import_shebang, $sudo_python_import_dir, $sudo_python_import_line, $sudo_python_import_kind, $sudo_python_import_name, $sudo_python_import_member, $sudo_python_import_statement, $sudo_python_import_candidate, $sudo_python_import_parent, $sudo_python_import_access, $sudo_python_import_sticky, $sudo_python_import_walk, $sudo_python_import_remaining, $sudo_python_import_part, $sudo_python_import_probe_count, $sudo_python_cache_scripts, $sudo_python_cache_script, $sudo_python_cache_size, $sudo_python_cache_shebang, $sudo_python_cache_interpreter, $sudo_python_cache_version, $sudo_python_cache_tag, $sudo_python_cache_dir, $sudo_python_cache_line, $sudo_python_cache_module, $sudo_python_cache_source, $sudo_python_cache_pyc, $sudo_python_cache_sticky, $sudo_python_cache_owner, $sudo_python_cache_access, $sudo_python_tar_commands, $sudo_python_tar_command, $sudo_python_binary, $sudo_python_version, $sudo_python_tar_dir, $sudo_python_tar_size
# Fat linpeas: 0
# Small linpeas: 1


print_2title "Checking 'sudo -l', sudoers files, and privileged Python paths" "T1548.003"
print_info "https://book.hacktricks.wiki/en/linux-hardening/linux-basics/linux-privilege-escalation/index.html#sudo-and-suid"

sudo_l_colorize() {
  sed "s,_proxy,${SED_RED},g" | sed "s,$sudoG,${SED_GREEN},g" | sed -${E} "s,$sudoVB1,${SED_RED_YELLOW}," | sed -${E} "s,$sudoVB2,${SED_RED_YELLOW}," | sed -${E} "s,$sudoB,${SED_RED},g"
}

sudo_l_colorize_output() {
  printf "%s\n" "$1" | sudo_l_colorize | sed "s,\!root,${SED_RED},"
}

sudo_l_colorize_file() {
  grep -Iv "^$" "$1" | grep -v "#" | sudo_l_colorize | sed "s,pwfeedback,${SED_RED},g"
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

# A bare root-capable BBOT grant permits caller-selected preset arguments.
# Presets may name Python module directories; importing a module is the risk.
# Review only captured sudo policy text. Never run BBOT or load a preset.
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
        continue
      }
      if (command ~ /^\/([[:alnum:]_.-]+\/)*bbot$/) found = 1
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
  }
' | sed -"${E}" "s,.*,${SED_RED_YELLOW},"

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
