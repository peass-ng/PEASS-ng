# Title: LinPeasBase - check_unknown_sxid_bin
# ID: check_unknown_sxid_bin
# Author: HT Bot
# Last Update: 24-02-2026
# Description: Shared checks for unknown SUID/SGID binaries
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $E, $FAST, $ITALIC, $LDD, $NC, $READELF, $RED, $SED_RED, $SED_RED_YELLOW, $STRACE, $STRINGS, $TIMEOUT, $Wfolders, $cfuncs
# Initial Functions:
# Generated Global Variables: $LD_LIBRARY_PATH, $OLD_LD_LIBRARY_PATH, $filter_dotdot, $readelf_filter_wfolders, $sline, $sline_first, $strings_suppress_errors, $strings_url_suffix, $sxid_ls, $sxid_name, $unknown_msg, $url_suffix
# Fat linpeas: 0
# Small linpeas: 1


check_unknown_sxid_bin(){
  local sxid_ls="$1"
  local sxid_name="$2"
  local unknown_msg="$3"
  local strings_url_suffix="$4"
  local strings_suppress_errors="$5"
  local filter_dotdot="$6"
  local readelf_filter_wfolders="$7"
  local url_suffix=""

  if [ "$strings_url_suffix" ]; then
    url_suffix=" $strings_url_suffix"
  fi

  echo "$sxid_ls ($unknown_msg)" | sed -${E} "s,/.*,${SED_RED},"
  printf $ITALIC
  if ! [ "$FAST" ]; then

    if [ "$STRINGS" ]; then
      if [ "$strings_suppress_errors" = "1" ]; then
        "$STRINGS" "$sxid_name" 2>/dev/null | sort | uniq | while read sline; do
          sline_first="$(echo "$sline" | cut -d ' ' -f1)"
          if echo "$sline_first" | grep -qEv "$cfuncs"; then
            if echo "$sline_first" | grep -q "/" && [ -f "$sline_first" ]; then #If a path
              if [ -O "$sline_first" ] || [ -w "$sline_first" ]; then #And modifiable
                printf "$ITALIC  --- It looks like $RED$sxid_name$NC$ITALIC is using $RED$sline_first$NC$ITALIC and you can modify it (strings line: $sline)$url_suffix\n"
              fi
            elif echo "$sline_first" | grep -q "/" && [ -d "$(dirname "$sline_first")" ] && [ -w "$(dirname "$sline_first")" ]; then #If path does not exist but can be created
              printf "$ITALIC  --- It looks like $RED$sxid_name$NC$ITALIC is using $RED$sline_first$NC$ITALIC and you can create it inside writable dir $RED$(dirname "$sline_first")$NC$ITALIC (strings line: $sline)$url_suffix\n"
            else #If not a path
              if [ ${#sline_first} -gt 2 ] && command -v "$sline_first" 2>/dev/null | grep -q "/"; then #Check if existing binary
                if [ "$filter_dotdot" = "1" ] && echo "$sline_first" | grep -Eq "\.\."; then
                  true
                else
                  printf "$ITALIC  --- It looks like $RED$sxid_name$NC$ITALIC is executing $RED$sline_first$NC$ITALIC and you can impersonate it (strings line: $sline)$url_suffix\n"
                fi
              fi
            fi
          fi
        done
      else
        "$STRINGS" "$sxid_name" | sort | uniq | while read sline; do
          sline_first="$(echo $sline | cut -d ' ' -f1)"
          if echo "$sline_first" | grep -qEv "$cfuncs"; then
            if echo "$sline_first" | grep -q "/" && [ -f "$sline_first" ]; then #If a path
              if [ -O "$sline_first" ] || [ -w "$sline_first" ]; then #And modifiable
                printf "$ITALIC  --- It looks like $RED$sxid_name$NC$ITALIC is using $RED$sline_first$NC$ITALIC and you can modify it (strings line: $sline)$url_suffix\n"
              fi
            elif echo "$sline_first" | grep -q "/" && [ -d "$(dirname "$sline_first")" ] && [ -w "$(dirname "$sline_first")" ]; then #If path does not exist but can be created
              printf "$ITALIC  --- It looks like $RED$sxid_name$NC$ITALIC is using $RED$sline_first$NC$ITALIC and you can create it inside writable dir $RED$(dirname "$sline_first")$NC$ITALIC (strings line: $sline)$url_suffix\n"
            else #If not a path
              if [ ${#sline_first} -gt 2 ] && command -v "$sline_first" 2>/dev/null | grep -q "/"; then #Check if existing binary
                if [ "$filter_dotdot" = "1" ] && echo "$sline_first" | grep -Eq "\.\."; then
                  true
                else
                  printf "$ITALIC  --- It looks like $RED$sxid_name$NC$ITALIC is executing $RED$sline_first$NC$ITALIC and you can impersonate it (strings line: $sline)$url_suffix\n"
                fi
              fi
            fi
          fi
        done
      fi
    fi

    if [ "$LDD" ] || [ "$READELF" ]; then
      echo "$ITALIC  --- Checking for writable dependencies of $sxid_name...$NC"
    fi
    if [ "$LDD" ]; then
      "$LDD" "$sxid_name" | grep -E "$Wfolders" | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
    fi
    if [ "$READELF" ]; then
      if [ "$readelf_filter_wfolders" = "1" ]; then
        "$READELF" -d "$sxid_name" | grep PATH | grep -E "$Wfolders" | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
      else
        "$READELF" -d "$sxid_name" | grep PATH | sed -${E} "s,$Wfolders,${SED_RED_YELLOW},g"
      fi
    fi

    if [ "$TIMEOUT" ] && [ "$STRACE" ] && [ -x "$sxid_name" ]; then
      printf $ITALIC
      echo "----------------------------------------------------------------------------------------"
      echo "  --- Trying to execute $sxid_name with strace in order to look for hijackable libraries..."
      OLD_LD_LIBRARY_PATH=$LD_LIBRARY_PATH
      export LD_LIBRARY_PATH=""
      timeout 2 "$STRACE" "$sxid_name" 2>&1 | grep -i -E "open|access|no such file" | sed -${E} "s,open|access|No such file,${SED_RED}$ITALIC,g"
      printf $NC
      export LD_LIBRARY_PATH=$OLD_LD_LIBRARY_PATH
      echo "----------------------------------------------------------------------------------------"
      echo ""
    fi

  fi
}
