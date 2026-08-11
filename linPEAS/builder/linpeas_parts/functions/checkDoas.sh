# Title: Function - checkDoas
# ID: checkDoas
# Author: Carlos Polop
# Last Update: 11-08-2026
# Description: Helpers for detecting dangerous doas rules, package versions, and effective permissions without executing privileged commands.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $TIMEOUT, $doas_current_gids, $doas_current_groups, $doas_current_uid, $doas_current_user, $sudoVB1, $sudoVB2
# Initial Functions:
# Generated Global Variables: $doas_candidate, $doas_candidate_version, $doas_homepage_lower, $doas_package_full_version, $doas_package_homepage, $doas_package_implementation, $doas_package_line, $doas_package_manager, $doas_package_name, $doas_package_source, $doas_rule_cmd, $doas_rule_identity_value
# Fat linpeas: 0
# Small linpeas: 1


doas_extract_upstream_version() {
  printf "%s\n" "$1" | grep -oE '[0-9]+(\.[0-9]+){1,2}' | head -n 1
}

doas_version_ge() {
  awk -v left="$1" -v right="$2" 'BEGIN {
    left_n = split(left, left_v, ".")
    right_n = split(right, right_v, ".")
    max_n = left_n > right_n ? left_n : right_n
    for (i = 1; i <= max_n; i++) {
      left_i = (i <= left_n ? left_v[i] : 0) + 0
      right_i = (i <= right_n ? right_v[i] : 0) + 0
      if (left_i > right_i) exit 0
      if (left_i < right_i) exit 1
    }
    exit 0
  }'
}

doas_version_lt() {
  if doas_version_ge "$1" "$2"; then
    return 1
  fi
  return 0
}

doas_version_le() {
  if doas_version_lt "$2" "$1"; then
    return 1
  fi
  return 0
}

doas_read_rules() {
  awk '
    function trim(value) {
      sub(/^[[:space:]]+/, "", value)
      sub(/[[:space:]]+$/, "", value)
      return value
    }
    {
      source = $0
      output = ""
      quoted = 0
      escaped = 0
      for (i = 1; i <= length(source); i++) {
        char = substr(source, i, 1)
        if (char == "#" && !quoted) break
        output = output char
        if (char == "\"" && !escaped) quoted = !quoted
        if (char == "\\" && !escaped) escaped = 1
        else escaped = 0
      }
      output = trim(output)
      if (output ~ /^(permit|deny)([[:space:]]|$)/)
        printf "%d\t%s\n", NR, output
    }
  ' "$1" 2>/dev/null
}

doas_rule_identity() {
  printf "%s\n" "$1" | awk '
    {
      in_setenv = 0
      for (i = 2; i <= NF; i++) {
        token = $i
        if (in_setenv) {
          if (token ~ /}/) in_setenv = 0
          continue
        }
        if (token == "setenv") {
          in_setenv = 1
          continue
        }
        if (token == "nopass" || token == "nolog" || token == "persist" || token == "keepenv")
          continue
        gsub(/^"|"$/, "", token)
        print token
        exit
      }
    }
  '
}

doas_rule_has_option() {
  printf "%s\n" "$1" | awk -v wanted="$2" '
    {
      in_setenv = 0
      for (i = 2; i <= NF; i++) {
        token = $i
        if (in_setenv) {
          if (token ~ /}/) in_setenv = 0
          continue
        }
        if (token == "setenv") {
          if (wanted == token) exit 0
          in_setenv = 1
          continue
        }
        if (token == "nopass" || token == "nolog" || token == "persist" || token == "keepenv") {
          if (wanted == token) exit 0
          continue
        }
        exit 1
      }
      exit 1
    }
  '
}

doas_rule_has_dangerous_environment() {
  if doas_rule_has_option "$1" keepenv; then
    return 0
  fi
  printf "%s\n" "$1" | grep -Eq '(^|[[:space:]{])(setenv[[:space:]]*\{[^}]*[[:space:]])?(PATH|LD_PRELOAD|LD_LIBRARY_PATH|BASH_ENV|ENV|PYTHONPATH|PERL5LIB|RUBYLIB)([=[:space:]}]|$)'
}

doas_rule_command() {
  printf "%s\n" "$1" | awk '
    {
      for (i = 1; i < NF; i++) {
        if ($i == "cmd") {
          command = $(i + 1)
          gsub(/^"|"$/, "", command)
          print command
          exit
        }
      }
    }
  '
}

doas_rule_targets_root() {
  printf "%s\n" "$1" | awk '
    {
      for (i = 1; i < NF; i++) {
        if ($i == "as") {
          target = $(i + 1)
          gsub(/^"|"$/, "", target)
          exit(target == "root" || target == "0" || target == "#0" ? 0 : 1)
        }
      }
      exit 0
    }
  '
}

doas_rule_applies_to_current_user() {
  doas_rule_identity_value="$(doas_rule_identity "$1")"
  case "$doas_rule_identity_value" in
    "$doas_current_user"|"$doas_current_uid"|"#$doas_current_uid") return 0 ;;
  esac

  case "$doas_rule_identity_value" in
    :*)
      doas_rule_identity_value="${doas_rule_identity_value#:}"
      for doas_candidate in $doas_current_groups $doas_current_gids; do
        if [ "$doas_candidate" = "$doas_rule_identity_value" ] || [ "#$doas_candidate" = "$doas_rule_identity_value" ]; then
          return 0
        fi
      done
      ;;
  esac
  return 1
}

