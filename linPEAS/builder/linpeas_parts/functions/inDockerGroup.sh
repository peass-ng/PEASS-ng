# Title: Container - inDockerGroup
# ID: inDockerGroup
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Check if the user is in the docker group
# License: GNU GPL
# Version: 1.0
# Functions Used:
# Global Variables:
# Initial Functions:
# Generated Global Variables: $DOCKER_GROUP
# Fat linpeas: 0
# Small linpeas: 1


inDockerGroup() {
  DOCKER_GROUP="No"
  if groups 2>/dev/null | grep -q '\bdocker\b'; then
    DOCKER_GROUP="Yes"
  fi
}