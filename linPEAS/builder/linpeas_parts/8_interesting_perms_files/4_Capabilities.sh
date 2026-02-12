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
# Generated Global Variables: $cap_name, $cap_value, $cap_line, $capVB, $capname, $capbins, $capsVB_vuln, $proc_status, $proc_pid, $proc_name, $proc_uid, $user_name, $proc_inh, $proc_prm, $proc_eff, $proc_bnd, $proc_amb, $proc_inh_dec, $proc_prm_dec, $proc_eff_dec, $proc_bnd_dec, $proc_amb_dec
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
        # Add validation check for cap_value
        # For more POSIX-compliant formatting, the following could be used instead:
        # if echo "$cap_value" | grep -E '^[0-9a-fA-F]+$' > /dev/null 2>&1; then
        if [[ "$cap_value" =~ ^[0-9a-fA-F]+$ ]]; then
          # Memory errors can occur with certain values (e.g., ffffffffffffffff)
          # so we redirect stderr to prevent error propagation
          echo "$cap_name	 $(capsh --decode=0x"$cap_value" 2>/dev/null | sed -${E} "s,$capsB,${SED_RED_YELLOW},")"
        else
          echo "$cap_name	 [Invalid capability format]"
        fi
      else
        # Add validation check for cap_value
        if [[ "$cap_value" =~ ^[0-9a-fA-F]+$ ]]; then
          # Memory errors can occur with certain values (e.g., ffffffffffffffff)
          # so we redirect stderr to prevent error propagation
          echo "$cap_name  $(capsh --decode=0x"$cap_value" 2>/dev/null | sed -${E} "s,$capsB,${SED_RED},")"
        else
          echo "$cap_name  [Invalid capability format]"
        fi
      fi
    done
    echo ""
    print_info "Parent process capabilities"
    cat "/proc/$PPID/status" | grep Cap | while read -r cap_line; do
      cap_name=$(echo "$cap_line" | awk '{print $1}')
      cap_value=$(echo "$cap_line" | awk '{print $2}')
      if [ "$cap_name" = "CapEff:" ]; then
        # Add validation check for cap_value
        if [[ "$cap_value" =~ ^[0-9a-fA-F]+$ ]]; then
          # Memory errors can occur with certain values (e.g., ffffffffffffffff)
          # so we redirect stderr to prevent error propagation
          echo "$cap_name	 $(capsh --decode=0x"$cap_value" 2>/dev/null | sed -${E} "s,$capsB,${SED_RED_YELLOW},")"
        else
          echo "$cap_name	 [Invalid capability format]"
        fi
      else
        # Add validation check for cap_value
        if [[ "$cap_value" =~ ^[0-9a-fA-F]+$ ]]; then
          # Memory errors can occur with certain values (e.g., ffffffffffffffff)
          # so we redirect stderr to prevent error propagation
          echo "$cap_name	 $(capsh --decode=0x"$cap_value" 2>/dev/null | sed -${E} "s,$capsB,${SED_RED},")"
        else
          echo "$cap_name	 [Invalid capability format]"
        fi
      fi
    done
    echo ""

    print_3title "Processes with capability sets (non-zero CapEff/CapAmb, limit 40)"
    find /proc -maxdepth 2 -path "/proc/[0-9]*/status" 2>/dev/null | head -n 400 | while read -r proc_status; do
      proc_pid=$(echo "$proc_status" | cut -d/ -f3)
      proc_name=$(awk '/^Name:/{print $2}' "$proc_status" 2>/dev/null)
      proc_uid=$(awk '/^Uid:/{print $2}' "$proc_status" 2>/dev/null)
      user_name=$(awk -F: -v uid="$proc_uid" '$3==uid{print $1; exit}' /etc/passwd 2>/dev/null)
      [ -z "$user_name" ] && user_name="$proc_uid"

      proc_inh=$(awk '/^CapInh:/{print $2}' "$proc_status" 2>/dev/null)
      proc_prm=$(awk '/^CapPrm:/{print $2}' "$proc_status" 2>/dev/null)
      proc_eff=$(awk '/^CapEff:/{print $2}' "$proc_status" 2>/dev/null)
      proc_bnd=$(awk '/^CapBnd:/{print $2}' "$proc_status" 2>/dev/null)
      proc_amb=$(awk '/^CapAmb:/{print $2}' "$proc_status" 2>/dev/null)

      [ -z "$proc_eff" ] && continue
      if [ "$proc_eff" != "0000000000000000" ] || [ "$proc_amb" != "0000000000000000" ]; then
        echo "PID $proc_pid ($proc_name) user=$user_name"

        proc_inh_dec=$(capsh --decode=0x"$proc_inh" 2>/dev/null)
        proc_prm_dec=$(capsh --decode=0x"$proc_prm" 2>/dev/null)
        proc_eff_dec=$(capsh --decode=0x"$proc_eff" 2>/dev/null)
        proc_bnd_dec=$(capsh --decode=0x"$proc_bnd" 2>/dev/null)
        proc_amb_dec=$(capsh --decode=0x"$proc_amb" 2>/dev/null)

        echo "  CapInh: $proc_inh_dec" | sed -${E} "s,$capsB,${SED_RED},g"
        echo "  CapPrm: $proc_prm_dec" | sed -${E} "s,$capsB,${SED_RED},g"
        echo "  CapEff: $proc_eff_dec" | sed -${E} "s,$capsB,${SED_RED_YELLOW},g"
        echo "  CapBnd: $proc_bnd_dec" | sed -${E} "s,$capsB,${SED_RED},g"
        echo "  CapAmb: $proc_amb_dec" | sed -${E} "s,$capsB,${SED_RED_YELLOW},g"
        echo ""
      fi
    done | head -n 240
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
