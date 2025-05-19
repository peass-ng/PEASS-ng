# Title: Container - Container details
# ID: CT_Container_details
# Author: Carlos Polop
# Last Update: 07-03-2024
# Description: Get detailed container information relevant to privilege escalation:
#   - Container type and runtime
#   - Running containers
#   - Container configuration
#   - Common vulnerable scenarios:
#     * Misconfigured containers
#     * Privileged containers
#     * Exposed container APIs
#     * Container networking
#   - Exploitation methods:
#     * Container breakout: Exploit container misconfigurations
#     * Common attack vectors:
#       - Runtime escape
#       - Privilege escalation
#       - Container breakout
#       - Network escape
#     * Exploit techniques:
#       - Container misconfiguration abuse
#       - Privileged container exploitation
#       - Container API abuse
#       - Network escape techniques
# License: GNU GPL
# Version: 1.0
# Functions Used: containerCheck, echo_no, print_2title, print_list, warn_exec
# Global Variables: $containerType
# Initial Functions: containerCheck
# Generated Global Variables: $dockercontainers, $podmancontainers, $lxccontainers, $rktcontainers, $containerCounts
# Fat linpeas: 0
# Small linpeas: 1

print_2title "Container details"

print_list "Is this a container? ...........$NC $containerType"

# Get container runtime info
if [ "$(command -v docker || echo -n '')" ]; then
    print_list "Docker version ...............$NC "
    warn_exec docker version
    print_list "Docker info .................$NC "
    warn_exec docker info
fi

if [ "$(command -v podman || echo -n '')" ]; then
    print_list "Podman version ..............$NC "
    warn_exec podman version
    print_list "Podman info ................$NC "
    warn_exec podman info
fi

if [ "$(command -v lxc || echo -n '')" ]; then
    print_list "LXC version ................$NC "
    warn_exec lxc version
    print_list "LXC info ...................$NC "
    warn_exec lxc info
fi

print_list "Any running containers? ........ "$NC
# Get counts of running containers for each platform
dockercontainers=$(docker ps --format "{{.Names}}" 2>/dev/null | wc -l)
podmancontainers=$(podman ps --format "{{.Names}}" 2>/dev/null | wc -l)
lxccontainers=$(lxc list -c n --format csv 2>/dev/null | wc -l)
rktcontainers=$(rkt list 2>/dev/null | tail -n +2  | wc -l)
if [ "$dockercontainers" -eq "0" ] && [ "$lxccontainers" -eq "0" ] && [ "$rktcontainers" -eq "0" ] && [ "$podmancontainers" -eq "0" ]; then
    echo_no
else
    containerCounts=""
    if [ "$dockercontainers" -ne "0" ]; then containerCounts="${containerCounts}docker($dockercontainers) "; fi
    if [ "$podmancontainers" -ne "0" ]; then containerCounts="${containerCounts}podman($podmancontainers) "; fi
    if [ "$lxccontainers" -ne "0" ]; then containerCounts="${containerCounts}lxc($lxccontainers) "; fi
    if [ "$rktcontainers" -ne "0" ]; then containerCounts="${containerCounts}rkt($rktcontainers) "; fi
    echo "Yes $containerCounts" | sed -${E} "s,.*,${SED_RED},"
    
    # List any running containers with more details
    if [ "$dockercontainers" -ne "0" ]; then 
        echo "Running Docker Containers" | sed -${E} "s,.*,${SED_RED},"
        docker ps -a 2>/dev/null
        #echo "Docker Container Details" | sed -${E} "s,.*,${SED_RED},"
        #docker inspect $(docker ps -q) 2>/dev/null | grep -E "Privileged|CapAdd|CapDrop|SecurityOpt|HostConfig" | sed -${E} "s,true|privileged|host,${SED_RED},g"
        echo ""
    fi
    if [ "$podmancontainers" -ne "0" ]; then 
        echo "Running Podman Containers" | sed -${E} "s,.*,${SED_RED},"
        podman ps -a 2>/dev/null
        #echo "Podman Container Details" | sed -${E} "s,.*,${SED_RED},"
        #podman inspect $(podman ps -q) 2>/dev/null | grep -E "Privileged|CapAdd|CapDrop|SecurityOpt|HostConfig" | sed -${E} "s,true|privileged|host,${SED_RED},g"
        echo ""
    fi
    if [ "$lxccontainers" -ne "0" ]; then 
        echo "Running LXC Containers" | sed -${E} "s,.*,${SED_RED},"
        lxc list 2>/dev/null
        #echo "LXC Container Details" | sed -${E} "s,.*,${SED_RED},"
        #lxc config show $(lxc list -c n --format csv) 2>/dev/null | grep -E "security.privileged|security.capabilities|security.syscalls" | sed -${E} "s,true|privileged|host,${SED_RED},g"
        echo ""
    fi
    if [ "$rktcontainers" -ne "0" ]; then 
        echo "Running RKT Containers" | sed -${E} "s,.*,${SED_RED},"
        rkt list 2>/dev/null
        #echo "RKT Container Details" | sed -${E} "s,.*,${SED_RED},"
        #rkt status $(rkt list --format=json 2>/dev/null | jq -r '.[].id') 2>/dev/null | grep -E "privileged|capabilities|security" | sed -${E} "s,true|privileged|host,${SED_RED},g"
        echo ""
    fi
fi


echo ""