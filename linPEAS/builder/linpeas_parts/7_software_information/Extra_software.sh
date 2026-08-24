# Title: Software Information - Extra sotftare
# ID: SI_Extra_software
# Author: Carlos Polop
# Last Update: 17-08-2026
# Description: Add all extra software checks from build_lists/sensitive_files.yaml and check needrestart interpreter-scanner LPE exposure.
# License: GNU GPL
# Version: 1.1
# Mitre: T1082,T1068
# Functions Used: checkNeedrestartCVE202448990, print_3title, warn_exec
# Global Variables: $NGINX_KNOWN_MODULES, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


if ! [ "$SEARCH_IN_FOLDER" ]; then
  checkNeedrestartCVE202448990
fi

peass{EXTRA_SECTIONS}
