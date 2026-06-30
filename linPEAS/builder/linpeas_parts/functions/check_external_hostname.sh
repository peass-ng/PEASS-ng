# Title: LinPeasBase - check_external_hostname
# ID: check_external_hostname
# Author: Carlos Polop
# Last Update: 30-06-2026
# Description: This will check the public IP and hostname in known malicious lists and leaks to find any relevant information about the host.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $VERSION, $ONLINE_VULN_CHECKS, $NOT_CHECK_EXTERNAL_HOSTNAME, $E, $SED_RED_YELLOW
# Initial Functions:
# Generated Global Variables: $$INTERNET_SEARCH_TIMEOUT, $TMPDIR, $HACKTRICKS_HOST_CHECKER_URL, $HACKTRICKS_PACKAGE_LIMIT, $LINPEAS_HOST_CHECKER_OUT, $LINPEAS_HOST_CHECKER_ERR, $LINPEAS_HOST_CHECKER_PID, $LINPEAS_HOST_CHECKER_STARTED, $_os_id, $_os_version_id, $_os_version_major, $_ecosystem, $_pkg_name, $_pkg_line, $_pkg_version, $_limit, $_os_name, $_os_codename, $_hostname, $_hacktricks_host_checker_url, $_hacktricks_payload, $source, $binary, $Version
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

linpeas_tabbed_packages_to_json_lines(){
  awk -F '\t' -v manager="$1" -v ecosystem="$2" '
    function esc(s) { gsub(/\\/,"\\\\",s); gsub(/"/,"\\\"",s); gsub(/\r/," ",s); return s }
    $1 != "" && $2 != "" {
      key=$1 "|" $2
      if (seen[key]++) next
      printf "{\"name\":\"%s\",\"version\":\"%s\",\"ecosystem\":\"%s\",\"manager\":\"%s\"}\n", esc($1), esc($2), esc(ecosystem), manager
    }'
}

