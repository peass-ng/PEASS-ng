# Title: Functions - lp_extract_upstream_version
# ID: lp_extract_upstream_version
# Author: Chack Agent
# Last Update: 07-09-2026
# Description: Extract a numeric dotted upstream version from a package version.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


lp_extract_upstream_version() {
  printf '%s' "$1" | sed -E 's/^[0-9]+://; s/^[^0-9]*//; s/[^0-9.].*$//'
}
