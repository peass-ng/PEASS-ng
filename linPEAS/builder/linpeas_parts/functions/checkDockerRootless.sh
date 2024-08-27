# Title: Container - checkDockerRootless
# ID: checkDockerRootless
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the container is running in rootless mode
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables: $TIP_DOCKER_ROOTLESS
# Initial Functions:
# Generated Global Variables: $DOCKER_ROOTLESS
# Fat linpeas: 0
# Small linpeas: 1


checkDockerRootless() {
  DOCKER_ROOTLESS="No"
  if docker info 2>/dev/null|grep -q rootless; then
    DOCKER_ROOTLESS="Yes ($TIP_DOCKER_ROOTLESS)"
  fi
}