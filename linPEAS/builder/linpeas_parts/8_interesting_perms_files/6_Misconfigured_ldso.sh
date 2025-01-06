# Title: Interesting Permissions Files - Misconfigured ld.so
# ID: IP_Misconfigured_ldso
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Checking misconfigurations of ld.so
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $IAMROOT, $ITALIC, $SEARCH_IN_FOLDER, $USER, $Wfolders, $wgroups
# Initial Functions:
# Generated Global Variables: $ini_path, $fpath
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ] && ! [ "$IAMROOT" ]; then
  print_2title "Checking misconfigurations of ld.so"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#ldso"
  if [ -f "/etc/ld.so.conf" ] && [ -w "/etc/ld.so.conf" ]; then 
    echo "You have write privileges over /etc/ld.so.conf" | sed -${E} "s,.*,${SED_RED_YELLOW},"; 
    printf $RED$ITALIC"/etc/ld.so.conf\n"$NC;
  else
    printf $GREEN$ITALIC"/etc/ld.so.conf\n"$NC;
  fi

  echo "Content of /etc/ld.so.conf:"
  cat /etc/ld.so.conf 2>/dev/null | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"

  # Check each configured folder
  cat /etc/ld.so.conf 2>/dev/null | while read l; do
    if echo "$l" | grep -q include; then
      ini_path=$(echo "$l" | cut -d " " -f 2)
      fpath=$(dirname "$ini_path")

      if [ -d "/etc/ld.so.conf" ] && [ -w "$fpath" ]; then 
        echo "You have write privileges over $fpath" | sed -${E} "s,.*,${SED_RED_YELLOW},"; 
        printf $RED_YELLOW$ITALIC"$fpath\n"$NC;
      else
        printf $GREEN$ITALIC"$fpath\n"$NC;
      fi

      if [ "$(find $fpath -type f '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null)" ]; then
        echo "You have write privileges over $(find $fpath -type f '(' '(' -user $USER ')' -or '(' -perm -o=w ')' -or  '(' -perm -g=w -and '(' $wgroups ')' ')' ')' 2>/dev/null)" | sed -${E} "s,.*,${SED_RED_YELLOW},"; 
      fi

      for f in $fpath/*; do
        if [ -w "$f" ]; then 
          echo "You have write privileges over $f" | sed -${E} "s,.*,${SED_RED_YELLOW},"; 
          printf $RED_YELLOW$ITALIC"$f\n"$NC;
        else
          printf $GREEN$ITALIC"  $f\n"$NC;
        fi

        cat "$f" | grep -v "^#" | while read l2; do
          if [ -f "$l2" ] && [ -w "$l2" ]; then 
            echo "You have write privileges over $l2" | sed -${E} "s,.*,${SED_RED_YELLOW},"; 
            printf $RED_YELLOW$ITALIC"  - $l2\n"$NC;
          else
            echo $ITALIC"  - $l2"$NC | sed -${E} "s,$l2,${SED_GREEN}," | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g";
          fi
        done
      done
    fi
  done
  echo ""


  if [ -f "/etc/ld.so.preload" ] && [ -w "/etc/ld.so.preload" ]; then 
    echo "You have write privileges over /etc/ld.so.preload" | sed -${E} "s,.*,${SED_RED_YELLOW},"; 
  else
    printf $ITALIC$GREEN"/etc/ld.so.preload\n"$NC;
  fi
  cat /etc/ld.so.preload 2>/dev/null | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
  cat /etc/ld.so.preload 2>/dev/null | while read l; do
    if [ -f "$l" ] && [ -w "$l" ]; then echo "You have write privileges over $l" | sed -${E} "s,.*,${SED_RED_YELLOW},"; fi
  done

fi