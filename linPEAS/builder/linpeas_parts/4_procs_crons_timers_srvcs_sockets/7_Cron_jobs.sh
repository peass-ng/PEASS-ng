# Title: Processes & Cron & Services & Timers - Cron jobs and Wildcards
# ID: PR_Cron_jobs
# Author: Carlos Polop
# Last Update: 2024-03-19
# Description: Enumerate system cron jobs and check for privilege escalation vectors
# License: GNU GPL
# Version: 1.2
# Mitre: T1053.003
# Functions Used: check_pg_basebackup_boundary, echo_not_found, print_2title, print_3title, print_info
# Global Variables: $cronjobsG, $nosh_usrs, $SEARCH_IN_FOLDER, $sh_usrs, $USER, $Wfolders, $cronjobsB, $PATH, $PG_BASEBACKUP_DESTS
# Initial Functions:
# Generated Global Variables: $cmd, $VAR, $file, $path, $user_crontab, $username, $job_id, $cron_dir, $crontab, $findings, $line, $finding, $bin, $cron_log_timeout, $cron_log_status, $files, $cron_file, $prefix, $spool, $bash, $script, $log, $parent, $safe, $candidate, $rest, $part, $route, $mode, $sticky, $cron_tar_timeout, $cron_tar_status, $current_uid, $schedule, $spool_owner, $runas, $helper, $schedule_line, $owner_uid, $helper_text, $dir, $tar_cmd, $helper_line, $version, $cron_process_timeout, $cron_process_status, $magick_cwd, $magick_bin, $magick_cd_line, $magick_exec_line, $magick_marker, $cron_replace_status, $cron_replace_timeout, $cron_ansible_status, $cron_ansible_timeout, $glob, $depth
# Fat linpeas: 0
# Small linpeas: 1

# Correlate a literal root cron playbook glob with a directory the current
# user can populate. Never expand the glob, inspect playbooks, or run Ansible.
cron_ansible_glob_probe() {
  cron_ansible_timeout=$(command -v timeout 2>/dev/null || command -v gtimeout 2>/dev/null)
  [ -n "$cron_ansible_timeout" ] || return 0
  "$cron_ansible_timeout" 3 sh -c '
    LC_ALL=C; export LC_ALL
    [ "$(id -u 2>/dev/null)" != 0 ] || exit 0
    files=0
    for cron_file do
      files=$((files + 1))
      if [ "$files" -gt 12 ]; then
        echo "Cron Ansible playbook correlation: incomplete (12-file limit)."
        break
      fi
      [ -f "$cron_file" ] && [ -r "$cron_file" ] && [ ! -L "$cron_file" ] || continue
      schedule=$(dd if="$cron_file" bs=8193 count=1 2>/dev/null) || continue
      if [ "${#schedule}" -ge 8192 ]; then
        echo "Cron Ansible playbook correlation: incomplete (8 KiB schedule limit): $cron_file"
        continue
      fi
      case "$schedule" in *ansible-parallel*) ;; *) continue ;; esac
      case "$cron_file" in */spool/cron/root|*/spool/cron/crontabs/root) spool=1 ;; *) spool=0 ;; esac
      printf "%s\n" "$schedule" | awk -v spool="$spool" '\''
        NR > 32 || length($0) > 512 { partial = 1; next }
        /^[[:space:]]*(#|$)/ { next }
        {
          for (i = 1; i <= 5; i++) if ($i !~ /^[0-9*,\/-]+$/) next
          if (spool) {
            if (NF != 7) next
            helper = $6; glob = $7
          } else {
            if (NF != 8 || $6 != "root") next
            helper = $7; glob = $8
          }
          if (helper !~ /^\/[A-Za-z0-9_.\/+\-]+\/ansible-parallel$/ ||
              glob !~ /^\/[A-Za-z0-9_.\/+\-]+\/\*\.(yml|yaml)$/) next
          print glob "|" NR
        }
        END { if (partial) print "#PARTIAL" }
      '\'' | while IFS="|" read -r glob schedule_line; do
        if [ "$glob" = "#PARTIAL" ]; then
          echo "Cron Ansible playbook correlation: incomplete (line/column limit): $cron_file"
          continue
        fi
        case "$glob" in *"/../"*|*"/./"*|*"//"*) continue ;; esac
        dir=${glob%/*}
        [ -d "$dir" ] && [ -w "$dir" ] && [ -x "$dir" ] || continue
        path=; rest=${dir#/}; safe=1; depth=0
        while [ -n "$rest" ]; do
          depth=$((depth + 1))
          [ "$depth" -le 16 ] || { safe=0; break; }
          part=${rest%%/*}; path=$path/$part
          if [ -L "$path" ]; then safe=0; break; fi
          case "$rest" in */*) rest=${rest#*/} ;; *) rest= ;; esac
        done
        [ "$safe" -eq 1 ] || continue
        mode=$(ls -ld "$dir" 2>/dev/null) || continue
        mode=${mode%% *}
        case $(printf "%s" "$mode" | cut -c 10) in t|T) continue ;; esac
        echo "Cron Ansible playbook review candidate: $cron_file:$schedule_line (root cron passes $glob to helper; current user can create entries in $dir; verify helper behavior, ACLs, mount policy, and scheduler state)"
      done
    done
  ' sh "$@"
  cron_ansible_status=$?
  case "$cron_ansible_status" in
    124|137) echo "Cron Ansible playbook correlation: incomplete (3-second timeout)." ;;
    0) ;;
    *) echo "Cron Ansible playbook correlation: incomplete (metadata error)." ;;
  esac
}

