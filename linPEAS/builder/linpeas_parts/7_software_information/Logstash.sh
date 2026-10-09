# Title: Software Information - Logstash
# ID: SI_Logstash
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: Review output/filter directives and bounded input-path leads in known Logstash directories
# License: GNU GPL
# Version: 1.1
# Mitre: T1552.001
# Functions Used: print_2title
# Global Variables: $DEBUG, $E, $SED_BLUE, $SED_GREEN, $SED_LIGHT_CYAN, $SED_LIGHT_MAGENTA, $SED_RED, $knw_usrs, $nosh_usrs, $sh_usrs, $USER
# Cached storage inputs (builder): $PSTORAGE_LOGSTASH
# Initial Functions:
# Generated Global Variables: $d, $ls_config, $ls_count, $ls_lines, $ls_root_count
# Fat linpeas: 0
# Small linpeas: 1


if [ "$PSTORAGE_LOGSTASH" ] || [ "$DEBUG" ]; then
  print_2title "Searching logstash files" "T1552.001"
  printf '%s\n' "$PSTORAGE_LOGSTASH"
  ls_root_count=0
  printf '%s\n' "$PSTORAGE_LOGSTASH" | while IFS= read -r d; do
    [ -n "$d" ] && [ -d "$d" ] || continue
    ls_root_count=$((ls_root_count + 1))
    if [ -r "$d/startup.options" ]; then
      echo "Logstash startup.options account setting (verify running identity):"
      cat "$d/startup.options" 2>/dev/null | grep "LS_USER\|LS_GROUP" | sed -${E} "s,$sh_usrs,${SED_LIGHT_CYAN}," | sed -${E} "s,$nosh_usrs,${SED_BLUE}," | sed -${E} "s,$knw_usrs,${SED_GREEN}," | sed -${E} "s,${USER:-^$},${SED_LIGHT_MAGENTA}," | sed -${E} "s,root,${SED_RED},"
    fi
    # Preserve full-file output/filter coverage, including extensionless names.
    # Keep the directory component quoted while allowing the filename glob.
    for ls_config in "$d"/conf.d/out*; do
      [ -f "$ls_config" ] && [ -r "$ls_config" ] || continue
      grep -E 'exec[[:space:]]*\{|command[[:space:]]*=>' "$ls_config" 2>/dev/null |
        sed -${E} "s,exec\W*\{|command\W*=>,${SED_RED},"
    done
    for ls_config in "$d"/conf.d/filt*; do
      [ -f "$ls_config" ] && [ -r "$ls_config" ] || continue
      grep -E 'path[[:space:]]*=>|code[[:space:]]*=>|match[[:space:]]*=>|ruby[[:space:]]*\{' "$ls_config" 2>/dev/null |
        sed -${E} "s,path\W*=>|code\W*=>|match\W*=>|ruby\W*\{,${SED_RED},"
    done
    # Input-path cues are a separate bounded pass over already-known dirs.
    if [ "$ls_root_count" -le 8 ]; then
      ls_count=0
      for ls_config in "$d"/conf.d/in*; do
        [ -f "$ls_config" ] && [ -r "$ls_config" ] && [ ! -L "$ls_config" ] || continue
        ls_count=$((ls_count + 1))
        if [ "$ls_count" -gt 16 ]; then
          echo '  Additional input-named Logstash files not inspected (16-file cap).'
          break
        fi
        ls_lines=$(dd if="$ls_config" bs=8192 count=1 2>/dev/null |
          grep -E '^[[:space:]]*path[[:space:]]*=>' | head -n 16)
        if [ -n "$ls_lines" ]; then
          printf '  Input path directive lead in %s (verify file input and permissions):\n' "$ls_config"
          printf '%s\n' "$ls_lines"
        fi
      done
    fi
  done
fi
echo ""
