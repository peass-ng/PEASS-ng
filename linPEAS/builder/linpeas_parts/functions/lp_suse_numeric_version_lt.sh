# Title: Functions - lp_suse_numeric_version_lt
# ID: lp_suse_numeric_version_lt
# Author: PEASS-ng
# Last Update: 08-10-2026
# Description: Compare numeric SUSE package version-release strings; return 2 when the format is unknown.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1

lp_suse_numeric_version_lt() {
  case "$1" in *[!0-9.-]*|"") return 2 ;; esac
  case "$2" in *[!0-9.-]*|"") return 2 ;; esac
  awk -v installed="$1" -v fixed="$2" 'BEGIN {
    n = split(installed, a, /[.:-]/)
    m = split(fixed, b, /[.:-]/)
    for (i = 1; i <= n || i <= m; i++) {
      if (a[i] + 0 < b[i] + 0) exit 0
      if (a[i] + 0 > b[i] + 0) exit 1
    }
    exit 1
  }'
}
