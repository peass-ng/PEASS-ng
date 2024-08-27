# Title: Users Information - Brute su
# ID: UG_Brute_su
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Brute su
# License: GNU GPL
# Version: 1.0
# Functions Used: check_if_su_brute, print_2title, su_brute_user_num
# Global Variables: $IAMROOT, $PASSTRY, $TIMEOUT
# Initial Functions:
# Generated Global Variables: $SHELLUSERS, $POSSIBE_SU_BRUTE
# Fat linpeas: 0
# Small linpeas: 0


if ! [ "$FAST" ] && ! [ "$SUPERFAST" ] && [ "$TIMEOUT" ] && ! [ "$IAMROOT" ]; then
  print_2title "Testing 'su' as other users with shell using as passwords: null pwd, the username and top2000pwds\n"$NC
  POSSIBE_SU_BRUTE=$(check_if_su_brute);
  if [ "$POSSIBE_SU_BRUTE" ]; then
    SHELLUSERS=$(cat /etc/passwd 2>/dev/null | grep -i "sh$" | cut -d ":" -f 1)
    printf "%s\n" "$SHELLUSERS" | while read u; do
      echo "  Bruteforcing user $u..."
      su_brute_user_num "$u" $PASSTRY
    done
  else
    printf $GREEN"It's not possible to brute-force su.\n\n"$NC
  fi
else
  print_2title "Do not forget to test 'su' as any other user with shell: without password and with their names as password (I don't do it in FAST mode...)\n"$NC
fi
print_2title "Do not forget to execute 'sudo -l' without password or with valid password (if you know it)!!\n"$NC