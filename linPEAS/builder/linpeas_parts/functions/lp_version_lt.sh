# Title: Functions - lp_version_lt
# ID: lp_version_lt
# Author: Chack Agent
# Last Update: 07-09-2026
# Description: Compare numeric dotted versions and succeed when the first is lower.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


lp_version_lt() {
  [ -n "$1" ] && [ -n "$2" ] || return 1
  awk -v lp_a="$1" -v lp_b="$2" 'BEGIN {
    lp_na = split(lp_a, lp_av, ".")
    lp_nb = split(lp_b, lp_bv, ".")
    lp_n = lp_na > lp_nb ? lp_na : lp_nb
    for (lp_i = 1; lp_i <= lp_n; lp_i++) {
      lp_ai = lp_av[lp_i] + 0
      lp_bi = lp_bv[lp_i] + 0
      if (lp_ai < lp_bi) exit 0
      if (lp_ai > lp_bi) exit 1
    }
    exit 1
  }'
}
