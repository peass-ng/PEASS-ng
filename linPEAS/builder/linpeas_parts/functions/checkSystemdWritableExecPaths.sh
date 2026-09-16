# Title: Function - checkSystemdWritableExecPaths
# ID: checkSystemdWritableExecPaths
# Author: Chack Agent
# Last Update: 16-09-2026
# Description: Passively identify active root systemd services whose executable can be replaced through its writable parent directory.
# License: GNU GPL
# Version: 1.0
# Mitre: T1543.002,T1574.010
# Functions Used: print_3title, print_info
# Global Variables: $E, $IAMROOT, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $sdwed_dynamic_user, $sdwed_exec, $sdwed_exec_host, $sdwed_exec_parent, $sdwed_exec_records, $sdwed_findings, $sdwed_properties, $sdwed_root_dir, $sdwed_root_image, $sdwed_unit, $sdwed_units, $sdwed_user
# Fat linpeas: 0
# Small linpeas: 1


checkSystemdWritableExecPaths() {
  [ "$(uname -s 2>/dev/null)" = "Linux" ] || return 0
  [ -z "${IAMROOT:-}" ] || return 0
  command -v systemctl >/dev/null 2>&1 || return 0

  # Active units provide the strongest signal: their configured executable is
  # already trusted by PID 1 and is likely to be invoked again on restart.
  sdwed_units="$(systemctl list-units --type=service --state=active --no-legend --no-pager 2>/dev/null |
    awk '$1 ~ /\.service$/ { print $1 }' | head -n 200)"
  [ -n "$sdwed_units" ] || return 0

  sdwed_findings=""
  for sdwed_unit in $sdwed_units; do
    sdwed_properties="$(systemctl show "$sdwed_unit" \
      -p User -p DynamicUser -p RootImage -p RootDirectory \
      -p ExecStartPre -p ExecStart -p ExecStartPost -p ExecReload \
      -p ExecStop -p ExecStopPost 2>/dev/null)"
    sdwed_user="$(printf '%s\n' "$sdwed_properties" | sed -n 's/^User=//p' | head -n 1)"
    case "$sdwed_user" in
      ""|root|0) ;;
      *) continue ;;
    esac
    sdwed_dynamic_user="$(printf '%s\n' "$sdwed_properties" | sed -n 's/^DynamicUser=//p' | head -n 1)"
    if [ -z "$sdwed_user" ] && [ "$sdwed_dynamic_user" = "yes" ]; then
      continue
    fi

    # RootImage paths are not ordinary host paths. Avoid guessing whether a
    # writable host directory maps into the image and producing a false alert.
    sdwed_root_image="$(printf '%s\n' "$sdwed_properties" | sed -n 's/^RootImage=//p' | head -n 1)"
    [ -z "$sdwed_root_image" ] || continue
    sdwed_root_dir="$(printf '%s\n' "$sdwed_properties" | sed -n 's/^RootDirectory=//p' | head -n 1)"

    sdwed_exec_records="$(printf '%s\n' "$sdwed_properties" |
      awk '
        {
          sdwed_rest = $0
          while (match(sdwed_rest, /path=[^ ;}]*/)) {
            print substr(sdwed_rest, RSTART + 5, RLENGTH - 5)
            sdwed_rest = substr(sdwed_rest, RSTART + RLENGTH)
          }
        }
      ' | sort -u)"

    for sdwed_exec in $sdwed_exec_records; do
      case "$sdwed_exec" in
        /*) ;;
        *) continue ;;
      esac

      if [ -n "$sdwed_root_dir" ] && [ "$sdwed_root_dir" != "/" ]; then
        sdwed_exec_host="${sdwed_root_dir%/}${sdwed_exec}"
      else
        sdwed_exec_host="$sdwed_exec"
      fi
      sdwed_exec_parent="${sdwed_exec_host%/*}"
      [ -n "$sdwed_exec_parent" ] || sdwed_exec_parent="/"

      [ -d "$sdwed_exec_parent" ] || continue
      [ -w "$sdwed_exec_parent" ] && [ -x "$sdwed_exec_parent" ] || continue

      # In a sticky directory, a user cannot replace somebody else's existing
      # entry despite being able to create files alongside it.
      if [ -k "$sdwed_exec_parent" ] && [ -e "$sdwed_exec_host" ] && ! [ -O "$sdwed_exec_host" ]; then
        continue
      fi

      if [ -z "$sdwed_findings" ]; then
        print_3title "Root systemd service executables replaceable through writable directories" "T1543.002,T1574.010"
        print_info "https://www.freedesktop.org/software/systemd/man/latest/systemd.exec.html#User="
        sdwed_findings="found"
      fi
      echo "$sdwed_unit: $sdwed_exec_host (writable parent: $sdwed_exec_parent)" |
        sed -${E} "s,.*,${SED_RED_YELLOW},"
      ls -ld "$sdwed_exec_parent" "$sdwed_exec_host" 2>/dev/null
    done
  done

  [ -z "$sdwed_findings" ] || echo ""
}
