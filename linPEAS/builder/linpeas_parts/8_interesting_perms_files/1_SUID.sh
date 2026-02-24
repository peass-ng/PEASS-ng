# Title: Interesting Permissions Files - SUID
# ID: IP_SUID
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: SUID - Check easy privesc, exploits and write perms
# License: GNU GPL
# Version: 1.0
# Functions Used: check_unknown_sxid_bin, echo_not_found, print_2title, print_info
# Global Variables: $IAMROOT, $ROOT_FOLDER, $sidB, $sidG1, $sidG2, $sidG3, $sidG4, $sidVB, $sidVB2, $STRACE, $STRINGS
# Initial Functions:
# Generated Global Variables: $suids_files, $sname
# Fat linpeas: 0
# Small linpeas: 1


print_2title "SUID - Check easy privesc, exploits and write perms"
print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#sudo-and-suid"
if ! [ "$STRINGS" ]; then
  echo_not_found "strings"
fi
if ! [ "$STRACE" ]; then
  echo_not_found "strace"
fi
suids_files=$(find $ROOT_FOLDER -perm -4000 -type f ! -path "/dev/*" 2>/dev/null)
printf "%s\n" "$suids_files" | while read s; do
  [ -z "$s" ] && continue
  s=$(ls -lahtr "$s")
  #If starts like "total 332K" then no SUID bin was found and xargs just executed "ls" in the current folder
  if echo "$s" | grep -qE "^total"; then break; fi

  sname="$(echo $s | awk '{print $9}')"
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
        check_unknown_sxid_bin "$s" "$sname" "Unknown SUID binary!" "(https://tinyurl.com/suidpath)" "1" "1" "0"
      fi
    fi
  fi
done;
echo ""