# A read-only cron script may still be replaceable through its parent. Inspect
# only literal shell-script paths in a few visible cron files; no job is run.
cron_replaceable_script_probe() {
  cron_replace_timeout=$(command -v timeout 2>/dev/null || command -v gtimeout 2>/dev/null)
  [ -n "$cron_replace_timeout" ] || return 0
  "$cron_replace_timeout" 3 sh -c '
    LC_ALL=C; export LC_ALL
    current_uid=$(id -u 2>/dev/null) || exit 1
    files=0
    for cron_file do
      files=$((files + 1))
      if [ "$files" -gt 12 ]; then
        echo "Cron script replacement review incomplete (12-file limit)."
        break
      fi
      [ -f "$cron_file" ] && [ -r "$cron_file" ] && [ ! -L "$cron_file" ] || continue
      schedule=$(dd if="$cron_file" bs=8193 count=1 2>/dev/null) || continue
      if [ "${#schedule}" -ge 8192 ]; then
        echo "Cron script replacement review incomplete (8 KiB schedule limit): $cron_file"
        continue
      fi
      spool_owner=
      case "$cron_file" in */spool/cron/root|*/spool/cron/crontabs/root) spool_owner=root ;; esac
      printf "%s\n" "$schedule" | awk -v owner="$spool_owner" '\''
        NR > 32 || length($0) > 512 { partial = 1; next }
        /^[[:space:]]*(#|$)/ { next }
        {
          for (i = 1; i <= 5; i++) if ($i !~ /^[0-9*,\/-]+$/) next
          if (owner == "") {
            if (NF != 8) next
            runas = $6; shell = $7; script = $8
          } else {
            if (NF != 7) next
            runas = owner; shell = $6; script = $7
          }
          if (runas !~ /^[A-Za-z_][A-Za-z0-9_-]*$/ ||
              (shell != "/bin/sh" && shell != "/usr/bin/sh" &&
               shell != "/bin/bash" && shell != "/usr/bin/bash") ||
              script !~ /^\/[A-Za-z0-9_\/.+-]+$/) next
          print runas "|" script "|" NR
        }
        END { if (partial) print "#PARTIAL" }
      '\'' | while IFS="|" read -r runas script schedule_line; do
        if [ "$runas" = "#PARTIAL" ]; then
          echo "Cron script replacement review incomplete (line/column limit): $cron_file"
          continue
        fi
        case "$script" in *"/../"*|*"/.."|*"/./"*|*"/."|*"//"*) continue ;; esac
        owner_uid=$(id -u "$runas" 2>/dev/null) || continue
        [ "$owner_uid" != "$current_uid" ] || continue
        [ -f "$script" ] && [ ! -L "$script" ] && [ ! -w "$script" ] || continue
        parent=${script%/*}; [ -n "$parent" ] || parent=/
        [ -d "$parent" ] && [ -w "$parent" ] && [ -x "$parent" ] || continue
        path=; rest=${script#/}; safe=1
        while [ -n "$rest" ]; do
          part=${rest%%/*}; path=$path/$part
          if [ -L "$path" ]; then safe=0; break; fi
          case "$rest" in */*) rest=${rest#*/} ;; *) rest= ;; esac
        done
        [ "$safe" -eq 1 ] || continue
        mode=$(ls -ld "$parent" 2>/dev/null) || continue
        mode=${mode%% *}
        sticky=$(printf "%s" "$mode" | cut -c 10)
        case "$sticky" in t|T) continue ;; esac
        echo "Cron script replacement review candidate: $cron_file:$schedule_line ($runas runs $script; current user can replace its directory entry in $parent; verify ACLs, mount policy, and scheduler state)"
      done
    done
  ' sh "$@"
  cron_replace_status=$?
  case "$cron_replace_status" in
    124|137) echo 'Cron script replacement review incomplete (3-second timeout).' ;;
    0) ;;
    *) echo 'Cron script replacement review incomplete (metadata error).' ;;
  esac
}

