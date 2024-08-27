# Title: Variables - PATH
# ID: PATH
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Path
# License: GNU GPL
# Version: 1.0
# Functions Used: 
# Global Variables:
# Initial Functions:
# Generated Global Variables: $PATH, $ADDPATH, $OLDPATH, $spath
# Fat linpeas: 0
# Small linpeas: 1


OLDPATH=$PATH
ADDPATH=":/usr/local/sbin\
 :/usr/local/bin\
 :/usr/sbin\
 :/usr/bin\
 :/sbin\
 :/bin"
spath=":$PATH"
for P in $ADDPATH; do
  if [ "${spath##*$P*}" ]; then export PATH="$PATH$P" 2>/dev/null; fi
done