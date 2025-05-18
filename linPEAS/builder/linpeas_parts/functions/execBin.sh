# Title: LinPeasBase - execBin
# ID: execBin
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Write and execute an embedded binary
# License: GNU GPL
# Version: 1.0
# Functions Used: print_3title, print_info
# Global Variables: $Wfolder
# Initial Functions:
# Generated Global Variables: $TOOL_NAME, $TOOL_LINK, $B64_BIN, $PARAMS, $TMP_BIN, $cmdpid, $watcher, $rc
# Fat linpeas: 0
# Small linpeas: 1


execBin() {
  TOOL_NAME=$1        # Display name
  TOOL_LINK=$2        # Reference URL
  B64_BIN=$3          # base64‑encoded executable
  PARAMS=$4           # Arguments to the tool

  [ -z "$B64_BIN" ] && return 0   # nothing to do

  echo
  print_3title "Running $TOOL_NAME"
  print_info  "$TOOL_LINK"

  TMP_BIN=$(mktemp "${Wfolder:-/tmp}/bin.XXXXXX") || { echo "mktemp failed"; return 1; }
  printf '%s' "$B64_BIN" | base64 -d > "$TMP_BIN" || { echo "decode failed"; rm -f "$TMP_BIN"; return 1; }
  chmod +x "$TMP_BIN"

  # ---------------- 120‑second wall‑clock timeout ----------------
  if command -v timeout >/dev/null 2>&1; then                 # GNU/BSD timeout
      timeout --preserve-status -s 9 120 "$TMP_BIN" $PARAMS
  elif command -v gtimeout >/dev/null 2>&1; then              # Homebrew coreutils (macOS)
      gtimeout --preserve-status -s 9 120 "$TMP_BIN" $PARAMS
  else                                                        # POSIX fall‑back
      (
        "$TMP_BIN" $PARAMS &                                  # run in background
        cmdpid=$!
        ( sleep 120 && kill -9 "$cmdpid" 2>/dev/null) &
        watcher=$!
        wait "$cmdpid"
        rc=$?
        kill -9 "$watcher" 2>/dev/null
        exit $rc
      )
  fi
  rc=$?

  rm -f "$TMP_BIN"
  echo
  return $rc
}