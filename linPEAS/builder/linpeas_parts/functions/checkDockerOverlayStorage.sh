# Title: Container - checkDockerOverlayStorage
# ID: checkDockerOverlayStorage
# Author: Carlos Polop
# Last Update: 09-10-2026
# Description: List a bounded set of already-mounted Docker overlay roots traversable by this unprivileged account.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $cdos_mounts, $cdos_path, $cdos_root
# Fat linpeas: 0
# Small linpeas: 1


checkDockerOverlayStorage() {
  # Optional paths allow a synthetic mount table and directory tree in tests.
  cdos_root="${1:-/var/lib/docker}"
  cdos_mounts="${2:-/proc/self/mountinfo}"
  [ "$(id -u 2>/dev/null)" = "0" ] && return
  [ -d "$cdos_root/overlay2" ] && [ -x "$cdos_root" ] &&
    [ -x "$cdos_root/overlay2" ] && [ -r "$cdos_mounts" ] || return 0

  # The mount table gives exact active paths even when overlay2 is not listable.
  # Accept only overlay filesystem mounts at 64-hex-id merged roots.
  # Stop after twelve candidates.
  awk -v prefix="$cdos_root/overlay2/" '
    index($5, prefix) == 1 {
      for (i = 7; i <= NF; i++) if ($i == "-") break
      if (i >= NF || $(i + 1) != "overlay") next
      suffix = substr($5, length(prefix) + 1)
      if (length(suffix) != 71 || substr(suffix, 1, 64) !~ /^[0-9a-f]+$/ ||
          substr(suffix, 65) != "/merged") next
      if ($6 ~ /(^|,)nosuid(,|$)/ || $6 ~ /(^|,)noexec(,|$)/) next
      if (!seen[$5]++) {
        print $5
        if (++count >= 12) exit
      }
    }
  ' "$cdos_mounts" 2>/dev/null | while IFS= read -r cdos_path; do
    [ -d "$cdos_path" ] && [ -x "$cdos_path" ] && printf '%s\n' "$cdos_path"
  done
}
