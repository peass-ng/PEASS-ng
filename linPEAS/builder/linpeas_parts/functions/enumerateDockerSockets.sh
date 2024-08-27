# Title: Container - enumerateDockerSockets
# ID: enumerateDockerSockets
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Search Docker Sockets
# License: GNU GPL
# Version: 1.0
# Functions Used: echo_not_found
# Global Variables: $GREP_DOCKER_SOCK_INFOS, $GREP_DOCKER_SOCK_INFOS_IGNORE, $IAMROOT
# Initial Functions:
# Generated Global Variables: $SEARCHED_DOCKER_SOCKETS, $dock_sock, $docker_enumerated, $dockerVersion, $int_sock, $sockInfoResponse
# Fat linpeas: 0
# Small linpeas: 1


enumerateDockerSockets() {
  dockerVersion="$(echo_not_found)"
  if ! [ "$SEARCHED_DOCKER_SOCKETS" ]; then
    SEARCHED_DOCKER_SOCKETS="1"
    for int_sock in $(find / ! -path "/sys/*" -type s -name "docker.sock" -o -name "docker.socket" -o -name "dockershim.sock" -o -name "containerd.sock" -o -name "crio.sock" -o -name "frakti.sock" -o -name "rktlet.sock" 2>/dev/null); do
      if ! [ "$IAMROOT" ] && [ -w "$int_sock" ]; then
        if echo "$int_sock" | grep -Eq "docker"; then
          dock_sock="$int_sock"
          echo "You have write permissions over Docker socket $dock_sock" | sed -${E} "s,$dock_sock,${SED_RED_YELLOW},g"
          echo "Docker enummeration:"
          docker_enumerated=""

          if [ "$(command -v curl || echo -n '')" ]; then
            sockInfoResponse="$(curl -s --unix-socket $dock_sock http://localhost/info)"
            dockerVersion=$(echo "$sockInfoResponse" | tr ',' '\n' | grep 'ServerVersion' | cut -d'"' -f 4)
            echo $sockInfoResponse | tr ',' '\n' | grep -E "$GREP_DOCKER_SOCK_INFOS" | grep -v "$GREP_DOCKER_SOCK_INFOS_IGNORE" | tr -d '"'
            if [ "$sockInfoResponse" ]; then docker_enumerated="1"; fi
          fi

          if [ "$(command -v docker || echo -n '')" ] && ! [ "$docker_enumerated" ]; then
            sockInfoResponse="$(docker info)"
            dockerVersion=$(echo "$sockInfoResponse" | tr ',' '\n' | grep 'Server Version' | cut -d' ' -f 4)
            printf "$sockInfoResponse" | tr ',' '\n' | grep -E "$GREP_DOCKER_SOCK_INFOS" | grep -v "$GREP_DOCKER_SOCK_INFOS_IGNORE" | tr -d '"'
          fi
        
        else
          echo "You have write permissions over interesting socket $int_sock" | sed -${E} "s,$int_sock,${SED_RED},g"
        fi

      else
        echo "You don't have write permissions over interesting socket $int_sock" | sed -${E} "s,$int_sock,${SED_GREEN},g"
      fi
    done
  fi
}
