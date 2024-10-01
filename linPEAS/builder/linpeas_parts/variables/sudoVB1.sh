# Title: Variables - sudoVB1
# ID: sudoVB1
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Very bad sudoers configuration
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $sudoVB1, $sudoVB2
# Fat linpeas: 0
# Small linpeas: 1


sudoVB1=" \*|env_keep\W*\+=.*LD_PRELOAD|env_keep\W*\+=.*LD_LIBRARY_PATH|peass{SUDOVB1_HERE}"
sudoVB2="peass{SUDOVB2_HERE}"