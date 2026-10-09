# Title: Interesting Permissions Files - SUID
# ID: IP_SUID
# Author: Carlos Polop, HT Bot
# Last Update: 09-10-2026
# Description: SUID - Check easy privesc, exploits, write perms, and risky file placement
# License: GNU GPL
# Version: 1.4
# Mitre: T1548.001
# Functions Used: check_privileged_file_location, echo_not_found, print_2title, print_info
# Global Variables: $IAMROOT, $LDD, $ROOT_FOLDER, $READELF, $sidB, $sidG1, $sidG2, $sidG3, $sidG4, $sidVB, $sidVB2, $STRACE, $STRINGS, $TIMEOUT, $Wfolders, $cfuncs
# Initial Functions:
# Generated Global Variables: $suids_files, $sfile, $sname, $sowner, $sline_first, $sline, $OLD_LD_LIBRARY_PATH, $LD_LIBRARY_PATH, $ndsudo_uid, $ndsudo_nnp, $ndsudo_mount_options, $ndsudo_uncertainty, $pinns_uid, $pinns_nnp, $pinns_mount_options, $pinns_uncertainty
# Fat linpeas: 0
# Small linpeas: 1


print_2title "SUID - Check easy privesc, exploits and write perms" "T1548.001"
print_info "https://book.hacktricks.wiki/en/linux-hardening/interesting-files-permissions/suid-sgid-and-acl-triage.html#enumerate-privileged-executables"
if ! [ "$STRINGS" ]; then
  echo_not_found "strings"
fi
if ! [ "$STRACE" ]; then
  echo_not_found "strace"