# Inspect only literal, visible root cron commands. The whole probe has a wall-clock
# limit; without timeout, filesystem metadata on an unresponsive mount is unbounded.
cron_log_input_probe() {
  cron_log_timeout=$(command -v timeout 2>/dev/null || command -v gtimeout 2>/dev/null)
  if [ -z "$cron_log_timeout" ]; then
    echo "Cron log input correlation: unknown (timeout unavailable)."
    return
  fi
  "$cron_log_timeout" 3 sh -c '
    LC_ALL=C; export LC_ALL
    files=0
    for cron_file do
      files=$((files + 1))
      if [ "$files" -gt 8 ]; then
        echo "Cron log input correlation: truncated at 8 cron files."
        break
      fi
      if [ -L "$cron_file" ] || [ ! -f "$cron_file" ] || [ ! -r "$cron_file" ]; then
        continue
      fi
      prefix=$(dd if="$cron_file" bs=8193 count=1 2>/dev/null) || continue
      if [ "${#prefix}" -ge 8192 ]; then
        echo "Cron log input correlation: skipped oversized cron file: $cron_file"
        continue
      fi
      case "$cron_file" in
        */root) spool=1 ;;
        *) spool=0 ;;
      esac
      printf "%s\n" "$prefix" | awk -v spool="$spool" '\''NR <= 16 && length($0) <= 512 && $0 !~ /^[[:space:]]*(#|$)/ {
        for (i=1; i<=5; i++) if ($i !~ /^[0-9*,\/-]+$/) next
        if (spool == 0) {
          if (NF != 9 || $6 != "root") next
          bash=$7; script=$8; input=$9
        } else {
          if (NF != 8) next
          bash=$6; script=$7; input=$8
        }
        if (bash != "/bin/bash" && bash != "/usr/bin/bash") next
        if (script !~ /^\/[A-Za-z0-9_.\/-]+$/ || input !~ /^\/[A-Za-z0-9_.\/-]+\.log$/) next
        print bash "|" script "|" input
      }
      END { if (NR > 16) print "#TRUNCATED" }'\'' | while IFS="|" read -r bash script log; do
        if [ "$bash" = "#TRUNCATED" ]; then
          echo "Cron log input correlation: truncated at 16 lines: $cron_file"
          continue
        fi
        case "$script:$log" in *"/../"*|*"/..:"*|*"/./"*|*"//"*) continue ;; esac
        [ -L "$script" ] && continue
        [ -f "$script" ] && [ -r "$script" ] || continue
        [ -L "$log" ] && continue
        [ -f "$log" ] || continue
        parent=${log%/*}; [ -n "$parent" ] || parent=/
        [ -d "$parent" ] || continue
        # Reject symlinks in every ancestor; metadata otherwise describes a
        # different path than the one the privileged command would resolve.
        safe=1
        for candidate in "$script" "$log"; do
          path=; rest=${candidate#/}
          while [ -n "$rest" ]; do
            part=${rest%%/*}; path=$path/$part
            if [ -L "$path" ]; then safe=0; break; fi
            case "$rest" in */*) rest=${rest#*/} ;; *) rest= ;; esac
          done
          [ "$safe" -eq 1 ] || break
        done
        [ "$safe" -eq 1 ] || continue
        route=
        if [ -w "$log" ]; then route="current user can write input file"; fi
        if [ -w "$parent" ] && [ -x "$parent" ]; then
          mode=$(ls -ld "$parent" 2>/dev/null) || continue
          mode=${mode%% *}
          sticky=$(printf "%s" "$mode" | cut -c 10)
          case "$sticky" in t|T) ;; *) route="current user can replace directory entry" ;; esac
        fi
        [ -n "$route" ] || continue
        echo "Cron log input review candidate: $cron_file"
        echo "  Root command: $bash $script $log"
        echo "  Input: $log ($route)"
        ls -ld "$parent" "$log" 2>/dev/null
        echo "  Metadata only; parser behavior and ACL/mount policy need review."
      done
    done
  ' sh "$@"
  cron_log_status=$?
  case "$cron_log_status" in
    124|137) echo "Cron log input correlation: timed out; visibility unknown." ;;
    0) ;;
    *) echo "Cron log input correlation: incomplete; visibility unknown." ;;
  esac
}

