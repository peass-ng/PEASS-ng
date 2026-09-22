# Title: Software Information - Gogs privileged file writes
# ID: SI_Gogs
# Author: HT Bot
# Last Update: 22-09-2026
# Description: Detect Gogs, highlight root-running instances and CVE-2025-8110, and report privilege-relevant paths from known configuration files
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $MACPEAS, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $gogs_bin, $gogs_bin_dir, $gogs_cmd, $gogs_config, $gogs_config_arg, $gogs_config_candidates, $gogs_config_line, $gogs_config_summary, $gogs_cwd, $gogs_major, $gogs_minor, $gogs_patch, $gogs_pid, $gogs_processes, $gogs_root_process, $gogs_seen_configs, $gogs_version, $gogs_version_output
# Fat linpeas: 0
# Small linpeas: 1

if ! [ "$SEARCH_IN_FOLDER" ] && ! [ "$MACPEAS" ]; then
  # Keep the process format stable: user, PID, command name, full command line.
  gogs_processes="$(ps -eo user=,pid=,comm=,args= 2>/dev/null | awk '
    $3 == "gogs" {
      for (i = 4; i <= NF; i++) {
        if ($i == "web") {
          print
          break
        }
      }
    }
  ')"

  gogs_pid="$(printf "%s\n" "$gogs_processes" | awk 'NR == 1 { print $2 }')"
  gogs_cmd="$(printf "%s\n" "$gogs_processes" | awk 'NR == 1 { for (i = 4; i <= NF; i++) printf "%s%s", (i == 4 ? "" : " "), $i; print "" }')"
  gogs_bin=""
  if [ "$gogs_pid" ] && [ -e "/proc/$gogs_pid/exe" ]; then
    gogs_bin="$(readlink "/proc/$gogs_pid/exe" 2>/dev/null)"
  fi
  if ! [ -x "$gogs_bin" ] && [ "$gogs_cmd" ]; then
    gogs_bin="$(printf "%s\n" "$gogs_cmd" | awk '{ print $1 }')"
  fi
  if ! [ -x "$gogs_bin" ]; then
    gogs_bin="$(command -v gogs 2>/dev/null || echo -n '')"
  fi

  if [ "$gogs_processes" ] || [ "$gogs_bin" ] || [ -r /etc/gogs/conf/app.ini ] || [ -r /etc/gogs/app.ini ] || [ "$DEBUG" ]; then
    print_2title "Gogs privileged file-write checks" "T1068"
    print_info "Gogs <= 0.13.3 is vulnerable to authenticated symlink-based file writes through the PutContents API (CVE-2025-8110): https://github.com/gogs/gogs/security/advisories/GHSA-gg64-xxr9-qhjp"

    gogs_root_process="$(printf "%s\n" "$gogs_processes" | awk '$1 == "root" { print "yes"; exit }')"
    if [ "$gogs_processes" ]; then
      echo "Gogs web processes:"
      printf "%s\n" "$gogs_processes" | while read -r gogs_config_line; do
        case "$gogs_config_line" in
          root*) echo "$gogs_config_line" | sed -${E} "s,.*,${SED_RED_YELLOW}," ;;
          *) echo "$gogs_config_line" ;;
        esac
      done
    else
      echo "Gogs web process not found"
    fi

    gogs_version_output=""
    gogs_version=""
    if [ -x "$gogs_bin" ]; then
      gogs_version_output="$("$gogs_bin" --version 2>/dev/null | head -n 1)"
      gogs_version="$(printf "%s\n" "$gogs_version_output" | grep -oE '[0-9]+\.[0-9]+\.[0-9]+' | head -n 1)"
      echo "Gogs executable: $gogs_bin"
      [ "$gogs_version_output" ] && echo "Gogs version: $gogs_version_output"
    fi

    if [ "$gogs_version" ]; then
      gogs_major="$(printf "%s" "$gogs_version" | cut -d. -f1)"
      gogs_minor="$(printf "%s" "$gogs_version" | cut -d. -f2)"
      gogs_patch="$(printf "%s" "$gogs_version" | cut -d. -f3)"
      if [ "$gogs_major" -eq 0 ] 2>/dev/null && { [ "$gogs_minor" -lt 13 ] 2>/dev/null || { [ "$gogs_minor" -eq 13 ] 2>/dev/null && [ "$gogs_patch" -le 3 ] 2>/dev/null; }; }; then
        if [ "$gogs_root_process" ]; then
          echo "Gogs $gogs_version is vulnerable to CVE-2025-8110 and is running as root; an authenticated repository writer may obtain root code execution" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        else
          echo "Gogs $gogs_version is vulnerable to CVE-2025-8110; successful exploitation executes as the Gogs service user" | sed -${E} "s,.*,${SED_YELLOW},"
        fi
      else
        echo "Gogs $gogs_version is not in the CVE-2025-8110 vulnerable range (<= 0.13.3)" | sed -${E} "s,.*,${SED_GREEN},"
      fi
    elif [ "$gogs_root_process" ]; then
      echo "Gogs is running as root, but its version could not be determined; verify that it is newer than 0.13.3" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    fi

    # Gogs accepts --config/-c. Add only bounded standard locations derived from
    # the active process; do not recursively search the filesystem or repositories.
    gogs_config_arg="$(printf "%s\n" "$gogs_cmd" | awk '
      {
        for (i = 1; i <= NF; i++) {
          if (($i == "--config" || $i == "-c") && i < NF) {
            print $(i + 1)
            exit
          }
          if ($i ~ /^--config=/) {
            sub(/^--config=/, "", $i)
            print $i
            exit
          }
        }
      }
    ')"
    gogs_bin_dir=""
    [ "$gogs_bin" ] && gogs_bin_dir="$(dirname "$gogs_bin" 2>/dev/null)"
    gogs_cwd=""
    if [ "$gogs_pid" ] && [ -e "/proc/$gogs_pid/cwd" ]; then
      gogs_cwd="$(readlink "/proc/$gogs_pid/cwd" 2>/dev/null)"
    fi
    gogs_config_candidates="$gogs_config_arg
