# Title: Container - enumerateDockerDesktopAPI
# ID: enumerateDockerDesktopAPI
# Author: Carlos Polop
# Last Update: 24-06-2026
# Description: Check from inside a container if the Docker Desktop internal Engine API (CVE-2025-9074) is reachable over TCP on 192.168.65.7:2375 even when the docker socket is NOT mounted, which allows a full container escape.
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $GREP_DOCKER_SOCK_INFOS, $GREP_DOCKER_SOCK_INFOS_IGNORE
# Initial Functions:
# Generated Global Variables: $SEARCHED_DOCKER_DESKTOP_API, $ddEndpoint, $ddInfoResponse
# Fat linpeas: 0
# Small linpeas: 1


enumerateDockerDesktopAPI() {
  if ! [ "$SEARCHED_DOCKER_DESKTOP_API" ]; then
    SEARCHED_DOCKER_DESKTOP_API="1"
    # Docker Desktop exposes its internal Engine API on the VM services host 192.168.65.7.
    # CVE-2025-9074 (fixed in Docker Desktop 4.44.3) let a container reach this UNAUTHENTICATED
    # Engine API on 192.168.65.7:2375 even when /var/run/docker.sock was NOT mounted, enabling a
    # full container escape (e.g. creating a container that bind-mounts the host filesystem).
    # Ref: https://nvd.nist.gov/vuln/detail/CVE-2025-9074
    ddEndpoint="http://192.168.65.7:2375/info"
    ddInfoResponse=""
    if [ "$(command -v curl 2>/dev/null || echo -n '')" ]; then
      ddInfoResponse="$(curl -s --max-time 3 "$ddEndpoint" 2>/dev/null)"
    elif [ "$(command -v wget 2>/dev/null || echo -n '')" ]; then
      ddInfoResponse="$(wget -q -T 3 -O - "$ddEndpoint" 2>/dev/null)"
    fi
    if echo "$ddInfoResponse" | grep -q "ServerVersion"; then
      echo "Docker Desktop internal Engine API (CVE-2025-9074) reachable at 192.168.65.7:2375 - container escape possible!" | sed -${E} "s,reachable at 192.168.65.7:2375,${SED_RED_YELLOW},g"
      echo "$ddInfoResponse" | tr ',' '\n' | grep -E "$GREP_DOCKER_SOCK_INFOS" | grep -v "$GREP_DOCKER_SOCK_INFOS_IGNORE" | tr -d '"'
    fi
  fi
}