# Correlate a visible cron identity with one literal helper and a GNU tar glob.
# This intentionally recognizes only simple, adjacent `cd DIR` and `tar -cf
# ARCHIVE *` lines. Shell expansion, other archivers, and nested helpers need
# manual review; no scheduled command or archive operation is run here.
cron_tar_wildcard_probe() {
  cron_tar_timeout=$(command -v timeout 2>/dev/null || command -v gtimeout 2>/dev/null)
  if [ -z "$cron_tar_timeout" ]; then
    echo "Cron tar wildcard correlation: unknown (timeout unavailable)."
    return
  fi
  "$cron_tar_timeout" 5 sh -c '
    LC_ALL=C; export LC_ALL
    current_uid=$(id -u 2>/dev/null) || exit 1
    [ "$current_uid" -ne 0 ] || exit 0
    files=0
    for cron_file do
      files=$((files + 1))
      if [ "$files" -gt 12 ]; then
        echo "Cron tar wildcard correlation: unknown beyond 12 schedule files."
        break
      fi
      [ -f "$cron_file" ] && [ -r "$cron_file" ] && [ ! -L "$cron_file" ] || continue
      schedule=$(dd if="$cron_file" bs=8193 count=1 2>/dev/null) || continue
      if [ "${#schedule}" -ge 8192 ]; then
        echo "Cron tar wildcard correlation: unknown (oversized schedule: $cron_file)."
        continue
      fi
      spool_owner=
      case "$cron_file" in */spool/cron/*) spool_owner=${cron_file##*/} ;; esac
      printf "%s\n" "$schedule" | awk -v owner="$spool_owner" '\''
        NR > 32 { truncated=1; next }
        length($0) > 512 { truncated=1; next }
        /^[[:space:]]*(#|$)/ { next }
        {
          for (i=1; i<=5; i++) if ($i !~ /^[0-9*,\/-]+$/) next
          if (owner == "") {
            if (NF != 8) next
            runas=$6; interpreter=$7; helper=$8
          } else {
            if (NF != 7) next
            runas=owner; interpreter=$6; helper=$7
          }
          if (interpreter != "/bin/sh" && interpreter != "/usr/bin/sh" &&
              interpreter != "/bin/bash" && interpreter != "/usr/bin/bash") next
          if (helper ~ /^\/[A-Za-z0-9_.\/-]+$/) print runas "|" helper "|" NR
        }
        END { if (truncated) print "#TRUNCATED" }
      '\'' | while IFS="|" read -r runas helper schedule_line; do
        if [ "$runas" = "#TRUNCATED" ]; then
          echo "Cron tar wildcard correlation: unknown beyond schedule line/length cap: $cron_file."
          continue
        fi
        case "$runas" in ""|*[!A-Za-z0-9_-]*) continue ;; esac
        case "$helper" in *"/../"*|*"/.."|*"/./"*|*"/."|*"//"*) continue ;; esac
        owner_uid=$(id -u "$runas" 2>/dev/null) || continue
        [ "$owner_uid" != "$current_uid" ] || continue
        [ -f "$helper" ] && [ -r "$helper" ] && [ ! -L "$helper" ] || continue
        helper_text=$(dd if="$helper" bs=4097 count=1 2>/dev/null) || continue
        if [ "${#helper_text}" -ge 4096 ]; then
          echo "Cron tar wildcard correlation: unknown (oversized helper: $helper)."
          continue
        fi
        printf "%s\n" "$helper_text" | awk '\''
          NR > 24 { truncated=1; next }
          length($0) > 512 { truncated=1; next }
          /^[[:space:]]*(#|$)/ { next }
          {
            if (NF == 2 && $1 == "cd" && $2 ~ /^\/[A-Za-z0-9_.\/-]+$/) {
              dir=$2; next
            }
            if (dir != "" && NF == 4 &&
                ($1 == "tar" || $1 == "/usr/bin/tar" || $1 == "/bin/tar" || $1 == "gtar") &&
                $2 ~ /^-[A-Za-z]+$/ && $2 ~ /c/ && $2 ~ /f/ &&
                $3 ~ /^[A-Za-z0-9_.\/-]+$/ && $4 == "*") {
              print dir "|" $1 "|" NR
            }
            dir=""
          }
          END { if (truncated) print "#TRUNCATED" }
        '\'' | while IFS="|" read -r dir tar_cmd helper_line; do
          if [ "$dir" = "#TRUNCATED" ]; then
            echo "Cron tar wildcard correlation: unknown beyond helper line/length cap: $helper."
            continue
          fi
          case "$dir" in *"/../"*|*"/.."|*"/./"*|*"/."|*"//"*) continue ;; esac
          [ -d "$dir" ] && [ -w "$dir" ] && [ -x "$dir" ] && [ ! -L "$dir" ] || continue
          version=$($tar_cmd --version 2>/dev/null | awk '\''NR == 1 { print; exit }'\'')
          case "$version" in *"GNU tar"*) ;; *) continue ;; esac
          echo "Cron GNU tar wildcard review candidate: $cron_file:$schedule_line"
          echo "  Schedule owner: $runas (uid $owner_uid); helper: $helper:$helper_line"
          echo "  Working directory: $dir (current user can write and enter)"
          echo "  Command: $tar_cmd archive * (unquoted glob, no --); GNU tar version observed"
          echo "  Candidate only; execution context and file ownership need review."
        done
      done
    done
  ' sh "$@"
  cron_tar_status=$?
  case "$cron_tar_status" in
    124|137) echo "Cron tar wildcard correlation: timed out; visibility unknown." ;;
    0) ;;
    *) echo "Cron tar wildcard correlation: incomplete; visibility unknown." ;;
  esac
}

