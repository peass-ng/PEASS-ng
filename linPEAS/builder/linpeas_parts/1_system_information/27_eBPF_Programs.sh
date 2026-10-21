# Title: System Information - eBPF Programs
# ID: SY_eBPF_Programs
# Author: HT Bot
# Last Update: 21-10-2026
# Description: Passively inventory BPF programs, links, maps and bpffs pins for userspace-tampering hooks.
# License: GNU GPL
# Version: 1.0
# Mitre: T1014,T1548.001
# Functions Used: print_2title, print_info
# Global Variables: $MACPEAS, $SEARCH_IN_FOLDER, $TIMEOUT
# Initial Functions:
# Generated Global Variables: $ebpf_prog_json, $ebpf_link_json, $ebpf_map_json, $ebpf_candidate_ids, $ebpf_candidate_id, $ebpf_helper_hits
# Fat linpeas: 0
# Small linpeas: 0


# bpftool's -n avoids mounting bpffs/tracefs while inspecting the host.
# Bound every external inspection; never load a program or dump map contents.
if [ ! "$MACPEAS" ] && [ ! "$SEARCH_IN_FOLDER" ] && [ -d /sys/fs/bpf ]; then
  print_2title "Live eBPF state (passive review)" "T1014"
  print_info "BPF programs can rewrite sudo/read or process listings without changing files or kernel modules. Compare tags and attach points with a trusted baseline; capabilities are shown in the Capabilities section."

  if [ "$TIMEOUT" ] && command -v bpftool >/dev/null 2>&1; then
    if command -v jq >/dev/null 2>&1; then
      if ebpf_prog_json=$("$TIMEOUT" 1 bpftool -j -n prog show 2>/dev/null) &&
         printf '%s' "$ebpf_prog_json" | jq -e 'type == "array"' >/dev/null 2>&1; then
        echo "Programs (first 20; tags are more stable than IDs):"
        printf '%s' "$ebpf_prog_json" | jq -r '.[:20][] | "  id=\(.id) type=\(.type // "-") name=\(.name // "-") tag=\(.tag // "-")"'
      else
        echo "Programs: unavailable (permissions, unsupported bpftool, or timeout)."
        ebpf_prog_json=""
      fi
      if ebpf_link_json=$("$TIMEOUT" 1 bpftool -j -n link show 2>/dev/null) &&
         printf '%s' "$ebpf_link_json" | jq -e 'type == "array"' >/dev/null 2>&1; then
        echo "Links (first 20; inspect syscall tracepoints and kprobes):"
        printf '%s' "$ebpf_link_json" | jq -r '.[:20][] | "  id=\(.id) prog_id=\(.prog_id // "-") type=\(.type // "-") attach=\(.tracepoint // .func // .target_name // .name // .attach_type // "-")"'
      else
        echo "Links: unavailable (permissions, unsupported bpftool, or timeout)."
        ebpf_link_json=""
      fi
      if ebpf_map_json=$("$TIMEOUT" 1 bpftool -j -n map show 2>/dev/null) &&
         printf '%s' "$ebpf_map_json" | jq -e 'type == "array"' >/dev/null 2>&1; then
        echo "Maps (first 20):"
        printf '%s' "$ebpf_map_json" | jq -r '.[:20][] | "  id=\(.id) type=\(.type // "-") name=\(.name // "-")"'
      else
        echo "Maps: unavailable (permissions, unsupported bpftool, or timeout)."
      fi

      # Only inspect two programs tied by name or link to sensitive syscall hooks.
      # The translated dump is read-only; timeout also bounds very large programs.
      if [ -n "$ebpf_prog_json" ]; then
        ebpf_candidate_ids=$(
          printf '%s' "$ebpf_link_json" | jq -r '.[] | select((.tracepoint // .func // .target_name // .name // "") | test("(openat|read|close|getdents64|execve|ptrace|sys_bpf)"; "i")) | .prog_id // empty' 2>/dev/null
          printf '%s' "$ebpf_prog_json" | jq -r '.[] | select((.name // "") | test("(openat|read|close|getdents64|execve|ptrace|sys_bpf)"; "i")) | .id // empty' 2>/dev/null
        )
        ebpf_candidate_ids=$(printf '%s\n' "$ebpf_candidate_ids" | sort -u | head -n 2)
        for ebpf_candidate_id in $ebpf_candidate_ids; do
          case "$ebpf_candidate_id" in *[!0-9]*|'') continue ;; esac
          ebpf_helper_hits=$("$TIMEOUT" 1 bpftool -n prog dump xlated id "$ebpf_candidate_id" 2>/dev/null |
            grep -Eo 'bpf_(probe_write_user|override_return|send_signal)(#[0-9]+)?' | sort -u | head -n 3)
          if [ "$ebpf_helper_hits" ]; then
            printf 'Review BPF program id %s (sensitive hook/name and helper): %s\n' "$ebpf_candidate_id" "$(printf '%s' "$ebpf_helper_hits" | tr '\n' ' ')"
          fi
        done
      fi
    else
      echo "Install jq for bounded JSON summaries; raw bpftool inventory (first 30 lines):"
      "$TIMEOUT" 1 bpftool -n prog show 2>/dev/null | head -n 30
      "$TIMEOUT" 1 bpftool -n link show 2>/dev/null | head -n 30
      "$TIMEOUT" 1 bpftool -n map show 2>/dev/null | head -n 30
    fi
  else
    echo "bpftool and timeout are needed to inspect live BPF programs, links and maps."
  fi

  echo "bpffs pins (first 20, depth <= 3):"
  if [ "$TIMEOUT" ]; then
    "$TIMEOUT" 1 find /sys/fs/bpf -maxdepth 3 -type f -print 2>/dev/null | head -n 20
  else
    echo "Skipped: timeout is unavailable."
  fi
  if [ -w /sys/fs/bpf ] && [ "$(id -u 2>/dev/null)" != 0 ]; then
    echo "Review: bpffs root is writable by this non-root user (pinning capability is separate)."
  fi
  echo "BPF use and pins are not proof of compromise; investigate unexpected loaders and boot-time reload services."
  echo ""
fi
