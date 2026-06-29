# Title: LinPeasBase - check_external_hostname
# ID: check_external_hostname
# Author: Carlos Polop
# Last Update: 23-05-2025
# Description: This will check the public IP and hostname in known malicious lists and leaks to find any relevant information about the host.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $VERSION, $ONLINE_VULN_CHECKS
# Initial Functions:
# Generated Global Variables: $$INTERNET_SEARCH_TIMEOUT, $HACKTRICKS_HOST_CHECKER_URL, $HACKTRICKS_PACKAGE_LIMIT, $_os_id, $_os_version_id, $_os_version_major, $_ecosystem, $_pkg_name, $_pkg_line, $_pkg_version, $_limit, $_os_name, $_os_codename, $_hostname, $_hacktricks_host_checker_url, $_hacktricks_payload, $source, $binary, $Version
# Fat linpeas: 0
# Small linpeas: 1

linpeas_json_escape(){
  printf "%s" "$1" | sed 's/\\/\\\\/g; s/"/\\"/g; s/	/ /g'
}

linpeas_os_release_field(){
  [ -r /etc/os-release ] || return
  awk -F= -v key="$1" '
    $1 == key {
      value=$0
      sub(/^[^=]*=/, "", value)
      gsub(/^"/, "", value)
      gsub(/"$/, "", value)
      print value
      exit
    }
  ' /etc/os-release 2>/dev/null
}

linpeas_os_package_ecosystem(){
  _os_id="$(linpeas_os_release_field ID)"
  _os_version_id="$(linpeas_os_release_field VERSION_ID)"
  _os_version_major="$(printf "%s" "$_os_version_id" | cut -d. -f1)"

  case "$_os_id" in
    debian)  [ "$_os_version_major" ] && printf "Debian:%s" "$_os_version_major" ;;
    ubuntu)  [ "$_os_version_id" ] && printf "Ubuntu:%s" "$_os_version_id" ;;
    alpine)  [ "$_os_version_id" ] && printf "Alpine:v%s" "$_os_version_id" ;;
    fedora)  [ "$_os_version_id" ] && printf "Fedora:%s" "$_os_version_id" ;;
    amzn)    [ "$_os_version_id" ] && printf "Amazon Linux:%s" "$_os_version_id" ;;
    rhel)    [ "$_os_version_major" ] && printf "Red Hat:%s" "$_os_version_major" ;;
    centos)  [ "$_os_version_major" ] && printf "CentOS:%s" "$_os_version_major" ;;
    rocky)   [ "$_os_version_major" ] && printf "Rocky Linux:%s" "$_os_version_major" ;;
    almalinux) [ "$_os_version_major" ] && printf "AlmaLinux:%s" "$_os_version_major" ;;
  esac
}

linpeas_print_package_json_lines(){
  _ecosystem="$(linpeas_os_package_ecosystem)"

  if command -v dpkg-query >/dev/null 2>&1; then
    dpkg-query -W -f='${source:Package}\t${source:Version}\t${binary:Package}\t${Version}\n' 2>/dev/null | awk -F '\t' -v ecosystem="$_ecosystem" '
      function esc(s) { gsub(/\\/,"\\\\",s); gsub(/"/,"\\\"",s); gsub(/\r/," ",s); return s }
      {
        name=$1; version=$2
        if (name == "" || name == "-") { name=$3; version=$4 }
        if (version == "" || version == "-") { version=$4 }
        sub(/^src:/, "", name)
        if (name == "" || version == "") next
        key=name "|" version
        if (seen[key]++) next
        printf "{\"name\":\"%s\",\"version\":\"%s\",\"ecosystem\":\"%s\",\"manager\":\"dpkg\"}\n", esc(name), esc(version), esc(ecosystem)
      }'
  elif command -v rpm >/dev/null 2>&1; then
    rpm -qa --qf '%{NAME}\t%{VERSION}-%{RELEASE}\n' 2>/dev/null | awk -F '\t' -v ecosystem="$_ecosystem" '
      function esc(s) { gsub(/\\/,"\\\\",s); gsub(/"/,"\\\"",s); gsub(/\r/," ",s); return s }
      $1 != "" && $2 != "" {
        key=$1 "|" $2
        if (seen[key]++) next
        printf "{\"name\":\"%s\",\"version\":\"%s\",\"ecosystem\":\"%s\",\"manager\":\"rpm\"}\n", esc($1), esc($2), esc(ecosystem)
      }'
  elif command -v apk >/dev/null 2>&1; then
    apk info 2>/dev/null | while IFS= read -r _pkg_name; do
      [ "$_pkg_name" ] || continue
      _pkg_line="$(apk info -e -v "$_pkg_name" 2>/dev/null | head -n 1)"
      _pkg_version="$(printf "%s" "$_pkg_line" | awk -v n="$_pkg_name" 'index($0, n "-") == 1 { print substr($0, length(n) + 2); exit }')"
      [ "$_pkg_version" ] || continue
      printf '{"name":"%s","version":"%s","ecosystem":"%s","manager":"apk"}\n' \
        "$(linpeas_json_escape "$_pkg_name")" "$(linpeas_json_escape "$_pkg_version")" "$(linpeas_json_escape "$_ecosystem")"
    done
  elif command -v pacman >/dev/null 2>&1; then
    pacman -Q 2>/dev/null | awk -v ecosystem="$_ecosystem" '
      function esc(s) { gsub(/\\/,"\\\\",s); gsub(/"/,"\\\"",s); gsub(/\r/," ",s); return s }
      $1 != "" && $2 != "" {
        key=$1 "|" $2
        if (seen[key]++) next
        printf "{\"name\":\"%s\",\"version\":\"%s\",\"ecosystem\":\"%s\",\"manager\":\"pacman\"}\n", esc($1), esc($2), esc(ecosystem)
      }'
  fi
}