# Read only literal shell helpers in visible root cron entries. This is a
# conservative text correlation, not a shell parser or proof of exploitability.
cron_process_args_probe() {
  cron_process_timeout=$(command -v timeout 2>/dev/null || command -v gtimeout 2>/dev/null)
  if [ -z "$cron_process_timeout" ]; then
    echo "Cron process arguments correlation: unknown (timeout unavailable)."
    return
  fi
  "$cron_process_timeout" 5 sh -c '
    LC_ALL=C; export LC_ALL
    files=0
    for cron_file do
      files=$((files + 1))
      if [ "$files" -gt 6 ]; then
        echo "Cron process arguments correlation: unknown beyond 6 schedule files."
        break
      fi
      [ -e "$cron_file" ] || continue
      if [ -L "$cron_file" ] || [ ! -f "$cron_file" ] || [ ! -r "$cron_file" ]; then
        echo "Cron process arguments correlation: unknown (unreadable schedule: $cron_file)."
        continue
      fi
      schedule=$(dd if="$cron_file" bs=8193 count=1 2>/dev/null) || {
        echo "Cron process arguments correlation: unknown (unreadable schedule: $cron_file)."
        continue
      }
      if [ "${#schedule}" -ge 8192 ]; then
        echo "Cron process arguments correlation: unknown (oversized schedule: $cron_file)."
        continue
      fi
      spool_owner=
      case "$cron_file" in */spool/cron/root|*/spool/cron/crontabs/root) spool_owner=root ;; esac
      printf "%s\n" "$schedule" | awk -v owner="$spool_owner" '\''
        NR > 32 { truncated=1; next }
        length($0) > 512 { truncated=1; next }
        /^[[:space:]]*(#|$)/ { next }
        {
          for (i=1; i<=5; i++) if ($i !~ /^[0-9*,\/-]+$/) next
          if (owner == "") {
            if (NF != 8 || $6 != "root") next
            interpreter=$7; helper=$8
          } else {
            if (NF != 7) next
            interpreter=$6; helper=$7
          }
          if (interpreter != "/bin/sh" && interpreter != "/usr/bin/sh" &&
              interpreter != "/bin/bash" && interpreter != "/usr/bin/bash") next
          if (helper !~ /^\/[A-Za-z0-9_.\/-]+$/ ||
              helper ~ /\/\.\.?($|\/)/ || helper ~ /\/\//) next
          if (++helpers > 2) { capped=1; next }
          print helper "|" NR
        }
        END {
          if (truncated) print "#LINES"
          if (capped) print "#HELPERS"
        }
      '\'' | while IFS="|" read -r helper schedule_line; do
        case "$helper" in
          "#LINES") echo "Cron process arguments correlation: unknown beyond schedule line/length cap: $cron_file."; continue ;;
          "#HELPERS") echo "Cron process arguments correlation: unknown beyond 2 helpers: $cron_file."; continue ;;
        esac
        if [ -L "$helper" ] || [ ! -f "$helper" ] || [ ! -r "$helper" ]; then
          echo "Cron process arguments correlation: unknown (unreadable helper: $helper)."
          continue
        fi
        helper_text=$(dd if="$helper" bs=4097 count=1 2>/dev/null) || {
          echo "Cron process arguments correlation: unknown (unreadable helper: $helper)."
          continue
        }
        if [ "${#helper_text}" -ge 4096 ]; then
          echo "Cron process arguments correlation: unknown (oversized helper: $helper)."
          continue
        fi
        printf "%s\n" "$helper_text" | awk -v cron="$cron_file" -v schedule_line="$schedule_line" -v helper="$helper" '\''
          NR > 32 { truncated=1; next }
          length($0) > 512 { truncated=1; next }
          /^[[:space:]]*(#|$)/ { next }
          {
            line=$0
            if (line ~ /pgrep[[:space:]]+(-[A-Za-z]*f[A-Za-z]*|--full)([[:space:]]|$)/ &&
                line ~ /[[:space:]]read[[:space:]]/) {
              capture=line
              sub(/^.*[[:space:]]read[[:space:]]+/, "", capture)
              sub(/^-r[[:space:]]+/, "", capture)
              sub(/[;|].*$/, "", capture)
              n=split(capture, fields, /[[:space:]]+/)
              if (fields[n] ~ /^[A-Za-z_][A-Za-z0-9_]*$/) {
                input=fields[n]; select_line=NR
                derived=""; derive_line=0; execute_line=0; apache=0; config=0
              }
            }
            if (input != "" && line ~ /^[[:space:]]*[A-Za-z_][A-Za-z0-9_]*=/ &&
                index(line, "$" input) && line ~ /sed|\/\//) {
              derived=line
              sub(/^[[:space:]]*/, "", derived)
              sub(/=.*/, "", derived)
              if (derived ~ /^[A-Za-z_][A-Za-z0-9_]*$/) {
                derive_line=NR
                if (line ~ /apache2ctl|apachectl|httpd/) apache=1
                if (line ~ /[[:space:]]-t([^A-Za-z0-9_-]|$)/) config=1
              }
            }
            if (derived != "" && index(line, "$" derived) &&
                line ~ /[[:space:]]-t([^A-Za-z0-9_-]|$)/) config=1
            if (derived != "" && line ~ /^[[:space:]]*\$[A-Za-z_][A-Za-z0-9_]*/ && NR > derive_line) {
              executed=line
              sub(/^[[:space:]]*\$/, "", executed)
              sub(/[^A-Za-z0-9_].*$/, "", executed)
              if (executed == derived) execute_line=NR
            }
          }
          END {
            if (truncated) {
              print "Cron process arguments correlation: unknown beyond helper line/length cap: " helper "."
            } else if (select_line && derive_line && execute_line) {
              print "Cron process arguments review candidate: " cron ":" schedule_line
              print "  Schedule owner: root; helper: " helper
              print "  Full-args pgrep/read: helper line " select_line "; derived command: line " derive_line "; execution: line " execute_line
              if (apache && config) print "  Apache config-test option flow observed; review process identity and config inputs."
              print "  Candidate only; process arguments are unauthenticated input."
            }
          }
        '\''
        # Reuse the same bounded helper text for the Linux AppImage search-path
        # lead. No image tool is executed: a crafted CWD could load code on run.
        if [ "$(uname -s 2>/dev/null)" = Linux ]; then
          printf "%s\n" "$helper_text" | awk '\''
            NR > 32 || length($0) > 512 { exit }
            /^[[:space:]]*(#|$)/ { next }
            {
              line=$0
              sub(/^[[:space:]]+/, "", line)
              sub(/[[:space:]]+#.*$/, "", line)
              if (line ~ /^cd[[:space:]]+/) {
                cwd=line
                sub(/^cd[[:space:]]+/, "", cwd)
                sub(/[[:space:]]+$/, "", cwd)
                if (cwd ~ /^\/[A-Za-z0-9_.\/-]+$/ &&
                    cwd !~ /\/\.\.?($|\/)/ && cwd !~ /\/\//) {
                  cd_line=NR
                } else { cwd=""; cd_line=0 }
                next
              }
              if (!cwd) next
              n=split(line, words, /[[:space:]]+/)
              for (i=1; i<n; i++) {
                if (words[i] ~ /^\/[A-Za-z0-9_.\/-]+\/magick$/ &&
                    words[i+1] == "identify" &&
                    (i == 1 || words[i-1] == "xargs")) {
                  print cwd "|" words[i] "|" cd_line "|" NR
                  exit
                }
              }
            }
          '\'' | while IFS="|" read -r magick_cwd magick_bin magick_cd_line magick_exec_line; do
            [ -d "$magick_cwd" ] && [ -w "$magick_cwd" ] && [ -x "$magick_cwd" ] || continue
            [ -f "$magick_bin" ] && [ -x "$magick_bin" ] || continue
            magick_marker=""
            if [ -r "$magick_bin" ] && command -v od >/dev/null 2>&1; then
              magick_marker=$(dd if="$magick_bin" bs=1 skip=8 count=3 2>/dev/null | od -An -tx1 2>/dev/null | tr -d "[:space:]")
            fi
            echo "Cron ImageMagick working-directory review candidate: $cron_file:$schedule_line"
            echo "  Schedule owner: root; helper: $helper (cd line $magick_cd_line; identify line $magick_exec_line)"
            echo "  Caller-writable working directory: $magick_cwd; executable: $magick_bin"
            if [ "$magick_marker" = 414902 ]; then
              echo "  AppImage type-2 marker found; verify affected ImageMagick AppRun search paths and version."
            else
              echo "  AppImage format not confirmed; native builds and safe search paths may be unaffected."
            fi
            echo "  Passive correlation only; cron execution, loader paths, and mount/ACL policy need review."
          done
        fi
      done
    done
  ' sh "$@"
  cron_process_status=$?
  case "$cron_process_status" in
    124|137) echo "Cron process arguments correlation: timed out; visibility unknown." ;;
    0) ;;
    *) echo "Cron process arguments correlation: incomplete; visibility unknown." ;;
  esac
}

