# Title: Software Information - Tmux
# ID: SI_Tmux
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Enumerate Tmux
# License: GNU GPL
# Version: 1.0
# Mitre: T1563
# Functions Used: print_2title, print_info
# Global Variables: $DEBUG, $SEARCH_IN_FOLDER
# Initial Functions:
# Generated Global Variables: $tmuxdefsess, $tmuxnondefsess, $tmuxsess2, $tmux_scan_output, $tmux_socket_candidates, $tmux_scan_partial, $tmux_root, $tmux_root_seen, $tmux_dir, $tmux_socket, $tmux_dir_count, $tmux_entry_count, $tmux_partial, $tmux_uid, $tmux_groups, $tmux_meta, $tmux_owner_uid, $tmux_owner_gid, $tmux_rest, $tmux_mode, $tmux_mode_writable, $TMUX_TMPDIR
# Fat linpeas: 0
# Small linpeas: 1


# The standard socket lives directly below tmux-UID. Inspect only that shape,
# never recurse through /tmp or attach to a session. The second root is used
# only when TMUX_TMPDIR names a real absolute directory.
tmux_socket_review() (
  tmux_uid=$(id -u 2>/dev/null) || exit 0
  tmux_groups=$(id -G 2>/dev/null) || exit 0
  tmux_dir_count=0
  tmux_entry_count=0
  tmux_partial=
  tmux_root_seen=0
  for tmux_root in "$1" "$2"; do
    [ -n "$tmux_root" ] || continue
    case "$tmux_root" in /*) ;; *) continue ;; esac
    [ -d "$tmux_root" ] && [ ! -L "$tmux_root" ] || continue
    if [ "$tmux_root_seen" -gt 0 ] && [ "$tmux_root" = "$1" ]; then continue; fi
    tmux_root_seen=$((tmux_root_seen + 1))
    for tmux_dir in "$tmux_root"/tmux-*; do
      [ -d "$tmux_dir" ] && [ ! -L "$tmux_dir" ] || continue
      if [ "$tmux_dir_count" -ge 32 ]; then tmux_partial=1; break 2; fi
      tmux_dir_count=$((tmux_dir_count + 1))
      printf 'DIR %s\n' "$tmux_dir"
      for tmux_socket in "$tmux_dir"/*; do
        [ -e "$tmux_socket" ] || [ -L "$tmux_socket" ] || continue
        if [ "$tmux_entry_count" -ge 64 ]; then tmux_partial=1; break 3; fi
        tmux_entry_count=$((tmux_entry_count + 1))
        [ ! -L "$tmux_socket" ] && [ -S "$tmux_socket" ] || continue
        tmux_meta=$(stat -c '%u:%g:%A' "$tmux_socket" 2>/dev/null) ||
          tmux_meta=$(stat -f '%u:%g:%Sp' "$tmux_socket" 2>/dev/null) || continue
        case "$tmux_meta" in *:*:*) ;; *) continue ;; esac
        tmux_owner_uid=${tmux_meta%%:*}
        tmux_rest=${tmux_meta#*:}
        tmux_owner_gid=${tmux_rest%%:*}
        tmux_mode=${tmux_rest#*:}
        [ "$tmux_owner_uid" != "$tmux_uid" ] || continue
        tmux_mode_writable=
        case "$tmux_mode" in ????????w?) tmux_mode_writable=1 ;; esac
        case "$tmux_mode" in
          ?????w????)
            case " $tmux_groups " in *" $tmux_owner_gid "*) tmux_mode_writable=1 ;; esac
            ;;
        esac
        [ "$tmux_mode_writable" ] && [ -w "$tmux_socket" ] || continue
        printf 'SOCKET %s\n' "$tmux_socket"
      done
    done
  done
  [ "$tmux_partial" ] && printf 'PARTIAL\n'
  return 0
)

tmuxdefsess=$(tmux ls 2>/dev/null)
tmuxnondefsess=$(ps auxwww | grep "tmux " | grep -v grep)
TMUX_TMPDIR=${TMUX_TMPDIR:-}
tmux_scan_output=
if ! [ "$SEARCH_IN_FOLDER" ]; then
  tmux_scan_output=$(tmux_socket_review /tmp "$TMUX_TMPDIR")
fi
tmuxsess2=$(printf '%s\n' "$tmux_scan_output" | sed -n 's/^DIR //p')
tmux_socket_candidates=$(printf '%s\n' "$tmux_scan_output" | sed -n 's/^SOCKET //p')
tmux_scan_partial=$(printf '%s\n' "$tmux_scan_output" | sed -n '/^PARTIAL$/p')
if ([ "$tmuxdefsess" ] || [ "$tmuxnondefsess" ] || [ "$tmuxsess2" ] || [ "$tmux_scan_partial" ] || [ "$DEBUG" ]) && ! [ "$SEARCH_IN_FOLDER" ]; then
  print_2title "Searching tmux sessions"$N
  print_info "https://book.hacktricks.wiki/en/linux-hardening/linux-basics/linux-privilege-escalation/index.html#open-shell-sessions"
  tmux -V
  printf '%s\n%s\n%s\n' "$tmuxdefsess" "$tmuxnondefsess" "$tmuxsess2" | sed -${E} "s,.*,${SED_RED}," | sed -${E} "s,no server running on.*,${C}[32m&${C}[0m,"

  if [ "$tmux_socket_candidates" ]; then
    printf '%s\n' "$tmux_socket_candidates" | sed "s,.*,Other user tmux socket is writable: ${SED_RED_YELLOW}&,"
  fi
  if [ "$tmux_scan_partial" ]; then
    echo "Tmux socket review partial: stopped after 32 directories or 64 direct entries."
  fi
  echo ""
fi
