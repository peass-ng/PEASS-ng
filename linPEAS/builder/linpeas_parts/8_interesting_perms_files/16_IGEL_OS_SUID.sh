# Title: Interesting Permissions Files - IGEL OS SUID setup/date abuse
# ID: IP_IGEL_OS_SUID
# Author: HT Bot
# Last Update: 29-11-2025
# Description: Detect IGEL OS environments that expose the SUID-root `setup`/`date` binaries and highlight writable NetworkManager/systemd configs that enable the documented privilege escalation chain (Metasploit linux/local/igel_network_priv_esc).
# License: GNU GPL
# Version: 1.0
# Functions Used: print_2title, print_info
# Global Variables: $ITALIC, $NC, $SED_GREEN, $SED_RED, $SED_RED_YELLOW, $SUPERFAST
# Initial Functions:
# Generated Global Variables: $igel_markers, $igel_marker_sources, $marker, $igel_suid_hits, $candidate, $writable_nm, $writable_systemd, $unitdir, $tmp_units
# Fat linpeas: 0
# Small linpeas: 1

igel_markers=""
igel_marker_sources=""
if [ -f /etc/os-release ] && grep -qi "igel" /etc/os-release 2>/dev/null; then
  igel_markers="Yes"
  igel_marker_sources="/etc/os-release"
fi
if [ -f /etc/issue ] && grep -qi "igel" /etc/issue 2>/dev/null; then
  igel_markers="Yes"
  igel_marker_sources="${igel_marker_sources} /etc/issue"
fi
for marker in /etc/igel /wfs/igel /userhome/.igel /config/sessions/igel; do
  if [ -e "$marker" ]; then
    igel_markers="Yes"
    igel_marker_sources="${igel_marker_sources} $marker"
  fi
done

igel_suid_hits=""
for candidate in /usr/bin/setup /bin/setup /usr/sbin/setup /opt/igel/bin/setup /usr/bin/date /bin/date /usr/lib/igel/date; do
  if [ -u "$candidate" ]; then
    igel_suid_hits="${igel_suid_hits}$(ls -lah "$candidate" 2>/dev/null)\n"
  fi
done

if [ -n "$igel_markers" ] || [ -n "$igel_suid_hits" ]; then
  print_2title "IGEL OS SUID setup/date privilege escalation surface"
  print_info "https://www.rapid7.com/blog/post/pt-metasploit-wrap-up-11-28-2025"
  if [ -n "$igel_markers" ]; then
    echo "Potential IGEL OS detected via: $igel_marker_sources" | sed -${E} "s,.*,${SED_GREEN},"
  else
    echo "IGEL-specific SUID helpers found but IGEL markers were not detected" | sed -${E} "s,.*,${SED_RED},"
  fi
  if [ -n "$igel_suid_hits" ]; then
    echo "SUID-root helpers exposing configuration primitives:" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    printf "%b" "$igel_suid_hits"
  else
    echo "No SUID setup/date binaries were located (system may be patched)."
  fi

  writable_nm=""
  writable_systemd=""
  if ! [ "$SUPERFAST" ]; then
    if [ -d /etc/NetworkManager ]; then
      writable_nm=$(find /etc/NetworkManager -maxdepth 3 -type f -writable 2>/dev/null | head -n 25)
    fi
    for unitdir in /etc/systemd/system /lib/systemd/system /usr/lib/systemd/system; do
      if [ -d "$unitdir" ]; then
        tmp_units=$(find "$unitdir" -maxdepth 2 -type f -writable 2>/dev/null | head -n 15)
        if [ -n "$tmp_units" ]; then
          writable_systemd="${writable_systemd}${tmp_units}\n"
        fi
      fi
    done
  fi

  if [ -n "$writable_nm" ]; then
    echo "Writable NetworkManager profiles/hooks (swap Exec path to your payload):" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    echo "$writable_nm"
  fi
  if [ -n "$writable_systemd" ]; then
    echo "Writable systemd unit files (edit ExecStart, then restart via setup/date):" | sed -${E} "s,.*,${SED_RED_YELLOW},"
    printf "%b" "$writable_systemd"
  fi
  printf "$ITALIC  Known exploitation chain: Use the SUID setup/date binaries to edit NetworkManager or systemd configs so ExecStart points to your payload, then trigger a service restart via the same helper to run as root (Metasploit linux/local/igel_network_priv_esc).$NC\n"
fi
echo ""