linpeas_print_package_json_lines(){
  _ecosystem="$(linpeas_os_package_ecosystem)"

  if command -v dpkg-query >/dev/null 2>&1; then
    dpkg-query -W -f='${source:Package}\t${source:Version}\t${binary:Package}\t${Version}\n' 2>/dev/null | awk -F '\t' '
      {
        name=$1; version=$2
        if (name == "" || name == "-") { name=$3; version=$4 }
        if (version == "" || version == "-") { version=$4 }
        sub(/^src:/, "", name)
        if (name == "" || version == "") next
        printf "%s\t%s\n", name, version
      }' | linpeas_tabbed_packages_to_json_lines "dpkg" "$_ecosystem"
  elif command -v rpm >/dev/null 2>&1; then
    rpm -qa --qf '%{NAME}\t%{VERSION}-%{RELEASE}\n' 2>/dev/null | linpeas_tabbed_packages_to_json_lines "rpm" "$_ecosystem"
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
    pacman -Q 2>/dev/null | linpeas_tabbed_packages_to_json_lines "pacman" "$_ecosystem"
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


linpeas_start_host_checker_lookup(){
  [ "$NOT_CHECK_EXTERNAL_HOSTNAME" ] && return
  [ "$LINPEAS_HOST_CHECKER_STARTED" ] && return

  INTERNET_SEARCH_TIMEOUT=15
  LINPEAS_HOST_CHECKER_STARTED="1"
  LINPEAS_HOST_CHECKER_OUT="${TMPDIR:-/tmp}/linpeas_host_checker_$$.json"
  LINPEAS_HOST_CHECKER_ERR="${TMPDIR:-/tmp}/linpeas_host_checker_$$.err"
  _hacktricks_host_checker_url="${HACKTRICKS_HOST_CHECKER_URL:-https://tools.hacktricks.wiki/api/host-checker}"
  _hacktricks_payload="$(linpeas_host_checker_payload)"

  if command -v curl >/dev/null 2>&1; then
    (curl -s "$_hacktricks_host_checker_url" -H "User-Agent: linpeas" --data-binary "$_hacktricks_payload" -H "Content-Type: application/json" --max-time "$INTERNET_SEARCH_TIMEOUT" > "$LINPEAS_HOST_CHECKER_OUT" 2>"$LINPEAS_HOST_CHECKER_ERR") &
    LINPEAS_HOST_CHECKER_PID=$!
  elif command -v wget >/dev/null 2>&1; then
    (wget -q -O - "$_hacktricks_host_checker_url" --header "User-Agent: linpeas" --header "Content-Type: application/json" --post-data "$_hacktricks_payload" --timeout "$INTERNET_SEARCH_TIMEOUT" > "$LINPEAS_HOST_CHECKER_OUT" 2>"$LINPEAS_HOST_CHECKER_ERR") &
    LINPEAS_HOST_CHECKER_PID=$!
  else
    printf '{"error":"wget or curl not found"}\n' > "$LINPEAS_HOST_CHECKER_OUT"
  fi
}

linpeas_wait_host_checker_lookup(){
  [ "$NOT_CHECK_EXTERNAL_HOSTNAME" ] && return 1
  linpeas_start_host_checker_lookup
  if [ "$LINPEAS_HOST_CHECKER_PID" ]; then
    wait "$LINPEAS_HOST_CHECKER_PID" 2>/dev/null
    LINPEAS_HOST_CHECKER_PID=""
  fi
  [ -s "$LINPEAS_HOST_CHECKER_OUT" ]
}

linpeas_strip_package_vulns_from_host_response(){
  if command -v jq >/dev/null 2>&1; then
    jq 'del(.package_vulnerabilities)' "$LINPEAS_HOST_CHECKER_OUT" 2>/dev/null && return
  fi

  awk '
    function brace_delta(s, t, opens, closes) {
      t=s; opens=gsub(/{/,"{",t)
      t=s; closes=gsub(/}/,"}",t)
      return opens - closes
    }
    skip {
      depth += brace_delta($0)
      if (depth <= 0) skip=0
      next
    }
    /"package_vulnerabilities"[[:space:]]*:/ {
      sub(/,[[:space:]]*$/, "", prev)
      skip=1
      depth=brace_delta($0)
      if (depth <= 0) skip=0
      next
    }
    {
      if (have) print prev
      prev=$0
      have=1
    }
    END {
      if (have) print prev
    }
  ' "$LINPEAS_HOST_CHECKER_OUT"
}

check_external_hostname(){
  if linpeas_wait_host_checker_lookup; then
    linpeas_strip_package_vulns_from_host_response
  else
    echo "HackTricks host checker did not return data"
  fi
}

linpeas_print_package_vulnerabilities_with_jq(){
  jq -r '
    .package_vulnerabilities as $pv
    | if ($pv == null) then
        empty
      elif (($pv.affected // 0) | tonumber) == 0 then
        "No vulnerable packages found by online lookup (checked \($pv.checked // 0) packages)."
      else
        "Online package vulnerabilities found: \($pv.affected) vulnerable package(s), checked \($pv.checked // 0).",
        (
          $pv.vulnerable_packages[:50][]?
          | "- \(.name // "?") \(.version // "?") [\(.ecosystem // "unknown")]: \((.vulns // []) | join(", "))"
        ),
        (
          if (($pv.vulnerable_packages | length) > 50) then
            "... \((($pv.vulnerable_packages | length) - 50)) more vulnerable package(s) not shown."
          else empty end
        )
      end
  ' "$LINPEAS_HOST_CHECKER_OUT" 2>/dev/null
}

linpeas_print_package_vulnerabilities_with_awk(){
  awk '
    function json_value(line) {
      sub(/^[^:]*:[[:space:]]*"/, "", line)
      sub(/",?[[:space:]]*$/, "", line)
      return line
    }
    function json_number(line) {
      sub(/^[^:]*:[[:space:]]*/, "", line)
      sub(/,?[[:space:]]*$/, "", line)
      return line
    }
    function json_string(line) {
      sub(/^[[:space:]]*"/, "", line)
      sub(/",?[[:space:]]*$/, "", line)
      return line
    }
    function flush_pkg() {
      if (name != "" && version != "") {
        total++
        if (shown < 50) {
          print "- " name " " version " [" (ecosystem != "" ? ecosystem : "unknown") "]: " vulns
          shown++
        }
      }
      name=""; version=""; ecosystem=""; vulns=""; in_vulns=0
    }
    /"package_vulnerabilities"[[:space:]]*:/ { in_pv=1; next }
    in_pv && /"checked"[[:space:]]*:/ { checked=json_number($0); next }
    in_pv && /"affected"[[:space:]]*:/ { affected=json_number($0); next }
    in_pv && /"vulnerable_packages"[[:space:]]*:[[:space:]]*\[/ { in_pkgs=1; next }
    in_pkgs && /"name"[[:space:]]*:/ { name=json_value($0); next }
    in_pkgs && /"version"[[:space:]]*:/ { version=json_value($0); next }
    in_pkgs && /"ecosystem"[[:space:]]*:/ { ecosystem=json_value($0); next }
    in_pkgs && /"vulns"[[:space:]]*:[[:space:]]*\[/ { in_vulns=1; next }
    in_vulns && /"/ {
      v=json_string($0)
      if (v != "") vulns = vulns (vulns != "" ? ", " : "") v
      next
    }
    in_vulns && /\]/ { in_vulns=0; next }
    in_pkgs && /^[[:space:]]*}[,]?[[:space:]]*$/ { flush_pkg(); next }
    END {
      if (affected == "") exit
      if ((affected + 0) == 0) {
        print "No vulnerable packages found by online lookup (checked " (checked != "" ? checked : 0) " packages)."
      } else {
        print "Online package vulnerabilities found: " affected " vulnerable package(s), checked " (checked != "" ? checked : 0) "."
        if (total > 50) print "... " (total - 50) " more vulnerable package(s) not shown."
      }
    }
  ' "$LINPEAS_HOST_CHECKER_OUT" | awk '
    /^Online package vulnerabilities found:/ { header=$0; next }
    /^No vulnerable packages found/ { print; next }
    /^\.\.\. / { more=$0; next }
    /^- / { lines[++count]=$0; next }
    END {
      if (header) print header
      for (i=1; i<=count && i<=50; i++) print lines[i]
      if (more) print more
    }
  '
}

linpeas_print_package_vulnerabilities(){
  [ "$ONLINE_VULN_CHECKS" ] || return

  if ! linpeas_wait_host_checker_lookup; then
    echo "Online package vulnerability lookup did not return data"
    return
  fi

  if command -v jq >/dev/null 2>&1; then
    linpeas_print_package_vulnerabilities_with_jq | sed -${E} "s,CVE-[0-9]{4}-[0-9]+,${SED_RED_YELLOW},g"
  else
    linpeas_print_package_vulnerabilities_with_awk | sed -${E} "s,CVE-[0-9]{4}-[0-9]+,${SED_RED_YELLOW},g"
  fi
}
