# Title: Interesting Files - AI Storage Permissions
# ID: IF_AI_storage_permissions
# Author: PEASS-ng contributors
# Last Update: 11-10-2026
# Description: Review current-user AI storage directory permissions without reading stored data
# License: GNU GPL
# Version: 1.0
# Mitre: T1083
# Functions Used: print_2title
# Global Variables: $HOME, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $ai_mode, $ai_shared, $ai_relative, $ai_link, $ai_tool, $ai_found, $ai_path, $ai_ancestor
# Fat linpeas: 0
# Small linpeas: 0

if [ -z "$SEARCH_IN_FOLDER" ] && [ -n "$HOME" ]; then
  print_2title "Current-user AI storage directory permissions" "T1083"
  (
    case "$HOME" in /*) ;; *) exit ;; esac
    printf '%s\n' "Directory modes only; review ACLs separately. Stored data may include private conversations and configuration."
    ai_found=0
    while IFS='|' read -r ai_tool ai_relative; do
      ai_path="$HOME/$ai_relative"
      # Check every ancestor before inspecting the directory. Do not follow links.
      ai_ancestor="$ai_path"
      ai_link=0
      while [ "$ai_ancestor" != / ] && [ -n "$ai_ancestor" ]; do
        if [ -L "$ai_ancestor" ]; then ai_link=1; break; fi
        ai_ancestor=${ai_ancestor%/*}
      done
      if [ "$ai_link" = 1 ]; then
        printf '  %s: skipped symbolic link in directory path\n' "$ai_tool"
        continue
      fi
      [ -d "$ai_path" ] || continue
      ai_found=1
      ai_mode=$(LC_ALL=C ls -ld "$ai_path" 2>/dev/null | awk '{print $1}')
      if [ -z "$ai_mode" ]; then
        printf '  %s: directory permissions unavailable\n' "$ai_tool"
        continue
      fi
      printf '  %s: %s (%s)\n' "$ai_tool" "$ai_path" "$ai_mode"
      ai_shared=$(printf '%s' "$ai_mode" | cut -c5-10)
      case "$ai_shared" in
        *[rwxst]*) printf '    Review group/other access to potentially sensitive AI storage.\n' ;;
      esac
    done <<'AI_STORAGE_ROOTS'
Codex|.codex
Claude Code|.claude
Cursor CLI|.cursor
Cursor desktop (Linux)|.config/Cursor
Cursor desktop (macOS)|Library/Application Support/Cursor
Antigravity CLI|.gemini/antigravity-cli
Grok|.grok
AI_STORAGE_ROOTS
    [ "$ai_found" = 1 ] || printf '%s\n' "  No supported storage directories found."
  )
  echo ""
fi