linpeas_packages_json(){
  _limit="${HACKTRICKS_PACKAGE_LIMIT:-300}"
  linpeas_print_package_json_lines | awk -v max="$_limit" '
    BEGIN { first=1; printf "[" }
    NF {
      if (count >= max) next
      if (!first) printf ","
      printf "%s", $0
      first=0
      count++
    }
    END { printf "]" }'
}

linpeas_os_json(){
  _os_id="$(linpeas_os_release_field ID)"
  _os_name="$(linpeas_os_release_field PRETTY_NAME)"
  _os_version_id="$(linpeas_os_release_field VERSION_ID)"
  _os_codename="$(linpeas_os_release_field VERSION_CODENAME)"
  printf '{"id":"%s","name":"%s","version_id":"%s","codename":"%s","kernel":{"release":"%s","version":"%s","arch":"%s"}}' \
    "$(linpeas_json_escape "$_os_id")" \
    "$(linpeas_json_escape "$_os_name")" \
    "$(linpeas_json_escape "$_os_version_id")" \
    "$(linpeas_json_escape "$_os_codename")" \
    "$(linpeas_json_escape "$(uname -r 2>/dev/null)")" \
    "$(linpeas_json_escape "$(uname -v 2>/dev/null)")" \
    "$(linpeas_json_escape "$(uname -m 2>/dev/null)")"
}

linpeas_host_checker_payload(){
  _hostname="$(hostname 2>/dev/null)"
  if [ "$ONLINE_VULN_CHECKS" ]; then
    printf '{"hostname":"%s","source":"linpeas","version":"%s","online_package_check":true,"os":%s,"packages":%s}' \
      "$(linpeas_json_escape "$_hostname")" \
      "$(linpeas_json_escape "$VERSION")" \
      "$(linpeas_os_json)" \
      "$(linpeas_packages_json)"
  else
    printf '{"hostname":"%s","source":"linpeas","version":"%s"}' \
      "$(linpeas_json_escape "$_hostname")" \
      "$(linpeas_json_escape "$VERSION")"
  fi
}


check_external_hostname(){
  INTERNET_SEARCH_TIMEOUT=15
  _hacktricks_host_checker_url="${HACKTRICKS_HOST_CHECKER_URL:-https://tools.hacktricks.wiki/api/host-checker}"
  _hacktricks_payload="$(linpeas_host_checker_payload)"
  # wget or curl?
  if command -v curl >/dev/null 2>&1; then
    curl -s "$_hacktricks_host_checker_url" -H "User-Agent: linpeas" --data-binary "$_hacktricks_payload" -H "Content-Type: application/json" --max-time "$INTERNET_SEARCH_TIMEOUT"
  elif command -v wget >/dev/null 2>&1; then
    wget -q -O - "$_hacktricks_host_checker_url" --header "User-Agent: linpeas" --header "Content-Type: application/json" --post-data "$_hacktricks_payload" --timeout "$INTERNET_SEARCH_TIMEOUT"
  else
    echo "wget or curl not found"
  fi
}
