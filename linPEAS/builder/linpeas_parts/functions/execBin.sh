# Title: LinPeasBase - execBin
# ID: execBin
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Write and exected an embedded binary
# License: GNU GPL
# Version: 1.0
# Functions Used: print_3title, print_info
# Global Variables: $Wfolder
# Initial Functions:
# Generated Global Variables: $TOOL_NAME, $TOOL_LINK, $B64_BIN, $PARAMS
# Fat linpeas: 0
# Small linpeas: 1


execBin(){
  TOOL_NAME=$1
  TOOL_LINK=$2
  B64_BIN=$3
  PARAMS=$4
  if [ "$B64_BIN" ]; then
    echo ""
    print_3title "Running $TOOL_NAME"
    print_info "$TOOL_LINK"
    echo "$B64_BIN" | base64 -d > $Wfolder/bin
    chmod +x $Wfolder/bin
    eval "$Wfolder/bin $PARAMS"
    rm -f $Wfolder/bin
    echo ""
  fi
}