# Title: Container - Docker Container details
# ID: CT_Docker_container_details
# Author: Carlos Polop
# Last Update: 22-08-2023
# Description: Get docker Container details from the inside
# License: GNU GPL
# Version: 1.0
# Functions Used: checkDockerRootless, checkDockerVersionExploits, containerCheck, enumerateDockerSockets, inDockerGroup, print_2title, print_list
# Global Variables: $containerType, $DOCKER_GROUP, $DOCKER_ROOTLESS, $dockerVersion, $inContainer, $VULN_CVE_2019_5736, $VULN_CVE_2019_13139, $VULN_CVE_2021_41091
# Initial Functions: containerCheck
# Generated Global Variables:
# Fat linpeas: 0
# Small linpeas: 1


#If docker
if echo "$containerType" | grep -qi "docker"; then
    print_2title "Docker Container details"
    inDockerGroup
    print_list "Am I inside Docker group .......$NC $DOCKER_GROUP\n" | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "Looking and enumerating Docker Sockets (if any):\n"$NC
    enumerateDockerSockets
    print_list "Docker version .................$NC$dockerVersion"
    checkDockerVersionExploits
    print_list "Vulnerable to CVE-2019-5736 ....$NC$VULN_CVE_2019_5736"$NC | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "Vulnerable to CVE-2019-13139 ...$NC$VULN_CVE_2019_13139"$NC | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    print_list "Vulnerable to CVE-2021-41091 ...$NC$VULN_CVE_2021_41091"$NC | sed -${E} "s,Yes,${SED_RED_YELLOW},"
    if [ "$inContainer" ]; then
        checkDockerRootless
        print_list "Rootless Docker? ............... $DOCKER_ROOTLESS\n"$NC | sed -${E} "s,No,${SED_RED}," | sed -${E} "s,Yes,${SED_GREEN},"
        echo ""
    fi
    if df -h | grep docker; then
        print_2title "Docker Overlays"
        df -h | grep docker
    fi
fi