doas_command_is_dangerous() {
  doas_rule_cmd="$1"
  doas_rule_cmd="${doas_rule_cmd##*/}"
  case "$doas_rule_cmd" in
    ash|awk|bash|busybox|csh|dash|dstat|ed|env|expect|find|fish|gdb|git|ionice|jrunscript|ksh|less|lua|make|more|mv|nano|nawk|nc|ncat|nice|node|nvim|perl|php|python|python2|python3|rake|rlwrap|ruby|run-parts|rvim|sed|sh|socat|sqlite3|tar|tee|tclsh|vi|vim|watch|xargs|zsh)
      return 0
      ;;
  esac

  if [ -n "${sudoVB1:-}" ] && printf " %s\n" "$1" | grep -Eq "$sudoVB1" 2>/dev/null; then
    return 0
  fi
  if [ -n "${sudoVB2:-}" ] && printf " %s\n" "$1" | grep -Eq "$sudoVB2" 2>/dev/null; then
    return 0
  fi
  return 1
}

doas_get_package_details() {
  doas_package_manager=""
  doas_package_name=""
  doas_package_full_version=""
  doas_package_source=""
  doas_package_homepage=""
  doas_package_implementation="unknown"

  if command -v dpkg-query >/dev/null 2>&1; then
    doas_package_name="$(dpkg-query -S "$1" 2>/dev/null | head -n 1 | sed 's/: .*//' | cut -d: -f1)"
    if [ -n "$doas_package_name" ]; then
      doas_package_manager="dpkg"
      doas_package_full_version="$(dpkg-query -W -f='$''{Version}\n' "$doas_package_name" 2>/dev/null | head -n 1)"
      doas_package_source="$(dpkg-query -W -f='$''{source:Package}\n' "$doas_package_name" 2>/dev/null | head -n 1 | sed 's/^src://')"
      doas_package_homepage="$(dpkg-query -W -f='$''{Homepage}\n' "$doas_package_name" 2>/dev/null | head -n 1)"
    fi
  elif command -v rpm >/dev/null 2>&1; then
    doas_package_line="$(rpm -qf --qf '%{NAME}|%{VERSION}-%{RELEASE}|%{URL}\n' "$1" 2>/dev/null | head -n 1)"
    if [ -n "$doas_package_line" ]; then
      doas_package_manager="rpm"
      doas_package_name="$(printf "%s" "$doas_package_line" | cut -d'|' -f1)"
      doas_package_full_version="$(printf "%s" "$doas_package_line" | cut -d'|' -f2)"
      doas_package_homepage="$(printf "%s" "$doas_package_line" | cut -d'|' -f3-)"
    fi
  elif command -v apk >/dev/null 2>&1; then
    for doas_candidate in opendoas doas; do
      doas_candidate_version="$(apk info -e -v "$doas_candidate" 2>/dev/null | head -n 1)"
      if [ -n "$doas_candidate_version" ]; then
        doas_package_manager="apk"
        doas_package_name="$doas_candidate"
        doas_package_full_version="${doas_candidate_version#${doas_candidate}-}"
        doas_package_homepage="$(apk info -a "$doas_candidate" 2>/dev/null | sed -n 's/^webpage[[:space:]]*:[[:space:]]*//p' | head -n 1)"
        break
      fi
    done
  elif command -v pacman >/dev/null 2>&1; then
    doas_package_line="$(pacman -Qo "$1" 2>/dev/null | head -n 1)"
    doas_package_name="$(printf "%s\n" "$doas_package_line" | sed -nE 's/.* is owned by ([^ ]+) .*/\1/p')"
    doas_package_full_version="$(printf "%s\n" "$doas_package_line" | sed -nE 's/.* is owned by [^ ]+ ([^ ]+).*/\1/p')"
    if [ -n "$doas_package_name" ]; then
      doas_package_manager="pacman"
      doas_package_homepage="$(pacman -Qi "$doas_package_name" 2>/dev/null | sed -n 's/^URL[[:space:]]*:[[:space:]]*//p' | head -n 1)"
    fi
  elif command -v pkg >/dev/null 2>&1; then
    doas_package_name="$(pkg which -q "$1" 2>/dev/null | head -n 1)"
    if [ -n "$doas_package_name" ]; then
      doas_package_manager="pkg"
      doas_package_line="$(pkg query '%n|%v|%o|%w' "$doas_package_name" 2>/dev/null | head -n 1)"
      doas_package_name="$(printf "%s" "$doas_package_line" | cut -d'|' -f1)"
      doas_package_full_version="$(printf "%s" "$doas_package_line" | cut -d'|' -f2)"
      doas_package_source="$(printf "%s" "$doas_package_line" | cut -d'|' -f3)"
      doas_package_homepage="$(printf "%s" "$doas_package_line" | cut -d'|' -f4-)"
    fi
  fi

  doas_homepage_lower="$(printf "%s" "$doas_package_homepage" | tr '[:upper:]' '[:lower:]')"
  case "$doas_homepage_lower:$doas_package_source:$doas_package_name" in
    *duncaen/opendoas*|*:opendoas:*|*:*:opendoas) doas_package_implementation="opendoas" ;;
    *slicer69/doas*) doas_package_implementation="slicer69" ;;
  esac
  if [ "$(uname -s 2>/dev/null)" = "OpenBSD" ]; then
    doas_package_implementation="openbsd"
  fi
}

doas_config_syntax_valid() {
  if [ -n "${TIMEOUT:-}" ]; then
    "$TIMEOUT" 5 "$1" -C "$2" >/dev/null 2>&1
  else
    "$1" -C "$2" >/dev/null 2>&1
  fi
}

doas_check_command() {
  if [ -n "${TIMEOUT:-}" ]; then
    "$TIMEOUT" 5 "$1" -C "$2" "$3" 2>/dev/null
  else
    "$1" -C "$2" "$3" 2>/dev/null
  fi
}
