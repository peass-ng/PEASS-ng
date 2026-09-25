# Title: Interesting Permissions Files - SUID
# ID: IP_SUID
# Author: Carlos Polop, HT Bot
# Last Update: 25-09-2026
# Description: SUID - Check easy privesc, exploits, write perms, and risky file placement
# License: GNU GPL
# Version: 1.1
# Mitre: T1548.001
# Functions Used: echo_not_found, print_2title, print_info
# Global Variables: $IAMROOT, $LDD, $ROOT_FOLDER, $READELF, $sidB, $sidG1, $sidG2, $sidG3, $sidG4, $sidVB, $sidVB2, $STRACE, $STRINGS, $TIMEOUT, $Wfolders, $cfuncs
# Initial Functions:
# Generated Global Variables: $suids_files, $sfile, $sname, $sowner, $sparent, $sunusual, $sline_first, $sline, $OLD_LD_LIBRARY_PATH, $LD_LIBRARY_PATH
# Fat linpeas: 0
# Small linpeas: 1


print_2title "SUID - Check easy privesc, exploits and write perms" "T1548.001"
print_info "https://book.hacktricks.wiki/en/linux-hardening/linux-basics/linux-privilege-escalation/index.html#sudo-and-suid"
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
  if [ "$sname" = "."  ] || [ "$sname" = ".."  ]; then
    true #Don't do nothing
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

  # A privileged file in a user-controlled location is especially suspicious.
  # Reuse the SUID enumeration above instead of performing another filesystem scan.
  sunusual=""
  case "$sname" in
    "${ROOT_FOLDER}tmp/"*|"${ROOT_FOLDER}var/tmp/"*|"${ROOT_FOLDER}dev/shm/"*|"${ROOT_FOLDER}run/user/"*|"${ROOT_FOLDER}var/run/user/"*|"${ROOT_FOLDER}home/"*)
      echo "SUID file in a user-writable or unusual location: $sname" | sed -${E} "s,.*,${SED_RED_YELLOW},"
      sunusual="1"
      ;;
  esac

  sparent="$(dirname "$sname")"
  if ! [ "$IAMROOT" ] && [ -d "$sparent" ] && [ -w "$sparent" ] && [ -x "$sparent" ] && ! [ -k "$sparent" ]; then
    echo "You can replace entries in the SUID file's parent directory: $sparent" | sed -${E} "s,.*,${SED_RED_YELLOW},"
  fi
  if [ "$sowner" ] && [ "$sowner" != "root" ]; then
    echo "SUID file is owned by non-root user $sowner: $sname" | sed -${E} "s,.*,${SED_RED},"
  fi
  if [ "$sunusual" ] && find "$sname" -mtime -7 -print 2>/dev/null | grep -q .; then
    echo "SUID file in an unusual location was modified in the last 7 days: $sname" | sed -${E} "s,.*,${SED_RED},"
  fi
done;
echo ""
