# Title: Functions - checkPamAllowActiveCVE20256018
# ID: checkPamAllowActiveCVE20256018
# Author: PEASS-ng
# Last Update: 08-10-2026
# Description: Passive SUSE 15 PAM stack evidence for CVE-2025-6018.
# License: GNU GPL
# Version: 1.0
# Mitre: T1068
# Functions Used: lp_suse_numeric_version_lt, print_3title, print_info
# Global Variables: $ROOT_FOLDER
# Initial Functions:
# Generated Global Variables: $pa6018_root, $pa6018_os, $pa6018_id, $pa6018_version, $pa6018_auth, $pa6018_session, $pa6018_sshd, $pa6018_env_line, $pa6018_pam, $pa6018_pam_config, $pa6018_config, $pa6018_status
# Fat linpeas: 0
# Small linpeas: 1

checkPamAllowActiveCVE20256018() { (
  [ "$(uname -s 2>/dev/null)" = Linux ] || return 0
  pa6018_root="${ROOT_FOLDER:-/}"
  case "$pa6018_root" in */) ;; *) pa6018_root="$pa6018_root/" ;; esac
  pa6018_os="${pa6018_root}etc/os-release"
  [ -r "$pa6018_os" ] || return 0
  pa6018_id="$(sed -n '1,100{s/^ID=[" ]*\([^" ]*\).*/\1/p;};101q' "$pa6018_os" | head -n 1)"
  pa6018_version="$(sed -n '1,100{s/^VERSION_ID=[" ]*\([^" ]*\).*/\1/p;};101q' "$pa6018_os" | head -n 1)"
  case "$pa6018_id:$pa6018_version" in
    sles:15*|sled:15*|opensuse-leap:15*|opensuse:15*) ;;
    *) return 0 ;;
  esac

  pa6018_auth="${pa6018_root}etc/pam.d/common-auth"
  pa6018_session="${pa6018_root}etc/pam.d/common-session"
  pa6018_sshd="${pa6018_root}etc/pam.d/sshd"
  [ -r "$pa6018_auth" ] && [ -r "$pa6018_session" ] && [ -r "$pa6018_sshd" ] || return 0
  # Inspect only the named SSH stack files. A user supplied PAM environment is
  # relevant only when pam_env precedes pam_systemd in this stack.
  sed -n '1,200p;201q' "$pa6018_sshd" | grep -Eq '^[[:space:]]*(auth[[:space:]]+(include|substack)[[:space:]]+common-auth|@include[[:space:]]+common-auth)' || return 0
  sed -n '1,200p;201q' "$pa6018_sshd" | grep -Eq '^[[:space:]]*(session[[:space:]]+(include|substack)[[:space:]]+common-session|@include[[:space:]]+common-session)' || return 0
  pa6018_env_line="$(sed -n '1,200p;201q' "$pa6018_auth" | grep -E '^[[:space:]]*auth[[:space:]].*pam_env[.]so([[:space:]]|$)' | head -n 1)"
  [ -n "$pa6018_env_line" ] || return 0
  sed -n '1,200p;201q' "$pa6018_session" | grep -Eq '^[[:space:]]*session[[:space:]].*pam_systemd[.]so([[:space:]]|$)' || return 0
  if printf '%s\n' "$pa6018_env_line" | grep -Eq '(^|[[:space:]])user_readenv=0($|[[:space:]])'; then
    return 0
  elif printf '%s\n' "$pa6018_env_line" | grep -Eq '(^|[[:space:]])user_readenv=1($|[[:space:]])'; then
    pa6018_config="explicit user_readenv=1"
  else
    pa6018_config="implicit user_readenv; effective default unknown"
  fi

  pa6018_pam="unknown"
  pa6018_pam_config="unknown"
  if command -v timeout >/dev/null 2>&1 && command -v rpm >/dev/null 2>&1; then
    pa6018_pam="$(timeout 3 rpm --root "$pa6018_root" -q --qf '%{VERSION}-%{RELEASE}\n' pam 2>/dev/null | awk 'NR == 1 && /^[0-9]/ { print }')"
    [ -n "$pa6018_pam" ] || pa6018_pam="unknown"
    pa6018_pam_config="$(timeout 3 rpm --root "$pa6018_root" -q --qf '%{VERSION}-%{RELEASE}\n' pam-config 2>/dev/null | awk 'NR == 1 && /^[0-9]/ { print }')"
    [ -n "$pa6018_pam_config" ] || pa6018_pam_config="unknown"
  fi
  pa6018_status="unknown"
  case "$pa6018_id:$pa6018_version" in
    sles:15.6|sles:15.7|sled:15.6|sled:15.7|opensuse-leap:15.6|opensuse:15.6)
      if [ "$pa6018_config" = "explicit user_readenv=1" ]; then
        # The PAM update changes the default; an explicit setting overrides it.
        pa6018_status="explicit user_readenv=1 in the effective auth stack"
      elif lp_suse_numeric_version_lt "$pa6018_pam" "1.3.0-150000.6.83.1"; then
        # The package that controls pam_env's default is below SUSE's fix.
        # The actual stack still has pam_env in auth, regardless of whether
        # a newer pam-config package has been installed since it was written.
        pa6018_status="pam below SUSE fixed package version"
      else
        return 0
      fi
      ;;
    *) return 0 ;;
  esac
  print_3title "SUSE PAM allow_active path (CVE-2025-6018)" "T1068"
  print_info "https://www.suse.com/security/cve/CVE-2025-6018.html"
  echo "SSH includes common-auth and common-session; common-auth loads pam_env before common-session loads pam_systemd"
  echo "pam_env: $pa6018_config; pam package: $pa6018_pam; pam-config package: $pa6018_pam_config"
  echo "Package evidence: $pa6018_status; effective patch status: unknown, verify vendor advisories and PAM configuration"
  echo ""
) ; }
