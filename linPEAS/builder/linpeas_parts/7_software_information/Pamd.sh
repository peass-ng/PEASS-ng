# Title: Software Information - Pam.d
# ID: SI_Pamd
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: Password strings, nonstandard modules, and writable pam_exec helpers in PAM policy
# License: GNU GPL
# Version: 1.1
# Mitre: T1556.003
# Functions Used: checkPamAllowActiveCVE20256018, print_2title
# Global Variables: $DEBUG, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $pamdpass, $pamd_files, $pamd_hits, $pamd_module_hits, $pamd_module_limited, $pamd_config, $pamd_size, $pamd_helpers, $pamd_helper, $pamd_target, $pamd_module, $pamd_line
# Fat linpeas: 0
# Small linpeas: 1


pamdpass=$(grep -Ri "passwd" "${ROOT_FOLDER}etc/pam.d/" 2>/dev/null | grep -v ":#")
if [ "$pamdpass" ] || [ "$DEBUG" ]; then
  print_2title "Passwords inside pam.d" "T1556.003"
  if [ "$pamdpass" ]; then
    printf '%s\n' "$pamdpass" | sed "s,passwd,${SED_RED},"
  fi
  echo ""
fi

# Inspect only small local PAM policy files; do not execute or read helper contents.
(
  pamd_files=0
  pamd_hits=0
  pamd_module_hits=0
  pamd_module_limited=0
  for pamd_config in "${ROOT_FOLDER}etc/pam.d/"* "${ROOT_FOLDER}etc/pam.conf"; do
    if [ "$pamd_files" -ge 128 ] || [ "$pamd_hits" -ge 24 ]; then break; fi
    [ -f "$pamd_config" ] && [ ! -L "$pamd_config" ] && [ -r "$pamd_config" ] || continue
    pamd_files=$((pamd_files + 1))
    pamd_size=$(wc -c < "$pamd_config" 2>/dev/null) || continue
    [ "$pamd_size" -le 8192 ] || continue
    pamd_helpers=$(awk '
      /^[[:space:]]*#/ { next }
      {
        for (i = 1; i <= NF; i++) {
          if (substr($i, 1, 1) == "#") break
          if ($i ~ /(^|\/)pam_exec[.]so$/) {
            for (j = i + 1; j <= NF; j++) {
              if (substr($j, 1, 1) == "#") break
              if ($j ~ /^\/[A-Za-z0-9._+\/-]+$/) { print "E:" $j; break }
            }
          }
        }
        module_start = 0
        if ($1 ~ /^-?(auth|account|password|session)$/) module_start = 3
        else if ($2 ~ /^-?(auth|account|password|session)$/) module_start = 4
        if (module_start) {
          for (i = module_start; i <= NF; i++) {
            if (substr($i, 1, 1) == "#") break
            if ($i ~ /^[A-Za-z0-9._+\/-]+[.]so$/) {
              module = $i
              sub(/^.*\//, "", module)
              if (tolower(module) !~ /^pam_/) print "M:" NR ":" $i
              break
            }
          }
        }
      }
    ' "$pamd_config" 2>/dev/null)
    [ -n "$pamd_helpers" ] || continue
    while IFS= read -r pamd_helper; do
      case "$pamd_helper" in
        M:*)
          if [ "$pamd_module_hits" -ge 16 ]; then
            pamd_module_limited=1
            continue
          fi
          pamd_line=${pamd_helper#M:}
          pamd_module=${pamd_line#*:}
          pamd_line=${pamd_line%%:*}
          pamd_module_hits=$((pamd_module_hits + 1))
          printf 'PAM nonstandard module review candidate: %s:%s -> %s (confirm installed module, service reachability, and control flow)\n' "$pamd_config" "$pamd_line" "$pamd_module"
          continue
          ;;
        E:*) pamd_helper=${pamd_helper#E:} ;;
        *) continue ;;
      esac
      [ "$pamd_hits" -ge 24 ] && continue
      pamd_target="${ROOT_FOLDER%/}${pamd_helper}"
      [ -f "$pamd_target" ] && [ ! -L "$pamd_target" ] && [ -x "$pamd_target" ] && [ -w "$pamd_target" ] || continue
      pamd_hits=$((pamd_hits + 1))
      printf 'PAM exec writable helper candidate: %s -> %s (confirm PAM event, effective UID, parent paths, and ACLs)\n' "$pamd_config" "$pamd_target"
      ls -ld "$pamd_target" 2>/dev/null
      if command -v lsattr >/dev/null 2>&1; then lsattr -d "$pamd_target" 2>/dev/null; fi
    done <<EOF
$pamd_helpers
EOF
  done
  [ "$pamd_module_limited" -eq 1 ] && printf 'PAM nonstandard module review capped at 16 findings; more policy lines may need review.\n'
)

if ! [ "$SEARCH_IN_FOLDER" ]; then
  checkPamAllowActiveCVE20256018
fi
