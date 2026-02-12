# Title: Container - enumerateDockerSockets
# ID: enumerateDockerSockets
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Search Docker Sockets
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found
# Global Variables: $GREP_DOCKER_SOCK_INFOS, $GREP_DOCKER_SOCK_INFOS_IGNORE
# Initial Functions:
# Generated Global Variables: $SEARCHED_DOCKER_SOCKETS, $docker_enumerated, $dockerVersion, $int_sock, $sockInfoResponse
# Fat linpeas: 0
# Small linpeas: 1


enumerateDockerSockets() {
  dockerVersion="$(echo_not_found)"
  if ! [ "$SEARCHED_DOCKER_SOCKETS" ]; then
    SEARCHED_DOCKER_SOCKETS="1"
    # NOTE: This is intentionally "lightweight" (checks common runtime socket names) and avoids
    # pseudo filesystems (/sys, /proc) to reduce noise and latency.
    for int_sock in $(find / \
      -path "/sys" -prune -o \
      -path "/proc" -prune -o \
      -type s \( \
        -name "docker.sock" -o \
        -name "docker.socket" -o \
        -name "dockershim.sock" -o \
        -name "containerd.sock" -o \
        -name "crio.sock" -o \
        -name "frakti.sock" -o \
        -name "rktlet.sock" \
      \) -print 2>/dev/null); do

      # Basic permissions hint (you generally need write perms to connect to a unix socket).
      if [ -w "$int_sock" ]; then
        if echo "$int_sock" | grep -Eq "docker"; then
          echo "You have write permissions over Docker socket $int_sock" | sed -${E} "s,$int_sock,${SED_RED_YELLOW},g"
        else
          echo "You have write permissions over interesting socket $int_sock" | sed -${E} "s,$int_sock,${SED_RED},g"
        fi
      else
        echo "You don't have write permissions over interesting socket $int_sock" | sed -${E} "s,$int_sock,${SED_GREEN},g"
      fi

      # Validate whether this looks like a Docker Engine API socket (amicontained-style) when curl exists.
      docker_enumerated=""
      if [ "$(command -v curl 2>/dev/null || echo -n '')" ]; then
        sockInfoResponse="$(curl -s --max-time 2 --unix-socket "$int_sock" http://localhost/info 2>/dev/null)"
        if echo "$sockInfoResponse" | grep -q "ServerVersion"; then
          echo "Valid Docker API socket: $int_sock" | sed -${E} "s,$int_sock,${SED_RED_YELLOW},g"
          dockerVersion=$(echo "$sockInfoResponse" | tr ',' '\n' | grep 'ServerVersion' | cut -d'"' -f 4)
          echo "$sockInfoResponse" | tr ',' '\n' | grep -E "$GREP_DOCKER_SOCK_INFOS" | grep -v "$GREP_DOCKER_SOCK_INFOS_IGNORE" | tr -d '"'
          docker_enumerated="1"
        fi
      fi

      # Fallback to docker CLI if curl is missing or the /info request didn't work.
      # Use DOCKER_HOST so we can target non-default socket paths when possible.
      if [ "$(command -v docker 2>/dev/null || echo -n '')" ] && ! [ "$docker_enumerated" ]; then
        if [ -w "$int_sock" ] && echo "$int_sock" | grep -Eq "docker"; then
          sockInfoResponse="$(DOCKER_HOST="unix://$int_sock" docker info 2>/dev/null)"
          if [ "$sockInfoResponse" ]; then
            dockerVersion=$(echo "$sockInfoResponse" | grep -i "^ Server Version:" | awk '{print $4}' | head -n 1)
            printf "%s\n" "$sockInfoResponse" | grep -E "$GREP_DOCKER_SOCK_INFOS" | grep -v "$GREP_DOCKER_SOCK_INFOS_IGNORE" | tr -d '"'
          fi
        fi
      fi
    done
  fi
}