fi
suids_files=$(find $ROOT_FOLDER -perm -4000 -type f ! -path "/dev/*" 2>/dev/null)
printf "%s\n" "$suids_files" | while IFS= read -r sfile; do
  [ -z "$sfile" ] && continue
  s=$(ls -lahtr "$sfile")
  #If starts like "total 332K" then no SUID bin was found and xargs just executed "ls" in the current folder
  if echo "$s" | grep -qE "^total"; then break; fi

  # Keep the path returned by find: parsing it from ls output breaks on whitespace.
  sname="$sfile"
  sowner="$(echo "$s" | awk '{print $3}')"
  if [ "${sname##*/}" = "pinns" ] && [ "$(uname -s 2>/dev/null)" = "Linux" ]; then
    # The privileged CRI-O helper is a lead only; never invoke it to test.
    pinns_uid=$(stat -c '%u' "$sname" 2>/dev/null || stat -f '%u' "$sname" 2>/dev/null)
    if [ "$IAMROOT" ] || [ "$pinns_uid" != "0" ] || ! [ -u "$sname" ] || ! [ -x "$sname" ]; then
      echo "  pinns: no local privilege-escalation candidate for this user (requires root ownership, SUID, and execute access)."
    else
      pinns_nnp=$(awk '/^NoNewPrivs:/ {print $2; exit}' /proc/self/status 2>/dev/null)
      pinns_mount_options=$(findmnt -no OPTIONS -T "$sname" 2>/dev/null | head -n 1)
      if [ "$pinns_nnp" = "1" ]; then
        echo "  pinns: SUID transition blocked by NoNewPrivs for this process tree."
      elif echo ",$pinns_mount_options," | grep -q ',nosuid,'; then
        echo "  pinns: SUID transition blocked by nosuid mount options."
      else
        pinns_uncertainty=""
        [ "$pinns_nnp" = "0" ] || pinns_uncertainty="NoNewPrivs unknown; "
        [ -n "$pinns_mount_options" ] || pinns_uncertainty="${pinns_uncertainty}mount SUID policy unknown; "
        echo "  pinns review candidate (CVE-2022-0811): root-owned SUID executable by current user; NoNewPrivs=${pinns_nnp:-unknown}; mount options=${pinns_mount_options:-unknown}."
        echo "  ${pinns_uncertainty}Installed helper build and vendor fixes unknown; verify the exact binary and host sysctl policy. No helper execution performed."
      fi
    fi
  fi
  if [ "$sname" = "."  ] || [ "$sname" = ".."  ]; then
    true #Don't do nothing
  elif echo "$sname" | grep -qE '/netdata/plugins[.]d/ndsudo$'; then
    # Keep this branch passive: even the generic non-FAST SUID probe must not run ndsudo.
    echo "$s"
    ndsudo_uid=$(stat -c '%u' "$sname" 2>/dev/null || stat -f '%u' "$sname" 2>/dev/null)
    if [ "$ndsudo_uid" != "0" ] || ! [ -u "$sname" ] || ! [ -x "$sname" ]; then
      echo "  ndsudo: no privilege-escalation candidate for this user (requires root ownership, SUID, and execute access)."
    else
      ndsudo_nnp=$(awk '/^NoNewPrivs:/ {print $2; exit}' /proc/self/status 2>/dev/null)
      ndsudo_mount_options=$(findmnt -no OPTIONS -T "$sname" 2>/dev/null | head -n 1)
      if [ "$ndsudo_nnp" = "1" ]; then
        echo "  ndsudo: SUID transition blocked by NoNewPrivs for this process tree."
      elif echo ",$ndsudo_mount_options," | grep -q ',nosuid,'; then
        echo "  ndsudo: SUID transition blocked by nosuid mount options."
      else
        ndsudo_uncertainty=""
        [ "$ndsudo_nnp" = "0" ] || ndsudo_uncertainty="NoNewPrivs unknown; "
        [ -n "$ndsudo_mount_options" ] || ndsudo_uncertainty="${ndsudo_uncertainty}mount SUID policy unknown; "
        echo "  ndsudo candidate (CVE-2024-32019): root-owned SUID executable by current user; NoNewPrivs=${ndsudo_nnp:-unknown}; mount options=${ndsudo_mount_options:-unknown}."
        echo "  ${ndsudo_uncertainty}Installed build and vendor backports unknown; confirm affected version and PATH lookup manually. Vendor fixes: v1.45.3 and v1.45.0-169. No helper execution performed."
      fi
    fi
  elif ! [ "$IAMROOT" ] && [ -O "$sname" ]; then
    echo "You own the SUID file: $sname" | sed -${E} "s,.*,${SED_RED},"
  elif ! [ "$IAMROOT" ] && [ -w "$sname" ]; then #If write permision, win found (no check exploits)
    echo "You can write SUID file: $sname" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  else
    c="a"
    for b in $sidB; do
      if echo "$sname" | grep -q $(echo $b | cut -d % -f 1); then
        echo "$s" | sed -${E} "s,$(echo $b | cut -d % -f 1),${C}[1;31m&  --->  $(echo $b | cut -d % -f 2)${C}[0m,"
        c=""
        break;
      fi
    done;
    if [ "$c" ]; then
      if echo "$sname" | grep -qE "$sidG1" || echo "$sname" | grep -qE "$sidG2" || echo "$sname" | grep -qE "$sidG3" || echo "$sname" | grep -qE "$sidG4" || echo "$sname" | grep -qE "$sidVB" || echo "$sname" | grep -qE "$sidVB2"; then
        echo "$s" | sed -${E} "s,$sidG1,${SED_GREEN}," | sed -${E} "s,$sidG2,${SED_GREEN}," | sed -${E} "s,$sidG3,${SED_GREEN}," | sed -${E} "s,$sidG4,${SED_GREEN}," | sed -${E} "s,$sidVB,${SED_RED_YELLOW}," | sed -${E} "s,$sidVB2,${SED_RED_YELLOW},"
      else
        echo "$s (Unknown SUID binary!)" | sed -${E} "s,/.*,${SED_RED},"
        printf $ITALIC
        if ! [ "$FAST" ]; then
          
          if [ "$STRINGS" ]; then
            $STRINGS "$sname" 2>/dev/null | sort | uniq | while read sline; do
              sline_first="$(echo "$sline" | cut -d ' ' -f1)"
              if echo "$sline_first" | grep -qEv "$cfuncs"; then
                if echo "$sline_first" | grep -q "/" && [ -f "$sline_first" ]; then #If a path
                  if [ -O "$sline_first" ] || [ -w "$sline_first" ]; then #And modifiable
                    printf "$ITALIC  --- It looks like $RED$sname$NC$ITALIC is using $RED$sline_first$NC$ITALIC and you can modify it (strings line: $sline) (https://tinyurl.com/suidpath)\n"
                  fi
                elif echo "$sline_first" | grep -q "/" && [ -d "$(dirname "$sline_first")" ] && [ -w "$(dirname "$sline_first")" ]; then #If path does not exist but can be created
                  printf "$ITALIC  --- It looks like $RED$sname$NC$ITALIC is using $RED$sline_first$NC$ITALIC and you can create it inside writable dir $RED$(dirname "$sline_first")$NC$ITALIC (strings line: $sline) (https://tinyurl.com/suidpath)\n"
                else #If not a path
                  if [ ${#sline_first} -gt 2 ] && command -v "$sline_first" 2>/dev/null | grep -q '/' && echo "$sline_first" | grep -Eqv "\.\."; then #Check if existing binary
                    printf "$ITALIC  --- It looks like $RED$sname$NC$ITALIC is executing $RED$sline_first$NC$ITALIC and you can impersonate it (strings line: $sline) (https://tinyurl.com/suidpath)\n"
                  fi
                fi
              fi
            done
          fi

          if [ "$LDD" ] || [ "$READELF" ]; then
            echo "$ITALIC  --- Checking for writable dependencies of $sname...$NC"
          fi
          if [ "$LDD" ]; then
            "$LDD" "$sname" | grep -E "$Wfolders" | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
          fi
          if [ "$READELF" ]; then
            "$READELF" -d "$sname" | grep PATH | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
          fi
          
          if [ "$TIMEOUT" ] && [ "$STRACE" ] && [ -x "$sname" ]; then
            printf $ITALIC
            echo "----------------------------------------------------------------------------------------"
            echo "  --- Trying to execute $sname with strace in order to look for hijackable libraries..."
            OLD_LD_LIBRARY_PATH=$LD_LIBRARY_PATH
            export LD_LIBRARY_PATH=""
            timeout 2 "$STRACE" "$sname" 2>&1 | grep -i -E "open|access|no such file" | sed -${E} "s,open|access|No such file,${SED_RED}$ITALIC,g"
            printf $NC
            export LD_LIBRARY_PATH=$OLD_LD_LIBRARY_PATH
            echo "----------------------------------------------------------------------------------------"
            echo ""
          fi
        
        fi
      fi
    fi
  fi

  # Reuse this enumeration for the shared privileged-file placement checks.
  check_privileged_file_location "SUID" "$sname" "$sowner"
done;
echo ""