if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Check for vulnerable cron jobs" "T1053.003"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/processes-crontab-systemd-dbus/cron-and-systemd-timers.html#enumerate-schedules"

  print_3title "Cron jobs list" "T1053.003"
  command -v crontab 2>/dev/null || echo_not_found "crontab"
  crontab -l 2>/dev/null | tr -d "\r" | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed "s,root,${SED_RED},"
  command -v incrontab 2>/dev/null || echo_not_found "incrontab"
  incrontab -l 2>/dev/null
  ls -alR /etc/cron* /var/spool/cron/crontabs /var/spool/anacron 2>/dev/null | sed -${E} "s,$cronjobsG,${SED_GREEN},g" | sed "s,$cronjobsB,${SED_RED},g"
  cat /etc/cron* /etc/at* /etc/anacrontab /var/spool/cron/crontabs/* /etc/incron.d/* /var/spool/incron/* 2>/dev/null | tr -d "\r" | grep -v "^#" | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed "s,$USER,${SED_LIGHT_MAGENTA}," | sed -${E} "s,$nosh_usrs,${SED_BLUE},"  | sed "s,root,${SED_RED},"
  grep -Hn '^PATH=' /etc/crontab /etc/cron.d/* 2>/dev/null | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
  grep -RInE 'pg_basebackup|run-parts|crontab-ui' /etc/crontab /etc/cron.d /etc/anacrontab /var/spool/cron/crontabs /etc/incron.d /var/spool/incron 2>/dev/null | sed -${E} "s,$cronjobsB,${SED_RED},g" | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
  print_3title "Root cron literal log inputs (passive review)" "T1053.003"
  cron_log_input_probe /etc/crontab /etc/cron.d/* /var/spool/cron/crontabs/root /var/spool/cron/root
  cron_replaceable_script_probe /etc/crontab /etc/cron.d/* /var/spool/cron/crontabs/root /var/spool/cron/root
  cron_ansible_glob_probe /etc/crontab /etc/cron.d/* /var/spool/cron/crontabs/root /var/spool/cron/root
  echo "Only readable root entries were inspected; private crontabs and other schedules may be invisible."
  print_3title "GNU tar cron wildcard inputs (passive review)" "T1053.003"
  cron_tar_wildcard_probe /etc/crontab /etc/cron.d/* /var/spool/cron/crontabs/* /var/spool/cron/*
  echo "Only readable cron schedules were inspected; private tasks remain unknown."
  print_3title "Root cron process arguments (passive review)" "T1053.003"
  cron_process_args_probe /etc/crontab /etc/cron.d/* /var/spool/cron/crontabs/root /var/spool/cron/root
  echo "Only literal, readable root cron helpers were inspected; private tasks remain unknown."
  PG_BASEBACKUP_DESTS=
  check_pg_basebackup_boundary
  crontab -l -u "$USER" 2>/dev/null | tr -d "\r"
  ls -lR /usr/lib/cron/tabs/ /private/var/at/jobs /var/at/tabs/ /etc/periodic/ 2>/dev/null | sed -${E} "s,$cronjobsG,${SED_GREEN},g" | sed "s,$cronjobsB,${SED_RED},g" #MacOS paths
  atq 2>/dev/null
  echo ""

  print_3title "Cron files with hidden carriage returns" "T1053.003"
  grep -IRl $'\r' /etc/crontab /etc/cron.d /var/spool/cron/crontabs 2>/dev/null | while read -r file; do
    [ -n "$file" ] || continue
    echo "$file" | sed -${E} "s,.*,${SED_RED},g"
    sed -n 'l' "$file" 2>/dev/null | head -n 20
  done
  echo ""

  print_3title "Checking for specific cron jobs vulnerabilities" "T1053.003"
  # Function to check if a binary is writable and executable
  check_binary_perms() {
    local bin="$1"
    [ -z "$bin" ] && return
    
    # Skip if binary doesn't exist
    [ ! -e "$bin" ] && return
    
    # Check if it's a regular file
    [ ! -f "$bin" ] && return
    
    # Check if it's writable and executable
    if [ -w "$bin" ]; then
      echo "Writable binary: $bin"
      ls -l "$bin" 2>/dev/null
    fi
  }

  # Function to extract binary path from command
  get_binary_path() {
    local cmd="$1"
    local bin=""
    
    # Try to get the first word of the command
    bin=$(echo "$cmd" | awk '{print $1}')
    [ -z "$bin" ] && return
    
    # If it's an absolute path, use it directly
    if [ "$(echo "$bin" | cut -c1)" = "/" ]; then
      echo "$bin"
      return
    fi
    
    # If it's a relative path, try to resolve it
    if [ -e "$bin" ]; then
      echo "$(pwd)/$bin"
      return
    fi
    
    # Try to find it in PATH
    for path in $(echo "$PATH" | tr ':' ' '); do
      if [ -x "$path/$bin" ]; then
        echo "$path/$bin"
        return
      fi
    done
  }

  # Function to check for privilege escalation vectors in a command
  check_privesc_vectors() {
    local cmd="$1"
    local file="$2"
    local findings=""
    local bin=""

    # Skip common false positives (mail commands, shell conditionals, variable assignments)
    if echo "$cmd" | grep -qE '^(mail|echo|then|else|fi|if|for|while|do|done|case|esac|exit|return|break|continue|:|\[|test|\[\[|\]\]|true|false|source|\.|cd|pwd|export|unset|readonly|local|declare|typeset|alias|unalias|set|unset|shift|wait|trap|umask|ulimit|exec|eval|command|builtin|let|read|printf|^[[:space:]]*[A-Za-z0-9_]+[[:space:]]*[=:])'; then
      return
    fi

    # Get the binary path
    bin=$(get_binary_path "$cmd")
    if [ -n "$bin" ]; then
      check_binary_perms "$bin"
    fi

    # Check for wildcard injection vectors
    # Attack: Using wildcards in tar/chmod/chown to execute arbitrary commands
    # Example: tar cf archive.tar * (where * expands to --checkpoint=1 --checkpoint-action=exec=sh)
    if echo "$cmd" | grep -qE '\*'; then
      findings="${findings}POTENTIAL_WILDCARD_INJECTION: Command uses wildcards with potentially exploitable command\n"
    fi

    # Check for path hijacking vectors
    # Attack: Using relative paths or commands without full path that can be hijacked
    # Example: script.sh instead of /usr/bin/script.sh
    if echo "$cmd" | grep -qE '^[[:space:]]*[^/][^[:space:]]*[[:space:]]'; then
      # Skip common false positives like shell builtins, control structures, and variable assignments
      # Also skip test commands ([ ]), logical operators (&& ||), and complex shell constructs
      if ! echo "$cmd" | grep -qE '^[[:space:]]*(cd|\.|source|\./|if|then|else|fi|for|while|do|done|case|esac|exit|return|break|continue|:|\[[[:space:]]|test|\[\[|\]\]|true|false|export|unset|readonly|local|declare|typeset|alias|unalias|set|unset|shift|wait|trap|umask|ulimit|exec|eval|command|builtin|let|read|printf|[A-Za-z0-9_]+[[:space:]]*[=:]|&&|\|\||;|\(|\)|\{|\})'; then
        findings="${findings}PATH_HIJACKING: Command uses relative path\n"
      fi
    fi

    # Check for command injection vectors
    # Attack: Using unquoted variables or command substitution that can be injected
    # Example: echo $VAR or echo $(command)
    if echo "$cmd" | grep -qE '\$\{?[A-Za-z0-9_]|\$\(|`'; then
      findings="${findings}COMMAND_INJECTION: Command uses unquoted variables or command substitution\n"
    fi

    # Check for overly permissive commands
    # Attack: Commands that can be used to escalate privileges
    # Example: chmod 777, chown root, etc.
    if echo "$cmd" | grep -qE '\b(chmod\s+[0-7]{3,4}|chown\s+root|chgrp\s+root|sudo|su |pkexec)\b'; then
      findings="${findings}PERMISSIVE_COMMAND: Command modifies permissions or uses privilege escalation tools\n"
    fi

    # If any findings, print them
    if [ -n "$findings" ]; then
      echo "Potential privilege escalation in cron job:"
      echo "  └─ File: $file"
      echo "  └─ Command: $cmd"
      if [ -n "$bin" ]; then
        echo "  └─ Binary: $bin"
      fi
      echo "  └─ Findings:"
      echo "$findings" | while read -r finding; do
        [ -n "$finding" ] && echo "     * $finding"
      done
    fi
  }

  # Check system crontabs
  #echo "Checking system crontabs..."
  #for crontab in /etc/cron.d/* /etc/cron.daily/* /etc/cron.hourly/* /etc/cron.monthly/* /etc/cron.weekly/* /var/spool/cron/crontabs/* /etc/at* /etc/anacrontab /etc/incron.d/* /var/spool/incron/*; do
  #  [ ! -f "$crontab" ] && continue
  #  [ ! -r "$crontab" ] && continue

  #  # Check if the file is writable
  #  if [ -w "$crontab" ]; then
  #    echo "Writable cron file: $crontab"
  #  fi

  #  # Check each line for privilege escalation vectors
  #  while IFS= read -r line || [ -n "$line" ]; do
  #    # Skip comments and empty lines
  #    case "$line" in
  #      \#*|"") continue ;;
  #    esac

  #    # Extract the command part (everything after the time specification)
  #    cmd=$(echo "$line" | sed -E 's/^[^ ]+ [^ ]+ [^ ]+ [^ ]+ [^ ]+ //')
  #    [ -z "$cmd" ] && continue

  #    check_privesc_vectors "$cmd" "$crontab"
  #  done < "$crontab"
  #done

  # Check user crontabs
  #echo "Checking user crontabs..."
  #if command -v crontab >/dev/null 2>&1; then
  #  # Check current user's crontab
  #  crontab -l 2>/dev/null | while IFS= read -r line || [ -n "$line" ]; do
  #    case "$line" in
  #      \#*|"") continue ;;
  #    esac
  #    cmd=$(echo "$line" | sed -E 's/^[^ ]+ [^ ]+ [^ ]+ [^ ]+ [^ ]+ //')
  #    [ -z "$cmd" ] && continue
  #    check_privesc_vectors "$cmd" "current user crontab"
  #  done

  #  # Check other users' crontabs if accessible
  #  for user_crontab in /var/spool/cron/crontabs/*; do
  #    [ ! -f "$user_crontab" ] && continue
  #    [ ! -r "$user_crontab" ] && continue
  #    username=$(basename "$user_crontab")
  #    [ "$username" = "$USER" ] && continue
      
  #    echo "Found crontab for user: $username"
  #    while IFS= read -r line || [ -n "$line" ]; do
  #      case "$line" in
  #        \#*|"") continue ;;
  #      esac
  #      cmd=$(echo "$line" | sed -E 's/^[^ ]+ [^ ]+ [^ ]+ [^ ]+ [^ ]+ //')
  #      [ -z "$cmd" ] && continue
  #      check_privesc_vectors "$cmd" "$user_crontab"
  #    done < "$user_crontab"
  #  done
  #else
  #  echo_not_found "crontab"
  #fi

  # Check for writable cron directories
  echo "Checking cron directories..."
  for cron_dir in /etc/cron.d /etc/cron.daily /etc/cron.hourly /etc/cron.monthly /etc/cron.weekly /var/spool/cron/crontabs /usr/lib/cron/tabs /private/var/at/jobs /var/at/tabs /etc/periodic; do
    [ ! -d "$cron_dir" ] && continue
    if [ -w "$cron_dir" ]; then
      echo "Writable cron directory: $cron_dir"
    fi
  done

  if command -v run-parts >/dev/null 2>&1; then
    print_3title "run-parts executable entries" "T1053.003"
    for cron_dir in /etc/cron.hourly /etc/cron.daily /etc/cron.weekly /etc/cron.monthly; do
      [ -d "$cron_dir" ] || continue
      echo "[$cron_dir]"
      run-parts --test "$cron_dir" 2>/dev/null | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
    done
    echo ""
  fi

  # Check for at jobs
  #if command -v atq >/dev/null 2>&1; then
  #  echo "Checking at jobs..."
  #  atq 2>/dev/null | while IFS= read -r line || [ -n "$line" ]; do
  #    [ -z "$line" ] && continue
  #    job_id=$(echo "$line" | awk '{print $1}')
  #    [ -z "$job_id" ] && continue
  #    at -c "$job_id" 2>/dev/null | while IFS= read -r cmd || [ -n "$cmd" ]; do
  #      case "$cmd" in
  #        \#*|"") continue ;;
  #      esac
  #      check_privesc_vectors "$cmd" "at job $job_id"
  #    done
  #  done
  #fi

  # Check for incron jobs
  #if command -v incrontab >/dev/null 2>&1; then
  #  echo "Checking incron jobs..."
  #  incrontab -l 2>/dev/null | while IFS= read -r line || [ -n "$line" ]; do
  #    case "$line" in
  #      \#*|"") continue ;;
  #    esac
  #    cmd=$(echo "$line" | awk '{print $3}')
  #    [ -z "$cmd" ] && continue
  #    check_privesc_vectors "$cmd" "incron job"
  #  done
  #fi
else
  print_2title "Cron jobs" "T1053.003"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/processes-crontab-systemd-dbus/cron-and-systemd-timers.html#enumerate-schedules"
  find "$SEARCH_IN_FOLDER" '(' -type d -or -type f ')' '(' -name "cron*" -or -name "anacron" -or -name "anacrontab" -or -name "incron.d" -or -name "incron" -or -name "at" -or -name "periodic" ')' -exec echo {} \; -exec ls -lR {} \;
fi
echo ""
