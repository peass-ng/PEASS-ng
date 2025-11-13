# Title: Software Information - Imunify360/Ai-Bolit RCE (<=32.7.4.0)
# ID: SI_Imunify360_AiBolit
# Author: HT Bot
# Last Update: 13-11-2025
# Description: Detect Imunify360/Ai-Bolit presence, version and risky execution flags related to the deobfuscation RCE fixed in v32.7.4.0
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_3title, print_info
# Global Variables: $DEBUG, $HOME
# Initial Functions:
# Generated Global Variables: $ai_bolit_version, $ai_bolit_vuln, $imunify_pkgs, $ai_bolit_installed, $ps_matches, $units, $writable_webroot, $risk, $vmin, $deobf_refs
# Fat linpeas: 0
# Small linpeas: 1


# Quick detector for Imunify360 / Ai-Bolit installation
ai_bolit_installed=""
for p in \
  /opt/ai-bolit \
  /opt/ai-bolit/wrapper \
  /usr/bin/imunify-antivirus \
  /usr/bin/imunify360-agent \
  /opt/imunify* \
  /usr/share/imunify*; do
  [ -e "$p" ] && ai_bolit_installed=1 && break
done

# Also consider it present if any package is installed
imunify_pkgs=$( (rpm -qa 2>/dev/null | grep -Ei '^(imunify|imunify360|imunify-antivirus)'; dpkg -l 2>/dev/null | grep -Ei 'imunify|imunify360') 2>/dev/null )
if [ "$imunify_pkgs" ] && [ -z "$ai_bolit_installed" ]; then ai_bolit_installed=1; fi

if [ "$ai_bolit_installed" ] || [ "$DEBUG" ]; then
  print_2title "Imunify360/Ai-Bolit RCE (<=32.7.4.0) exposure check"

  # Show installed packages
  if [ "$imunify_pkgs" ]; then
    print_info "Installed Imunify packages (package manager):"
    printf "%s\n" "$imunify_pkgs"
    echo ""
  fi

  # Try to obtain Ai-Bolit version from common locations (do not execute third-party binaries)
  ai_bolit_version=""
  if [ -r "/opt/ai-bolit/VERSION" ]; then
    ai_bolit_version=$(head -n1 /opt/ai-bolit/VERSION 2>/dev/null | tr -d ' \t\r')
  elif [ -r "/opt/ai-bolit/version" ]; then
    ai_bolit_version=$(head -n1 /opt/ai-bolit/version 2>/dev/null | tr -d ' \t\r')
  fi

  if [ "$ai_bolit_version" ]; then
    printf "Ai-Bolit version: %s\n" "$ai_bolit_version"
  else
    printf "Ai-Bolit version: unknown (could not read /opt/ai-bolit/VERSION)\n"
  fi

  # Determine if version is vulnerable (< 32.7.4.0)
  ai_bolit_vuln=""
  if [ "$ai_bolit_version" ]; then
    vmin=$(printf '%s\n' "$ai_bolit_version" "32.7.4.0" | sort -V | head -n1)
    if [ "$vmin" = "$ai_bolit_version" ] && [ "$ai_bolit_version" != "32.7.4.0" ]; then
      ai_bolit_vuln=1
    fi
  else
    # If we cannot read the version but the product is present, assume unknown/possibly vulnerable
    ai_bolit_vuln="unknown"
  fi

  # Look for running processes that may invoke Ai-Bolit or Imunify and check for --deobfuscate and privileges
  ps_matches=$(ps -eo user:12,pid,cmd 2>/dev/null | grep -Ei '(ai-bolit|imunify|scanner\.py)' | grep -v grep)
  if [ "$ps_matches" ]; then
    print_info "Running Imunify/Ai-Bolit related processes:"
    # Highlight --deobfuscate and root user
    printf "%s\n" "$ps_matches" \
      | sed -${E} "s, --deobfuscate, ${SED_RED}," \
      | sed -${E} "s,^root,${SED_RED},"
    echo ""
  fi

  # Check systemd units and whether --deobfuscate is in ExecStart
  if command -v systemctl >/dev/null 2>&1; then
    units=$(systemctl list-units --type=service --all --no-pager 2>/dev/null | grep -Ei '(imunify|ai-bolit)' | awk '{print $1}' | sort -u)
    if [ "$units" ]; then
      print_info "Systemd service definitions (grep ExecStart/User):"
      for u in $units; do
        echo "[Unit] $u"
        systemctl cat "$u" 2>/dev/null | grep -E '^(User=|Group=|ExecStart=)' \
          | sed -${E} "s, --deobfuscate, ${SED_RED}," \
          | sed -${E} "s,^User=\s*root,${SED_RED},"
      done
      echo ""
    fi
  fi

  # Wrapper/orchestrator hint: check common source paths for the --deobfuscate flag (bounded search)
  deobf_refs=$(grep -RIl --max-depth=4 --binary-files=without-match -E "--deobfuscate" \
    /opt/imunify* /usr/share/imunify* /opt/ai-bolit* 2>/dev/null | head -n 5)
  if [ "$deobf_refs" ]; then
    print_info "Files referencing --deobfuscate (first hits):"
    printf "%s\n" "$deobf_refs"
    echo ""
  fi

  # Simple heuristic: can the current user write to common website roots?
  writable_webroot=""
  for w in "$HOME/public_html" "$HOME/www" "$HOME/html" "$HOME/htdocs" "$HOME/public_www"; do
    if [ -d "$w" ] && [ -w "$w" ]; then writable_webroot=1; echo "Writable webroot detected: $w" | sed -${E} "s,.*,${SED_YELLOW},"; fi
  done
  [ "$writable_webroot" ] && echo ""

  # Final risk summary
  risk="LOW"
  if [ "$ai_bolit_vuln" = "1" ]; then
    risk="MEDIUM"
  elif [ "$ai_bolit_vuln" = "unknown" ]; then
    risk="MEDIUM (version unknown)"
  fi
  if [ "$ps_matches" ] && echo "$ps_matches" | grep -q -- "--deobfuscate" && echo "$ps_matches" | awk '{print $1}' | grep -q '^root$'; then
    if [ "$ai_bolit_vuln" ]; then risk="HIGH"; fi
  fi

  print_3title "Ai-Bolit deobfuscation RCE exposure: RISK = $risk"
  if [ "$ai_bolit_vuln" = "1" ]; then
    echo "Detected Ai-Bolit < 32.7.4.0. Update to >= 32.7.4.0 or later." | sed -${E} "s,.*,${SED_RED},"
  elif [ "$ai_bolit_vuln" = "unknown" ]; then
    echo "Ai-Bolit present but version unknown. Verify patch level (>= 32.7.4.0)." | sed -${E} "s,.*,${SED_YELLOW},"
  fi
  echo "If wrapper/services run with --deobfuscate as root, a low-privileged user who can place PHP files in scanned paths may achieve code execution via the scanner." | sed -${E} "s,.*,${SED_YELLOW},"
  echo ""
fi