$gogs_bin_dir/custom/conf/app.ini
$gogs_cwd/custom/conf/app.ini
/etc/gogs/conf/app.ini
/etc/gogs/app.ini
/var/lib/gogs/custom/conf/app.ini
/home/git/gogs/custom/conf/app.ini
/opt/gogs/custom/conf/app.ini"

    gogs_seen_configs="|"
    printf "%s\n" "$gogs_config_candidates" | while read -r gogs_config; do
      [ "$gogs_config" ] || continue
      case "$gogs_seen_configs" in
        *"|$gogs_config|"*) continue ;;
      esac
      gogs_seen_configs="${gogs_seen_configs}${gogs_config}|"
      [ -r "$gogs_config" ] || continue

      echo "Privilege-relevant Gogs configuration: $gogs_config"
      gogs_config_summary="$(awk '
        /^[[:space:]]*[#;]/ || /^[[:space:]]*$/ { next }
        /^[[:space:]]*\[/ {
          section = tolower($0)
          gsub(/[[:space:]\[\]]/, "", section)
          next
        }
        index($0, "=") {
          key = substr($0, 1, index($0, "=") - 1)
          value = substr($0, index($0, "=") + 1)
          gsub(/^[[:space:]]+|[[:space:]]+$/, "", key)
          gsub(/^[[:space:]]+|[[:space:]]+$/, "", value)
          key = toupper(key)
          if ((section == "" && (key == "RUN_USER" || key == "APP_DATA_PATH")) ||
              (section == "repository" && (key == "ROOT" || key == "ROOT_PATH")) ||
              (section == "server" && (key == "HTTP_ADDR" || key == "HTTP_PORT" || key == "EXTERNAL_URL" || key == "ROOT_URL")) ||
              ((section == "auth" || section == "service") && key == "DISABLE_REGISTRATION") ||
              (section == "attachment" && key == "PATH") ||
              (section == "repository.upload" && key == "TEMP_PATH")) {
            printf "[%s] %s = %s\n", section, key, value
          }
        }
      ' "$gogs_config" 2>/dev/null)"
      if [ "$gogs_config_summary" ]; then
        printf "%s\n" "$gogs_config_summary" | while read -r gogs_config_line; do
          case "$gogs_config_line" in
            *"RUN_USER = root"*|*"ROOT = /root"*|*"ROOT_PATH = /root"*)
              echo "$gogs_config_line" | sed -${E} "s,.*,${SED_RED_YELLOW},"
              ;;
            *"DISABLE_REGISTRATION = false"*)
              echo "$gogs_config_line" | sed -${E} "s,.*,${SED_YELLOW},"
              ;;
            *) echo "$gogs_config_line" ;;
          esac
        done
      fi
    done
    echo ""
  fi
fi
