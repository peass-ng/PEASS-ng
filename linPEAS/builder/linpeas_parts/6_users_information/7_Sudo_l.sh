# Title: Users Information - Sudo -l
# ID: UG_Sudo_l
# Author: Carlos Polop
# Last Update: 04-10-2026
# Description: Checking 'sudo -l', /etc/sudoers, /etc/sudoers.d, and privileged Python scripts that load code from writable paths
# License: GNU GPL
# Version: 1.1
# Mitre: T1548.003
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables:$IAMROOT, $PASSWORD, $TIMEOUT, $sudoB, $sudoG, $sudoVB1, $sudoVB2
# Initial Functions:
# Generated Global Variables: $sudo_l_output, $sudo_l_password_output, $sudo_l_cached_output, $secure_path_line, $sudo_python_scripts, $python_sudo_script, $python_loader_lines, $python_loader_root, $python_loader_roots, $python_loader_path_lines, $python_loader_literal, $python_loader_parent, $python_candidate_dir, $python_seen_dirs, $python_pth_file, $python_pth_imports, $python_writable_pth, $python_script_dir
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

secure_path_line=$(printf "%s\n%s\n%s\n" "$sudo_l_cached_output" "$sudo_l_password_output" "$sudo_l_output" | grep -o "secure_path=[^,]*" | head -n 1 | cut -d= -f2)
if [ "$secure_path_line" ]; then
  for p in $(echo "$secure_path_line" | tr ':' ' '); do
    if [ -w "$p" ]; then
      echo "Writable secure_path entry: $p" | sed -${E} "s,.*,${SED_RED},g"
    fi
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
