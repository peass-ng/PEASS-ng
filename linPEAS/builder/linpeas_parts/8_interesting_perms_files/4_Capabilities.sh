# Title: Interesting Permissions Files - Capabilities
# ID: IP_Capabilities
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Capabilities
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found, print_2title, print_info, print_3title
# Global Variables: $capsB, $capsVB, $IAMROOT, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $cap_name, $cap_value, $cap_line, $capVB, $capname, $capbins, $capsVB_vuln
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Capabilities"
  print_info "https://book.hacktricks.wiki/en/linux-hardening/privilege-escalation/index.html#capabilities"
  if [ "$(command -v capsh || echo -n '')" ]; then

    print_3title "Current shell capabilities"
    cat "/proc/$$/status" | grep Cap | while read -r cap_line; do
      cap_name=$(echo "$cap_line" | awk '{print $1}')
      cap_value=$(echo "$cap_line" | awk '{print $2}')
      if [ "$cap_name" = "CapEff:" ]; then
        echo "$cap_name	 $(capsh --decode=0x"$cap_value" | sed -${E} "s,$capsB,${SED_RED_YELLOW},")"
      else
        echo "$cap_name  $(capsh --decode=0x"$cap_value" | sed -${E} "s,$capsB,${SED_RED},")"
      fi
    done
    echo ""

    print_info "Parent process capabilities"
    cat "/proc/$PPID/status" | grep Cap | while read -r cap_line; do
      cap_name=$(echo "$cap_line" | awk '{print $1}')
      cap_value=$(echo "$cap_line" | awk '{print $2}')
      if [ "$cap_name" = "CapEff:" ]; then
        echo "$cap_name	 $(capsh --decode=0x"$cap_value" | sed -${E} "s,$capsB,${SED_RED_YELLOW},")"
      else
        echo "$cap_name	 $(capsh --decode=0x"$cap_value" | sed -${E} "s,$capsB,${SED_RED},")"
      fi
    done
    echo ""
  
  else
    print_3title "Current shell capabilities"
    (cat "/proc/$$/status" | grep Cap | sed -${E} "s,.*0000000000000000|CapBnd:	0000003fffffffff,${SED_GREEN},") 2>/dev/null || echo_not_found "/proc/$$/status"
    echo ""
    
    print_3title "Parent proc capabilities"
    (cat "/proc/$PPID/status" | grep Cap | sed -${E} "s,.*0000000000000000|CapBnd:	0000003fffffffff,${SED_GREEN},") 2>/dev/null || echo_not_found "/proc/$PPID/status"
    echo ""
  fi
  echo ""
  echo "Files with capabilities (limited to 50):"
  getcap -r / 2>/dev/null | head -n 50 | while read cb; do
    capsVB_vuln=""
    
    for capVB in $capsVB; do
      capname="$(echo $capVB | cut -d ':' -f 1)"
      capbins="$(echo $capVB | cut -d ':' -f 2)"
      if [ "$(echo $cb | grep -Ei $capname)" ] && [ "$(echo $cb | grep -E $capbins)" ]; then
        echo "$cb" | sed -${E} "s,.*,${SED_RED_YELLOW},"
        capsVB_vuln="1"
        break
      fi
    done
    
    if ! [ "$capsVB_vuln" ]; then
      echo "$cb" | sed -${E} "s,$capsB,${SED_RED},"
    fi

    if ! [ "$IAMROOT" ] && [ -w "$(echo $cb | cut -d" " -f1)" ]; then
      echo "$cb is writable" | sed -${E} "s,.*,${SED_RED},"
    fi
  done
  echo ""
fi